using HarmonyLib;
using ItemStatsSystem;
using UnityEngine;
using Duckov.UI;

namespace Backpack_QuickWheel.ShortcutSystem.Patches
{
    /// <summary>
    /// 补丁 CharacterInputControl.ShortCutInput 来拦截界面关闭时的快捷键输入
    ///
    /// 注：鼠标输入拦截通过 ItemWheelSelector 中的全屏透明面板实现
    /// 该面板会拦截所有鼠标事件，防止开枪、投掷等操作
    /// </summary>
    [HarmonyPatch(typeof(CharacterInputControl))]
    [HarmonyPatch("ShortCutInput", MethodType.Normal)]
    public static class CharacterInputControlPatch
    {
        [HarmonyPrefix]
        static bool ShortCutInputPrefix(int index, CharacterInputControl __instance)
        {
            Debug.Log($"[CharacterInputControlPatch] ShortCutInput 被调用，index = {index}");
            Debug.Log($"[CharacterInputControlPatch] View.ActiveView: {(View.ActiveView != null ? "打开" : "关闭")}");

            // 只有在界面关闭时才处理（界面打开时由 UIInputManager 事件处理）
            if (View.ActiveView == null)
            {
                // 转换为 0-based 索引（游戏使用 3-8，我们使用 0-5）
                int adjustedIndex = index - 3;

                Debug.Log($"[CharacterInputControlPatch] 界面关闭，调整后索引 = {adjustedIndex}");
                Debug.Log($"[CharacterInputControlPatch] 快捷键系统启用: {BackpackShortcutManager.IsShortcutSystemEnabled}");
                Debug.Log($"[CharacterInputControlPatch] 索引是否在范围内: {BackpackShortcutManager.IsBackpackShortcutIndex(adjustedIndex)}");

                // 如果是我们的快捷键范围，拦截并处理
                if (BackpackShortcutManager.IsShortcutSystemEnabled &&
                    BackpackShortcutManager.IsBackpackShortcutIndex(adjustedIndex))
                {
                    Debug.Log($"[CharacterInputControlPatch] ✓ 拦截快捷键 {adjustedIndex}");

                    var category = BackpackShortcutManager.IndexToCategory(adjustedIndex);
                    BackpackShortcutManager.Instance?.HandleShortcutInput(category);

                    return false; // 跳过原方法
                }
                else
                {
                    Debug.Log($"[CharacterInputControlPatch] ✗ 不拦截，让游戏处理");
                }
            }
            else
            {
                Debug.Log($"[CharacterInputControlPatch] 界面打开，让 UIInputManager 事件处理");
            }

            return true; // 继续执行原方法
        }
    }
}
