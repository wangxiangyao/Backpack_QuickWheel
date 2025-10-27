using System.Collections.Generic;
using Great_backpack.AttachmentSystem;
using Great_backpack.Localization;

namespace Great_backpack
{
    public static class BackpackModConfig
    {
        // 背包TypeID常量
        public static readonly int[] BackpackTypeIDs = { 36, 37, 38, 39, 40 };

        // 插槽类型定义
        public static readonly Dictionary<string, SlotConfig> SlotConfigs = new Dictionary<string, SlotConfig>
        {
            { "SidePocket_Small", new SlotConfig("侧面小包", "SidePocket_Small", "小型侧面包，可存放小件物品") },
            { "SidePocket_Large", new SlotConfig("侧面大包", "SidePocket_Large", "大型侧面包，可存放较大物品") },
            { "TacticalPouch_Small", new SlotConfig("战术小包", "TacticalPouch_Small", "战术小包，用于存放战术装备") },
            { "TacticalPouch_Large", new SlotConfig("战术大包", "TacticalPouch_Large", "战术大包，用于存放大型战术装备") },
            { "LockBuckle", new SlotConfig("锁扣", "LockBuckle", "锁扣配件，用于固定其他装备") },
            { "ShoulderStrap", new SlotConfig("肩带", "ShoulderStrap", "肩带配件，改善背负舒适度") },
            { "ShoulderPouch", new SlotConfig("肩带包", "ShoulderPouch", "肩带上的小包，方便取用物品") },
            { "AmmoPouch", new SlotConfig("子弹袋", "AmmoPouch", "专用子弹袋，增加弹药携带量") }
        };

        // 背包插槽配置
        public static readonly Dictionary<int, List<string>> BackpackSlotConfigs = new Dictionary<int, List<string>>
        {
            { 36, new List<string> { "SidePocket_Small" } }, // 装饰包
            { 37, new List<string> { "SidePocket_Small", "TacticalPouch_Small", "SidePocket_Small" } }, // 小学生包
            { 38, new List<string> { "SidePocket_Small", "TacticalPouch_Small", "LockBuckle", "ShoulderPouch", "SidePocket_Small" } }, // 旅行包
            { 39, new List<string> { "SidePocket_Small", "TacticalPouch_Small", "TacticalPouch_Large", "LockBuckle", "ShoulderStrap", "ShoulderPouch", "SidePocket_Small" } }, // 生存者背包
            { 40, new List<string> { "SidePocket_Large", "TacticalPouch_Small", "TacticalPouch_Large", "LockBuckle", "LockBuckle", "AmmoPouch", "AmmoPouch", "ShoulderStrap", "ShoulderPouch", "SidePocket_Large" } } // 行军背包MAX
        };

        // 定义统一的插槽类型 - 确保每个都有正确的限制Tag
        public static readonly Dictionary<string, SlotConfig> UnifiedSlotTypes = new Dictionary<string, SlotConfig>
        {
            {
                "Food",
                new SlotConfig("食物", "Food", "存放食物", new List<string> { "Food" })
            },
            {
                "Small",
                new SlotConfig("小物件", "Small", "存放钥匙、注射器等小物件",
                    new List<string> { "key", "SpecialKey", "Injector" })
            },
            {
                "Large",
                new SlotConfig("大物件", "Large", "存放各种大尺寸物品",
                    new List<string> { "key", "SpecialKey", "Injector", "Healing", "Drink", "Food", "Explosive", "MeleeWeapon" })
            },
            {
                "Key",
                new SlotConfig("钥匙", "Key", "专门存放钥匙",
                    new List<string> { "key", "SpecialKey" })
            },
            {
                "Hook",
                new SlotConfig("挂钩", "Hook", "可挂载手雷、钥匙等物品",
                    new List<string> { "key", "SpecialKey", "Explosive", "MeleeWeapon" })
            },
            {
                "Explosive",
                new SlotConfig("手雷", "Explosive", "专门存放手雷",
                    new List<string> { "Explosive" })
            },
            {
                "Magazine",
                new SlotConfig("弹夹", "Magazine", "专门存放弹夹",
                    new List<string> { "Magazine" })
            }
        };

