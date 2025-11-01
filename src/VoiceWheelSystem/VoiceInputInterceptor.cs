using UnityEngine;
using HarmonyLib;
using ItemStatsSystem;

namespace Backpack_QuickWheel.VoiceWheelSystem
{
    /// <summary>
    /// 语音轮盘输入拦截器
    /// 检测T键的按下和释放，实现长按显示轮盘，短按直接播放语音
    /// 复用现有物品轮盘的输入处理逻辑
    /// </summary>
    public class VoiceInputInterceptor : MonoBehaviour
    {
        #region 单例模式
        private static VoiceInputInterceptor _instance;
        public static VoiceInputInterceptor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<VoiceInputInterceptor>();
                    if (_instance == null)
                    {
                        var go = new GameObject("VoiceInputInterceptor");
                        _instance = go.AddComponent<VoiceInputInterceptor>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region 配置常量
        [Header("输入配置")]
        private const KeyCode VOICE_WHEEL_KEY = KeyCode.F1;       // 语音轮盘快捷键（F1/Quack）
        private const float LONG_PRESS_THRESHOLD = 0.2f;         // 长按时间阈值（秒）
        #endregion

        #region 私有字段
        // 按键状态追踪
        private bool _isKeyPressed = false;
        private float _pressDuration = 0f;
        private bool _wheelShown = false;
        private float _pressStartTime = 0f;

        // 鼠标位置记录
        private Vector2 _pressDownMousePos = Vector2.zero;
        private Coroutine _longPressCheckCoroutine;
        #endregion

        #region Unity生命周期
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log("[VoiceInputInterceptor] 初始化完成，使用F1/Quack键作为语音轮盘触发");
        }

        // Update方法已移除 - 我们完全依赖Harmony补丁来处理输入，不再需要轮询
        #endregion

        #region 输入处理
        // HandleVoiceKeyInput方法已移除 - 我们完全依赖Harmony补丁来处理输入

        /// <summary>
        /// T键按下处理
        /// </summary>
        private void OnVoiceKeyPressed()
        {
            if (_isKeyPressed) return; // 防止重复触发

            _isKeyPressed = true;
            _pressDuration = 0f;
            _wheelShown = false;
            _pressDownMousePos = Input.mousePosition;

            Debug.Log("[VoiceInputInterceptor] ✓ T键按下");

            // 通知VoiceWheelManager开始计时
            if (VoiceWheelManager.Instance != null)
            {
                VoiceWheelManager.Instance.OnWheelKeyPressed();
            }
        }

        /// <summary>
        /// T键释放处理
        /// </summary>
        private void OnVoiceKeyReleased()
        {
            if (!_isKeyPressed) return;

            _isKeyPressed = false;

            Debug.Log($"[VoiceInputInterceptor] T键释放，按下时长: {_pressDuration:F2}s");

            if (_wheelShown)
            {
                // 轮盘已显示，执行选择
                Debug.Log("[VoiceInputInterceptor] 轮盘显示中，执行选择");
                ExecuteWheelSelection();
            }
            else
            {
                // 轮盘未显示，执行短按（直接播放当前选中的语音）
                Debug.Log("[VoiceInputInterceptor] 轮盘未显示，执行短按");
                ExecuteShortPress();
            }

            // 重置状态
            _wheelShown = false;
            _pressDuration = 0f;
        }
        #endregion

        #region 轮盘控制
        /// <summary>
        /// 显示语音轮盘
        /// </summary>
        private void ShowVoiceWheel()
        {
            if (VoiceWheelManager.Instance != null)
            {
                VoiceWheelManager.Instance.ShowVoiceWheel();
            }
            else
            {
                Debug.LogWarning("[VoiceInputInterceptor] VoiceWheelManager实例不存在");
            }
        }

        /// <summary>
        /// 隐藏语音轮盘
        /// </summary>
        private void HideVoiceWheel()
        {
            if (VoiceWheelManager.Instance != null)
            {
                VoiceWheelManager.Instance.HideVoiceWheel();
            }
        }
        #endregion

        #region 操作执行
        /// <summary>
        /// 执行轮盘选择操作
        /// 长按释放时调用，选择当前高亮的语音但不播放（类似手雷逻辑）
        /// </summary>
        private void ExecuteWheelSelection()
        {
            if (VoiceWheelManager.Instance != null)
            {
                var selectedVoice = VoiceWheelManager.Instance.GetSelectedVoice();
                if (selectedVoice != null)
                {
                    // 选择语音但不播放
                    VoiceWheelManager.Instance.SelectVoice(selectedVoice);
                    Debug.Log($"[VoiceInputInterceptor] ✓ 选择语音: {selectedVoice.displayName}");
                }
                else
                {
                    Debug.Log("[VoiceInputInterceptor] 没有选中的语音");
                }

                HideVoiceWheel();
            }
        }

