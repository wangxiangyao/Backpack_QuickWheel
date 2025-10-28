using HarmonyLib;
using ItemStatsSystem;
using UnityEngine;

namespace Great_backpack.ShortcutSystem.Patches
{
    /// <summary>
    /// 补丁 ItemShortcut.IsItemValid 方法
    /// 使配件中的物品也被认为是有效的快捷键物品
    /// 这样可以避免游戏不断将这些物品标记为dirty并清空
    /// </summary>
    [HarmonyPatch(typeof(Duckov.ItemShortcut))]
    public static class ItemShortcutIsValidPatch
    {
        // 防止递归调用的标志
        [System.ThreadStatic]
        private static bool _isValidating = false;

        [HarmonyPatch("IsItemValid", MethodType.Normal)]
        [HarmonyPostfix]
        static void IsItemValidPostfix(Item item, ref bool __result)
        {
            // 防止递归
            if (_isValidating)
            {
                return;
            }

            _isValidating = true;
            try
            {
                // 如果游戏原版已经认为有效，就不需要干预
                if (__result)
                {
                    return;
                }

                // 如果我们的快捷键系统未启用，也不干预
                if (!BackpackShortcutManager.IsShortcutSystemEnabled)
                {
                    return;
                }

                // 检查这个物品是否是我们系统中的某个快捷键物品
                if (item != null && BackpackShortcutManager.Instance != null)
                {
                    // 首先检查物品是否仍然有效（未被消耗）
                    if (item.IsBeingDestroyed || item.ParentItem == null)
                    {
                        // 物品已被消耗或无效，不干预
                        return;
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        var category = BackpackShortcutManager.IndexToCategory(i);
                        var ourItem = BackpackShortcutManager.Instance.GetCurrentItem(category);

                        if (ourItem == item)
                        {
                            // 这是我们系统中的物品，应该被认为是有效的
                            __result = true;
                            return;
                        }
                    }
                }
            }
            finally
            {
                _isValidating = false;
            }
        }
    }
}
