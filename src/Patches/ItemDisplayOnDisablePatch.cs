using HarmonyLib;
using ItemStatsSystem;
using Duckov.UI;
using System.Reflection;
using UnityEngine;

namespace Backpack_QuickWheel.Patches
{
    /// <summary>
    /// 修补 ItemDisplay.OnDisable() 方法
    ///
    /// 问题背景：
    /// 当点击插槽物品时，会调用Select(newItemDisplay)来更新Selection
    /// 但在setter内部会触发OnSelectionChanged事件
    /// 同时旧ItemDisplay开始销毁，OnDisable()被调用
    /// OnDisable()会执行Select(null)，覆盖了新的Selection！
    /// 导致最终SelectedItem为null，详情消失
    ///
    /// 解决方案：
    /// Patch OnDisable()，在调用Select(null)之前，检查当前的SelectedItemDisplayRaw
    /// 如果已经有新的Selection了（不是this），就不要调用Select(null)
    /// 这样就能保留新的Selection
    /// </summary>
    [HarmonyPatch(typeof(ItemDisplay), "OnDisable")]
    public class ItemDisplayOnDisablePatch
    {
        static bool Prefix(ItemDisplay __instance)
        {
            // 执行取消事件订阅
            var selectionChangedEvent = typeof(ItemUIUtilities).GetField("OnSelectionChanged",
                BindingFlags.NonPublic | BindingFlags.Static);

            if (selectionChangedEvent != null)
            {
                var eventValue = selectionChangedEvent.GetValue(null) as System.Delegate;
                if (eventValue != null)
                {
                    var method = typeof(ItemDisplay).GetMethod("OnItemUtilitiesSelectionChanged",
                        BindingFlags.NonPublic | BindingFlags.Instance);

                    if (method != null)
                    {
                        var newDelegate = System.Delegate.Remove(eventValue,
                            System.Delegate.CreateDelegate(eventValue.GetType(), __instance, method));
                        selectionChangedEvent.SetValue(null, newDelegate);
                    }
                }
            }

            // 关键：阻止所有ItemDisplay在OnDisable时清除Selection
            // 这样可以让配件详情页面稳定显示
            bool isCurrentlySelected = __instance.Selected;
            Item targetItem = __instance.Target;

            // 🔍 增强调试：记录所有销毁的ItemDisplay，不仅仅是选中的
            Debug.Log($"[ItemDisplayOnDisablePatch] ItemDisplay正在销毁，物品: {targetItem?.DisplayName}, Selected: {isCurrentlySelected}, HashCode: {__instance.GetHashCode()}, TargetIsNull: {targetItem == null}");

            if (isCurrentlySelected && targetItem != null)
            {
                // 🚨 关键修复：当当前选中的ItemDisplay被销毁时，保护全局Selection不被清空
                Debug.Log($"[ItemDisplayOnDisablePatch] 🛡️ 当前选中的ItemDisplay '{targetItem.DisplayName}' 正在销毁，开始保护全局Selection");

                // 🔍 核心：检查当前ItemUIUtilities.selectedItemDisplay状态
                var selectedItemDisplayField = typeof(ItemUIUtilities).GetField("selectedItemDisplay",
                    BindingFlags.NonPublic | BindingFlags.Static);

                Debug.Log($"[ItemDisplayOnDisablePatch] selectedItemDisplay字段查找结果: {selectedItemDisplayField != null}");

                if (selectedItemDisplayField != null)
                {
                    var currentSelection = selectedItemDisplayField.GetValue(null) as ItemDisplay;
                    Debug.Log($"[ItemDisplayOnDisablePatch] 当前全局Selection: {currentSelection?.GetHashCode()}, 是否为当前实例: {currentSelection == __instance}");

                    if (currentSelection == __instance)
                    {
                        // 🚨 关键：当前选中的ItemDisplay正在被销毁，保护全局Selection
                        Debug.Log($"[ItemDisplayOnDisablePatch] 🛡️ 保护selectedItemDisplay字段，阻止被清空");

                        // 不允许清空selectedItemDisplay字段 - 即使在OnDisable中也要保持
                        selectedItemDisplayField.SetValue(null, __instance);
                        Debug.Log($"[ItemDisplayOnDisablePatch] ✅ 已保护selectedItemDisplay字段: {__instance.GetHashCode()}");

                        // 确保ItemDisplay的Target也不被清空
                        var targetProperty = typeof(ItemDisplay).GetProperty("Target",
                            BindingFlags.Public | BindingFlags.Instance);

                        if (targetProperty != null)
                        {
                            targetProperty.SetValue(__instance, targetItem);
                            Debug.Log($"[ItemDisplayOnDisablePatch] ✅ 已保护ItemDisplay.Target: {targetItem.DisplayName}");
                        }
                        else
                        {
                            Debug.LogError("[ItemDisplayOnDisablePatch] ❌ 无法找到Target属性！");
                        }
                    }
                    else
                    {
                        Debug.Log($"[ItemDisplayOnDisablePatch] 当前Selection不是这个ItemDisplay，无需保护");
                    }
                }
                else
                {
                    Debug.LogError("[ItemDisplayOnDisablePatch] ❌ 无法找到selectedItemDisplay字段！");
                }

                // 寻找替代ItemDisplay
                ItemDisplay replacementItemDisplay = FindReplacementItemDisplay(targetItem, __instance);

                if (replacementItemDisplay != null)
                {
                    Debug.Log($"[ItemDisplayOnDisablePatch] 找到替代ItemDisplay: {replacementItemDisplay.GetHashCode()}，物品: {replacementItemDisplay.Target?.DisplayName}");

                    // 将Selection转移到替代的ItemDisplay
                    if (selectedItemDisplayField != null)
                    {
                        selectedItemDisplayField.SetValue(null, replacementItemDisplay);
                        Debug.Log($"[ItemDisplayOnDisablePatch] 已成功将Selection转移到替代ItemDisplay");
                    }
                    else
                    {
                        Debug.LogError("[ItemDisplayOnDisablePatch] 无法找到selectedItemDisplay字段");
                    }
                }
                else
                {
                    Debug.LogWarning($"[ItemDisplayOnDisablePatch] 无法找到替代ItemDisplay，将保持当前ItemDisplay的Target");

                    // 确保 selectedItemDisplay 字段指向当前实例
                    if (selectedItemDisplayField != null)
                    {
                        var currentSelection = selectedItemDisplayField.GetValue(null) as ItemDisplay;
                        if (currentSelection != __instance)
                        {
                            selectedItemDisplayField.SetValue(null, __instance);
                            Debug.Log($"[ItemDisplayOnDisablePatch] 强制设置selectedItemDisplay字段指向当前ItemDisplay");
                        }
                    }
                }
            }
            else
            {
                // 📝 记录非选中状态的ItemDisplay销毁情况
                Debug.Log($"[ItemDisplayOnDisablePatch] 非选中ItemDisplay销毁：{targetItem?.DisplayName}, HashCode: {__instance.GetHashCode()}");
            }

            // 调用UnregisterEvents
            var unregisterMethod = typeof(ItemDisplay).GetMethod("UnregisterEvents",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (unregisterMethod != null)
            {
                unregisterMethod.Invoke(__instance, null);
            }

            return false;  // 不执行原OnDisable（我们已处理所有逻辑）
        }

        /// <summary>
        /// 为指定物品寻找替代的ItemDisplay
        /// </summary>
        /// <param name="targetItem">目标物品</param>
        /// <param name="excludeInstance">要排除的ItemDisplay实例（即将被销毁的）</param>
        /// <returns>替代的ItemDisplay，如果找不到则返回null</returns>
        private static ItemDisplay FindReplacementItemDisplay(Item targetItem, ItemDisplay excludeInstance)
        {
            try
            {
                // 方法1：通过ItemUIUtilities的事件系统寻找
                var onSelectionChangedEvent = typeof(ItemUIUtilities).GetField("OnSelectionChanged",
                    BindingFlags.NonPublic | BindingFlags.Static);

                if (onSelectionChangedEvent != null)
                {
                    var eventValue = onSelectionChangedEvent.GetValue(null) as System.Delegate;
                    if (eventValue != null)
                    {
                        var invocationList = eventValue.GetInvocationList();
                        foreach (var handler in invocationList)
                        {
                            if (handler.Target is ItemDisplay itemDisplay &&
                                itemDisplay != excludeInstance &&
                                itemDisplay.Target == targetItem &&
                                itemDisplay.gameObject != null &&
                                itemDisplay.gameObject.activeInHierarchy)
                            {
                                Debug.Log($"[ItemDisplayOnDisablePatch] 通过事件系统找到替代ItemDisplay: {itemDisplay.GetHashCode()}");
                                return itemDisplay;
                            }
                        }
                    }
                }

                // 方法2：通过GameObject.FindObjectsOfType寻找（较慢但全面）
                var allItemDisplays = UnityEngine.Object.FindObjectsOfType<ItemDisplay>();
                foreach (var itemDisplay in allItemDisplays)
                {
                    if (itemDisplay != excludeInstance &&
                        itemDisplay.Target == targetItem &&
                        itemDisplay.gameObject != null &&
                        itemDisplay.gameObject.activeInHierarchy)
                    {
                        Debug.Log($"[ItemDisplayOnDisablePatch] 通过全局搜索找到替代ItemDisplay: {itemDisplay.GetHashCode()}");
                        return itemDisplay;
                    }
                }

                Debug.Log($"[ItemDisplayOnDisablePatch] 无法为物品 '{targetItem.DisplayName}' 找到替代ItemDisplay");
                return null;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ItemDisplayOnDisablePatch] 寻找替代ItemDisplay时出错: {e.Message}");
                return null;
            }
        }
    }
}