        /// <summary>
        /// 执行短按操作
        /// 直接播放当前选中的语音，但"嘎"语音使用官方逻辑
        /// </summary>
        private void ExecuteShortPress()
        {
            Debug.LogError("[VoiceInputInterceptor] === 执行短按操作开始 ===");

            if (VoiceWheelManager.Instance != null)
            {
                Debug.LogError($"[VoiceInputInterceptor] VoiceWheelManager实例存在");
                Debug.LogError($"[VoiceInputInterceptor] 语音数据总数: {VoiceWheelManager.Instance.VoiceData.voiceItems.Count}");
                Debug.LogError($"[VoiceInputInterceptor] 选中索引: {VoiceWheelManager.Instance.VoiceData.selectedVoiceIndex}");

                var currentVoice = VoiceWheelManager.Instance.VoiceData.GetSelectedVoice();
                if (currentVoice != null)
                {
                    Debug.LogError($"[VoiceInputInterceptor] ✓ 找到当前语音: {currentVoice.displayName}");
                    Debug.LogError($"[VoiceInputInterceptor] 气泡文本: '{currentVoice.bubbleText}'");
                    Debug.LogError($"[VoiceInputInterceptor] 音频路径: {currentVoice.audioPath}");
                    Debug.LogError($"[VoiceInputInterceptor] IsAvailable: {currentVoice.IsAvailable()}");

                    // 🔧 优化：检查是否是"嘎"语音
                    if (IsGaVoice(currentVoice))
                    {
                        Debug.LogError("[VoiceInputInterceptor] 🔧 检测到'嘎'语音，不播放自定义音频，让官方逻辑处理");
                        // 不调用PlayVoice，让官方逻辑播放官方的"嘎"语音
                        // 但仍然显示我们的气泡效果
                        ShowVoiceBubble(currentVoice);
                    }
                    else
                    {
                        // 直接播放语音
                        Debug.LogError($"[VoiceInputInterceptor] 调用VoiceWheelManager.PlayVoice");
                        VoiceWheelManager.Instance.PlayVoice(currentVoice);
                        Debug.LogError($"[VoiceInputInterceptor] ✓ PlayVoice调用完成");
                    }
                }
                else
                {
                    Debug.LogError("[VoiceInputInterceptor] ✗ 没有选中的语音可播放");
                    Debug.LogError($"[VoiceInputInterceptor] 语音数据状态: 总数={VoiceWheelManager.Instance.VoiceData.voiceItems.Count}, 选中索引={VoiceWheelManager.Instance.VoiceData.selectedVoiceIndex}");

                    // 列出所有语音
                    for (int i = 0; i < VoiceWheelManager.Instance.VoiceData.voiceItems.Count; i++)
                    {
                        var voice = VoiceWheelManager.Instance.VoiceData.voiceItems[i];
                        Debug.LogError($"[VoiceInputInterceptor] 语音[{i}]: {voice.displayName}, Available: {voice.IsAvailable()}");
                    }
                }
            }
            else
            {
                Debug.LogError("[VoiceInputInterceptor] ✗ VoiceWheelManager实例不存在");
            }

            Debug.LogError("[VoiceInputInterceptor] === 执行短按操作完成 ===");
        }

        /// <summary>
        /// 🔧 检查是否是"嘎"语音
        /// 通过名称或气泡文本来识别
        /// </summary>
        private bool IsGaVoice(VoiceItem voice)
        {
            if (voice == null) return false;

            // 检查显示名称
            if (voice.displayName == "嘎") return true;

            // 检查气泡文本
            if (voice.bubbleText == "嘎") return true;

            return false;
        }

        /// <summary>
        /// 🔧 显示语音气泡（用于"嘎"语音的特殊处理）
        /// </summary>
        private void ShowVoiceBubble(VoiceItem voice)
        {
            if (voice == null) return;

            Debug.LogError($"[VoiceInputInterceptor] 🔧 显示'嘎'语音气泡: {voice.bubbleText}");

            if (VoiceWheelManager.Instance != null)
            {
                // 只显示气泡，不播放音频
                VoiceWheelManager.Instance.ShowVoiceBubbleOnly(voice.bubbleText);
                Debug.LogError($"[VoiceInputInterceptor] ✓ '嘎'语音气泡已显示");
            }
            else
            {
                Debug.LogError("[VoiceInputInterceptor] ✗ VoiceWheelManager实例不存在，无法显示气泡");
            }
        }
        #endregion

        #region 公共接口
        /// <summary>
        /// 检查语音轮盘是否正在显示
        /// </summary>
        public bool IsVoiceWheelVisible()
        {
            return VoiceWheelManager.Instance != null && VoiceWheelManager.Instance.IsVoiceWheelActive;
        }

        /// <summary>
        /// 强制隐藏语音轮盘（用于特殊情况）
        /// </summary>
        public void ForceHideVoiceWheel()
        {
            if (_wheelShown)
            {
                HideVoiceWheel();
                _wheelShown = false;
                _isKeyPressed = false;
                _pressDuration = 0f;
                Debug.Log("[VoiceInputInterceptor] 强制隐藏语音轮盘");
            }
        }

        /// <summary>
        /// 获取当前输入状态
        /// </summary>
        public (bool keyPressed, float pressDuration, bool wheelShown) GetInputState()
        {
            return (_isKeyPressed, _pressDuration, _wheelShown);
        }

