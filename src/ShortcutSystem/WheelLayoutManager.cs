using System;
using System.Collections.Generic;
using System.Linq;
using ItemStatsSystem;
using UnityEngine;

namespace Backpack_QuickWheel.ShortcutSystem
{
    /// <summary>
    /// 轮盘布局管理器
    /// 统一管理轮盘格子和物品，替代双数组设计的混乱架构
    /// </summary>
    public class WheelLayoutManager
    {
        // 按类别管理的轮盘格子列表
        private Dictionary<ItemCategory, List<WheelSlot>> _wheelSlots;

        // 事件委托
        public event Action<ItemCategory> OnLayoutChanged;

        public WheelLayoutManager()
        {
            _wheelSlots = new Dictionary<ItemCategory, List<WheelSlot>>();

            // 初始化所有类别的格子列表
            foreach (ItemCategory category in Enum.GetValues(typeof(ItemCategory)))
            {
                if (category != ItemCategory.None)
                {
                    _wheelSlots[category] = new List<WheelSlot>();
                }
            }
        }

        /// <summary>
        /// 获取指定类别的轮盘格子列表
        /// </summary>
        public List<WheelSlot> GetSlots(ItemCategory category)
        {
            return _wheelSlots.ContainsKey(category) ? new List<WheelSlot>(_wheelSlots[category]) : new List<WheelSlot>();
        }

        /// <summary>
        /// 获取指定类别的有效物品列表（非null且未被销毁）
        /// </summary>
        public List<Item> GetValidItems(ItemCategory category)
        {
            var slots = GetSlots(category);
            return slots.Where(slot => slot.HasValidItem())
                         .Select(slot => slot.Item)
                         .ToList();
        }

        /// <summary>
        /// 获取指定类别的轮盘布局（包含所有格子状态）
        /// 用于UI显示
        /// </summary>
        public List<Item> GetLayoutForUI(ItemCategory category)
        {
            var slots = GetSlots(category);
            return slots.Select(slot => slot.HasValidItem() ? slot.Item : null).ToList();
        }

        /// <summary>
        /// 根据物品收集更新轮盘布局
        /// 这是核心方法，统一管理物品和布局的关系
        /// </summary>
        public void UpdateFromCollectedItems(ItemCategory category, List<Item> collectedItems)
        {
            Debug.Log($"[WheelLayoutManager] 更新类别 {category} 的轮盘布局");
            Debug.Log($"[WheelLayoutManager] 收集到的物品数量: {collectedItems.Count}");

            var currentSlots = GetSlots(category);
            var newSlots = new List<WheelSlot>();

            // 🔧 核心算法：智能合并现有布局和新物品
            // 1. 保留现有的有效格子
            // 2. 为新物品创建格子
            // 3. 维护布局稳定性

            // 第一步：标记现有格子的物品状态
            var usedItems = new HashSet<Item>();
            foreach (var slot in currentSlots)
            {
                if (slot.HasValidItem())
                {
                    if (collectedItems.Contains(slot.Item))
                    {
                        // 物品仍然存在，保留格子
                        newSlots.Add(slot);
                        usedItems.Add(slot.Item);
                        Debug.Log($"[WheelLayoutManager] 保留现有格子: {slot.Item.DisplayName}");
                    }
                    else
                    {
                        // 物品不存在了，标记为已移除
                        newSlots.Add(slot.SetRemoved());
                        Debug.Log($"[WheelLayoutManager] 物品已移除: {slot.Item.DisplayName}");
                    }
                }
                else
                {
                    // 空格子，根据用户行为决定是否保留
                    if (slot.State == WheelSlot.SlotState.Cleared)
                    {
                        // 用户手动清空的格子，保留为Cleared状态
                        newSlots.Add(slot);
                    }
                    else
                    {
                        // 系统空格子，重置为Empty状态
                        newSlots.Add(slot.ResetToEmpty());
                    }
                }
            }

            // 第二步：为新物品创建格子
            foreach (var item in collectedItems)
            {
                if (!usedItems.Contains(item))
                {
                    // 找到合适的插入位置（优先使用空格子）
                    int insertIndex = FindBestInsertPosition(newSlots);
                    var newSlot = WheelSlot.CreateOccupied(item, insertIndex);
                    newSlots.Insert(insertIndex, newSlot);
                    usedItems.Add(item);
                    Debug.Log($"[WheelLayoutManager] 新增物品格子: {item.DisplayName} 在位置 {insertIndex}");
                }
            }

            // 第三步：优化布局（移除过多的空格子）
            OptimizeLayout(newSlots);

            // 更新布局
            _wheelSlots[category] = newSlots;

            // 触发布局变更事件
            OnLayoutChanged?.Invoke(category);

            Debug.Log($"[WheelLayoutManager] 更新完成，最终格子数量: {newSlots.Count}");
        }

