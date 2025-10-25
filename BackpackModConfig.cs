using System.Collections.Generic;
using Great_backpack.AttachmentSystem;

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

        // 配件物品配置
        public static readonly List<AttachmentItemConfig> AttachmentItemConfigs = new List<AttachmentItemConfig>
        {
            // === 侧面小包 ===
            new AttachmentItemConfig(
                "NetPocket_Item", "网兜", "网眼侧袋，可存放各种小物件",
                1001, 0.3f, 200, "SidePocket_Small",
                new List<SlotConfig>
                {
                    new SlotConfig("NetSlot1", "网兜插槽", new List<string> { "TODO: 水", "TODO: 食物", "TODO: 钥匙" })
                }
            ),
            new AttachmentItemConfig(
                "CanteenPocket_Item", "水壶袋", "专门用于存放水壶的侧袋",
                1002, 0.4f, 250, "SidePocket_Small",
                new List<SlotConfig>
                {
                    new SlotConfig("CanteenSlot1", "水壶插槽1", new List<string> { "TODO: 小物件" }),
                    new SlotConfig("CanteenSlot2", "水壶插槽2", new List<string> { "TODO: 小物件" })
                }
            ),

            // === 侧面大包 ===
            new AttachmentItemConfig(
                "BianFengStorage_Item", "边锋收纳包", "多功能收纳包，提供灵活的存储方案",
                1011, 0.8f, 500, "SidePocket_Large",
                new List<SlotConfig>
                {
                    new SlotConfig("StorageSlot1", "通用插槽", new List<string> { "TODO: 所有" }),
                    new SlotConfig("StorageSlot2", "小物件插槽", new List<string> { "TODO: 小物件" }),
                    new SlotConfig("StorageSlot3", "小物件插槽", new List<string> { "TODO: 小物件" })
                }
            ),

            // === 战术小包 ===
            new AttachmentItemConfig(
                "SmallKeyPouch_Item", "小钥匙袋", "专门存放钥匙的小袋",
                1021, 0.2f, 150, "TacticalPouch_Small",
                new List<SlotConfig>
                {
                    new SlotConfig("KeySlot1", "钥匙插槽1", new List<string> { "TODO: 钥匙" }),
                    new SlotConfig("KeySlot2", "钥匙插槽2", new List<string> { "TODO: 钥匙" })
                }
            ),
            new AttachmentItemConfig(
                "TacticalTransparent_Item", "战术小透明", "透明战术包，方便查看内容",
                1022, 0.5f, 300, "TacticalPouch_Small",
                new List<SlotConfig>
                {
                    new SlotConfig("TransparentSlot1", "透明插槽1", new List<string> { "TODO: 小物件" }),
                    new SlotConfig("TransparentSlot2", "透明插槽2", new List<string> { "TODO: 小物件" }),
                    new SlotConfig("TransparentSlot3", "透明插槽3", new List<string> { "TODO: 小物件" }),
                    new SlotConfig("TransparentSlot4", "透明插槽4", new List<string> { "TODO: 小物件" }),
                    new SlotConfig("TransparentSlot5", "透明插槽5", new List<string> { "TODO: 小物件" })
                }
            ),

            // === 战术大包 ===
            new AttachmentItemConfig(
                "ToolBox_Item", "工具箱", "用于存放各种工具的箱子",
                1031, 1.5f, 800, "TacticalPouch_Large",
                new List<SlotConfig>
                {
                    new SlotConfig("ToolSlot1", "工具插槽1", new List<string> { "TODO: 锤子", "TODO: 铲子", "TODO: 手雷" }),
                    new SlotConfig("ToolSlot2", "工具插槽2", new List<string> { "TODO: 锤子", "TODO: 铲子", "TODO: 手雷" }),
                    new SlotConfig("ToolSlot3", "工具插槽3", new List<string> { "TODO: 锤子", "TODO: 铲子", "TODO: 手雷" })
                }
            ),
            new AttachmentItemConfig(
                "BianFengTactical_Item", "边锋战术包", "专业战术包，提供多种存储方案",
                1032, 1.2f, 1000, "TacticalPouch_Large",
                new List<SlotConfig>
                {
                    new SlotConfig("GrenadeSlot1", "手雷插槽1", new List<string> { "TODO: 手雷" }),
                    new SlotConfig("GrenadeSlot2", "手雷插槽2", new List<string> { "TODO: 手雷" }),
                    new SlotConfig("GrenadeSlot3", "手雷插槽3", new List<string> { "TODO: 手雷" }),
                    new SlotConfig("MixedSlot4", "混合插槽4", new List<string> { "TODO: 小物件", "TODO: 手雷" }),
                    new SlotConfig("MixedSlot5", "混合插槽5", new List<string> { "TODO: 小物件", "TODO: 手雷" })
                }
            ),

            // === 锁扣 ===
            new AttachmentItemConfig(
                "MagneticLock_Item", "磁吸锁扣", "磁性锁扣，方便快速开合",
                1041, 0.1f, 100, "LockBuckle",
                new List<SlotConfig>() // 无插槽
            ),
            new AttachmentItemConfig(
                "ToolLock_Item", "工具锁扣", "带有工具挂载点的锁扣",
                1042, 0.3f, 200, "LockBuckle",
                new List<SlotConfig>
                {
                    new SlotConfig("ToolLockSlot1", "工具锁插槽1", new List<string> { "TODO: 手雷", "TODO: 钥匙" }),
                    new SlotConfig("ToolLockSlot2", "工具锁插槽2", new List<string> { "TODO: 手雷", "TODO: 钥匙" })
                }
            ),

            // === 肩带 ===
            new AttachmentItemConfig(
                "ZeroGravityStrap_Item", "边锋零重力肩带", "采用零重力技术的舒适肩带",
                1051, 0.4f, 600, "ShoulderStrap",
                new List<SlotConfig>() // 无插槽
            ),

            // === 肩带包 ===
            new AttachmentItemConfig(
                "PhonePocket_Item", "手机袋", "肩带上的手机袋，方便取用",
                1061, 0.2f, 150, "ShoulderPouch",
                new List<SlotConfig>
                {
                    new SlotConfig("PhoneSlot", "手机插槽", new List<string> { "TODO: 手电", "TODO: 打火机", "TODO: 香烟" })
                }
            ),

            // === 子弹袋 ===
            new AttachmentItemConfig(
                "TacticalAmmoPouch_Item", "战术子弹袋", "专业子弹袋，增加弹药携带效率",
                1071, 0.7f, 400, "AmmoPouch",
                new List<SlotConfig>
                {
                    new SlotConfig("AmmoSlot1", "弹夹插槽1", new List<string> { "TODO: 弹夹" }),
                    new SlotConfig("AmmoSlot2", "弹夹插槽2", new List<string> { "TODO: 弹夹" }),
                    new SlotConfig("AmmoSlot3", "弹夹插槽3", new List<string> { "TODO: 弹夹" }),
                    new SlotConfig("AmmoSlot4", "弹夹插槽4", new List<string> { "TODO: 弹夹" })
                }
            )
        };
    }
}