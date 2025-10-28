using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Great_backpack.ShortcutSystem.Patches
{
    /// <summary>
    /// 补丁 CharacterInputControl 的快捷键方法，拦截按键按下和释放事件
    /// 用于实现长按检测
    /// </summary>
    [HarmonyPatch(typeof(CharacterInputControl))]
    public static class CharacterInputShortcutPatch
    {
        // 快捷键按下和释放的事件
        public static event System.Action<int> OnShortcutKeyDown;
        public static event System.Action<int> OnShortcutKeyUp;

        /// <summary>
        /// 处理快捷键的前置补丁
        /// 检查 context 的 started 和 canceled，触发对应事件
        /// 当系统启用时，阻止原方法执行
        /// </summary>
        private static bool OnShortcutKeyPressed(InputAction.CallbackContext context, int index)
        {
            // 转换为 0-based 索引（游戏使用 3-8，我们使用 0-5）
            int adjustedIndex = index - 3;

            // 只有当我们的系统启用且索引在范围内时，才拦截
            if (!BackpackShortcutManager.IsShortcutSystemEnabled || !BackpackShortcutManager.IsBackpackShortcutIndex(adjustedIndex))
            {
                // 系统未启用，让原方法执行
                return true;
            }

            if (context.started)
            {
                Debug.Log($"[CharacterInputShortcutPatch] 快捷键 {adjustedIndex} 按下");
                OnShortcutKeyDown?.Invoke(adjustedIndex);
            }
            else if (context.canceled)
            {
                Debug.Log($"[CharacterInputShortcutPatch] 快捷键 {adjustedIndex} 释放");
                OnShortcutKeyUp?.Invoke(adjustedIndex);
            }

            // 当 context.performed 时，我们的系统会处理，不让原方法执行
            if (context.performed)
            {
                Debug.Log($"[CharacterInputShortcutPatch] 阻止原方法执行（系统已处理）");
                return false;  // 阻止原方法执行
            }

            // 其他情况（started 或 canceled）也阻止原方法，因为我们完全接管了处理
            return false;
        }

        // 为每个快捷键方法创建补丁
        [HarmonyPatch("OnShortCutInput3")]
        [HarmonyPrefix]
        private static bool OnShortCutInput3Prefix(InputAction.CallbackContext context)
        {
            return OnShortcutKeyPressed(context, 3);
        }

        [HarmonyPatch("OnShortCutInput4")]
        [HarmonyPrefix]
        private static bool OnShortCutInput4Prefix(InputAction.CallbackContext context)
        {
            return OnShortcutKeyPressed(context, 4);
        }

        [HarmonyPatch("OnShortCutInput5")]
        [HarmonyPrefix]
        private static bool OnShortCutInput5Prefix(InputAction.CallbackContext context)
        {
            return OnShortcutKeyPressed(context, 5);
        }

        [HarmonyPatch("OnShortCutInput6")]
        [HarmonyPrefix]
        private static bool OnShortCutInput6Prefix(InputAction.CallbackContext context)
        {
            return OnShortcutKeyPressed(context, 6);
        }

        [HarmonyPatch("OnShortCutInput7")]
        [HarmonyPrefix]
        private static bool OnShortCutInput7Prefix(InputAction.CallbackContext context)
        {
            return OnShortcutKeyPressed(context, 7);
        }

        [HarmonyPatch("OnShortCutInput8")]
        [HarmonyPrefix]
        private static bool OnShortCutInput8Prefix(InputAction.CallbackContext context)
        {
            return OnShortcutKeyPressed(context, 8);
        }
    }
}
