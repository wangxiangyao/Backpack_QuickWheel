using UnityEngine;
using System.Collections;
using System.IO;
using Duckov;

namespace VoiceWheelSystem
{
    /// <summary>
    /// 语音音频播放管理器
    /// 基于游戏现有的AudioManager系统
    /// 使用PostCustomSFX播放自定义音频文件
    /// </summary>
    public class VoiceAudioManager : MonoBehaviour
    {
        #region 音频配置
        [Header("音频配置")]
        private const string CUSTOM_AUDIO_FILENAME = "Ga_Sound.mp3"; // 自定义音频文件名
        private const float AUDIO_COOLDOWN = 0.1f; // 音频播放冷却时间
        #endregion

        #region 私有字段
        private CharacterMainControl _playerCharacter;
        private float _lastPlayTime = 0f;
        private bool _isInitialized = false;
        private string _customAudioPath = ""; // 自定义音频文件的完整路径
        #endregion

        #region Unity生命周期
        private void Start()
        {
            InitializeAudioManager();
        }

        private void OnDestroy()
        {
            CleanupAudioManager();
        }
        #endregion

        #region 初始化
        private void InitializeAudioManager()
        {
            // 获取玩家角色引用
            _playerCharacter = CharacterMainControl.Main;

            if (_playerCharacter != null)
            {
                // 解压并准备自定义音频文件
                ExtractCustomAudioFile();

                _isInitialized = true;
                Debug.Log("[VoiceAudioManager] 初始化完成，使用AudioManager.PostCustomSFX系统");
            }
            else
            {
                Debug.LogError("[VoiceAudioManager] 无法找到玩家角色");
            }
        }

        private void CleanupAudioManager()
        {
            // 这里可以添加音频清理逻辑
            Debug.Log("[VoiceAudioManager] 音频管理器清理完成");
        }
        #endregion

        #region 自定义音频文件处理
        /// <summary>
        /// 从嵌入资源解压自定义音频文件
        /// </summary>
        private void ExtractCustomAudioFile()
        {
            try
            {
                // 获取当前程序集
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                string resourceName = "VoiceWheelSystem.Audio.Ga_Sound.mp3";

                // 获取嵌入资源流
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        Debug.LogWarning($"[VoiceAudioManager] 找不到嵌入音频资源: {resourceName}");
                        return;
                    }

                    // 创建Mod目录下的Audio文件夹
                    string modAudioDir = Path.Combine(Application.dataPath, "..", "Mods", "Backpack_QuickWheel", "Audio");
                    if (!Directory.Exists(modAudioDir))
                    {
                        Directory.CreateDirectory(modAudioDir);
                    }

                    // 解压音频文件到Mod目录
                    _customAudioPath = Path.Combine(modAudioDir, CUSTOM_AUDIO_FILENAME);

                    // 如果文件已存在且较新，不需要重复解压
                    if (File.Exists(_customAudioPath))
                    {
                        var existingTime = File.GetLastWriteTime(_customAudioPath);
                        var assemblyTime = File.GetLastWriteTime(assembly.Location);
                        if (existingTime >= assemblyTime)
                        {
                            Debug.Log($"[VoiceAudioManager] 音频文件已存在且为最新: {_customAudioPath}");
                            return;
                        }
                    }

                    // 将流写入文件
                    using (var fileStream = File.Create(_customAudioPath))
                    {
                        stream.CopyTo(fileStream);
                    }

                    Debug.Log($"[VoiceAudioManager] ✓ 成功解压自定义音频文件: {_customAudioPath}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[VoiceAudioManager] 解压自定义音频文件失败: {e.Message}");
            }
        }
        #endregion

