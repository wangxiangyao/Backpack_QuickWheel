using System.Collections.Generic;
using Backpack_QuickWheel.AttachmentSystem;
using Backpack_QuickWheel.Localization;

namespace Backpack_QuickWheel
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
            { 38, new List<string> { "SidePocket_Small", "TacticalPouch_Small", "LockBuckle" /*, "ShoulderPouch"*/, "SidePocket_Small" } }, // 旅行包 - 暂时注释掉ShoulderPouch
            { 39, new List<string> { "SidePocket_Small", "TacticalPouch_Small", "TacticalPouch_Large", "LockBuckle" /*, "ShoulderStrap", "ShoulderPouch"*/, "SidePocket_Small" } }, // 生存者背包 - 暂时注释掉ShoulderStrap和ShoulderPouch
            { 40, new List<string> { "SidePocket_Large", "TacticalPouch_Small", "TacticalPouch_Large", "LockBuckle", "LockBuckle", "AmmoPouch", "AmmoPouch" /*, "ShoulderStrap", "ShoulderPouch"*/, "SidePocket_Large" } } // 行军背包MAX - 暂时注释掉ShoulderStrap和ShoulderPouch
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
                "NetPocket_Item", "ITEM_NET_POCKET_NAME", "",
                349100, 0.3f, 120, "SidePocket_Small",
                new List<SlotConfig> { UnifiedSlotTypes["Food"] },
                "Textures.NetPocket_Item.png"
            ){
                Quality = 1, // 调整为品质1
                DisplayQuality = ItemStatsSystem.DisplayQuality.White,
                Localization = new LocalizationData
                {
                    LanguageMappings = new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            "zh-CN", new Dictionary<string, string>
                            {
                                { "ITEM_NET_POCKET_NAME", "网兜" },
                                { "ITEM_NET_POCKET_NAME_Desc", "装瓶水？还是一个萝卜？反正食物就行～" }
                            }
                        },
                        {
                            "en-US", new Dictionary<string, string>
                            {
                                { "ITEM_NET_POCKET_NAME", "Net Pocket" },
                                { "ITEM_NET_POCKET_NAME_Desc", "A bottle of water? A carrot? Anything food works~" }
                            }
                        }
                    }
                }
            },
            new AttachmentItemConfig(
                "CanteenPocket_Item", "ITEM_CANTEEN_POCKET_NAME", "",
                349101, 0.4f, 450, "SidePocket_Small",
                new List<SlotConfig> { UnifiedSlotTypes["Food"], UnifiedSlotTypes["Small"] },
                "Textures.CanteenPocket_Item.png"
            ){
                Quality = 2, // 调整为品质2
                DisplayQuality = ItemStatsSystem.DisplayQuality.Green,
                Localization = new LocalizationData
                {
                    LanguageMappings = new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            "zh-CN", new Dictionary<string, string>
                            {
                                { "ITEM_CANTEEN_POCKET_NAME", "水壶袋" },
                                { "ITEM_CANTEEN_POCKET_NAME_Desc", "能放点吃喝，还能放个钥匙、针剂，就没地儿了。。" }
                            }
                        },
                        {
                            "en-US", new Dictionary<string, string>
                            {
                                { "ITEM_CANTEEN_POCKET_NAME", "Canteen Pocket" },
                                { "ITEM_CANTEEN_POCKET_NAME_Desc", "Room for some snacks and drinks, maybe a key and syringe... but then it's full." }
                            }
                        }
                    }
                }
            },

            // === 侧面大包 ===
            new AttachmentItemConfig(
                "GagaStorage_Item", "ITEM_GAGA_STORAGE_NAME", "",
                349120, 0.7f, 6200, "SidePocket_Large",
                new List<SlotConfig> { UnifiedSlotTypes["Small"], UnifiedSlotTypes["Large"], UnifiedSlotTypes["Large"] },
                "Textures.GagaStorage_Item.png"
            ){
                Quality = 4, // 调整为品质4
                DisplayQuality = ItemStatsSystem.DisplayQuality.Purple,
                Localization = new LocalizationData
                {
                    LanguageMappings = new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            "zh-CN", new Dictionary<string, string>
                            {
                                { "ITEM_GAGA_STORAGE_NAME", "嘎嘎收纳包" },
                                { "ITEM_GAGA_STORAGE_NAME_Desc", "诺亚方舟级收纳！大小物件都能装，这才是真正的整理大师" }
                            }
                        },
                        {
                            "en-US", new Dictionary<string, string>
                            {
                                { "ITEM_GAGA_STORAGE_NAME", "Gaga Storage" },
                                { "ITEM_GAGA_STORAGE_NAME_Desc", "Noah's Ark of storage! Everything finds its place. The master organizer!" }
                            }
                        }
                    }
                }
            },

            // === 战术小包 ===
            new AttachmentItemConfig(
                "SmallKeyPouch_Item", "ITEM_SMALL_KEY_POUCH_NAME", "",
                349130, 0.2f, 95, "TacticalPouch_Small",
                new List<SlotConfig> { UnifiedSlotTypes["Key"], UnifiedSlotTypes["Key"] },
                "Textures.SmallKeyPouch_Item.png"
            ){
                Quality = 1, // 调整为品质1
                DisplayQuality = ItemStatsSystem.DisplayQuality.White,
                Localization = new LocalizationData
                {
                    LanguageMappings = new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            "zh-CN", new Dictionary<string, string>
                            {
                                { "ITEM_SMALL_KEY_POUCH_NAME", "小钥匙袋" },
                                { "ITEM_SMALL_KEY_POUCH_NAME_Desc", "钥匙的家，装满了就都堵门口吧" }
                            }
                        },
                        {
                            "en-US", new Dictionary<string, string>
                            {
                                { "ITEM_SMALL_KEY_POUCH_NAME", "Small Key Pouch" },
                                { "ITEM_SMALL_KEY_POUCH_NAME_Desc", "Keys' home. Fill it up and you'll never lose one!" }
                            }
                        }
                    }
                }
            },
            new AttachmentItemConfig(
                "TacticalTransparent_Item", "ITEM_TACTICAL_TRANSPARENT_NAME", "",
                349131, 0.5f, 1500, "TacticalPouch_Small",
                new List<SlotConfig> { UnifiedSlotTypes["Small"], UnifiedSlotTypes["Small"], UnifiedSlotTypes["Small"], UnifiedSlotTypes["Small"] },
                "Textures.TacticalTransparent_Item.png"
            ){
                Quality = 3, // 保持品质3
                DisplayQuality = ItemStatsSystem.DisplayQuality.Blue,
                Localization = new LocalizationData
                {
                    LanguageMappings = new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            "zh-CN", new Dictionary<string, string>
                            {
                                { "ITEM_TACTICAL_TRANSPARENT_NAME", "战术小透明" },
                                { "ITEM_TACTICAL_TRANSPARENT_NAME_Desc", "透明材质，小物件一目了然，专业人士的秘密武器" }
                            }
                        },
                        {
                            "en-US", new Dictionary<string, string>
                            {
                                { "ITEM_TACTICAL_TRANSPARENT_NAME", "Tactical Transparent" },
                                { "ITEM_TACTICAL_TRANSPARENT_NAME_Desc", "Crystal clear visibility. Perfect for organizing those small essentials at a glance!" }
                            }
                        }
                    }
                }
            },

            // === 战术大包 ===
            new AttachmentItemConfig(
                "ToolBox_Item", "ITEM_TOOL_BOX_NAME", "",
                349140, 1.2f, 2200, "TacticalPouch_Large",
                new List<SlotConfig> { UnifiedSlotTypes["Large"], UnifiedSlotTypes["Large"], UnifiedSlotTypes["Large"] },
                "Textures.ToolBox_Item.png"
            ){
                Quality = 3, // 调整为品质3
                DisplayQuality = ItemStatsSystem.DisplayQuality.Blue,
                Localization = new LocalizationData
                {
                    LanguageMappings = new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            "zh-CN", new Dictionary<string, string>
                            {
                                { "ITEM_TOOL_BOX_NAME", "工具箱" },
                                { "ITEM_TOOL_BOX_NAME_Desc", "行动必备！医疗包、水、粮食...这箱子就是你的移动仓库" }
                            }
                        },
                        {
                            "en-US", new Dictionary<string, string>
                            {
                                { "ITEM_TOOL_BOX_NAME", "Tool Box" },
                                { "ITEM_TOOL_BOX_NAME_Desc", "Your mobile supply depot! Medical kits, water, rations... pack it all in!" }
                            }
                        }
                    }
                }
            },
            new AttachmentItemConfig(
                "GagaTactical_Item", "ITEM_GAGA_TACTICAL_NAME", "",
                349141, 1.0f, 8900, "TacticalPouch_Large",
                new List<SlotConfig> { UnifiedSlotTypes["Explosive"], UnifiedSlotTypes["Large"], UnifiedSlotTypes["Large"], UnifiedSlotTypes["Large"], UnifiedSlotTypes["Explosive"] },
                "Textures.GagaTactical_Item.png"
            ){
                Quality = 5, // 调整为品质5
                DisplayQuality = ItemStatsSystem.DisplayQuality.Orange,
                Localization = new LocalizationData
                {
                    LanguageMappings = new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            "zh-CN", new Dictionary<string, string>
                            {
                                { "ITEM_GAGA_TACTICAL_NAME", "嘎嘎战术包" },
                                { "ITEM_GAGA_TACTICAL_NAME_Desc", "终极之选！手雷、装备、补给...最专业的战术配置尽在其中" }
                            }
                        },
                        {
                            "en-US", new Dictionary<string, string>
                            {
                                { "ITEM_GAGA_TACTICAL_NAME", "Gaga Tactical" },
                                { "ITEM_GAGA_TACTICAL_NAME_Desc", "The ultimate choice! Grenades, gear, supplies... pure tactical perfection!" }
                            }
                        }
                    }
                }
            },

            // === 锁扣 ===
            // 磁吸锁扣 - 保留以兼容旧存档，但不在背包中显示插槽
            new AttachmentItemConfig(
                "MagneticLock_Item", "ITEM_MAGNETIC_LOCK_NAME", "",
                349150, 0.08f, 1450, "LockBuckle",
                new List<SlotConfig>() // 无插槽 - 禁用显示
            ){
                Quality = 1,
                DisplayQuality = ItemStatsSystem.DisplayQuality.Purple,
                Localization = new LocalizationData
                {
                    LanguageMappings = new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            "zh-CN", new Dictionary<string, string>
                            {
                                { "ITEM_MAGNETIC_LOCK_NAME", "磁吸锁扣" },
                                { "ITEM_MAGNETIC_LOCK_NAME_Desc", "吸一下就开，放一下就锁。快速又安全的背包好搭档" }
                            }
                        },
                        {
                            "en-US", new Dictionary<string, string>
                            {
                                { "ITEM_MAGNETIC_LOCK_NAME", "Magnetic Lock" },
                                { "ITEM_MAGNETIC_LOCK_NAME_Desc", "Snap and go! A quick and secure companion for your pack." }
                            }
                        }
                    }
                }
            },
            new AttachmentItemConfig(
                "GagaTacticalBelt_Item", "ITEM_GAGA_TACTICAL_BELT_NAME", "",
                349151, 0.35f, 4200, "LockBuckle",
                new List<SlotConfig> { UnifiedSlotTypes["Hook"], UnifiedSlotTypes["Hook"] },
                "Textures.GagaTacticalBelt_Item.png"
            ){
                Quality = 4, // 调整为品质4
                DisplayQuality = ItemStatsSystem.DisplayQuality.Purple,
                Localization = new LocalizationData
                {
                    LanguageMappings = new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            "zh-CN", new Dictionary<string, string>
                            {
                                { "ITEM_GAGA_TACTICAL_BELT_NAME", "嘎嘎战术腰带" },
                                { "ITEM_GAGA_TACTICAL_BELT_NAME_Desc", "专业级战术腰带，两个挂钩稳稳地固定你的武器和装备。行动中的好搭档" }
                            }
                        },
                        {
                            "en-US", new Dictionary<string, string>
                            {
                                { "ITEM_GAGA_TACTICAL_BELT_NAME", "Gaga Tactical Belt" },
                                { "ITEM_GAGA_TACTICAL_BELT_NAME_Desc", "Professional-grade tactical belt with dual hooks to secure your weapons and gear. The perfect companion for action!" }
                            }
                        }
                    }
                }
            },

            // === 肩带 ===
            // 暂时注释掉零重力肩带
            /*
            new AttachmentItemConfig(
                "ZeroGravityStrap_Item", "ITEM_ZERO_GRAVITY_STRAP_NAME", "",
                349160, 0.4f, 5680, "ShoulderStrap",
                new List<SlotConfig>(), // 无插槽
                "Textures.ZeroGravityStrap_Item.png"
            ){
                Quality = 5, // 恢复原始品质：Orange
                DisplayQuality = ItemStatsSystem.DisplayQuality.Orange,
                Localization = new LocalizationData
                {
                    LanguageMappings = new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            "zh-CN", new Dictionary<string, string>
                            {
                                { "ITEM_ZERO_GRAVITY_STRAP_NAME", "嘎嘎零重力肩带" },
                                { "ITEM_ZERO_GRAVITY_STRAP_NAME_Desc", "仿佛背的不是物资，而是空气。你的肩膀会感谢你" }
                            }
                        },
                        {
                            "en-US", new Dictionary<string, string>
                            {
                                { "ITEM_ZERO_GRAVITY_STRAP_NAME", "Gaga Zero Gravity Strap" },
                                { "ITEM_ZERO_GRAVITY_STRAP_NAME_Desc", "Feels like carrying air, not supplies. Your shoulders will thank you!" }
                            }
                        }
                    }
                }
            },
            */

            // === 肩带包 ===
            // 暂时注释掉手机袋
            /*
            new AttachmentItemConfig(
                "PhonePocket_Item", "ITEM_PHONE_POCKET_NAME", "",
                349170, 0.18f, 4950, "ShoulderPouch",
                new List<SlotConfig> { UnifiedSlotTypes["Small"] },
                "Textures.PhonePocket_Item.png"
            ){
                Quality = 5, // 恢复原始品质：Orange
                DisplayQuality = ItemStatsSystem.DisplayQuality.Orange,
                Localization = new LocalizationData
                {
                    LanguageMappings = new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            "zh-CN", new Dictionary<string, string>
                            {
                                { "ITEM_PHONE_POCKET_NAME", "手机袋" },
                                { "ITEM_PHONE_POCKET_NAME_Desc", "名叫手机袋，其实啥小东西都能装。钥匙、针剂、糖果...顺手一掏" }
                            }
                        },
                        {
                            "en-US", new Dictionary<string, string>
                            {
                                { "ITEM_PHONE_POCKET_NAME", "Phone Pocket" },
                                { "ITEM_PHONE_POCKET_NAME_Desc", "Called a phone pocket but holds everything small. Keys, syringes, candy... grab and go!" }
                            }
                        }
                    }
                }
            },
            */

            // === 子弹袋 ===
            new AttachmentItemConfig(
                "TacticalAmmoPouch_Item", "ITEM_TACTICAL_AMMO_POUCH_NAME", "",
                349180, 0.6f, 8500, "AmmoPouch",
                new List<SlotConfig> { UnifiedSlotTypes["Magazine"], UnifiedSlotTypes["Magazine"], UnifiedSlotTypes["Magazine"] },
                "Textures.TacticalAmmoPouch_Item.png"
            ){
                Quality = 5, // 调整为品质5
                DisplayQuality = ItemStatsSystem.DisplayQuality.Orange,
                Localization = new LocalizationData
                {
                    LanguageMappings = new Dictionary<string, Dictionary<string, string>>
                    {
                        {
                            "zh-CN", new Dictionary<string, string>
                            {
                                { "ITEM_TACTICAL_AMMO_POUCH_NAME", "战术子弹袋" },
                                { "ITEM_TACTICAL_AMMO_POUCH_NAME_Desc", "弹匣杀手！三个弹夹齐排队。火力全开从它开始" }
                            }
                        },
                        {
                            "en-US", new Dictionary<string, string>
                            {
                                { "ITEM_TACTICAL_AMMO_POUCH_NAME", "Tactical Ammo Pouch" },
                                { "ITEM_TACTICAL_AMMO_POUCH_NAME_Desc", "Magazine heaven! Three mags ready to roll. Non-stop firepower begins here!" }
                            }
                        }
                    }
                }
            }
        };
    }
}