        /// <summary>
        /// 根据物品使用情况更新格子状态
        /// </summary>
        public void UpdateSlotAfterItemUsage(Item usedItem)
        {
            Debug.Log($"[WheelLayoutManager] 更新物品使用后状态: {usedItem.DisplayName}");

            foreach (var kvp in _wheelSlots)
            {
                var category = kvp.Key;
                var slots = kvp.Value;

                for (int i = 0; i < slots.Count; i++)
                {
                    var slot = slots[i];
                    if (slot.HasValidItem() && slot.Item == usedItem)
                    {
                        // 检查物品是否还有剩余（这里简化处理，假设物品被使用完）
                        // 在实际实现中，可以检查物品的StackCount等属性
                        slots[i] = slot.SetRemoved();

                        Debug.Log($"[WheelLayoutManager] 物品使用完，更新格子状态: {usedItem.DisplayName}");

                        // 触发布局变更事件
                        OnLayoutChanged?.Invoke(category);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 获取当前选中的物品
        /// </summary>
        public Item GetCurrentSelection(ItemCategory category)
        {
            var slots = GetSlots(category);

            // 查找第一个有效物品
            foreach (var slot in slots)
            {
                if (slot.HasValidItem())
                {
                    return slot.Item;
                }
            }

            return null;
        }

        /// <summary>
        /// 设置当前选择（通过物品索引）
        /// </summary>
        public bool SetCurrentSelection(ItemCategory category, int slotIndex)
        {
            var slots = GetSlots(category);

            if (slotIndex >= 0 && slotIndex < slots.Count && slots[slotIndex].HasValidItem())
            {
                // 这里可以添加选择状态管理的逻辑
                Debug.Log($"[WheelLayoutManager] 设置当前选择: {category} 索引 {slotIndex} -> {slots[slotIndex].Item.DisplayName}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 找到最佳的插入位置
        /// 优先使用空格子，其次是追加到末尾
        /// </summary>
        private int FindBestInsertPosition(List<WheelSlot> slots)
        {
            // 优先找到第一个空格子
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsEmpty())
                {
                    return i;
                }
            }

            // 如果没有空格子，追加到末尾
            return slots.Count;
        }

        /// <summary>
        /// 优化布局，移除过多的空格子
        /// </summary>
        private void OptimizeLayout(List<WheelSlot> slots)
        {
            // 计算有效物品数量
            int validItemCount = slots.Count(slot => slot.HasValidItem());

            // 如果空格子过多，进行清理
            int maxEmptySlots = Math.Max(2, validItemCount / 2); // 最多保留一半数量的空格子

            var optimizedSlots = new List<WheelSlot>();
            int emptyCount = 0;

            foreach (var slot in slots)
            {
                if (slot.HasValidItem())
                {
                    optimizedSlots.Add(slot);
                }
                else if (slot.State == WheelSlot.SlotState.Cleared && emptyCount < maxEmptySlots)
                {
                    // 保留用户清空的格子（有限数量）
                    optimizedSlots.Add(slot);
                    emptyCount++;
                }
                // 其他空格子（系统Empty或Removed）被丢弃
            }

            // 如果优化后数量变化，更新列表
            if (optimizedSlots.Count != slots.Count)
            {
                slots.Clear();
                slots.AddRange(optimizedSlots);
                Debug.Log($"[WheelLayoutManager] 布局优化: {slots.Count} -> {optimizedSlots.Count}");
            }
        }

        /// <summary>
        /// 从布局中移除指定物品
        /// </summary>
        public void RemoveItem(ItemCategory category, Item item)
        {
            if (item == null) return;

            var slots = GetSlots(category);
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].Item == item)
                {
                    Debug.Log($"[WheelLayoutManager] 从类别 {category} 移除物品: {item.DisplayName}");
                    slots[i] = slots[i].SetRemoved();
                    OnLayoutChanged?.Invoke(category);
                    return;
                }
            }
        }

        /// <summary>
        /// 清空指定类别的布局
        /// </summary>
        public void ClearCategory(ItemCategory category)
        {
            if (_wheelSlots.ContainsKey(category))
            {
                _wheelSlots[category].Clear();
                Debug.Log($"[WheelLayoutManager] 已清空类别 {category} 的布局");
                OnLayoutChanged?.Invoke(category);
            }
        }

        /// <summary>
        /// 获取布局统计信息
        /// </summary>
        public string GetLayoutStats(ItemCategory category)
        {
            var slots = GetSlots(category);
            int occupied = slots.Count(slot => slot.State == WheelSlot.SlotState.Occupied);
            int empty = slots.Count(slot => slot.State == WheelSlot.SlotState.Empty);
            int cleared = slots.Count(slot => slot.State == WheelSlot.SlotState.Cleared);
            int removed = slots.Count(slot => slot.State == WheelSlot.SlotState.Removed);

            return $"类别{category}: 总计{slots.Count}, 占用{occupied}, 空格{empty}, 清空{cleared}, 移除{removed}";
        }
    }
}