        #region 音频播放接口
        /// <summary>
        /// 播放语音音频
        /// 优先使用自定义音频，如果没有则回退到简单方案
        /// </summary>
        public void PlayVoiceAudio(string audioEventName = null)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[VoiceAudioManager] 音频管理器未初始化");
                return;
            }

            if (_playerCharacter == null)
            {
                Debug.LogWarning("[VoiceAudioManager] 玩家角色无效");
                return;
            }

            // 性能优化：限制播放频率
            if (Time.time - _lastPlayTime < AUDIO_COOLDOWN)
            {
                return;
            }

            _lastPlayTime = Time.time;

            // 优先播放自定义音频文件
            if (!string.IsNullOrEmpty(_customAudioPath) && File.Exists(_customAudioPath))
            {
                PlayCustomAudio();
            }
            else
            {
                Debug.Log("[VoiceAudioManager] 使用简单音频播放方案");
                // 简单的音频播放方案
                PlaySimpleAudio();
            }
        }

        /// <summary>
        /// 播放自定义音频文件
        /// 使用AudioManager.PostCustomSFX
        /// </summary>
        private void PlayCustomAudio()
        {
            try
            {
                // 使用反射调用AudioManager.PostCustomSFX
                var audioManagerType = typeof(AudioManager);
                var postCustomSFXMethod = audioManagerType.GetMethod("PostCustomSFX",
                    new System.Type[] { typeof(string), typeof(GameObject), typeof(bool) });

                if (postCustomSFXMethod != null)
                {
                    var result = postCustomSFXMethod.Invoke(null, new object[] { _customAudioPath, _playerCharacter.gameObject, false });
                    Debug.Log($"[VoiceAudioManager] ✓ 播放自定义音频: {_customAudioPath}");
                }
                else
                {
                    Debug.LogWarning("[VoiceAudioManager] 无法找到PostCustomSFX方法，使用简单音频播放");
                    PlaySimpleAudio();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[VoiceAudioManager] 播放自定义音频失败: {e.Message}");
                PlaySimpleAudio();
            }
        }

        /// <summary>
        /// 简单的音频播放方案（回退方案）
        /// </summary>
        private void PlaySimpleAudio()
        {
            try
            {
                // 创建临时AudioSource播放音频
                var tempAudioSource = gameObject.GetComponent<AudioSource>();
                if (tempAudioSource == null)
                {
                    tempAudioSource = gameObject.AddComponent<AudioSource>();
                }

                // 使用WWW加载音频文件
                var www = new WWW("file://" + _customAudioPath);

                // 异步播放（简化处理）
                StartCoroutine(PlayAudioClipAsync(www));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[VoiceAudioManager] 简单音频播放失败: {e.Message}");
            }
        }

        /// <summary>
        /// 异步播放音频剪辑
        /// </summary>
        private IEnumerator PlayAudioClipAsync(WWW www)
        {
            yield return www;

            if (string.IsNullOrEmpty(www.error))
            {
                var audioClip = www.GetAudioClip();
                if (audioClip != null)
                {
                    var audioSource = gameObject.GetComponent<AudioSource>();
                    if (audioSource != null)
                    {
                        audioSource.PlayOneShot(audioClip);
                        Debug.Log("[VoiceAudioManager] ✓ 使用简单方式播放音频");
                    }
                }
            }
            else
            {
                Debug.LogError($"[VoiceAudioManager] 加载音频失败: {www.error}");
            }
        }

        /// <summary>
        /// 停止当前播放的音频
        /// </summary>
        public void StopCurrentAudio()
        {
            var audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
                Debug.Log("[VoiceAudioManager] ✓ 停止当前语音音频");
            }
        }

        /// <summary>
        /// 检查是否正在播放音频
        /// </summary>
        public bool IsPlaying()
        {
            var audioSource = gameObject.GetComponent<AudioSource>();
            return audioSource != null && audioSource.isPlaying;
        }
        #endregion

        #region 公共配置方法
        /// <summary>
        /// 检查音频管理器是否已初始化
        /// </summary>
        public bool IsInitialized()
        {
            return _isInitialized && _playerCharacter != null;
        }

        /// <summary>
        /// 刷新玩家角色引用
        /// </summary>
        public void RefreshPlayerCharacter()
        {
            _playerCharacter = CharacterMainControl.Main;
            _isInitialized = (_playerCharacter != null);
        }

        /// <summary>
        /// 重置播放冷却时间
        /// </summary>
        public void ResetCooldown()
        {
            _lastPlayTime = 0f;
        }

        /// <summary>
        /// 检查是否可以播放音频（冷却时间检查）
        /// </summary>
        public bool CanPlayAudio()
        {
            return Time.time - _lastPlayTime >= AUDIO_COOLDOWN;
        }

        /// <summary>
        /// 检查自定义音频是否可用
        /// </summary>
        public bool IsCustomAudioAvailable()
        {
            return !string.IsNullOrEmpty(_customAudioPath) && File.Exists(_customAudioPath);
        }

        /// <summary>
        /// 获取当前音频状态信息
        /// </summary>
        public (bool isCustom, string path, bool isPlaying) GetAudioStatus()
        {
            return (
                IsCustomAudioAvailable(),
                _customAudioPath,
                IsPlaying()
            );
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
            GUILayout.BeginArea(new Rect(10, 200, 350, 200));
            GUILayout.Label("VoiceAudioManager 状态:");
            GUILayout.Label($"初始化状态: {(_isInitialized ? "已初始化" : "未初始化")}");
            GUILayout.Label($"玩家角色: {(_playerCharacter?.name ?? "未找到")}");
            GUILayout.Label($"播放状态: {(IsPlaying() ? "播放中" : "停止")}");
            GUILayout.Label($"剩余冷却: {Mathf.Max(0, AUDIO_COOLDOWN - (Time.time - _lastPlayTime)):F2}s");

            var (isCustom, path, _) = GetAudioStatus();
            GUILayout.Label($"自定义音频: {(isCustom ? "可用" : "不可用")}");
            GUILayout.Label($"音频路径: {path}");

            GUILayout.EndArea();
        }
        #endregion
    }
}