        // 配件物品配置 - 使用统一的插槽类型
        public static readonly List<AttachmentItemConfig> AttachmentItemConfigs = new List<AttachmentItemConfig>
        {
            // === 侧面小包 ===
            new AttachmentItemConfig(
                "NetPocket_Item", "ITEM_NET_POCKET_NAME",
                349100, 0.3f, 200, "SidePocket_Small",
                new List<SlotConfig> { UnifiedSlotTypes["Food"] },
                "Textures.NetPocket_Item.png"
            ){
                Localization = new LocalizationData
                {
                    LanguageMappings = new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            "zh-CN", new Dictionary<string, string>
                            {
                                { "ITEM_NET_POCKET_NAME", "网兜" },
                                { "ITEM_NET_POCKET_NAME_Desc", "网眼侧袋，可存放食物" }
                            }
                        },
                        {
                            "en-US", new Dictionary<string, string>
                            {
                                { "ITEM_NET_POCKET_NAME", "Net Pocket" },
                                { "ITEM_NET_POCKET_NAME_Desc", "Mesh side pocket for storing food" }
                            }
                        }
                    }
                }
            },
            new AttachmentItemConfig(
                "CanteenPocket_Item", "ITEM_CANTEEN_POCKET_NAME",
                349101, 0.4f, 250, "SidePocket_Small",
                new List<SlotConfig> { UnifiedSlotTypes["Food"], UnifiedSlotTypes["Small"] },
                "Textures.CanteenPocket_Item.png"
            ){
                Localization = new LocalizationData
                {
                    LanguageMappings = new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            "zh-CN", new Dictionary<string, string>
                            {
                                { "ITEM_CANTEEN_POCKET_NAME", "水壶袋" },
                                { "ITEM_CANTEEN_POCKET_NAME_Desc", "专门用于存放水壶和食物的侧袋" }
                            }
                        },
                        {
                            "en-US", new Dictionary<string, string>
                            {
                                { "ITEM_CANTEEN_POCKET_NAME", "Canteen Pocket" },
                                { "ITEM_CANTEEN_POCKET_NAME_Desc", "Special pocket for canteens and food" }
                            }
                        }
                    }
                }
            },

            // === 侧面大包 ===
            new AttachmentItemConfig(
                "BianFengStorage_Item", "ITEM_BIANFENG_STORAGE_NAME",
                349120, 0.8f, 500, "SidePocket_Large",
                new List<SlotConfig> { UnifiedSlotTypes["Small"], UnifiedSlotTypes["Large"], UnifiedSlotTypes["Small"] }
            ){
                Localization = new LocalizationData
                {
                    LanguageMappings = new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            "zh-CN", new Dictionary<string, string>
                            {
                                { "ITEM_BIANFENG_STORAGE_NAME", "边锋收纳包" },
                                { "ITEM_BIANFENG_STORAGE_NAME_Desc", "多功能收纳包，提供灵活的存储方案" }
                            }
                        },
                        {
                            "en-US", new Dictionary<string, string>
                            {
                                { "ITEM_BIANFENG_STORAGE_NAME", "BianFeng Storage" },
                                { "ITEM_BIANFENG_STORAGE_NAME_Desc", "Multi-functional storage bag providing flexible storage solutions" }
                            }
                        }
                    }
                }
            },

            // === 战术小包 ===
            new AttachmentItemConfig(
                "SmallKeyPouch_Item", "ITEM_SMALL_KEY_POUCH_NAME",
                349130, 0.2f, 150, "TacticalPouch_Small",
                new List<SlotConfig> { UnifiedSlotTypes["Key"], UnifiedSlotTypes["Key"] }
            ){
                Localization = new LocalizationData
                {
                    LanguageMappings = new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            "zh-CN", new Dictionary<string, string>
                            {
                                { "ITEM_SMALL_KEY_POUCH_NAME", "小钥匙袋" },
                                { "ITEM_SMALL_KEY_POUCH_NAME_Desc", "专门存放钥匙的小袋" }
                            }
                        },
                        {
                            "en-US", new Dictionary<string, string>
                            {
                                { "ITEM_SMALL_KEY_POUCH_NAME", "Small Key Pouch" },
                                { "ITEM_SMALL_KEY_POUCH_NAME_Desc", "Small pouch specifically for keys" }
                            }
                        }
                    }
                }
            },
            new AttachmentItemConfig(
                "TacticalTransparent_Item", "ITEM_TACTICAL_TRANSPARENT_NAME",
                349131, 0.5f, 300, "TacticalPouch_Small",
                new List<SlotConfig> { UnifiedSlotTypes["Small"], UnifiedSlotTypes["Small"], UnifiedSlotTypes["Small"], UnifiedSlotTypes["Small"] }
            ){
                Localization = new LocalizationData
                {
                    LanguageMappings = new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            "zh-CN", new Dictionary<string, string>
                            {
                                { "ITEM_TACTICAL_TRANSPARENT_NAME", "战术小透明" },
                                { "ITEM_TACTICAL_TRANSPARENT_NAME_Desc", "透明战术包，方便查看内容" }
                            }
                        },
                        {
                            "en-US", new Dictionary<string, string>
                            {
                                { "ITEM_TACTICAL_TRANSPARENT_NAME", "Tactical Transparent" },
                                { "ITEM_TACTICAL_TRANSPARENT_NAME_Desc", "Transparent tactical bag for easy content viewing" }
                            }
                        }
                    }
                }
            },

            // === 战术大包 ===
            new AttachmentItemConfig(
                "ToolBox_Item", "ITEM_TOOL_BOX_NAME",
                349140, 1.5f, 800, "TacticalPouch_Large",
                new List<SlotConfig> { UnifiedSlotTypes["Large"], UnifiedSlotTypes["Large"], UnifiedSlotTypes["Large"] }
            ){
                Localization = new LocalizationData
                {
                    LanguageMappings = new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            "zh-CN", new Dictionary<string, string>
                            {
                                { "ITEM_TOOL_BOX_NAME", "工具箱" },
                                { "ITEM_TOOL_BOX_NAME_Desc", "用于存放各种工具的箱子" }
                            }
                        },
                        {
                            "en-US", new Dictionary<string, string>
                            {
                                { "ITEM_TOOL_BOX_NAME", "Tool Box" },
                                { "ITEM_TOOL_BOX_NAME_Desc", "Box for storing various tools" }
                            }
                        }
                    }
                }
            },
            new AttachmentItemConfig(
                "BianFengTactical_Item", "ITEM_BIANFENG_TACTICAL_NAME",
                349141, 1.2f, 1000, "TacticalPouch_Large",
                new List<SlotConfig> { UnifiedSlotTypes["Explosive"], UnifiedSlotTypes["Explosive"], UnifiedSlotTypes["Large"], UnifiedSlotTypes["Explosive"], UnifiedSlotTypes["Explosive"] }
            ){
                Localization = new LocalizationData
                {
                    LanguageMappings = new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            "zh-CN", new Dictionary<string, string>
                            {
                                { "ITEM_BIANFENG_TACTICAL_NAME", "边锋战术包" },
                                { "ITEM_BIANFENG_TACTICAL_NAME_Desc", "专业战术包，提供多种存储方案" }
                            }
                        },
                        {
                            "en-US", new Dictionary<string, string>
                            {
                                { "ITEM_BIANFENG_TACTICAL_NAME", "BianFeng Tactical" },
                                { "ITEM_BIANFENG_TACTICAL_NAME_Desc", "Professional tactical bag with multiple storage solutions" }
                            }
                        }
                    }
                }
            },

            // === 锁扣 ===
            new AttachmentItemConfig(
                "MagneticLock_Item", "ITEM_MAGNETIC_LOCK_NAME",
                349150, 0.1f, 100, "LockBuckle",
                new List<SlotConfig>() // 无插槽
            ){
                Localization = new LocalizationData
                {
                    LanguageMappings = new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            "zh-CN", new Dictionary<string, string>
                            {
                                { "ITEM_MAGNETIC_LOCK_NAME", "磁吸锁扣" },
                                { "ITEM_MAGNETIC_LOCK_NAME_Desc", "磁性锁扣，方便快速开合" }
                            }
                        },
                        {
                            "en-US", new Dictionary<string, string>
                            {
                                { "ITEM_MAGNETIC_LOCK_NAME", "Magnetic Lock" },
                                { "ITEM_MAGNETIC_LOCK_NAME_Desc", "Magnetic lock for quick opening and closing" }
                            }
                        }
                    }
                }
            },
            new AttachmentItemConfig(
                "ToolLock_Item", "ITEM_TOOL_LOCK_NAME",
                349151, 0.3f, 200, "LockBuckle",
                new List<SlotConfig> { UnifiedSlotTypes["Hook"], UnifiedSlotTypes["Hook"] }
            ){
                Localization = new LocalizationData
                {
                    LanguageMappings = new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            "zh-CN", new Dictionary<string, string>
                            {
                                { "ITEM_TOOL_LOCK_NAME", "工具锁扣" },
                                { "ITEM_TOOL_LOCK_NAME_Desc", "带有工具挂载点的锁扣" }
                            }
                        },
                        {
                            "en-US", new Dictionary<string, string>
                            {
                                { "ITEM_TOOL_LOCK_NAME", "Tool Lock" },
                                { "ITEM_TOOL_LOCK_NAME_Desc", "Lock with tool mounting points" }
                            }
                        }
                    }
                }
            },

            // === 肩带 ===
            new AttachmentItemConfig(
                "ZeroGravityStrap_Item", "ITEM_ZERO_GRAVITY_STRAP_NAME",
                349160, 0.4f, 600, "ShoulderStrap",
                new List<SlotConfig>() // 无插槽
            ){
                Localization = new LocalizationData
                {
                    LanguageMappings = new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            "zh-CN", new Dictionary<string, string>
                            {
                                { "ITEM_ZERO_GRAVITY_STRAP_NAME", "边锋零重力肩带" },
                                { "ITEM_ZERO_GRAVITY_STRAP_NAME_Desc", "采用零重力技术的舒适肩带" }
                            }
                        },
                        {
                            "en-US", new Dictionary<string, string>
                            {
                                { "ITEM_ZERO_GRAVITY_STRAP_NAME", "BianFeng Zero Gravity Strap" },
                                { "ITEM_ZERO_GRAVITY_STRAP_NAME_Desc", "Comfortable shoulder strap using zero-gravity technology" }
                            }
                        }
                    }
                }
            },

            // === 肩带包 ===
            new AttachmentItemConfig(
                "PhonePocket_Item", "ITEM_PHONE_POCKET_NAME",
                349170, 0.2f, 150, "ShoulderPouch",
                new List<SlotConfig> { UnifiedSlotTypes["Small"] }
            ){
                Localization = new LocalizationData
                {
                    LanguageMappings = new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            "zh-CN", new Dictionary<string, string>
                            {
                                { "ITEM_PHONE_POCKET_NAME", "手机袋" },
                                { "ITEM_PHONE_POCKET_NAME_Desc", "肩带上的手机袋，方便取用小物件" }
                            }
                        },
                        {
                            "en-US", new Dictionary<string, string>
                            {
                                { "ITEM_PHONE_POCKET_NAME", "Phone Pocket" },
                                { "ITEM_PHONE_POCKET_NAME_Desc", "Shoulder pocket for easy access to small items" }
                            }
                        }
                    }
                }
            },

            // === 子弹袋 ===
            new AttachmentItemConfig(
                "TacticalAmmoPouch_Item", "ITEM_TACTICAL_AMMO_POUCH_NAME",
                349180, 0.7f, 400, "AmmoPouch",
                new List<SlotConfig> { UnifiedSlotTypes["Magazine"], UnifiedSlotTypes["Magazine"], UnifiedSlotTypes["Magazine"], UnifiedSlotTypes["Magazine"] }
            ){
                Localization = new LocalizationData
                {
                    LanguageMappings = new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            "zh-CN", new Dictionary<string, string>
                            {
                                { "ITEM_TACTICAL_AMMO_POUCH_NAME", "战术子弹袋" },
                                { "ITEM_TACTICAL_AMMO_POUCH_NAME_Desc", "专业子弹袋，增加弹药携带效率" }
                            }
                        },
                        {
                            "en-US", new Dictionary<string, string>
                            {
                                { "ITEM_TACTICAL_AMMO_POUCH_NAME", "Tactical Ammo Pouch" },
                                { "ITEM_TACTICAL_AMMO_POUCH_NAME_Desc", "Professional ammo pouch for increased ammunition carrying efficiency" }
                            }
                        }
                    }
                }
            }
        };
    }
}