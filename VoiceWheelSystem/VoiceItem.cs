using System;

namespace VoiceWheelSystem
{
    /// <summary>
    /// 语音轮盘中的单个语音项数据结构
    /// </summary>
    [Serializable]
    public class VoiceItem
    {
        // 基础信息
        public string voiceId;           // 唯一标识符
        public string displayName;       // 显示名称（支持emoji和特殊符号）
        public string bubbleText;        // 气泡显示文字

        // 音频设置
        public string audioPath;         // 音频文件路径
        public float volume = 1.0f;      // 音量（0-1）

        // 效果设置
        public float affectRange = 25f;  // NPC影响范围（米）
        public bool isActive = true;     // 是否启用

        // 显示设置
        public string displayIcon;       // 显示图标（emoji或文字）
        public int wheelPosition = -1;   // 在轮盘中的位置（0-7，-1表示未设置）

        /// <summary>
        /// 构造函数
        /// </summary>
        public VoiceItem()
        {
            voiceId = Guid.NewGuid().ToString();
            displayName = "新语音";
            bubbleText = "语音";
            audioPath = "";
            displayIcon = "🎵";
        }

        /// <summary>
        /// 方便的构造函数
        /// </summary>
        public VoiceItem(string id, string name, string bubble, string audio, string icon = "🎵", float range = 25f)
        {
            voiceId = id;
            displayName = name;
            bubbleText = bubble;
            audioPath = audio;
            displayIcon = icon;
            affectRange = range;
            isActive = true;
            wheelPosition = -1;
        }

        /// <summary>
        /// 检查语音是否可用
        /// </summary>
        public bool IsAvailable()
        {
            return isActive && !string.IsNullOrEmpty(audioPath);
        }

        /// <summary>
        /// 获取显示文本（优先使用图标，否则使用名称）
        /// </summary>
        public string GetDisplayText()
        {
            return !string.IsNullOrEmpty(displayIcon) ? displayIcon : displayName;
        }
    }
}