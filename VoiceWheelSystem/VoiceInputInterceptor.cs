using UnityEngine;
using HarmonyLib;
using ItemStatsSystem;

namespace VoiceWheelSystem
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

        // 鼠标位置记录
        private Vector2 _pressDownMousePos = Vector2.zero;
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

        private void Update()
        {
            // 简单直接的U键检测（使用旧的Input System）
            HandleVoiceKeyInput();
        }
        #endregion

        #region 输入处理
        /// <summary>
        /// 处理语音轮盘键输入
        /// </summary>
        private void HandleVoiceKeyInput()
        {
            // 调试：每帧都检测按键状态
            bool uKeyDown = Input.GetKeyDown(VOICE_WHEEL_KEY);
            bool uKeyUp = Input.GetKeyUp(VOICE_WHEEL_KEY);
            bool uKeyPressed = Input.GetKey(VOICE_WHEEL_KEY);

            // 每秒输出一次状态（避免刷屏）
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[VoiceInputInterceptor] F1/Quack键状态检测: Down={uKeyDown}, Up={uKeyUp}, Pressed={uKeyPressed}");
            }

            // 检测按键按下
            if (uKeyDown)
            {
                Debug.Log("[VoiceInputInterceptor] F1/Quack键按下 - Input.GetKeyDown检测到");
                OnVoiceKeyPressed();
            }

            // 检测按键释放
            if (uKeyUp)
            {
                Debug.Log("[VoiceInputInterceptor] F1/Quack键释放 - Input.GetKeyUp检测到");
                OnVoiceKeyReleased();
            }

            // 处理长按检测
            if (_isKeyPressed)
            {
                _pressDuration += Time.deltaTime;

                // 如果达到长按阈值且轮盘未显示，显示轮盘
                if (_pressDuration >= LONG_PRESS_THRESHOLD && !_wheelShown)
                {
                    Debug.Log($"[VoiceInputInterceptor] 长按时间达到 {_pressDuration:F2}s，显示语音轮盘");
                    ShowVoiceWheel();
                    _wheelShown = true;
                }
            }
        }

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
        /// 直接播放当前选中的语音
        /// </summary>
        private void ExecuteShortPress()
        {
            if (VoiceWheelManager.Instance != null)
            {
                var currentVoice = VoiceWheelManager.Instance.VoiceData.GetSelectedVoice();
                if (currentVoice != null)
                {
                    // 直接播放语音
                    VoiceWheelManager.Instance.PlayVoice(currentVoice);
                    Debug.Log($"[VoiceInputInterceptor] ✓ 短按播放语音: {currentVoice.displayName}");
                }
                else
                {
                    Debug.LogWarning("[VoiceInputInterceptor] 没有选中的语音可播放");
                }
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
            _pressDuration = 0f;
            _wheelShown = false;
            _pressDownMousePos = Input.mousePosition;

            Debug.LogError("[VoiceInputInterceptor] ✓ F1键按下（通过VoiceInputPatch）");

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

            Debug.Log($"[VoiceInputInterceptor] U键释放（通过VoiceInputPatch），按下时长: {_pressDuration:F2}s");

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