        /// <summary>
        /// 按键按下事件（由VoiceInputPatch调用）
        /// </summary>
        public void OnKeyPressed()
        {
            // 立即输出最明显的调试信息
            Debug.LogError("!!! VOICE INPUT INTERCEPTOR OnKeyPressed CALLED !!!");

            if (_isKeyPressed) return; // 防止重复触发

            _isKeyPressed = true;
            _pressStartTime = Time.time;
            _pressDuration = 0f;
            _wheelShown = false;
            _pressDownMousePos = Input.mousePosition;

            Debug.LogError("[VoiceInputInterceptor] ✓ F1键按下（通过VoiceInputPatch）");

            // 启动长按检查协程
            _longPressCheckCoroutine = StartCoroutine(LongPressCheckCoroutine());

            // 通知VoiceWheelManager开始计时
            if (VoiceWheelManager.Instance != null)
            {
                VoiceWheelManager.Instance.OnWheelKeyPressed();
            }
        }

        /// <summary>
        /// 按键释放事件（由VoiceInputPatch调用）
        /// </summary>
        public void OnKeyReleased()
        {
            if (!_isKeyPressed) return;

            _isKeyPressed = false;
            _pressDuration = Time.time - _pressStartTime;

            Debug.LogError($"[VoiceInputInterceptor] === F1键释放事件开始 ===");
            Debug.LogError($"[VoiceInputInterceptor] 按下时长: {_pressDuration:F2}s");
            Debug.LogError($"[VoiceInputInterceptor] 轮盘是否已显示: {_wheelShown}");
            Debug.LogError($"[VoiceInputInterceptor] 长按阈值: {LONG_PRESS_THRESHOLD:F2}s");

            // 停止长按检查协程
            if (_longPressCheckCoroutine != null)
            {
                StopCoroutine(_longPressCheckCoroutine);
                _longPressCheckCoroutine = null;
            }

            if (_wheelShown)
            {
                // 轮盘已显示，执行选择
                Debug.LogError("[VoiceInputInterceptor] 轮盘显示中，执行选择");
                ExecuteWheelSelection();
            }
            else
            {
                // 轮盘未显示，执行短按（直接播放当前选中的语音）
                Debug.LogError("[VoiceInputInterceptor] 轮盘未显示，执行短按");
                ExecuteShortPress();
            }

            // 重置状态
            _wheelShown = false;
            _pressDuration = 0f;
            Debug.LogError($"[VoiceInputInterceptor] === F1键释放事件完成 ===");
        }

        /// <summary>
        /// 更新按键持续时间（由VoiceInputPatch调用）
        /// </summary>
        public void UpdatePressDuration(float deltaTime)
        {
            if (_isKeyPressed)
            {
                _pressDuration += deltaTime;
            }
        }

        /// <summary>
        /// 检查是否应该显示轮盘（由VoiceInputPatch调用）
        /// </summary>
        public bool ShouldShowWheel()
        {
            return _isKeyPressed && _pressDuration >= LONG_PRESS_THRESHOLD && !_wheelShown;
        }

        /// <summary>
        /// 设置轮盘显示状态（由VoiceInputPatch调用）
        /// </summary>
        public void SetWheelShown(bool shown)
        {
            _wheelShown = shown;
        }

        /// <summary>
        /// 长按检查协程
        /// </summary>
        private System.Collections.IEnumerator LongPressCheckCoroutine()
        {
            while (_isKeyPressed)
            {
                _pressDuration = Time.time - _pressStartTime;

                // 检查是否达到长按阈值且轮盘未显示
                if (_pressDuration >= LONG_PRESS_THRESHOLD && !_wheelShown)
                {
                    _wheelShown = true;
                    Debug.Log($"[VoiceInputInterceptor] 长按阈值达到（{_pressDuration:F2}s），显示语音轮盘");
                    ShowVoiceWheel();
                }

                yield return null; // 下一帧继续检查
            }
        }

        /// <summary>
        /// 处理轮盘内的鼠标移动和选择（由VoiceInputPatch调用）
        /// </summary>
        public void HandleWheelSelection()
        {
            if (VoiceWheelManager.Instance != null)
            {
                // 委托给VoiceWheelManager处理轮盘选择逻辑
                VoiceWheelManager.Instance.HandleWheelSelection();
            }
        }
        #endregion

        #region 调试信息
        /// <summary>
        /// 调试信息显示
        /// </summary>
        [Header("调试信息")]
        public bool showDebugInfo = false;

        private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUI.skin.label.fontSize = 14;
            GUILayout.BeginArea(new Rect(10, 400, 300, 150));
            GUILayout.Label("VoiceInputInterceptor 状态:");
            GUILayout.Label($"按键状态: {(_isKeyPressed ? "按下" : "释放")}");
            GUILayout.Label($"按下时长: {_pressDuration:F2}s");
            GUILayout.Label($"轮盘显示: {(_wheelShown ? "是" : "否")}");
            GUILayout.Label($"监听按键: {VOICE_WHEEL_KEY}");
            GUILayout.Label($"长按阈值: {LONG_PRESS_THRESHOLD:F1}s");
            GUILayout.EndArea();
        }
        #endregion
    }
}