using System;
using System.Collections;
using UnityEngine;
using VoiceWheelSystem;
using HarmonyLib;
using Duckov;

namespace VoiceWheelSystem
{
    /// <summary>
    /// 语音轮盘系统主控制器
    /// 负责管理语音轮盘的核心逻辑和状态
    /// </summary>
    public class VoiceWheelManager : MonoBehaviour
    {
        #region 单例模式
        private static VoiceWheelManager _instance;
        public static VoiceWheelManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<VoiceWheelManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("VoiceWheelManager");
                        _instance = go.AddComponent<VoiceWheelManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region 私有字段
        private VoiceWheelData _voiceData;
        private VoiceWheelSelector _wheelSelector;
        private VoiceBubbleManager _bubbleManager;
        private VoiceNPCManager _npcManager;
        private VoiceAudioManager _audioManager;

        [Header("输入设置")]
        private KeyCode _voiceWheelKey = KeyCode.T;
        private bool _isVoiceWheelActive = false;

        [Header("状态管理")]
        private bool _isPlayingVoice = false;
        private VoiceItem _currentPlayingVoice = null;
        #endregion

        #region 公共属性
        public VoiceWheelData VoiceData => _voiceData;
        public bool IsVoiceWheelActive => _isVoiceWheelActive;
        public bool IsPlayingVoice => _isPlayingVoice;
        public VoiceItem CurrentPlayingVoice => _currentPlayingVoice;
        #endregion

        #region Unity生命周期
        private void Awake()
        {
            // 单例检查
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeComponents();
            LoadVoiceData();
        }

        private void Start()
        {
            // 初始化各个管理器
            _bubbleManager = gameObject.AddComponent<VoiceBubbleManager>();
            _npcManager = gameObject.AddComponent<VoiceNPCManager>();
            _wheelSelector = gameObject.AddComponent<VoiceWheelSelector>();
            _audioManager = gameObject.AddComponent<VoiceAudioManager>();
        }

        private void Update()
        {
            // 输入处理现在由VoiceInputInterceptor管理
            // 这里主要用于更新组件状态
            UpdateComponents();
        }

        private void UpdateComponents()
        {
            // 确保各组件引用有效
            if (_bubbleManager == null)
                _bubbleManager = GetComponent<VoiceBubbleManager>();
            if (_npcManager == null)
                _npcManager = GetComponent<VoiceNPCManager>();
            if (_wheelSelector == null)
                _wheelSelector = GetComponent<VoiceWheelSelector>();
        }

        private void OnDestroy()
        {
            // 清理资源
            if (_voiceData != null)
            {
                SaveVoiceData();
            }
        }
        #endregion

        #region 初始化
        private void InitializeComponents()
        {
            // 初始化基础组件
        }

        private void LoadVoiceData()
        {
            // TODO: 从文件加载语音数据
            _voiceData = VoiceWheelData.CreateDefault();

            // 如果没有默认语音，创建一个
            if (_voiceData.voiceItems.Count == 0)
            {
                _voiceData = VoiceWheelData.CreateDefault();
            }
        }

        private void SaveVoiceData()
        {
            // TODO: 保存语音数据到文件
            // VoiceConfigManager.SaveData(_voiceData);
        }
        #endregion

        #region 输入处理接口（由VoiceInputInterceptor调用）
        /// <summary>
        /// 语音轮盘按键按下（由VoiceInputInterceptor调用）
        /// </summary>
        public void OnWheelKeyPressed()
        {
            Debug.Log("[VoiceWheelManager] 轮盘按键按下");

            // 这里可以添加按键按下时的处理逻辑
            // 比如准备显示轮盘等
        }
        #endregion

        #region 轮盘控制
        public void ShowVoiceWheel()
        {
            if (_isVoiceWheelActive) return;

            _isVoiceWheelActive = true;

            if (_wheelSelector != null)
            {
                _wheelSelector.Show(_voiceData.GetWheelVoices());
            }

            // 阻止游戏输入
            // GameInputManager.DisableInput(true);
        }

        public void HideVoiceWheel()
        {
            if (!_isVoiceWheelActive) return;

            _isVoiceWheelActive = false;

            if (_wheelSelector != null)
            {
                _wheelSelector.Hide();
            }

            // 恢复游戏输入
            // GameInputManager.DisableInput(false);
        }

