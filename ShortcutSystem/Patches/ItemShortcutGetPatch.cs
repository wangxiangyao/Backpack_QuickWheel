using HarmonyLib;
using ItemStatsSystem;
using UnityEngine;

namespace Great_backpack.ShortcutSystem.Patches
{
    /// <summary>
    /// 补丁 ItemShortcut.Get 方法，使其能够返回背包配件中的物品
    /// 这样背包界面的快捷栏（ItemShortcutEditorEntry）也能显示配件中的物品
    /// </summary>
    [HarmonyPatch(typeof(Duckov.ItemShortcut))]
    public static class ItemShortcutGetPatch
    {
        // 防止递归调用的标志（使用线程静态以支持多线程）
        [System.ThreadStatic]
        private static bool _isGetting = false;

        [HarmonyPatch("Get", MethodType.Normal)]
        [HarmonyPostfix]
        static void GetPostfix(int index, ref Item __result)
        {
            // 防止递归：如果正在获取中，直接返回
            if (_isGetting)
            {
                Debug.LogWarning($"[ItemShortcutGetPatch] 检测到递归调用，跳过处理 index = {index}");
                return;
            }

            _isGetting = true;
            try
            {
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
                    Item ourItem = BackpackShortcutManager.Instance?.GetCurrentItem(category);

                    if (ourItem != null)
                    {
                        // 额外检查物品是否仍然有效（未被消耗）
                        if (IsItemStillValid(ourItem))
                        {
                            __result = ourItem;
                        }
                        else
                        {
                            Debug.Log($"[ItemShortcutGetPatch] 物品已无效，返回null: {ourItem.DisplayName}");
                        }
                    }
                }
            }
            finally
            {
                _isGetting = false;
            }
        }

        /// <summary>
        /// 检查物品是否仍然有效（未被消耗或移除）
        /// </summary>
        private static bool IsItemStillValid(Item item)
        {
            if (item == null) return false;

            // 检查物品是否仍然存在于背包系统中
            // 这里可以添加更严格的检查逻辑
            return !item.IsBeingDestroyed && item.ParentItem != null;
        }
    }
}
