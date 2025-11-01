using ItemStatsSystem;
using System;

namespace Backpack_QuickWheel.ShortcutSystem
{
    /// <summary>
    /// 轮盘格子数据结构
    /// 用于替代使用null表示空格子的混乱设计
    /// </summary>
    [Serializable]
    public struct WheelSlot
    {
        /// <summary>
        /// 格子状态
        /// </summary>
        public SlotState State { get; private set; }

        /// <summary>
        /// 格子中的物品（当State为Occupied时有效）
        /// </summary>
        public Item Item { get; private set; }

        /// <summary>
        /// 格子在轮盘中的原始位置索引
        /// 用于布局持久化
        /// </summary>
        public int OriginalIndex { get; private set; }

        /// <summary>
        /// 格子是否被用户手动清空
        /// 用于区分系统清空和用户操作
        /// </summary>
        public bool IsUserCleared { get; private set; }

        /// <summary>
        /// 格子创建时间戳（用于排序和持久化）
        /// </summary>
        public long CreatedTimestamp { get; private set; }

        /// <summary>
        /// 格子状态枚举
        /// </summary>
        public enum SlotState
        {
            /// <summary>
            /// 空格子 - 可用但无物品
            /// </summary>
            Empty,

            /// <summary>
            /// 已占用 - 有物品
            /// </summary>
            Occupied,

            /// <summary>
            /// 已清空 - 用户手动清空
            /// </summary>
            Cleared,

            /// <summary>
            /// 已移除 - 物品被使用完或移除
            /// </summary>
            Removed
        }

        #region 构造函数

        /// <summary>
        /// 创建空格子
        /// </summary>
        /// <param name="originalIndex">原始位置索引</param>
        public static WheelSlot CreateEmpty(int originalIndex)
        {
            return new WheelSlot
            {
                State = SlotState.Empty,
                Item = null,
                OriginalIndex = originalIndex,
                IsUserCleared = false,
                CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

        /// <summary>
        /// 创建占用格子
        /// </summary>
        /// <param name="item">物品</param>
        /// <param name="originalIndex">原始位置索引</param>
        public static WheelSlot CreateOccupied(Item item, int originalIndex)
        {
            return new WheelSlot
            {
                State = SlotState.Occupied,
                Item = item,
                OriginalIndex = originalIndex,
                IsUserCleared = false,
                CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

        /// <summary>
        /// 创建已清空格子（用户手动清空）
        /// </summary>
        /// <param name="originalIndex">原始位置索引</param>
        public static WheelSlot CreateCleared(int originalIndex)
        {
            return new WheelSlot
            {
                State = SlotState.Cleared,
                Item = null,
                OriginalIndex = originalIndex,
                IsUserCleared = true,
                CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

        #endregion

        #region 状态操作方法

        /// <summary>
        /// 设置格子为已移除（物品被使用完）
        /// </summary>
        public WheelSlot SetRemoved()
        {
            return new WheelSlot
            {
                State = SlotState.Removed,
                Item = null,
                OriginalIndex = this.OriginalIndex,
                IsUserCleared = this.IsUserCleared,
                CreatedTimestamp = this.CreatedTimestamp
            };
        }

        /// <summary>
        /// 设置新物品
        /// </summary>
        /// <param name="item">新物品</param>
        public WheelSlot SetItem(Item item)
        {
            return new WheelSlot
            {
                State = item != null ? SlotState.Occupied : SlotState.Empty,
                Item = item,
                OriginalIndex = this.OriginalIndex,
                IsUserCleared = false, // 设置新物品时重置清空标记
                CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

        /// <summary>
        /// 用户清空格子
        /// </summary>
        public WheelSlot ClearByUser()
        {
            return new WheelSlot
            {
                State = SlotState.Cleared,
                Item = null,
                OriginalIndex = this.OriginalIndex,
                IsUserCleared = true,
                CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

        /// <summary>
        /// 重置为空格子
        /// </summary>
        public WheelSlot ResetToEmpty()
        {
            return new WheelSlot
            {
                State = SlotState.Empty,
                Item = null,
                OriginalIndex = this.OriginalIndex,
                IsUserCleared = false,
                CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

        #endregion

        #region 查询方法

        /// <summary>
        /// 检查格子是否有有效物品
        /// </summary>
        public bool HasValidItem()
        {
            return State == SlotState.Occupied && Item != null && !Item.IsBeingDestroyed;
        }

        /// <summary>
        /// 检查格子是否为空（Empty或Cleared）
        /// </summary>
        public bool IsEmpty()
        {
            return State == SlotState.Empty || State == SlotState.Cleared;
        }

        /// <summary>
        /// 获取物品显示名称
        /// </summary>
        public string GetDisplayName()
        {
            return HasValidItem() ? Item.DisplayName : "空";
        }

        #endregion

        /// <summary>
        /// 转换为字符串表示（用于调试）
        /// </summary>
        public override string ToString()
        {
            return $"WheelSlot[State={State}, Item={GetDisplayName()}, Index={OriginalIndex}, UserCleared={IsUserCleared}]";
        }
    }
}