using System;
using System.Collections.Generic;
using Duckov.UI;
using Great_backpack.AttachmentSystem;
using HarmonyLib;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using UnityEngine;
using UnityEngine.UI;

namespace Great_backpack.AttachmentUI.Patches
{
    /// <summary>
    /// 圆孔拖拽高亮Patch - 集中管理模式
    ///
    /// 原理：
    /// 1. 在SlotIndicator.Setup时，计算插槽规则 → 注册到SlotIndicatorCacheManager
    /// 2. 在SlotIndicator.OnDisable时，从Manager中反注册
    /// 3. Manager订阅全局拖拽事件，分帧匹配规则，高亮对应indicator
    /// 4. Manager负责协调所有高亮/取消高亮操作
    ///
    /// 性能优化：
    /// - 事件订阅从O(40) → O(1)（只Manager订阅）
    /// - 分帧按规则匹配，而非按indicator遍历
    /// - Setup时完成规则计算和注册，拖拽时纯查表
    /// </summary>
    [HarmonyPatch(typeof(SlotIndicator))]
    public class SlotIndicatorDragHighlightPatch
    {
        // 存储高亮状态下被激活的 contentIndicator，以便稍后恢复状态
        private static HashSet<SlotIndicator> _highlightedIndicators
            = new HashSet<SlotIndicator>();

        // 缓存每个SlotIndicator的contentIndicator GameObject和Graphic组件，避免重复反射和GetComponent
        private static Dictionary<SlotIndicator, (GameObject contentIndicator, Graphic graphic)> _cachedComponents
            = new Dictionary<SlotIndicator, (GameObject, Graphic)>();

        // 反射字段缓存
        private static System.Reflection.FieldInfo _contentIndicatorField;

