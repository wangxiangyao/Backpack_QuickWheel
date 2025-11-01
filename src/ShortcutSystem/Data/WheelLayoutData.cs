using System;
using System.Collections.Generic;

namespace Backpack_QuickWheel.ShortcutSystem.Data
{
    /// <summary>
    /// 🆕 新架构：单个物品的位置记录（支持WheelSlot状态）
    /// </summary>
    [Serializable]
    public class ItemLocation
    {
        // 旧字段（兼容性）
        /// <summary>
        /// 配件在背包中的槽位索引
        /// 使用 -1 表示 null（因为数组/列表不能直接存储 null 结构体的引用）
        /// </summary>
        public int attachmentSlotIndex;

        /// <summary>
        /// 物品在配件内的槽位索引
        /// 使用 -1 表示 null
        /// </summary>
        public int itemSlotIndex;

        // 🆕 新字段（WheelSlot支持）
        /// <summary>
        /// 物品唯一ID（用于精确匹配物品实例）
        /// </summary>
        public int ItemID;

        /// <summary>
        /// 物品类型ID（用于备用匹配）
        /// </summary>
        public int TypeID;

        /// <summary>
        /// 物品自定义名称（用于显示）
        /// </summary>
        public string CustomName;

        /// <summary>
        /// 在轮盘中的位置索引
        /// </summary>
        public int Position;

        /// <summary>
        /// 是否为空格子
        /// </summary>
        public bool IsEmpty;

        /// <summary>
        /// 格子状态（Empty, Occupied, Cleared, Removed）
        /// </summary>
        public string State;

        /// <summary>
        /// 创建时间戳
        /// </summary>
        public long Timestamp;

        /// <summary>
        /// 是否被用户手动清空
        /// </summary>
        public bool IsUserCleared;

        // 兼容性构造函数
        public ItemLocation() { }

        public ItemLocation(int attachmentSlotIndex, int itemSlotIndex)
        {
            this.attachmentSlotIndex = attachmentSlotIndex;
            this.itemSlotIndex = itemSlotIndex;
        }

        /// <summary>
        /// 检查是否为 null 位置标记（兼容旧版本）
        /// </summary>
        public bool IsNull()
        {
            return attachmentSlotIndex == -1 && itemSlotIndex == -1;
        }
    }

    /// <summary>
    /// 单个分类的轮盘布局
    /// </summary>
    [Serializable]
    public class CategoryLayout
    {
        /// <summary>
        /// 物品分类名称
        /// </summary>
        public string categoryName;

        /// <summary>
        /// 该分类的物品位置数组（包含 null 位置标记表示空位）
        /// 使用数组而不是 List，因为 JsonUtility 支持数组序列化
        /// </summary>
        public ItemLocation[] itemLocations = System.Array.Empty<ItemLocation>();

        public CategoryLayout() { }

        public CategoryLayout(string categoryName)
        {
            this.categoryName = categoryName;
        }

        /// <summary>
        /// 从列表转换为数组（在保存前调用）
        /// </summary>
        public void ConvertListToArray(List<ItemLocation> locationList)
        {
            itemLocations = locationList.ToArray();
        }
    }

    /// <summary>
    /// 🆕 新架构：轮盘布局数据 - 用于持久化保存用户在轮盘上的物品排列
    /// 支持版本控制和新的WheelSlot状态
    /// </summary>
    [Serializable]
    public class WheelLayoutData
    {
        /// <summary>
        /// 所有分类的轮盘布局（使用数组而不是列表，以支持 JsonUtility 序列化）
        /// </summary>
        public CategoryLayout[] categories = System.Array.Empty<CategoryLayout>();

        // 旧字段（兼容性）
        /// <summary>
        /// 保存时的时间戳（用于调试）
        /// </summary>
        public long savedTimestamp;

        // 🆕 新字段
        /// <summary>
        /// 数据版本号
        /// </summary>
        public string Version = "1.0";

        /// <summary>
        /// 保存时间戳（Unix时间戳）
        /// </summary>
        public long SaveTime;

        /// <summary>
        /// 所有分类的轮盘布局（新格式，使用List以支持动态更新）
        /// </summary>
        public List<CategoryLayout> Categories = new List<CategoryLayout>();

        public WheelLayoutData()
        {
            savedTimestamp = System.DateTime.Now.Ticks;
            SaveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
