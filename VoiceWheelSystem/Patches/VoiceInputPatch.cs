using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VoiceWheelSystem.Patches
{
    /// <summary>
    /// 补丁 CharacterInputControl 的Quack输入方法，拦截F1/嘎声音输入
    /// 完全模仿现有轮盘系统的实现方式
    /// </summary>
    [HarmonyPatch(typeof(CharacterInputControl))]
    public static class VoiceInputPatch
    {
        // 语音按键的事件
        public static event System.Action OnVoiceKeyPressed;
        public static event System.Action OnVoiceKeyReleased;

        /// <summary>
        /// 处理语音按键的前置补丁
        /// 完全模仿CharacterInputShortcutPatch的OnShortcutKeyPressed方法
        /// 检查 context 的 started 和 canceled，触发对应事件
        /// 当我们的语音轮盘系统启用时，阻止原方法执行
        /// </summary>
        private static bool HandleVoiceKeyPressed(InputAction.CallbackContext context)
        {
            // 立即输出最基础的调试信息，确认补丁被调用
            Debug.LogError("=== VOICE INPUT PATCH PREFIX CALLED ===");
            Debug.LogError("[VoiceInputPatch] Quack输入被拦截，context.phase=" + context.phase);

            // 只要我们的语音轮盘系统存在，就拦截
            if (VoiceInputInterceptor.Instance == null)
            {
                Debug.LogError("[VoiceInputPatch] VoiceInputInterceptor不存在，让原方法执行");
                return true; // 让原方法执行
            }

            if (context.started)
            {
                Debug.LogError("[VoiceInputPatch] Quack按键started - 触发语音轮盘事件");
                OnVoiceKeyPressed?.Invoke();

                // 调用VoiceInputInterceptor处理按键按下
                var voiceInterceptor = VoiceInputInterceptor.Instance;
                if (voiceInterceptor != null)
                {
                    voiceInterceptor.OnKeyPressed();
                }
            }
            else if (context.canceled)
            {
                Debug.LogError("[VoiceInputPatch] Quack按键canceled - 触发语音轮盘事件");
                OnVoiceKeyReleased?.Invoke();

                // 调用VoiceInputInterceptor处理按键释放
                var voiceInterceptor = VoiceInputInterceptor.Instance;
                if (voiceInterceptor != null)
                {
                    voiceInterceptor.OnKeyReleased();
                }
            }

            // 当 context.performed 时，我们的系统会处理，不让原方法执行
            if (context.performed)
            {
                Debug.LogError("[VoiceInputPatch] 阻止原方法执行（系统已处理）");
                return false;  // 阻止原方法执行
            }

            // 其他情况（started 或 canceled）也阻止原方法，因为我们完全接管了处理
            return false;
        }

        /// <summary>
        /// 拦截Quack输入（F1键/嘎声音）
        /// 使用Prefix并返回bool来控制原方法执行
        /// </summary>
        [HarmonyPatch("OnQuackInput")]
        [HarmonyPrefix]
        private static bool OnQuackInputPrefix(InputAction.CallbackContext context)
        {
            return HandleVoiceKeyPressed(context);
        }

        /// <summary>
        /// 同时拦截RegisterEvents，确保我们的补丁被正确应用
        /// </summary>
        [HarmonyPatch("RegisterEvents")]
        [HarmonyPostfix]
        private static void RegisterEventsPostfix(CharacterInputControl __instance)
        {
            Debug.LogError("[VoiceInputPatch] RegisterEvents完成，VoiceInputPatch已激活");
        }
    }
}