        /// <summary>
        /// 处理轮盘内的鼠标移动和选择
        /// 由VoiceInputInterceptor调用
        /// </summary>
        public void HandleWheelSelection()
        {
            if (_wheelSelector != null)
            {
                _wheelSelector.HandleSelection();
            }
        }

        public void OnWheelHover(VoiceItem voice)
        {
            // 鼠标悬停时的处理
            // 可以播放预览音效
        }

        public void OnWheelSelect(VoiceItem voice)
        {
            // 轮盘选择确认
            SelectVoice(voice);
        }
        #endregion

        #region 语音播放
        public void PlayVoice(VoiceItem voice)
        {
            if (voice == null || !voice.IsAvailable())
            {
                Debug.LogWarning($"[VoiceWheelManager] 尝试播放不可用的语音: {voice?.displayName}");
                return;
            }

            // 如果正在播放其他语音，停止它
            if (_isPlayingVoice)
            {
                StopCurrentVoice();
            }

            StartCoroutine(PlayVoiceCoroutine(voice));
        }

        private IEnumerator PlayVoiceCoroutine(VoiceItem voice)
        {
            _isPlayingVoice = true;
            _currentPlayingVoice = voice;

            // 显示气泡文字
            if (_bubbleManager != null)
            {
                _bubbleManager.ShowBubble(voice.bubbleText);
            }

            // 影响NPC
            if (_npcManager != null)
            {
                _npcManager.AffectNearbyNPCs(voice.affectRange);
            }

            // 播放音频（使用VoiceAudioManager）
            if (_audioManager != null)
            {
                _audioManager.PlayVoiceAudio(voice.audioPath);

                // 等待音频播放完成（简化处理，实际应该等待音频长度）
                yield return new WaitForSeconds(1.0f);
            }
            else
            {
                // 没有音频管理器时，只显示气泡
                yield return new WaitForSeconds(1.0f);
            }

            _isPlayingVoice = false;
            _currentPlayingVoice = null;
        }

        public void StopCurrentVoice()
        {
            if (_audioManager != null)
            {
                _audioManager.StopCurrentAudio();
            }

            if (_bubbleManager != null)
            {
                _bubbleManager.HideCurrentBubble();
            }

            _isPlayingVoice = false;
            _currentPlayingVoice = null;
        }
        #endregion

        #region 语音选择
        public void SelectVoice(VoiceItem voice)
        {
            if (voice == null) return;

            // 更新选中索引
            for (int i = 0; i < _voiceData.voiceItems.Count; i++)
            {
                if (_voiceData.voiceItems[i].voiceId == voice.voiceId)
                {
                    _voiceData.selectedVoiceIndex = i;
                    break;
                }
            }

            // 立即播放选中的语音（可选，根据需求决定是否自动播放）
            // PlayVoice(voice);
        }

        public void SelectVoiceByWheelPosition(int position)
        {
            var voice = _voiceData.GetVoiceAtWheelPosition(position);
            if (voice != null)
            {
                SelectVoice(voice);
            }
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 刷新语音数据（用于配置更新后）
        /// </summary>
        public void RefreshVoiceData()
        {
            LoadVoiceData();

            if (_wheelSelector != null && _isVoiceWheelActive)
            {
                _wheelSelector.RefreshWheel(_voiceData.GetWheelVoices());
            }
        }

        /// <summary>
        /// 添加新语音
        /// </summary>
        public bool AddVoice(VoiceItem voice, int wheelPosition = -1)
        {
            if (voice == null) return false;

            if (wheelPosition >= 0 && wheelPosition < 8)
            {
                return _voiceData.AddVoiceToWheel(voice, wheelPosition);
            }
            else
            {
                if (!_voiceData.voiceItems.Contains(voice))
                {
                    _voiceData.voiceItems.Add(voice);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取当前选中的语音
        /// </summary>
        public VoiceItem GetSelectedVoice()
        {
            if (_wheelSelector != null)
            {
                return _wheelSelector.GetCurrentSelectedVoice();
            }
            return null;
        }

        /// <summary>
        /// 移除语音
        /// </summary>
        public bool RemoveVoice(string voiceId)
        {
            var voice = _voiceData.voiceItems.Find(v => v.voiceId == voiceId);
            if (voice != null)
            {
                _voiceData.voiceItems.Remove(voice);
                return true;
            }
            return false;
        }
        #endregion
    }
}