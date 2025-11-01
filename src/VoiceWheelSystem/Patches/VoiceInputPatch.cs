using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Backpack_QuickWheel.VoiceWheelSystem.Patches
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

        // 长按检查
        private static bool _isKeyPressed = false;
        private static float _pressStartTime = 0f;
        private static bool _wheelShown = false;

        /// <summary>
        /// 🔧 处理语音按键的前置补丁 - 选择性拦截逻辑
        /// 对于"嘎"语音：长按显示轮盘，短按显示气泡+让官方播放
        /// 对于其他语音：使用我们的轮盘系统
        /// </summary>
        private static bool HandleVoiceKeyPressed(InputAction.CallbackContext context)
        {
            // 立即输出最基础的调试信息，确认补丁被调用
            Debug.LogError("=== VOICE INPUT PATCH PREFIX CALLED ===");
            Debug.LogError("[VoiceInputPatch] Quack输入被拦截，context.phase=" + context.phase);

            // 检查是否应该让官方逻辑执行
            bool isGaVoice = ShouldLetOfficialLogicExecute();
            Debug.LogError($"[VoiceInputPatch] 是否是嘎语音: {isGaVoice}");

            if (context.started)
            {
                Debug.LogError("[VoiceInputPatch] Quack按键started - 开始处理");

                if (isGaVoice)
                {
                    Debug.LogError("[VoiceInputPatch] 🔧 嘎语音按键started - 开始长按检测，同时触发我们的轮盘事件");
                    // 对于嘎语音，我们也要开始长按检测和轮盘显示
                    OnVoiceKeyPressed?.Invoke();

                    var voiceInterceptor = VoiceInputInterceptor.Instance;
                    if (voiceInterceptor != null)
                    {
                        voiceInterceptor.OnKeyPressed(); // 开始长按检测
                    }
                }
                else
                {
                    Debug.LogError("[VoiceInputPatch] 其他语音按键started - 触发语音轮盘事件");
                    OnVoiceKeyPressed?.Invoke();

                    var voiceInterceptor = VoiceInputInterceptor.Instance;
                    if (voiceInterceptor != null)
                    {
                        voiceInterceptor.OnKeyPressed();
                    }
                }
            }
            else if (context.canceled)
            {
                Debug.LogError("[VoiceInputPatch] Quack按键canceled - 开始处理");

                if (isGaVoice)
                {
                    Debug.LogError("[VoiceInputPatch] 🔧 嘎语音按键canceled - 触发释放事件");
                    OnVoiceKeyReleased?.Invoke();

                    var voiceInterceptor = VoiceInputInterceptor.Instance;
                    if (voiceInterceptor != null)
                    {
                        voiceInterceptor.OnKeyReleased(); // 处理长按/短按判断
                    }

                    // 如果是短按，显示我们的气泡
                    ShowGaVoiceBubble();
                }
                else
                {
                    Debug.LogError("[VoiceInputPatch] 其他语音按键canceled - 触发语音轮盘事件");
                    OnVoiceKeyReleased?.Invoke();

                    var voiceInterceptor = VoiceInputInterceptor.Instance;
                    if (voiceInterceptor != null)
                    {
                        voiceInterceptor.OnKeyReleased();
                    }
                }
            }

            // 对于嘎语音，让官方逻辑也执行（播放官方嘎语音）
            if (isGaVoice)
            {
                Debug.LogError("[VoiceInputPatch] 🔧 嘎语音 - 让官方逻辑执行（播放官方嘎语音）");
                return true; // 让原方法执行
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
        /// 🔧 检查是否应该让官方逻辑执行
        /// 当当前选中的是"嘎"语音时返回true
        /// </summary>
        private static bool ShouldLetOfficialLogicExecute()
        {
            try
            {
                if (VoiceWheelManager.Instance?.VoiceData?.GetSelectedVoice() is VoiceItem currentVoice)
                {
                    // 检查是否是"嘎"语音
                    if (currentVoice.displayName == "嘎" || currentVoice.bubbleText == "嘎")
                    {
                        Debug.LogError($"[VoiceInputPatch] 🔧 当前语音是'嘎'，让官方逻辑执行");
                        return true;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[VoiceInputPatch] 检查语音时出错: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// 🔧 显示"嘎"语音的气泡
        /// </summary>
        private static void ShowGaVoiceBubble()
        {
            try
            {
                if (VoiceWheelManager.Instance?.VoiceData?.GetSelectedVoice() is VoiceItem currentVoice)
                {
                    Debug.LogError($"[VoiceInputPatch] 🔧 显示嘎语音气泡: {currentVoice.bubbleText}");

                    // 调用VoiceWheelManager显示气泡
                    VoiceWheelManager.Instance.ShowVoiceBubbleOnly(currentVoice.bubbleText);
                }
                else
                {
                    Debug.LogError("[VoiceInputPatch] ✗ 无法获取当前嘎语音数据");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[VoiceInputPatch] 显示嘎语音气泡时出错: {ex.Message}");
            }
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