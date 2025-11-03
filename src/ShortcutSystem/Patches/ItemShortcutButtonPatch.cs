using HarmonyLib;
using ItemStatsSystem;
using Duckov.UI;
using UnityEngine;

namespace Backpack_QuickWheel.ShortcutSystem.Patches
{
    /// <summary>
    /// 补丁 ItemShortcutButton.GetTargetItem 来显示背包配件中的物品
    /// </summary>
    [HarmonyPatch(typeof(ItemShortcutButton))]
    public static class ItemShortcutButtonPatch
    {
        // 防止递归调用的标志
        [System.ThreadStatic]
        private static bool _isGetting = false;

        [HarmonyPatch("GetTargetItem", MethodType.Normal)]
        [HarmonyPostfix]
        static void GetTargetItemPostfix(ItemShortcutButton __instance, ref Item __result)
        {
            // 防止递归
            if (_isGetting)
            {
                return;
            }

            _isGetting = true;
            try
            {
                // 获取这个按钮对应的快捷键索引
                int index = __instance.Index;

                // 如果游戏原版已经找到物品了，就不需要我们干预
                if (__result != null)
                {
                    return;
                }

                // 如果我们的快捷键系统启用，并且索引在我们的范围内
                if (BackpackShortcutManager.IsShortcutSystemEnabled &&
                    BackpackShortcutManager.IsBackpackShortcutIndex(index))
                {
                    // 获取我们的物品
                    var category = BackpackShortcutManager.IndexToCategory(index);
                    // 🏗️ 架构优化：直接从WheelLayoutManager单例获取选中物品
                    Item ourItem = WheelLayoutManager.Instance?.GetSelectedItem(category);

                    if (ourItem != null)
                    {
                        // 检查物品是否仍然有效（未被消耗）
                        if (!ourItem.IsBeingDestroyed && ourItem.ParentItem != null)
                        {
                            __result = ourItem;
                        }
                    }
                }
            }
            finally
            {
                _isGetting = false;
            }
        }
    }
}
