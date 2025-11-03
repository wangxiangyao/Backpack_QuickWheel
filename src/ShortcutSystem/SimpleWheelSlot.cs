using ItemStatsSystem;
using System;

namespace Backpack_QuickWheel.ShortcutSystem
{
    /// <summary>
    /// 简化的轮盘槽位数据结构 - 固定8个槽位设计
    /// 直接用Item引用表示状态，移除冗余的SlotState枚举
    /// </summary>
    [Serializable]
    public struct SimpleWheelSlot
    {
        /// <summary>
        /// 槽位中的物品，null表示空槽位
        /// </summary>
        public Item Item;

        /// <summary>
        /// 槽位在轮盘中的索引（0-7）
        /// </summary>
        public int Index;

        /// <summary>
        /// 检查槽位是否有有效物品
        /// </summary>
        public bool HasItem => Item != null && !Item.IsBeingDestroyed;

        /// <summary>
        /// 检查槽位是否为空
        /// </summary>
        public bool IsEmpty => Item == null;

        /// <summary>
        /// 检查槽位是否有有效物品（兼容旧方法名）
        /// </summary>
        public bool HasValidItem => HasItem;

        /// <summary>
        /// 获取物品显示名称
        /// </summary>
        public string DisplayName => HasItem ? Item.DisplayName : "空";

        /// <summary>
        /// 转换为字符串表示（用于调试）
        /// </summary>
        public override string ToString()
        {
            return $"SimpleWheelSlot[Index={Index}, Item={DisplayName}]";
        }
    }
}