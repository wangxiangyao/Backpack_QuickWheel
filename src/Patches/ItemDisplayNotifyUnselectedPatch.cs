using HarmonyLib;
using ItemStatsSystem;
using Duckov.UI;
using System.Reflection;
using UnityEngine;

namespace Backpack_QuickWheel.Patches
{
    /// <summary>
    /// 修补 ItemDisplay.NotifyUnselected() 方法
    ///
    /// 问题背景：
    /// ItemUIUtilities.Select() 方法在切换选中的ItemDisplay时，
    /// 会调用旧ItemDisplay的NotifyUnselected()方法
    /// NotifyUnselected()内部调用ContextMenu.Hide(this)
    /// 这可能会清空ItemDisplay的Target，导致SelectedItemDisplayRaw返回null
    ///
    /// 解决方案：
    /// 在NotifyUnselected()被调用时，检查当前ItemDisplay是否为全局选中状态
    /// 如果是，则暂时阻止ContextMenu.Hide()的调用，保护Target不被清空
    /// </summary>
    [HarmonyPatch(typeof(ItemDisplay), "NotifyUnselected")]
    public class ItemDisplayNotifyUnselectedPatch
    {
        static bool Prefix(ItemDisplay __instance)
        {
            // 检查当前ItemDisplay是否为全局选中的ItemDisplay
            var selectedItemDisplayField = typeof(ItemUIUtilities).GetField("selectedItemDisplay",
                BindingFlags.NonPublic | BindingFlags.Static);

            if (selectedItemDisplayField != null)
            {
                var currentSelection = selectedItemDisplayField.GetValue(null) as ItemDisplay;
                bool isCurrentlySelected = currentSelection == __instance;

                if (isCurrentlySelected)
                {
                    Debug.Log($"[ItemDisplayNotifyUnselectedPatch] 🛡️ 当前选中的ItemDisplay正在被NotifyUnselected调用，阻止ContextMenu.Hide()以保护Target");

                    // 记录当前Target值用于保护
                    var targetProperty = typeof(ItemDisplay).GetProperty("Target",
                        BindingFlags.Public | BindingFlags.Instance);

                    if (targetProperty != null)
                    {
                        var currentTarget = targetProperty.GetValue(__instance);
                        Debug.Log($"[ItemDisplayNotifyUnselectedPatch] 当前Target: {currentTarget?.ToString() ?? "null"}");

                        if (currentTarget != null)
                        {
                            // 🚨 增强保护：立即保护Target并强制阻止原方法执行
                            targetProperty.SetValue(__instance, currentTarget);
                            Debug.Log($"[ItemDisplayNotifyUnselectedPatch] ✅ 已立即保护ItemDisplay.Target: {currentTarget}");

                            // 🛡️ 双重保护：确保selectedItemDisplay字段也指向当前实例
                            selectedItemDisplayField.SetValue(null, __instance);
                            Debug.Log($"[ItemDisplayNotifyUnselectedPatch] ✅ 已保护selectedItemDisplay字段指向当前实例");

                            // 🛡️ 三重保护：调用全局状态监控器，记录保护目标
                            ItemDisplayStateMonitor.RecordProtectedTarget(__instance, (Item)currentTarget);
                            Debug.Log($"[ItemDisplayNotifyUnselectedPatch] ✅ 已记录到全局状态监控器");

                            // 延迟多次保护，确保ContextMenu.Hide()之后Target仍然存在
                            MonoBehaviour instance = __instance;
                            if (instance != null)
                            {
                                instance.StartCoroutine(DelayedTargetProtection(__instance, (Item)currentTarget));
                            }
                        }
                        else
                        {
                            Debug.LogWarning("[ItemDisplayNotifyUnselectedPatch] ⚠️ Target为null，可能是第二次调用，尝试从其他地方恢复");

                            // 🔧 尝试从ItemUIUtilities.SelectedItemDisplayRaw恢复Target
                            var selectedItemDisplayRawProperty = typeof(ItemUIUtilities).GetProperty("SelectedItemDisplayRaw",
                                BindingFlags.NonPublic | BindingFlags.Static);

                            if (selectedItemDisplayRawProperty != null)
                            {
                                var rawSelection = selectedItemDisplayRawProperty.GetValue(null) as ItemDisplay;
                                if (rawSelection == __instance)
                                {
                                    // 如果RawSelection仍然指向当前实例，说明Target应该是存在的
                                    // 查找与当前ItemDisplay关联的Item
                                    var allItemDisplays = UnityEngine.Object.FindObjectsOfType<ItemDisplay>();
                                    foreach (var itemDisplay in allItemDisplays)
                                    {
                                        if (itemDisplay == __instance && itemDisplay.Target != null)
                                        {
                                            // 找到了其他地方保存的Target，恢复它
                                            targetProperty.SetValue(__instance, itemDisplay.Target);
                                            selectedItemDisplayField.SetValue(null, __instance);

                                            // 🛡️ 恢复后立即记录到全局状态监控器
                                            ItemDisplayStateMonitor.RecordProtectedTarget(__instance, itemDisplay.Target);
                                            Debug.Log($"[ItemDisplayNotifyUnselectedPatch] 🔧 从其他ItemDisplay实例恢复Target: {itemDisplay.Target} (已记录到监控器)");
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // 🚨 关键：始终阻止原来的NotifyUnselected执行，防止二次破坏
                    Debug.Log("[ItemDisplayNotifyUnselectedPatch] 🛡️ 阻止原NotifyUnselected方法执行");
                    return false;
                }
                else
                {
                    Debug.Log($"[ItemDisplayNotifyUnselectedPatch] 非选中ItemDisplay的NotifyUnselected，允许正常执行");
                    return true;
                }
            }

            Debug.LogWarning("[ItemDisplayNotifyUnselectedPatch] 无法获取selectedItemDisplay字段，允许正常执行");
            return true;
        }

        /// <summary>
        /// 延迟保护Target，确保在ContextMenu.Hide()执行后Target仍然存在
        /// </summary>
        private static System.Collections.IEnumerator DelayedTargetProtection(ItemDisplay itemDisplay, Item protectedTarget)
        {
            var targetProperty = typeof(ItemDisplay).GetProperty("Target",
                BindingFlags.Public | BindingFlags.Instance);
            var selectedItemDisplayField = typeof(ItemUIUtilities).GetField("selectedItemDisplay",
                BindingFlags.NonPublic | BindingFlags.Static);

            Debug.Log($"[ItemDisplayNotifyUnselectedPatch] 🔧 开始延迟保护Target: {protectedTarget?.DisplayName}");

            // 多次检查和恢复，确保Target不被清空
            for (int i = 0; i < 10; i++)
            {
                yield return new WaitForSeconds(0.05f); // 每50ms检查一次

                if (itemDisplay == null || protectedTarget == null)
                {
                    Debug.Log("[ItemDisplayNotifyUnselectedPatch] 🛑 延迟保护结束：itemDisplay或protectedTarget为null");
                    yield break;
                }

                // 保护Target
                if (targetProperty != null)
                {
                    var currentTarget = targetProperty.GetValue(itemDisplay);
                    if (currentTarget != protectedTarget)
                    {
                        targetProperty.SetValue(itemDisplay, protectedTarget);
                        Debug.Log($"[ItemDisplayNotifyUnselectedPatch] 延迟保护[{i+1}] - 重新设置Target: {protectedTarget.DisplayName}");
                    }
                }

                // 保护selectedItemDisplay字段
                if (selectedItemDisplayField != null)
                {
                    var currentSelection = selectedItemDisplayField.GetValue(null) as ItemDisplay;
                    if (currentSelection != itemDisplay)
                    {
                        selectedItemDisplayField.SetValue(null, itemDisplay);
                        Debug.Log($"[ItemDisplayNotifyUnselectedPatch] 延迟保护[{i+1}] - 重新设置selectedItemDisplay字段");
                    }
                }

                // 检查ItemDisplay是否仍然有效
                if (!itemDisplay.gameObject.activeInHierarchy)
                {
                    Debug.LogWarning($"[ItemDisplayNotifyUnselectedPatch] ⚠️ ItemDisplay在第{i+1}次检查时已失效，停止保护");
                    break;
                }
            }

            Debug.Log("[ItemDisplayNotifyUnselectedPatch] ✅ 延迟保护完成");
        }
    }
}