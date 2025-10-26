using HarmonyLib;
using Duckov.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using ItemStatsSystem.Items;
using UnityEngine;
using ItemStatsSystem;
using Great_backpack.AttachmentSystem;

namespace Great_backpack.Patches
{
    [HarmonyPatch(typeof(Slot))]
    [HarmonyPatch("CheckAbleToPlug")]
    public class SlotCheckPatch
    {
        // 存储需要特殊规则的插槽配置
        private static Dictionary<string, SlotConfig> customSpecialSlotConfigs = new Dictionary<string, SlotConfig>();

        static SlotCheckPatch()
        {
            InitializeSpecialSlots();
        }

        // 初始化特殊插槽配置
        public static void InitializeSpecialSlots()
        {
            customSpecialSlotConfigs.Clear();

            // 从 UnifiedSlotTypes 中找到 RestrictTags 数量 > 1 的配置
            foreach (var slotType in BackpackModConfig.UnifiedSlotTypes)
            {
                if (slotType.Value.RestrictTags != null && slotType.Value.RestrictTags.Count > 1)
                {
                    customSpecialSlotConfigs[slotType.Key] = slotType.Value;
                    Debug.Log($"注册特殊插槽规则: {slotType.Key} -> {string.Join(", ", slotType.Value.RestrictTags)}");
                }
            }

            Debug.Log($"已初始化 {customSpecialSlotConfigs.Count} 个特殊插槽配置");
        }

        [HarmonyPrefix]
        static bool Prefix(Slot __instance, Item otherItem, ref bool __result)
        {
            try
            {
                // 检查是否是我们的自定义插槽（wxy_前缀）
                string slotKey = __instance.Key;
                if (!string.IsNullOrEmpty(slotKey) && slotKey.StartsWith("wxy_"))
                {
                    // 提取插槽类型（去掉wxy_前缀和索引）
                    string slotType = ExtractSlotType(slotKey);
                    if (!string.IsNullOrEmpty(slotType) && customSpecialSlotConfigs.ContainsKey(slotType))
                    {
                        // 使用我们的自定义逻辑
                        __result = CustomCheckAbleToPlug(__instance, otherItem, customSpecialSlotConfigs[slotType]);
                        return false; // 跳过原始方法
                    }
                }

                return true; // 继续执行原始方法
            }
            catch (Exception e)
            {
                Debug.LogError($"插槽检查补丁出错: {e.Message}");
                return true;
            }
        }

        // 从插槽key中提取插槽类型
        // 输入: "wxy_Small_0" -> 输出: "Small"
        // 输入: "wxy_Large_1" -> 输出: "Large"
        private static string ExtractSlotType(string slotKey)
        {
            try
            {
                // 去掉 "wxy_" 前缀
                string withoutPrefix = slotKey.Substring(4);

                // 按 "_" 分割，取第一部分作为插槽类型
                string[] parts = withoutPrefix.Split('_');
                return parts.Length > 0 ? parts[0] : null;
            }
            catch (Exception e)
            {
                Debug.LogError($"提取插槽类型失败: {slotKey}, 错误: {e.Message}");
                return null;
            }
        }

        private static bool CustomCheckAbleToPlug(Slot slot, Item otherItem, SlotConfig slotConfig)
        {
            if (otherItem == null) return false;
            if (otherItem == slot.Content) return false;

            // 保持原有的重复ID检查
            if (slot.ForbidItemsWithSameID && slot.Master != null && slot.Master.Slots != null)
            {
                foreach (Slot otherSlot in slot.Master.Slots)
                {
                    if (otherSlot != null && otherSlot != slot && otherSlot.ForbidItemsWithSameID)
                    {
                        Item content = otherSlot.Content;
                        if (!(content == null) && !(content == otherItem) && content.TypeID == otherItem.TypeID)
                        {
                            return false;
                        }
                    }
                }
            }

            // 保持原有的父级关系检查
            if (slot.Master.GetAllParents(false).Contains(otherItem))
            {
                return false;
            }

            // 使用我们的OR逻辑检查Tag
            return CheckTagsWithORLogic(otherItem, slotConfig.RestrictTags, slot.excludeTags);
        }

        private static bool CheckTagsWithORLogic(Item item, List<string> allowedTags, List<Tag> excludeTags)
        {
            // 检查排除Tag（保持原有逻辑）
            foreach (Tag excludeTag in excludeTags)
            {
                if (!(excludeTag == null) && item.Tags.Contains(excludeTag))
                {
                    return false;
                }
            }

            // 使用OR逻辑检查允许的Tag
            foreach (string tagName in allowedTags)
            {
                Tag tag = GetSystemTag(tagName);
                if (tag != null && item.Tags.Contains(tag))
                {
                    return true; // 只要有一个Tag匹配就返回true
                }
            }

            return false; // 没有匹配的Tag
        }

        private static Tag GetSystemTag(string tagName)
        {
            // 从系统Tag中查找
            Tag[] allTags = Resources.FindObjectsOfTypeAll<Tag>();
            return Array.Find(allTags, t => t.name == tagName);
        }
    }
}