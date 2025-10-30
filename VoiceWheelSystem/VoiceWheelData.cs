using System;
using System.Collections.Generic;

namespace VoiceWheelSystem
{
    /// <summary>
    /// 语音轮盘配置数据
    /// </summary>
    [Serializable]
    public class VoiceWheelData
    {
        public List<VoiceItem> voiceItems = new List<VoiceItem>();
        public int selectedVoiceIndex = 0;  // 当前选中的语音索引
        public long savedTimestamp = 0;
        public string dataVersion = "1.0";

        /// <summary>
        /// 获取当前选中的语音
        /// </summary>
        public VoiceItem GetSelectedVoice()
        {
            if (selectedVoiceIndex >= 0 && selectedVoiceIndex < voiceItems.Count)
            {
                return voiceItems[selectedVoiceIndex];
            }
            return null;
        }

        /// <summary>
        /// 根据轮盘位置获取语音
        /// </summary>
        public VoiceItem GetVoiceAtWheelPosition(int position)
        {
            foreach (var voice in voiceItems)
            {
                if (voice.wheelPosition == position && voice.IsAvailable())
                {
                    return voice;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取所有轮盘位置上的语音（按位置排序）
        /// </summary>
        public List<VoiceItem> GetWheelVoices()
        {
            var wheelVoices = new List<VoiceItem>();
            for (int i = 0; i < 8; i++)
            {
                var voice = GetVoiceAtWheelPosition(i);
                if (voice != null)
                {
                    wheelVoices.Add(voice);
                }
            }
            return wheelVoices;
        }

        /// <summary>
        /// 添加语音到轮盘的指定位置
        /// </summary>
        public bool AddVoiceToWheel(VoiceItem voice, int position)
        {
            if (position < 0 || position >= 8) return false;

            // 检查位置是否已被占用
            foreach (var v in voiceItems)
            {
                if (v.wheelPosition == position)
                {
                    v.wheelPosition = -1; // 清除原位置
                }
            }

            voice.wheelPosition = position;
            if (!voiceItems.Contains(voice))
            {
                voiceItems.Add(voice);
            }
            return true;
        }

        /// <summary>
        /// 验证数据完整性
        /// </summary>
        public bool ValidateData()
        {
            // 检查必要字段
            foreach (var voice in voiceItems)
            {
                if (string.IsNullOrEmpty(voice.voiceId) ||
                    string.IsNullOrEmpty(voice.displayName))
                {
                    return false;
                }
            }

            // 检查轮盘位置唯一性
            var usedPositions = new HashSet<int>();
            foreach (var voice in voiceItems)
            {
                if (voice.wheelPosition >= 0 && voice.wheelPosition < 8)
                {
                    if (usedPositions.Contains(voice.wheelPosition))
                    {
                        return false; // 位置冲突
                    }
                    usedPositions.Add(voice.wheelPosition);
                }
            }

            return true;
        }

        /// <summary>
        /// 初始化默认语音数据
        /// </summary>
        public static VoiceWheelData CreateDefault()
        {
            var data = new VoiceWheelData();

            // 添加默认的"嘎"语音
            var gaVoice = new VoiceItem(
                "voice_default_ga",
                "嘎",
                "嘎",
                "", // 路径稍后设置
                "嘎", // 直接显示文字
                25f
            );
            gaVoice.wheelPosition = 0; // 默认放在第一个位置

            data.voiceItems.Add(gaVoice);
            data.selectedVoiceIndex = 0;
            data.savedTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();

            return data;
        }
    }
}