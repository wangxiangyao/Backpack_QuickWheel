using System;
using System.Collections.Generic;

namespace Great_backpack.ShortcutSystem.Data
{
    /// <summary>
    /// 单个物品的位置记录
    /// </summary>
    [Serializable]
    public class ItemLocation
    {
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

        public ItemLocation() { }

        public ItemLocation(int attachmentSlotIndex, int itemSlotIndex)
        {
            this.attachmentSlotIndex = attachmentSlotIndex;
            this.itemSlotIndex = itemSlotIndex;
        }

        /// <summary>
        /// 检查是否为 null 位置标记
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
    /// 轮盘布局数据 - 用于持久化保存用户在轮盘上的物品排列
    /// 只记录位置信息：配件在背包的槽位索引 + 物品在配件的槽位索引
    /// </summary>
    [Serializable]
    public class WheelLayoutData
    {
        /// <summary>
        /// 所有分类的轮盘布局（使用数组而不是列表，以支持 JsonUtility 序列化）
        /// </summary>
        public CategoryLayout[] categories = System.Array.Empty<CategoryLayout>();

        /// <summary>
        /// 保存时的时间戳（用于调试）
        /// </summary>
        public long savedTimestamp;

        public WheelLayoutData()
        {
            savedTimestamp = System.DateTime.Now.Ticks;
        }
    }
}
