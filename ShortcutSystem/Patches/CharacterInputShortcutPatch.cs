using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace Great_backpack.ShortcutSystem.Patches
{
    /// <summary>
    /// 补丁 CharacterInputControl 的快捷键方法和鼠标输入方法，拦截按键按下和释放事件
    /// 用于实现长按检测和轮盘显示时的鼠标输入拦截
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
        /// 当轮盘显示时，完全阻止鼠标输入传给游戏
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

            // 当轮盘显示时，阻止所有鼠标输入传给游戏（防止开枪、投掷等操作）
            if (InputInterceptor.IsWheelVisible)
            {
                Debug.Log($"[CharacterInputShortcutPatch] 轮盘可见，阻止鼠标输入传给游戏");
                return false;  // 完全阻止原方法执行
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

        /// <summary>
        /// 补丁鼠标触发输入（左键点击/开火）
        /// 使用 Postfix 在方法执行后清除触发标志，防止游戏逻辑执行，但允许 EventSystem 接收事件
        /// </summary>
        [HarmonyPatch("OnPlayerTriggerInputUsingMouseKeyboard")]
        [HarmonyPostfix]
        private static void OnPlayerTriggerInputUsingMouseKeyboardPostfix(CharacterInputControl __instance)
        {
            // 当轮盘显示时，清除触发标志，防止游戏开火
            if (InputInterceptor.IsWheelVisible)
            {
                // 清除所有触发标志
                __instance.SetPrivateField("mouseKeyboardTriggerInputThisFrame", false);
                __instance.SetPrivateField("mouseKeyboardTriggerInput", false);
                __instance.SetPrivateField("mouseKeyboardTriggerReleaseThisFrame", false);

                // Debug.Log($"[CharacterInputShortcutPatch] 轮盘显示，已清除触发标志，EventSystem可继续处理拖动");
            }
        }

        /// <summary>
        /// 检查鼠标是否指向轮盘UI
        /// 通过向上遍历父级来检查是否在轮盘 Canvas 内
        /// </summary>
        private static bool IsPointerOverWheel()
        {
            // 使用 EventSystem 检查鼠标是否在 UI 上
            UnityEngine.EventSystems.PointerEventData pointerData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            pointerData.position = Input.mousePosition;

            List<UnityEngine.EventSystems.RaycastResult> results = new List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointerData, results);

            // 检查是否有任何轮盘相关的UI被射线击中
            foreach (var result in results)
            {
                // 方法1: 检查是否击中了轮盘容器或其子物体
                if (result.gameObject.name.Contains("WheelItem") ||
                    result.gameObject.name.Contains("WheelContainer") ||
                    result.gameObject.name.Contains("ItemWheel"))
                {
                    Debug.Log($"[CharacterInputShortcutPatch] 鼠标在轮盘上（直接匹配），击中: {result.gameObject.name}");
                    return true;
                }

                // 方法2: 检查是否击中了 WheelItemDisplay 组件
                if (result.gameObject.GetComponent<WheelItemDisplay>() != null)
                {
                    Debug.Log($"[CharacterInputShortcutPatch] 鼠标在WheelItemDisplay上");
                    return true;
                }

                // 方法3: 检查父级中是否有轮盘 Canvas（重要：可以识别 Background、InputBlocker 等轮盘内的所有元素）
                Transform parent = result.gameObject.transform.parent;
                while (parent != null)
                {
                    // 检查父级是否是轮盘 Canvas
                    if (parent.name.Contains("ItemWheelCanvas"))
                    {
                        Debug.Log($"[CharacterInputShortcutPatch] 鼠标在轮盘内（通过父级检查），击中: {result.gameObject.name}，父级: {parent.name}");
                        return true;
                    }

                    // 检查父级是否包含 WheelContainer
                    if (parent.name.Contains("WheelContainer"))
                    {
                        Debug.Log($"[CharacterInputShortcutPatch] 鼠标在轮盘内（通过WheelContainer父级），击中: {result.gameObject.name}");
                        return true;
                    }

                    parent = parent.parent;
                }
            }

            if (results.Count > 0)
            {
                Debug.Log($"[CharacterInputShortcutPatch] 鼠标不在轮盘上，raycast结果数: {results.Count}，击中: {string.Join(", ", System.Linq.Enumerable.Select(results, r => r.gameObject.name))}");
            }
            return false;
        }

        /// <summary>
        /// 补丁鼠标滚轮输入
        /// 当轮盘显示时，阻止鼠标滚轮（防止切换武器）
        /// </summary>
        [HarmonyPatch("OnMouseScollerInput")]
        [HarmonyPrefix]
        private static bool OnMouseScollerInputPrefix(InputAction.CallbackContext context)
        {
            // 当轮盘显示时，阻止鼠标滚轮输入
            if (InputInterceptor.IsWheelVisible)
            {
                Debug.Log($"[CharacterInputShortcutPatch] 轮盘可见，阻止鼠标滚轮输入");
                return false;  // 阻止原方法执行
            }

            return true;  // 允许原方法执行
        }
    }
}