        // 静态构造函数，初始化反射字段
        static SlotIndicatorDragHighlightPatch()
        {
            _contentIndicatorField = typeof(SlotIndicator).GetField("contentIndicator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        }

        /// <summary>
        /// Patch Setup - 计算规则并注册到Manager
        /// </summary>
        [HarmonyPatch("Setup")]
        [HarmonyPostfix]
        public static void Setup_Postfix(SlotIndicator __instance)
        {
            if (__instance == null || __instance.Target == null)
                return;

            try
            {
                var parentItem = __instance.Target.Master;

                // 检查槽位所属的物品是否在主背包中
                if (!IsItemInMainBackpack(parentItem))
                    return;

                // 缓存contentIndicator和Graphic组件（一次性，避免之后每次都反射和GetComponent）
                if (_contentIndicatorField != null)
                {
                    var contentIndicatorGO = _contentIndicatorField.GetValue(__instance) as GameObject;
                    if (contentIndicatorGO != null)
                    {
                        var graphic = contentIndicatorGO.GetComponent<Graphic>();
                        if (graphic != null)
                        {
                            _cachedComponents[__instance] = (contentIndicatorGO, graphic);
                        }
                    }
                }

                // 计算规则key并注册到Manager
                string ruleKey = GenerateRuleKey(__instance.Target);
                SlotIndicatorCacheManager.RegisterIndicator(__instance, ruleKey);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SlotIndicatorDragHighlight] Setup注册失败: {ex}");
            }
        }

        /// <summary>
        /// 根据Slot生成规则key
        /// 官方插槽: "require: [tag1|tag2], exclusive: [tag3]"
        /// 自定义插槽: "require_or: [tag1|tag2], exclusive: [tag3]"
        /// </summary>
        private static string GenerateRuleKey(Slot slot)
        {
            if (slot == null)
                return "";

            string requireKey;
            if (slot.Key != null && slot.Key.StartsWith("wxy_"))
            {
                // 自定义插槽：使用OR逻辑
                // 需要从SlotConfig中获取RestrictTags
                string slotType = ExtractSlotType(slot.Key);
                List<string> restrictTags = GetRestrictTags(slotType);
                requireKey = "require_or: [" + string.Join("|", restrictTags) + "]";
            }
            else
            {
                // 官方插槽：使用AND逻辑
                List<string> requireTags = new List<string>();
                foreach (var tag in slot.requireTags)
                {
                    if (tag != null)
                        requireTags.Add(tag.name);
                }
                requireKey = "require: [" + string.Join("|", requireTags) + "]";
            }

            // 获取排除标签
            List<string> exclusiveTags = new List<string>();
            foreach (var tag in slot.excludeTags)
            {
                if (tag != null)
                    exclusiveTags.Add(tag.name);
            }
            string exclusiveKey = "exclusive: [" + string.Join("|", exclusiveTags) + "]";

            return requireKey + ", " + exclusiveKey;
        }

        /// <summary>
        /// 从插槽key中提取插槽类型（例如 wxy_Small_0 → Small）
        /// </summary>
        private static string ExtractSlotType(string slotKey)
        {
            try
            {
                string withoutPrefix = slotKey.Substring(4); // 去掉 "wxy_"
                string[] parts = withoutPrefix.Split('_');
                return parts.Length > 0 ? parts[0] : "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// 根据插槽类型获取RestrictTags
        /// </summary>
        private static List<string> GetRestrictTags(string slotType)
        {
            try
            {
                // 从BackpackModConfig.UnifiedSlotTypes中查找
                if (BackpackModConfig.UnifiedSlotTypes.ContainsKey(slotType))
                {
                    return BackpackModConfig.UnifiedSlotTypes[slotType].RestrictTags;
                }
            }
            catch
            {
            }
            return new List<string>();
        }

        /// <summary>
        /// 判断物品是否在主背包中
        /// 先检查 PluggedIntoSlot 作为快速路径，再用 GameObject 父子关系进行完整查找
        /// </summary>
        private static bool IsItemInMainBackpack(Item item)
        {
            if (item == null)
                return false;

            // 快速路径：如果 PluggedIntoSlot == null 且 Inventory != null，说明这是主背包
            if (item.PluggedIntoSlot == null && item.Inventory != null)
                return true;

            // 慢速路径：物品被插入到了槽位中，需要通过 GameObject 父子关系向上查找
            while (item != null)
            {
                // 获取 GameObject 的父节点
                Transform parentTransform = item.gameObject.transform.parent;
                if (parentTransform == null)
                    return false;

                // 在父节点上查找 Item 组件
                Item parentItem = parentTransform.GetComponent<Item>();
                if (parentItem == null)
                    return false;

                // 检查父物品的 Inventory
                if (parentItem.Inventory != null)
                    return true;

                // 继续向上查找
                item = parentItem;
            }

            return false;
        }

        /// <summary>
        /// Patch OnDisable - 从Manager中反注册
        /// </summary>
        [HarmonyPatch("OnDisable")]
        [HarmonyPostfix]
        public static void OnDisable_Postfix(SlotIndicator __instance)
        {
            if (__instance == null)
                return;

            try
            {
                // 从Manager中反注册
                SlotIndicatorCacheManager.UnregisterIndicator(__instance);

                // 清理本地缓存
                _cachedComponents.Remove(__instance);
                _highlightedIndicators.Remove(__instance);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SlotIndicatorDragHighlight] OnDisable反注册失败: {ex}");
            }
        }

        /// <summary>
        /// 高亮SlotIndicator的圆孔为绿色（供Manager调用）
        /// 使用缓存的组件避免重复反射和GetComponent调用
        /// </summary>
        public static void HighlightSlotPublic(SlotIndicator slotIndicator)
        {
            if (slotIndicator == null || !_cachedComponents.TryGetValue(slotIndicator, out var cached))
                return;

            var contentIndicatorGO = cached.contentIndicator;
            var graphic = cached.graphic;

            // 如果槽位原本是空的，contentIndicator 会是 inactive，需要临时激活来显示高亮
            if (!contentIndicatorGO.activeSelf)
            {
                contentIndicatorGO.SetActive(true);
                _highlightedIndicators.Add(slotIndicator);
            }

            // 设置颜色为绿色（只改颜色，不改布局）
            if (graphic != null)
            {
                graphic.color = new Color(0f, 1f, 0f, 1f); // 绿色
                // 只更新材质，不重新布局
                graphic.SetMaterialDirty();
            }
        }

        /// <summary>
        /// 恢复SlotIndicator的圆孔为白色（供Manager调用）
        /// 使用缓存的组件避免重复反射和GetComponent调用
        /// </summary>
        public static void UnhighlightSlotPublic(SlotIndicator slotIndicator)
        {
            if (slotIndicator == null || !_cachedComponents.TryGetValue(slotIndicator, out var cached))
                return;

            var contentIndicatorGO = cached.contentIndicator;
            var graphic = cached.graphic;

            // 恢复颜色为白色（只改颜色，不改布局）
            if (graphic != null)
            {
                graphic.color = Color.white;
                // 只更新材质，不重新布局
                graphic.SetMaterialDirty();
            }

            // 如果这个indicator是我们激活的（在_highlightedIndicators中），就停用它
            if (_highlightedIndicators.Contains(slotIndicator))
            {
                contentIndicatorGO.SetActive(false);
                _highlightedIndicators.Remove(slotIndicator);
            }
        }
    }
}
