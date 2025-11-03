using System;
using System.Collections.Generic;
using System.Linq;
using ItemStatsSystem;
using UnityEngine;
using Backpack_QuickWheel.ShortcutSystem.Data;

namespace Backpack_QuickWheel.ShortcutSystem
{
    /// <summary>
    /// 轮盘布局管理器 - 简化版本（单例模式）
    /// 使用固定8个槽位设计，移除复杂的动态槽位管理
    /// </summary>
    public class WheelLayoutManager
    {
        #region 单例初始化

        /// <summary>
        /// 单例实例
        /// </summary>
        private static WheelLayoutManager _instance;
        public static WheelLayoutManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new WheelLayoutManager();
                }
                return _instance;
            }
        }

        /// <summary>
        /// 固定槽位数量
        /// </summary>
        private const int FIXED_SLOT_COUNT = 8;

        private WheelLayoutManager()
        {
            _wheelSlots = new Dictionary<ItemCategory, SimpleWheelSlot[]>();
            _currentSelectedSlot = new Dictionary<ItemCategory, int>();

            // 🗑️ 删除：不再预初始化所有类别
            // 使用 GetOrCreateCategorySlots 方法按需创建
        }

        /// <summary>
        /// 🆕 新架构：确认UI更新器可用
        /// ShortcutUIUpdater是静态类，可以直接调用其方法
        /// </summary>
        public void SetUIUpdater()
        {
            // ShortcutUIUpdater是静态类，无需持有引用
            Debug.Log("[WheelLayoutManager] UI更新器已确认，轮盘可以直接更新UI");
        }

        #endregion

        #region 核心数据

        // 按类别管理的固定槽位数组
        private Dictionary<ItemCategory, SimpleWheelSlot[]> _wheelSlots;

        // 🆕 新增：基于格子索引的选中状态管理
        private Dictionary<ItemCategory, int> _currentSelectedSlot = new Dictionary<ItemCategory, int>();

        // 🗑️ 已删除：OnLayoutChanged事件
        // 轮盘现在直接更新UI，不再需要事件通知

        // 🆕 新架构：ShortcutUIUpdater是静态类，可以直接调用其方法
        // 无需持有引用，直接通过类名调用方法

        #endregion

        #region 槽位操作

        /// <summary>
        /// 获取指定类别的轮盘槽位列表（用于外部调用）
        /// 返回固定8个槽位的列表
        /// </summary>
        public List<SimpleWheelSlot> GetSlots(ItemCategory category)
        {
            var slots = GetOrCreateCategorySlots(category);
            return slots.ToList();
        }

        /// <summary>
        /// 获取指定类别的有效物品列表（非null且未被消耗）
        /// </summary>
        public List<Item> GetValidItems(ItemCategory category)
        {
            var slots = GetOrCreateCategorySlots(category);
            return slots.Where(slot => slot.HasValidItem) // Fixed: property access instead of method call
                         .Select(slot => slot.Item)
                         .ToList();
        }

        /// <summary>
        /// 获取指定类别的轮盘布局（包含所有格子状态）
        /// 用于UI显示
        /// </summary>
        public List<Item> GetLayoutForUI(ItemCategory category)
        {
            var slots = GetOrCreateCategorySlots(category);
            Debug.Log($"[WheelLayoutManager] GetLayoutForUI: 类别 {category}，总槽位数: {slots.Length}，有物品的槽位: {slots.Count(s => s.HasValidItem)}"); // Fixed: Count instead of Length, property access

            var result = slots.Select(slot => slot.HasValidItem ? slot.Item : null).ToList(); // Fixed: property access instead of method call
            Debug.Log($"[WheelLayoutManager] GetLayoutForUI: 返回 {result.Count} 个位置，其中 {result.Count(i => i != null)} 个有物品");

            // 🆕 调试：详细显示每个槽位的内容
            for (int i = 0; i < result.Count; i++)
            {
                if (result[i] != null)
                {
                    Debug.Log($"[WheelLayoutManager] GetLayoutForUI: 槽位 {i}: {result[i].DisplayName}");
                }
                else
                {
                    Debug.Log($"[WheelLayoutManager] GetLayoutForUI: 槽位 {i}: 空");
                }
            }

            return result;
        }

        /// <summary>
        /// 获取指定类别的槽位数组，如果未初始化则先初始化
        /// </summary>
        /// <param name="category">物品类别</param>
        /// <returns>类别的固定8个槽位数组</returns>
        private SimpleWheelSlot[] GetOrCreateCategorySlots(ItemCategory category)
        {
            if (!_wheelSlots.ContainsKey(category))
            {
                _wheelSlots[category] = new SimpleWheelSlot[8];
                for (int i = 0; i < 8; i++)
                {
                    _wheelSlots[category][i] = new SimpleWheelSlot { Item = null, Index = i };
                }
                Debug.Log($"[WheelLayoutManager] 初始化类别 {category} 的8个固定槽位");
            }

            return _wheelSlots[category];
        }

        /// <summary>
        /// 清空指定类别的布局
        /// </summary>
        public void ClearCategory(ItemCategory category)
        {
            if (_wheelSlots.ContainsKey(category))
            {
                var slots = _wheelSlots[category];
                for (int i = 0; i < slots.Length; i++)
                {
                    slots[i].Item = null;
                }
                Debug.Log($"[WheelLayoutManager] 已清空类别 {category} 的布局");

                // 🆕 重置该类别的选中状态为未选中（-1）
                SetSelectedSlot(category, -1);
                Debug.Log($"[WheelLayoutManager] 类别 {category} 已清空，选中状态已重置为-1");
            }
            else
            {
                Debug.LogWarning($"[WheelLayoutManager] 尝试清空不存在的类别: {category}");
            }
        }

        /// <summary>
        /// 获取布局统计信息
        /// </summary>
        public string GetLayoutStats(ItemCategory category)
        {
            var slots = GetOrCreateCategorySlots(category);
            int occupied = slots.Count(slot => slot.HasItem);
            int empty = slots.Count(slot => slot.IsEmpty);

            return $"类别{category}: 总计{slots.Length}, 占用{occupied}, 空格{empty}";
        }

        #endregion

        #region 物品操作

        /// <summary>
        /// 🔥 多类别批量更新 - 全类别轮盘重建
        /// ⚠️ 警告：这是一个全量操作，专门处理混合类别物品的批量更新
        /// 使用场景：系统初始化、背包更换、配件移除等需要重新扫描所有物品的情况
        /// 特点：传入的物品列表可能包含多种类别，方法内部自动按类别分组后批量更新
        /// 单类别增量更新请使用 AddItemToCategory / RemoveItem 方法
        /// </summary>
        public void BatchUpdateMultipleCategories(ItemCategory category, List<Item> mixedCategoryItems)
        {
            Debug.Log($"[WheelLayoutManager] 🔥 多类别批量更新 {category}，收集到 {mixedCategoryItems.Count} 个物品");

            // 获取或创建槽位数组（自动创建不存在的类别）
            var slots = GetOrCreateCategorySlots(category);
            Debug.Log($"[WheelLayoutManager] 类别 {category} 已准备就绪，槽位数: {slots.Length}");

            // 第一步：清理已消耗的物品
            for (int i = 0; i < FIXED_SLOT_COUNT; i++)
            {
                if (slots[i].HasItem && !mixedCategoryItems.Contains(slots[i].Item))
                {
                    // 物品被消耗，清空槽位
                    slots[i].Item = null;
                    Debug.Log($"[WheelLayoutManager] 🔥 批量更新：清理已消耗物品，清空位置 {i}");
                }
                // 🎯 关键：如果物品还在，什么都不做！
            }

            // 第二步：添加新增的物品
            foreach (var item in mixedCategoryItems)
            {
                bool itemExists = false;

                // 检查物品是否已经在槽位中
                for (int i = 0; i < FIXED_SLOT_COUNT; i++)
                {
                    if (slots[i].Item == item)  // 直接引用比较
                    {
                        itemExists = true;
                        break;
                    }
                }

                // 如果是新物品，放入第一个空槽位
                if (!itemExists)
                {
                    for (int i = 0; i < FIXED_SLOT_COUNT; i++)
                    {
                        if (slots[i].IsEmpty)
                        {
                            slots[i].Item = item;  // 设置物品
                            Debug.Log($"[WheelLayoutManager] 🔥 批量更新：新物品放入位置 {i}: {item.DisplayName}");
                            break;
                        }
                    }
                }
            }

            // 🆕 第三步：处理选中状态（关键修复）
            // 确保在物品更新后，选中状态是正确的（内部已自动调用UI更新）
            Item currentSelectedItem = GetSelectedItem(category);
            HandleSelectionAfterLayoutUpdate(category, currentSelectedItem, slots);

            // 保留事件用于其他可能的监听者（如日志记录等）
            // 🗑️ 已删除：OnLayoutChanged事件调用
            // 轮盘直接更新UI，不需要事件通知
        }

        /// <summary>
        /// 从UI更新布局（处理轮盘拖拽交易）
        /// 🆕 新增：统一处理UI布局更新，避免数据冲突
        /// </summary>
        public void UpdateLayoutFromUI(ItemCategory category, List<Item> newLayout)
        {
            Debug.Log($"[WheelLayoutManager] 更新UI布局: {category}");

            // 获取或创建槽位数组
            var slots = GetOrCreateCategorySlots(category);

            // 记录当前选中的物品
            Item selectedItem = GetSelectedItem(category);
            Debug.Log($"[WheelLayoutManager] 当前选中物品: {selectedItem?.DisplayName ?? "无"}");

            // 更新槽位
            for (int i = 0; i < 8; i++)
            {
                if (i < newLayout.Count && newLayout[i] != null)
                {
                    slots[i].Item = newLayout[i];
                }
                else
                {
                    slots[i].Item = null;
                }
            }

            // 处理选中状态（内部已自动调用UI更新）
            HandleSelectionAfterLayoutUpdate(category, selectedItem, slots);

            // 🗑️ 已删除：OnLayoutChanged事件调用
            // 轮盘直接更新UI，不需要事件通知

            // 保存布局
            WheelLayoutPersistence.SaveWheelSlots(this);
            Debug.Log($"[WheelLayoutManager] UI布局已保存 {category}，槽位数: {slots.Length}");
        }

        /// <summary>
        /// 🆕 添加物品到指定类别 - 简化版本
        /// 当单个物品被添加到背包时使用此方法
        /// </summary>
        public void AddItemToCategory(ItemCategory category, Item newItem)
        {
            if (newItem == null)
            {
                Debug.LogError("[WheelLayoutManager] AddItemToCategory: 物品为空");
                return;
            }

            Debug.Log($"[WheelLayoutManager] 添加物品到类别 {category}: {newItem.DisplayName} (引用: {newItem.GetHashCode()})");

            // 获取或创建类别槽位数组
            var slots = GetOrCreateCategorySlots(category);

            // 检查物品是否已存在（直接引用比较）
            for (int i = 0; i < FIXED_SLOT_COUNT; i++)
            {
                if (slots[i].Item == newItem)
                {
                    Debug.Log($"[WheelLayoutManager] 物品已存在于类别 {category} ：{newItem.DisplayName}，跳过添加");
                    return;
                }
            }

            // 寻找第一个空槽位
            int emptyIndex = -1;
            for (int i = 0; i < FIXED_SLOT_COUNT; i++)
            {
                if (slots[i].IsEmpty)
                {
                    emptyIndex = i;
                    break;
                }
            }

            // 检查是否有空槽位
            if (emptyIndex == -1)
            {
                Debug.LogWarning($"[WheelLayoutManager] 类别 {category} 8个槽位已满，无法添加物品: {newItem.DisplayName}");
                return;
            }

            // 在空槽位放置物品
            slots[emptyIndex].Item = newItem;

            Debug.Log($"[WheelLayoutManager] 成功添加物品 {newItem.DisplayName} 到类别 {category} 的位置 {emptyIndex}");

            // 检查是否是该类别的第一个物品，如果是则自动选中
            bool wasEmpty = true;
            for (int i = 0; i < FIXED_SLOT_COUNT; i++)
            {
                if (i != emptyIndex && slots[i].HasItem)
                {
                    wasEmpty = false;
                    break;
                }
            }

            if (wasEmpty)
            {
                SetSelectedSlot(category, emptyIndex);
                Debug.Log($"[WheelLayoutManager] 自动选中类别 {category} 的第一个物品: {newItem.DisplayName}");
            }

            // 🗑️ 已删除：重复的UI更新调用 - SetSelectedSlot已内置UI更新

            // 🗑️ 已删除：OnLayoutChanged事件调用
            // 轮盘直接更新UI，不需要事件通知
        }

        /// <summary>
        /// 🔥 批量移除物品 - 配件移除时使用
        /// ⚠️ 警告：这是一个批量操作，专门处理配件移除时的多物品清理
        /// 使用场景：配件拔出时批量移除配件内的所有物品
        /// </summary>
        public void BatchRemoveItems(ItemCategory category, List<Item> itemsToRemove)
        {
            if (itemsToRemove == null || itemsToRemove.Count == 0)
            {
                Debug.Log($"[WheelLayoutManager] 批量移除：物品列表为空，跳过操作");
                return;
            }

            Debug.Log($"[WheelLayoutManager] 🔥 批量移除类别 {category} 中的 {itemsToRemove.Count} 个物品");

            // 获取槽位数组
            if (!_wheelSlots.ContainsKey(category))
            {
                Debug.LogWarning($"[WheelLayoutManager] 尝试从不存在的类别布局中批量移除物品 {category}");
                return;
            }

            var slots = _wheelSlots[category];

            // 批量移除所有指定物品
            int removedCount = 0;
            foreach (var itemToRemove in itemsToRemove)
            {
                if (itemToRemove == null) continue;

                int itemHash = itemToRemove.GetHashCode();

                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i].Item != null && slots[i].Item.GetHashCode() == itemHash)
                    {
                        slots[i].Item = null;
                        removedCount++;
                        Debug.Log($"[WheelLayoutManager] 🔥 批量移除：已移除物品 {itemToRemove.DisplayName} (位置 {i})");
                        break;
                    }
                }
            }

            Debug.Log($"[WheelLayoutManager] 🔥 批量移除完成，共移除 {removedCount} 个物品");

            // 处理选中状态：如果当前选中的物品被移除，需要重新选择
            int currentSelectedSlot = GetSelectedSlot(category);
            if (currentSelectedSlot != -1 && currentSelectedSlot < slots.Length && slots[currentSelectedSlot].Item == null)
            {
                // 当前选中的物品被移除，尝试选择下一个有效物品
                int nextSlot = FindValidSlot(category, SlotSearchStrategy.NextFromRemoved, currentSelectedSlot);
                SetSelectedSlot(category, nextSlot);
                if (nextSlot != -1)
                {
                    Debug.Log($"[WheelLayoutManager] 🔥 批量移除后自动选中槽位{nextSlot}: {slots[nextSlot].Item?.DisplayName ?? "空"}");
                }
                else
                {
                    Debug.Log($"[WheelLayoutManager] 🔥 批量移除后类别 {category} 无有效物品，清空选中状态");
                }
            }
        }

        /// <summary>
        /// 从布局中移除指定物品
        /// 🔧 修复：物品消耗时允许查找正在销毁的物品，解决配件槽内物品消耗问题
        /// </summary>
        public void RemoveItem(ItemCategory category, Item item)
        {
            if (item == null) return;

            // 🆕 修复：直接操作原始数据，而不是副本
            if (!_wheelSlots.ContainsKey(category))
            {
                Debug.LogWarning($"[WheelLayoutManager] 尝试从不存在的类别布局中移除物品 {category}");
                return;
            }

            int itemHash = item.GetHashCode();
            var slots = _wheelSlots[category]; // 直接使用原始数据，不创建副本

            Debug.Log($"[WheelLayoutManager] 开始移除物品: {item.DisplayName} (引用: {itemHash}) 从类别 {category}");

            for (int i = 0; i < slots.Length; i++)
            {
                // 🔧 重要修复：对于物品消耗场景，不能使用HasValidItem，因为物品使用时IsBeingDestroyed会变为true
                // 直接检查物品引用和哈希匹配，这样可以找到正在销毁但仍然存在的物品
                if (slots[i].Item != null && slots[i].Item.GetHashCode() == itemHash)
                {
                    Debug.Log($"[WheelLayoutManager] 找到物品: {item.DisplayName} (引用: {itemHash}) 在位置 {i}");
                    Debug.Log($"[WheelLayoutManager] 槽位物品状态: HasValidItem={slots[i].HasValidItem}, IsBeingDestroyed={slots[i].Item.IsBeingDestroyed}");

                    slots[i].Item = null;

                    // 处理选中状态：如果移除的是当前选中的物品，需要重新选择
                    int currentSelectedSlot = GetSelectedSlot(category);
                    if (currentSelectedSlot == i)
                    {
                        // 尝试选择下一个有效物品
                        int nextSlot = FindValidSlot(category, SlotSearchStrategy.NextFromRemoved, i);
                        SetSelectedSlot(category, nextSlot); // Fixed: 实际更新选中状态
                        if (nextSlot != -1)
                        {
                            Debug.Log($"[WheelLayoutManager] 移除后自动选中槽位{nextSlot}: {slots[nextSlot].Item?.DisplayName ?? "空"}");
                        }
                        Debug.Log($"[WheelLayoutManager] 移除的物品是当前选中项，已重新选择");
                    }

                    Debug.Log($"[WheelLayoutManager] 物品移除完成: {item.DisplayName} (引用: {itemHash})");
                    return;
                }
            }

            Debug.LogWarning($"[WheelLayoutManager] 在类别 {category} 中找不到要移除的物品: {item.DisplayName} (引用: {itemHash})");

            // 🔧 调试：输出所有槽位的详细信息
            Debug.Log($"[WheelLayoutManager] 调试信息 - 类别 {category} 的所有槽位状态:");
            for (int i = 0; i < slots.Length; i++)
            {
                var slotItem = slots[i].Item;
                if (slotItem != null)
                {
                    Debug.Log($"  槽位{i}: {slotItem.DisplayName} (引用: {slotItem.GetHashCode()}, HasValidItem: {slots[i].HasValidItem}, IsBeingDestroyed: {slotItem.IsBeingDestroyed})");
                }
                else
                {
                    Debug.Log($"  槽位{i}: 空");
                }
            }
        }

        /// <summary>
        /// 根据物品使用情况更新槽位状态
        /// 🔧 修复：这个方法现在只是委托给RemoveItem方法处理
        /// 物品销毁时应该通过标准的移除流程来处理选中状态和UI更新
        /// </summary>
        public void UpdateSlotAfterItemUsage(Item usedItem)
        {
            Debug.Log($"[WheelLayoutManager] 更新物品使用后状态 {usedItem.DisplayName} (引用: {usedItem.GetHashCode()})");

            if (usedItem == null)
            {
                Debug.LogWarning("[WheelLayoutManager] UpdateSlotAfterItemUsage: 物品为null");
                return;
            }

            // 🔧 修复：使用标准的移除方法来处理物品消耗
            // 这样可以确保选中状态和UI更新都通过正确的流程处理
            var category = ItemCategorizer.CategorizeItem(usedItem);
            if (category != ItemCategory.None)
            {
                Debug.Log($"[WheelLayoutManager] 通过RemoveItem处理物品消耗: {usedItem.DisplayName} (类别: {category})");
                RemoveItem(category, usedItem);
            }
            else
            {
                Debug.LogWarning($"[WheelLayoutManager] 物品 {usedItem.DisplayName} 无法识别类别，无法处理消耗");
            }
        }

        /// <summary>
        /// 🆕 交换两个槽位的物品（用于拖拽调整）
        /// 同时处理选中状态的更新
        /// </summary>
        /// <param name="category">物品类别</param>
        /// <param name="fromIndex">源槽位索引</param>
        /// <param name="toIndex">目标槽位索引</param>
        public void SwapSlots(ItemCategory category, int fromIndex, int toIndex)
        {
            // 验证索引范围
            if (fromIndex < 0 || fromIndex >= FIXED_SLOT_COUNT || toIndex < 0 || toIndex >= FIXED_SLOT_COUNT)
            {
                Debug.LogWarning($"[WheelLayoutManager] 交换索引无效: from={fromIndex}, to={toIndex}, 范围0-{FIXED_SLOT_COUNT-1}");
                return;
            }

            Debug.Log($"[WheelLayoutManager] 开始交换槽位: 类别{category}, 索引{fromIndex}<->{toIndex}");

            // 获取槽位数组
            var slots = GetOrCreateCategorySlots(category);

            // 交换物品（使用元组解构）
            (slots[fromIndex].Item, slots[toIndex].Item) = (slots[toIndex].Item, slots[fromIndex].Item);

            // 处理选中状态的更新
            int currentSelectedSlot = GetSelectedSlot(category);
            if (currentSelectedSlot == fromIndex)
            {
                // 如果原选中位置是fromIndex，现在选中位置应该是toIndex
                SetSelectedSlot(category, toIndex);
                Debug.Log($"[WheelLayoutManager] 选中状态跟随物品: {fromIndex} -> {toIndex}");
            }
            else if (currentSelectedSlot == toIndex)
            {
                // 如果原选中位置是toIndex，现在选中位置应该是fromIndex
                SetSelectedSlot(category, fromIndex);
                Debug.Log($"[WheelLayoutManager] 选中状态跟随物品: {toIndex} -> {fromIndex}");
            }
            // 如果选中位置不是fromIndex或toIndex，保持不变

            Debug.Log($"[WheelLayoutManager] 槽位交换完成: {slots[fromIndex].DisplayName} <-> {slots[toIndex].DisplayName}");

            // 🆕 新架构：直接调用UI更新
            // 🗑️ 已删除：重复的UI更新调用 - SetSelectedSlot已内置UI更新

            // 🗑️ 已删除：OnLayoutChanged事件调用
            // 轮盘直接更新UI，不需要事件通知

            // 保存布局
            WheelLayoutPersistence.SaveWheelSlots(this);
        }

        #endregion

        #region 选中状态

        /// <summary>
        /// 设置选中槽位索引
        /// 🆕 架构修复：自动更新UI，确保选中状态变化立即反映到界面
        /// </summary>
        /// <param name="category">物品类别</param>
        /// <param name="slotIndex">槽位索引</param>
        public void SetSelectedSlot(ItemCategory category, int slotIndex)
        {
            _currentSelectedSlot[category] = slotIndex;
            Debug.Log($"[WheelLayoutManager] 设置 {category} 选中槽位: {slotIndex}");

            // 🆕 自动更新UI，确保选中状态变化立即反映到界面
            DirectUpdateUI(category);
        }

        /// <summary>
        /// 获取当前选中槽位索引
        /// </summary>
        /// <param name="category">物品类别</param>
        /// <returns>槽位索引，-1表示无选中</returns>
        public int GetSelectedSlot(ItemCategory category)
        {
            return _currentSelectedSlot.TryGetValue(category, out int slot) ? slot : -1;
        }

        /// <summary>
        /// 设置当前选择（通过物品索引）
        /// </summary>
        public void SetCurrentSelection(ItemCategory category, int slotIndex)
        {
            SetSelectedSlot(category, slotIndex);
        }

        /// <summary>
        /// 获取当前选中物品（基于槽位索引）
        /// </summary>
        /// <param name="category">物品类别</param>
        /// <returns>选中的物品，无则返回null</returns>
        public Item GetSelectedItem(ItemCategory category)
        {
            int selectedSlot = GetSelectedSlot(category);
            if (selectedSlot >= 0 && selectedSlot < FIXED_SLOT_COUNT)  // 使用常量保持一致性
            {
                if (_wheelSlots.TryGetValue(category, out var slots))
                {
                    var slot = slots[selectedSlot];  // 直接访问数组
                    if (slot.HasItem)
                    {
                        Debug.Log($"[WheelLayoutManager] 获取 {category} 选中物品: {slot.Item.DisplayName} (槽位: {selectedSlot})");
                        return slot.Item;
                    }
                }
            }

            Debug.Log($"[WheelLayoutManager] {category} 无有效选中物品");
            return null;
        }

        /// <summary>
        /// 处理布局更新后的选中状态
        /// </summary>
        private void HandleSelectionAfterLayoutUpdate(ItemCategory category, Item selectedItem, SimpleWheelSlot[] slots)
        {
            // 情况1：有选中物品，尝试跟随移动
            if (selectedItem != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (slots[i].Item == selectedItem)
                    {
                        SetSelectedSlot(category, i);
                        Debug.Log($"[WheelLayoutManager] 选中跟随物品: {selectedItem.DisplayName} 移动到位置 {i}");
                        return; // 找到选中物品，直接返回
                    }
                }
                Debug.Log($"[WheelLayoutManager] 原选中物品 {selectedItem.DisplayName} 已被移除");
            }
            else
            {
                Debug.Log($"[WheelLayoutManager] 原本没有选中物品");
            }

            // 情况2：没有选中物品或原选中物品已移除，尝试选中第一个有效物品
            for (int i = 0; i < 8; i++)
            {
                if (slots[i].HasItem)
                {
                    SetSelectedSlot(category, i);
                    Debug.Log($"[WheelLayoutManager] 自动选中第一个有效物品: {slots[i].DisplayName} 在位置 {i}");
                    return; // 找到第一个有效物品，直接返回
                }
            }

            // 情况3：分类下没有物品了，清空选中状态
            SetSelectedSlot(category, -1); // Fixed: 使用SetSelectedSlot方法保持一致性
            Debug.Log($"[WheelLayoutManager] 类别 {category} 无有效物品，清空选中状态");
        }

        #endregion

        #region 物品导航

        /// <summary>
        /// 🆕 按照布局获得下一个物品（基于槽位索引）
        /// 根据当前选中的槽位索引，查找下一个合适的物品
        /// 🆕 重新设计：不再依赖物品引用，而是基于槽位索引进行选择管理
        /// </summary>
        /// <param name="category">物品类别</param>
        /// <param name="currentItem">当前物品（用于日志，实际选择基于索引）</param>
        /// <returns>下一个物品，如果找不到则返回null</returns>
        public Item GetNextItemByLayout(ItemCategory category, Item currentItem)
        {
            var slots = GetOrCreateCategorySlots(category);

            // 🆕 新设计：基于槽位索引的选择管理
            int currentSlotIndex = GetSelectedSlot(category);

            // 如果没有选中记录，尝试初始化选择
            if (currentSlotIndex == -1)
            {
                Debug.Log($"[WheelLayoutManager] {category} 没有选中记录，尝试初始化选择");
                int firstValidSlot = FindValidSlot(category, SlotSearchStrategy.FirstValid);
                if (firstValidSlot != -1)
                {
                    SetSelectedSlot(category, firstValidSlot);
                    Debug.Log($"[WheelLayoutManager] 初始化选中槽位{firstValidSlot}: {slots[firstValidSlot].Item.DisplayName}");
                    return slots[firstValidSlot].Item;
                }
                Debug.Log($"[WheelLayoutManager] {category} 没有有效物品可选");
                return null;
            }

            Debug.Log($"[WheelLayoutManager] 获取下一个物品，当前选中槽位: {currentSlotIndex}, 当前物品: {currentItem?.DisplayName ?? "无"}");

            // 🆕 简化逻辑：使用统一查找方法
            int nextSlotIndex = FindValidSlot(category, SlotSearchStrategy.NextFromStart, currentSlotIndex);

            if (nextSlotIndex != -1)
            {
                SetSelectedSlot(category, nextSlotIndex);
                Debug.Log($"[WheelLayoutManager] 找到下一个物品({nextSlotIndex}): {slots[nextSlotIndex].Item.DisplayName}");
                return slots[nextSlotIndex].Item;
            }

            // 只有当前选中的物品有效，返回它自己
            if (slots[currentSlotIndex].HasValidItem)
            {
                Debug.Log($"[WheelLayoutManager] 只有当前物品: {slots[currentSlotIndex].Item.DisplayName}");
                return slots[currentSlotIndex].Item;
            }

            // 当前选中物品无效，清除选择记录
            Debug.Log($"[WheelLayoutManager] 当前物品无效，清除选择记录");
            _currentSelectedSlot.Remove(category);
            return null;
        }

        /// <summary>
        /// 🆕 统一的物品槽位查找方法
        /// 支持多种查找策略，简化重复代码
        /// </summary>
        /// <param name="category">物品类别</param>
        /// <param name="strategy">查找策略</param>
        /// <param name="startIndex">起始索引（某些策略需要）</param>
        /// <returns>找到的槽位索引，-1表示未找到</returns>
        private int FindValidSlot(ItemCategory category, SlotSearchStrategy strategy, int startIndex = -1)
        {
            var slots = GetOrCreateCategorySlots(category);

            switch (strategy)
            {
                case SlotSearchStrategy.FirstValid:
                    // 查找第一个有效物品
                    for (int i = 0; i < slots.Length; i++)
                    {
                        if (slots[i].HasValidItem) return i;
                    }
                    break;

                case SlotSearchStrategy.NextFromStart:
                    // 从指定位置往后查找
                    for (int i = startIndex + 1; i < slots.Length; i++)
                    {
                        if (slots[i].HasValidItem) return i;
                    }
                    // 从头循环到起始位置
                    for (int i = 0; i <= startIndex; i++)
                    {
                        if (slots[i].HasValidItem) return i;
                    }
                    break;

                case SlotSearchStrategy.NextFromRemoved:
                    // 从移除位置往后查找，然后从头查找到移除位置
                    for (int i = startIndex + 1; i < slots.Length; i++)
                    {
                        if (slots[i].HasValidItem) return i;
                    }
                    for (int i = 0; i < startIndex; i++)
                    {
                        if (slots[i].HasValidItem) return i;
                    }
                    break;

                case SlotSearchStrategy.NearestToIndex:
                    // 查找离指定索引最近的有效物品
                    if (startIndex >= 0 && startIndex < slots.Length && slots[startIndex].HasValidItem)
                    {
                        return startIndex; // 当前位置有效
                    }

                    // 向后查找
                    for (int i = 1; i < slots.Length; i++)
                    {
                        int forwardIndex = startIndex + i;
                        int backwardIndex = startIndex - i;

                        if (forwardIndex < slots.Length && slots[forwardIndex].HasValidItem)
                        {
                            return forwardIndex;
                        }
                        if (backwardIndex >= 0 && slots[backwardIndex].HasValidItem)
                        {
                            return backwardIndex;
                        }
                    }
                    break;
            }

            return -1;
        }

        /// <summary>
        /// 查找策略枚举
        /// </summary>
        private enum SlotSearchStrategy
        {
            FirstValid,      // 第一个有效物品
            NextFromStart,   // 从指定位置往后循环查找
            NextFromRemoved,  // 从移除位置往后查找
            NearestToIndex   // 查找最近的物品
        }

        #endregion

        #region UI更新

        /// <summary>
        /// 🆕 新架构：直接调用UI更新
        /// 让轮盘直接更新UI，无需通过BackpackShortcutManager中转
        /// </summary>
        private void DirectUpdateUI(ItemCategory category)
        {
            // ShortcutUIUpdater是静态类，直接调用
            Debug.Log($"[WheelLayoutManager] 直接更新UI: {category}");
            ShortcutUIUpdater.UpdateCategoryUI(category);
        }

        #endregion

        #region 布局恢复

        /// <summary>
        /// 🆕 尝试从历史记录恢复轮盘布局（方法）
        /// 只有当历史布局与当前物品完全匹配时才恢复，否则返回false
        /// </summary>
        /// <param name="currentItems">当前收集到的分类物品</param>
        /// <returns>是否成功恢复布局</returns>
        public bool TryRestoreLayout(Dictionary<ItemCategory, List<Item>> currentItems)
        {
            Debug.Log("[WheelLayoutManager] 🆕 尝试从历史记录恢复轮盘布局...");

            // 1. 加载保存的布局数据
            var savedLayoutData = WheelLayoutPersistence.LoadWheelSlots();
            if (savedLayoutData == null)
            {
                Debug.Log("[WheelLayoutManager] 没有找到历史布局记录，无法恢复");
                return false;
            }

            Debug.Log($"[WheelLayoutManager] 找到历史布局记录，版本: {savedLayoutData.Version}");

            // 2. 确认历史布局与当前物品的匹配度
            bool isLayoutValid = ValidateLayoutMatch(savedLayoutData, currentItems);
            if (!isLayoutValid)
            {
                Debug.Log("[WheelLayoutManager] 历史布局与当前物品不匹配，无法恢复");
                return false;
            }

            // 3. 直接根据保存的布局数据和当前物品恢复布局
            foreach (var categoryLayout in savedLayoutData.categories)
            {
                if (!Enum.TryParse<ItemCategory>(categoryLayout.categoryName, out var category))
                {
                    Debug.LogWarning($"[WheelLayoutManager] 无法解析类别名称: {categoryLayout.categoryName}");
                    continue;
                }

                if (!currentItems.ContainsKey(category))
                {
                    Debug.LogWarning($"[WheelLayoutManager] 当前没有类别 {category} 的物品");
                    continue;
                }

                var currentCategoryItems = currentItems[category];
                var savedLocations = categoryLayout.itemLocations;

                Debug.Log($"[WheelLayoutManager] 恢复类别 {category} 的布局: {savedLocations.Length} 个位置");

                // 根据保存的位置信息和当前物品恢复布局
                var restoredItems = RestoreCategoryLayout(savedLocations, currentCategoryItems);
                if (restoredItems != null)
                {
                    Debug.Log($"[WheelLayoutManager] 应用恢复布局到类别 {category}: {restoredItems.Count} 个位置");
                    ApplyRestoredLayout(category, restoredItems);
                }
            }

            Debug.Log("[WheelLayoutManager] ✓ 轮盘布局恢复成功");
            return true;
        }

        /// <summary>
        /// 🆕 确认历史布局与当前物品的匹配度（严格验证）
        /// 只有当历史布局中的所有物品都能在当前物品中找到时才返回true
        /// </summary>
        private bool ValidateLayoutMatch(WheelLayoutData savedLayoutData, Dictionary<ItemCategory, List<Item>> currentItems)
        {
            Debug.Log("[WheelLayoutManager] 🆕 确认历史布局与当前物品的匹配度...");

            foreach (var categoryLayout in savedLayoutData.categories)
            {
                if (!Enum.TryParse<ItemCategory>(categoryLayout.categoryName, out var category))
                {
                    Debug.LogWarning($"[WheelLayoutManager] 无法解析类别名称: {categoryLayout.categoryName}");
                    return false;
                }

                // 检查当前是否有这个类别的物品
                if (!currentItems.ContainsKey(category))
                {
                    Debug.Log($"[WheelLayoutManager] 当前没有类别 {category} 的物品，历史布局不匹配");
                    return false;
                }

                var currentCategoryItems = currentItems[category];
                var savedLocations = categoryLayout.itemLocations;

                // 统计历史布局中的非空物品数量
                int savedItemCount = savedLocations.Count(loc => !loc.IsEmpty && !loc.IsNull());

                // 严格验证：物品数量必须完全匹配
                if (savedItemCount != currentCategoryItems.Count)
                {
                    Debug.Log($"[WheelLayoutManager] 类别 {category} 物品数量不匹配: 历史{savedItemCount}，当前{currentCategoryItems.Count}");
                    return false;
                }

                // 确认每个保存的物品位置都能找到对应的当前物品
                var matchedItems = new HashSet<Item>();
                foreach (var savedLocation in savedLocations)
                {
                    if (savedLocation.IsEmpty || savedLocation.IsNull())
                    {
                        continue; // 跳过空位置
                    }

                    // 尝试在当前物品中找到匹配项
                    Item matchedItem = FindMatchingItem(savedLocation, currentCategoryItems);
                    if (matchedItem == null)
                    {
                        Debug.Log($"[WheelLayoutManager] 类别 {category} 中找不到匹配的物品 TypeID={savedLocation.TypeID}, 位置={savedLocation.Position}");
                        return false;
                    }

                    matchedItems.Add(matchedItem);
                }

                // 确认所有当前物品都被匹配
                if (matchedItems.Count != currentCategoryItems.Count)
                {
                    Debug.Log($"[WheelLayoutManager] 类别 {category} 中有未匹配的当前物品: 匹配{matchedItems.Count}，总计{currentCategoryItems.Count}");
                    return false;
                }

                Debug.Log($"[WheelLayoutManager] 类别 {category} 验证通过: {currentCategoryItems.Count} 个物品完全匹配");
            }

            Debug.Log("[WheelLayoutManager] 历史布局与当前物品完全匹配");
            return true;
        }

        /// <summary>
        /// 🆕 在当前物品列表中查找匹配的物品
        /// 使用类型ID作为主要匹配依据
        /// </summary>
        private Item FindMatchingItem(ItemLocation savedLocation, List<Item> currentItems)
        {
            foreach (var item in currentItems)
            {
                if (item.TypeID == savedLocation.TypeID)
                {
                    // 找到相同类型的物品
                    // 对于相同类型的多个物品，选择第一个匹配的
                    return item;
                }
            }
            return null;
        }

        /// <summary>
        /// 🆕 根据保存的位置信息恢复单个类别的布局
        /// </summary>
        private List<Item> RestoreCategoryLayout(ItemLocation[] savedLocations, List<Item> currentItems)
        {
            var restoredItems = new List<Item>();
            var usedItems = new HashSet<Item>();

            // 🆕 修复：限制恢复的槽数量最多为8个
            const int MAX_RESTORE_SLOTS = 8;

            // 根据保存的位置顺序恢复布局，最多恢复8个位置
            for (int i = 0; i < Math.Min(savedLocations.Length, MAX_RESTORE_SLOTS); i++)
            {
                var savedLocation = savedLocations[i];

                if (savedLocation.IsEmpty || savedLocation.IsNull())
                {
                    // 空位置，添加null
                    restoredItems.Add(null);
                    Debug.Log($"[WheelLayoutManager] 恢复空位置在索引 {i}");
                }
                else
                {
                    // 找到匹配的当前物品
                    Item matchedItem = FindMatchingItem(savedLocation, currentItems);
                    if (matchedItem != null && !usedItems.Contains(matchedItem))
                    {
                        restoredItems.Add(matchedItem);
                        usedItems.Add(matchedItem);
                        Debug.Log($"[WheelLayoutManager] 恢复物品: {matchedItem.DisplayName} 在索引 {i}");
                    }
                    else
                    {
                        // 没有找到匹配的物品，添加null
                        restoredItems.Add(null);
                        Debug.LogWarning($"[WheelLayoutManager] 索引 {i} 处找不到匹配的物品，添加空位");
                    }
                }
            }

            // 将未使用的物品添加到末尾（如果还有空隙）
            while (restoredItems.Count < MAX_RESTORE_SLOTS)
            {
                foreach (var item in currentItems)
                {
                    if (!usedItems.Contains(item) && restoredItems.Count < MAX_RESTORE_SLOTS)
                    {
                        restoredItems.Add(item);
                        usedItems.Add(item);
                        Debug.Log($"[WheelLayoutManager] 添加未匹配的物品到位置 {restoredItems.Count - 1}: {item.DisplayName}");
                    }
                }

                // 如果所有物品都已添加但还没到8个，添加null来填充
                if (restoredItems.Count < MAX_RESTORE_SLOTS)
                {
                    restoredItems.Add(null);
                    Debug.Log($"[WheelLayoutManager] 填充空位置到索引 {restoredItems.Count - 1}");
                }
            }

            Debug.Log($"[WheelLayoutManager] Layout restoration complete: {restoredItems.Count} slots (limit {MAX_RESTORE_SLOTS}).");
            return restoredItems;
        }

        /// <summary>
        /// 🆕 确认恢复结果的完整性
        /// 确保恢复的布局与当前物品完全对应
        /// </summary>
        private bool ValidateRestorationIntegrity(Dictionary<ItemCategory, List<Item>> restoredLayouts, Dictionary<ItemCategory, List<Item>> currentItems)
        {
            Debug.Log("[WheelLayoutManager] 🆕 确认恢复结果的完整性...");

            foreach (var kvp in currentItems)
            {
                var category = kvp.Key;
                var currentCategoryItems = kvp.Value;

                if (!restoredLayouts.ContainsKey(category))
                {
                    Debug.Log($"[WheelLayoutManager] 恢复的布局中没有类别 {category}");
                    return false;
                }

                var restoredCategoryItems = restoredLayouts[category];

                // 统计恢复布局中的非null物品
                var validRestoredItems = restoredCategoryItems.Where(item => item != null).ToList();

                if (validRestoredItems.Count != currentCategoryItems.Count)
                {
                    Debug.Log($"[WheelLayoutManager] 类别 {category} 恢复的物品数量不匹配: 恢复{validRestoredItems.Count}，当前{currentCategoryItems.Count}");
                    return false;
                }

                // 确认每个恢复的物品都在当前物品列表中
                foreach (var restoredItem in validRestoredItems)
                {
                    if (!currentCategoryItems.Contains(restoredItem))
                    {
                        Debug.Log($"[WheelLayoutManager] 类别 {category} 中恢复的物品不在当前物品列表中: {restoredItem.DisplayName}");
                        return false;
                    }
                }
            }

            Debug.Log("[WheelLayoutManager] ✓ 恢复结果完整性验证通过");
            return true;
        }

        /// <summary>
        /// 🆕 将恢复的布局直接应用到轮盘系统中
        /// 根据恢复的物品列表重新创建SimpleWheelSlot布局
        /// </summary>
        private void ApplyRestoredLayout(ItemCategory category, List<Item> restoredItems)
        {
            Debug.Log($"[WheelLayoutManager] 🆕 应用恢复布局到类别 {category}");

            var slots = GetOrCreateCategorySlots(category);
            const int REQUIRED_SLOTS = 8;

            // 🆕 修复：严格限制为8个槽位，即使restoredItems超过8个也只取前8个
            for (int i = 0; i < Math.Min(restoredItems.Count, REQUIRED_SLOTS); i++)
            {
                var item = restoredItems[i];
                if (item != null && !item.IsBeingDestroyed)
                {
                    // 恢复物品到槽位
                    slots[i].Item = item;
                    Debug.Log($"[WheelLayoutManager] 恢复物品槽位: {item.DisplayName} 在位置 {i}");
                }
                else
                {
                    // 清空槽位
                    slots[i].Item = null;
                    Debug.Log($"[WheelLayoutManager] 恢复空槽位在位置 {i}");
                }
            }

            // 清空剩余槽位
            for (int i = restoredItems.Count; i < REQUIRED_SLOTS; i++)
            {
                slots[i].Item = null;
                Debug.Log($"[WheelLayoutManager] 清空槽位在位置 {i}");
            }

            // 触发布局变更事件
            Debug.Log($"[WheelLayoutManager] 触发恢复布局变更事件: {category}");
            // 🗑️ 已删除：OnLayoutChanged事件调用
            // 轮盘直接更新UI，不需要事件通知

            Debug.Log($"[WheelLayoutManager] 已应用恢复布局到类别 {category}，总计 8 个格子");
        }

        #endregion
    }
}