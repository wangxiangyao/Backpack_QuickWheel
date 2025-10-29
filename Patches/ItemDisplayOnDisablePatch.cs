using HarmonyLib;
using ItemStatsSystem;
using Duckov.UI;
using System.Reflection;
using UnityEngine;

namespace Great_backpack.Patches
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

            // 关键：仅当这个ItemDisplay是插槽物品时，保留Selection
            // 否则执行Select(null)来清除
            bool isCurrentlySelected = __instance.Selected;

            if (isCurrentlySelected)
            {
                Item targetItem = __instance.Target;
                // 只有插槽物品销毁时才保留Selection（ItemDetailsDisplay.Setup()会重建UI）
                if (targetItem == null || targetItem.PluggedIntoSlot == null)
                {
                    ItemUIUtilities.Select(null);
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
    }
}
