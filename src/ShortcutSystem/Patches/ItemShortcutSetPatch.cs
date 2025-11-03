using HarmonyLib;
using ItemStatsSystem;
using Duckov;
using UnityEngine;

namespace Backpack_QuickWheel.ShortcutSystem.Patches
{
    /// <summary>
    /// 补丁 ItemShortcut.Set 方法
    /// 当我们的快捷键系统启用时，禁止官方快捷键系统对前4个快捷键（Index 0-3）的手动设置
    /// 这样可以避免用户hover物品后按快捷键按钮覆盖我们的自动收集逻辑
    /// 后两个快捷键（Index 4-5）仍然允许官方系统管理
    /// </summary>
    [HarmonyPatch(typeof(Duckov.ItemShortcut))]
    [HarmonyPatch("Set", new[] { typeof(int), typeof(Item) })]
    public static class ItemShortcutSetPatch
    {
        [HarmonyPrefix]
        static bool SetPrefix(int index, Item item, ref bool __result)
        {
            // 如果我们的快捷键系统启用，且要设置的是我们管理的快捷键（Index 0-3）
            if (BackpackShortcutManager.IsShortcutSystemEnabled && index < 4)
            {
                // 🔧 修复：允许设置null来清空快捷键，但阻止设置非null物品
                if (item == null)
                {
                    Debug.Log($"[ItemShortcutSetPatch] 允许清空快捷键索引 {index} (设置null)");
                    return true;  // 允许官方清空逻辑执行
                }
                else
                {
                    Debug.Log($"[ItemShortcutSetPatch] 快捷键系统启用中，禁止官方快捷键设置物品 {item.DisplayName} 到索引 {index}");
                    __result = false;  // 返回false表示设置失败
                    return false;  // 阻止原始方法执行
                }
            }

            // 快捷键系统未启用或设置的是后两个快捷键（Index 4-5），允许官方逻辑继续执行
            return true;
        }
    }
}
