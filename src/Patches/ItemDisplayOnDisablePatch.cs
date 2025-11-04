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

            if (isCurrentlySelected && targetItem != null)
            {
                // 关键修复：当当前选中的ItemDisplay被销毁时，保护全局Selection不被清空
                var selectedItemDisplayField = typeof(ItemUIUtilities).GetField("selectedItemDisplay",
                    BindingFlags.NonPublic | BindingFlags.Static);

                if (selectedItemDisplayField != null)
                {
                    var currentSelection = selectedItemDisplayField.GetValue(null) as ItemDisplay;

                    if (currentSelection == __instance)
                    {
                        // 当前选中的ItemDisplay正在被销毁，保护全局Selection
                        selectedItemDisplayField.SetValue(null, __instance);

                        // 确保ItemDisplay的Target也不被清空
                        var targetProperty = typeof(ItemDisplay).GetProperty("Target",
                            BindingFlags.Public | BindingFlags.Instance);

                        if (targetProperty != null)
                        {
                            targetProperty.SetValue(__instance, targetItem);
                        }
                    }
                }

                // 寻找替代ItemDisplay
                ItemDisplay replacementItemDisplay = FindReplacementItemDisplay(targetItem, __instance);

                if (replacementItemDisplay != null)
                {
                    // 将Selection转移到替代的ItemDisplay
                    if (selectedItemDisplayField != null)
                    {
                        selectedItemDisplayField.SetValue(null, replacementItemDisplay);
                    }
                }
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
                        return itemDisplay;
                    }
                }

                return null;
            }
            catch (System.Exception)
            {
                return null;
            }
        }
    }
}
