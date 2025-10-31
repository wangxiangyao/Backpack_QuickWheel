using UnityEngine;
using System.Collections;

namespace Backpack_QuickWheel.VoiceWheelSystem
{
    /// <summary>
    /// 语音NPC交互管理器
    /// 复用游戏现有的AI声音系统 (AIMainBrain.MakeSound)
    /// 让语音播放时能够影响周围的NPC敌人
    /// </summary>
    public class VoiceNPCManager : MonoBehaviour
    {
        #region 配置常量
        [Header("声音影响设置")]
        private const float DEFAULT_VOICE_RANGE = 25f;    // 默认语音影响范围
        private const float MIN_VOICE_RANGE = 10f;        // 最小影响范围
        private const float MAX_VOICE_RANGE = 50f;        // 最大影响范围

        [Header("性能优化")]
        private const float VOICE_COOLDOWN = 0.5f;       // 语音触发冷却时间
        private float _lastVoiceTime = 0f;
        #endregion

        #region 组件引用
        private CharacterMainControl _playerCharacter;
        #endregion

        #region Unity生命周期
        private void Start()
        {
            InitializeNPCManager();
        }

        private void Update()
        {
            // 确保玩家角色引用有效
            if (_playerCharacter == null)
            {
                RefreshPlayerCharacter();
            }
        }
        #endregion

        #region 初始化
        private void InitializeNPCManager()
        {
            RefreshPlayerCharacter();
            Debug.Log("[VoiceNPCManager] 初始化完成，使用AI声音系统");
        }
        #endregion

        #region NPC影响接口
        /// <summary>
        /// 影响范围内的NPC（通过AI声音系统）
        /// </summary>
        /// <param name="affectRange">影响范围</param>
        public void AffectNearbyNPCs(float affectRange = DEFAULT_VOICE_RANGE)
        {
            // 性能优化：限制触发频率
            if (Time.time - _lastVoiceTime < VOICE_COOLDOWN)
            {
                return;
            }

            // 验证玩家角色
            if (_playerCharacter == null)
            {
                Debug.LogWarning("[VoiceNPCManager] 无法找到玩家角色");
                return;
            }

            // 限制影响范围
            var range = Mathf.Clamp(affectRange, MIN_VOICE_RANGE, MAX_VOICE_RANGE);
            _lastVoiceTime = Time.time;

            // 使用游戏的AI声音系统
            MakeVoiceSound(range);

            Debug.Log($"[VoiceNPCManager] 发出语音声音，影响范围 {range:F1}米");
        }

        /// <summary>
        /// 创建AI声音事件，让范围内的敌人听到
        /// 基于ItemAgent_Gun.cs中的枪声实现
        /// </summary>
        private void MakeVoiceSound(float range)
        {
            try
            {
                // 创建AISound结构体（参考枪声实现）
                var voiceSound = new AISound
                {
                    fromCharacter = _playerCharacter,           // 发出声音的角色（玩家）
                    fromObject = _playerCharacter.gameObject,   // 发出声音的对象
                    pos = _playerCharacter.transform.position, // 声音位置（玩家位置）
                    soundType = SoundTypes.unknowNoise,        // 声音类型：未知噪音
                    fromTeam = _playerCharacter.Team,          // 玩家队伍
                    radius = range                             // 影响范围
                };

                // 调用游戏的AI声音系统
                AIMainBrain.MakeSound(voiceSound);

                Debug.Log("[VoiceNPCManager] ✓ 成功创建AI声音事件");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[VoiceNPCManager] 创建AI声音失败: {e.Message}");
            }
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 刷新玩家角色引用
        /// </summary>
        public void RefreshPlayerCharacter()
        {
            // 使用CharacterMainControl.Main获取主角色
            _playerCharacter = CharacterMainControl.Main;

            if (_playerCharacter != null)
            {
                Debug.Log($"[VoiceNPCManager] 找到玩家角色: {_playerCharacter.name}");
            }
        }

        /// <summary>
        /// 验证影响范围是否有效
        /// </summary>
        public bool IsValidRange(float range)
        {
            return range >= MIN_VOICE_RANGE && range <= MAX_VOICE_RANGE;
        }

        /// <summary>
        /// 获取范围限制信息
        /// </summary>
        public (float min, float max, float @default) GetRangeLimits()
        {
            return (MIN_VOICE_RANGE, MAX_VOICE_RANGE, DEFAULT_VOICE_RANGE);
        }

        /// <summary>
        /// 检查是否可以发出声音（冷却时间检查）
        /// </summary>
        public bool CanMakeSound()
        {
            return Time.time - _lastVoiceTime >= VOICE_COOLDOWN;
        }

        /// <summary>
        /// 重置冷却时间
        /// </summary>
        public void ResetCooldown()
        {
            _lastVoiceTime = 0f;
        }
        #endregion

        #region 调试和可视化
        private void OnDrawGizmosSelected()
        {
            if (_playerCharacter != null)
            {
                // 绘制默认语音影响范围（Scene视图中）
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_playerCharacter.transform.position, DEFAULT_VOICE_RANGE);

                // 绘制最大范围（半透明）
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
                Gizmos.DrawWireSphere(_playerCharacter.transform.position, MAX_VOICE_RANGE);
            }
        }

        /// <summary>
        /// 调试信息
        /// </summary>
        [Header("调试信息")]
        public bool showDebugInfo = false;

        private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUI.skin.label.fontSize = 16;
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("VoiceNPCManager 状态:");
            GUILayout.Label($"玩家角色: {_playerCharacter?.name ?? "未找到"}");
            GUILayout.Label($"队伍: {(_playerCharacter?.Team.ToString() ?? "未知")}");
            GUILayout.Label($"冷却时间: {VOICE_COOLDOWN:F1}s");
            GUILayout.Label($"剩余冷却: {Mathf.Max(0, VOICE_COOLDOWN - (Time.time - _lastVoiceTime)):F1}s");
            GUILayout.EndArea();
        }
        #endregion
    }
}