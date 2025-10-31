using System.Collections.Generic;
using UnityEngine;

namespace Backpack_QuickWheel.ShortcutSystem
{
    public static class ShortcutConfig
    {
        public static Dictionary<ItemCategory, KeyCode> DefaultKeybindings = new Dictionary<ItemCategory, KeyCode>
        {
            { ItemCategory.Medical, KeyCode.Alpha3 },
            { ItemCategory.Stim, KeyCode.Alpha4 },
            { ItemCategory.Food, KeyCode.Q },
            { ItemCategory.Explosive, KeyCode.G }
        };

        // 支持自定义键位配置
        public static Dictionary<ItemCategory, KeyCode> CurrentKeybindings { get; set; }

        static ShortcutConfig()
        {
            // 加载保存的配置或使用默认值
            CurrentKeybindings = new Dictionary<ItemCategory, KeyCode>(DefaultKeybindings);
        }
    }
}