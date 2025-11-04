using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Duckov;
using Duckov.Utilities;
using ItemStatsSystem;
using ItemStatsSystem.Items;

namespace Backpack_QuickWheel.ShortcutSystem
{
    /// <summary>
    /// 主背包轮盘数据管理器
    /// 基于主背包Inventory的数据源，实现轮盘与背包位置的双向同步
    /// 支持8个轮盘位置的智能插入算法
    /// </summary>
    public class MainBackpackWheelManager : IWheelDataManager
    {
        private readonly WheelLayoutManager _wheelLayoutManager;

        #region 核心数据结构

        // 主角色引用
        private CharacterMainControl _mainCharacter;

        // 主背包库存引用
        private Inventory _mainBackpackInventory;

        // 轮盘位置到背包位置的映射（8个轮盘位置）
        private int[] _wheelToBackpackPositions = new int[8];

        // 背包位置到轮盘位置的映射（反向查找）
        private Dictionary<int, int> _backpackToWheelPositions = new Dictionary<int, int>();

        // 事件订阅标识
        private bool _isSubscribedToBackpack = false;

        #endregion

        #region 构造和初始化

        public MainBackpackWheelManager(WheelLayoutManager wheelLayoutManager)
        {
            _wheelLayoutManager = wheelLayoutManager ?? throw new ArgumentNullException(nameof(wheelLayoutManager));
        }

        public void Initialize()
        {
            Debug.Log("MainBackpackWheelManager: 初始化主背包管理器");

            // 初始化映射关系
            Array.Fill(_wheelToBackpackPositions, -1);
            _backpackToWheelPositions.Clear();

            // 获取主角色引用
            _mainCharacter = CharacterMainControl.Main;
            if (_mainCharacter == null)
            {
                Debug.LogWarning("MainBackpackWheelManager: 无法获取主角色引用");
                return;
            }

            // 获取主背包引用
            _mainBackpackInventory = _mainCharacter.CharacterItem.Inventory;
            if (_mainBackpackInventory == null)
            {
                Debug.LogWarning("MainBackpackWheelManager: 无法获取主背包引用");
                return;
            }

            // 订阅主背包事件
            SubscribeToBackpackEvents();

            // 建立初始映射关系
            InitializeWheelMapping();

            Debug.Log("MainBackpackWheelManager: 主背包管理器初始化完成");
        }

        public void Shutdown()
        {
            Debug.Log("MainBackpackWheelManager: 关闭主背包管理器");

            // 取消事件订阅
            UnsubscribeFromBackpackEvents();

            // 清空轮盘显示
            _wheelLayoutManager.ClearAllCategories();

            // 清空映射关系
            Array.Fill(_wheelToBackpackPositions, -1);
            _backpackToWheelPositions.Clear();

            // 清空引用
            _mainBackpackInventory = null;
            _mainCharacter = null;

            Debug.Log("MainBackpackWheelManager: 主背包管理器已关闭");
        }

        #endregion

        #region IWheelDataManager 实现

        public void HandleBackpackChange(Item backpack)
        {
            Debug.Log($"MainBackpackWheelManager: 处理背包变化 -> {backpack?.DisplayName ?? "null"}");

            // 主背包模式不关心背包变化，始终使用主角色背包
            // 重新初始化映射关系
            RefreshWheelMapping();
        }

        public void OnGameStart()
        {
            Debug.Log("MainBackpackWheelManager: 游戏开始处理");

            // 重新获取角色和背包引用
            _mainCharacter = CharacterMainControl.Main;
            if (_mainCharacter != null)
            {
                _mainBackpackInventory = _mainCharacter.CharacterItem.Inventory;

                // 重新订阅事件
                UnsubscribeFromBackpackEvents();
                SubscribeToBackpackEvents();

                // 刷新轮盘映射
                RefreshWheelMapping();
            }
        }

        public string GetManagerType()
        {
            return "MainBackpackWheelManager";
        }

        #endregion

        #region 背包事件订阅

        /// <summary>
        /// 订阅主背包事件
        /// </summary>
        private void SubscribeToBackpackEvents()
        {
            if (_isSubscribedToBackpack || _mainBackpackInventory == null) return;

            Debug.Log("MainBackpackWheelManager: 订阅主背包事件");

            // 订阅主背包内容变化事件
            _mainBackpackInventory.onContentChanged += OnMainBackpackContentChanged;
            _isSubscribedToBackpack = true;

            Debug.Log("MainBackpackWheelManager: 已订阅主背包事件");
        }

        /// <summary>
        /// 取消订阅主背包事件
        /// </summary>
        private void UnsubscribeFromBackpackEvents()
        {
            if (!_isSubscribedToBackpack || _mainBackpackInventory == null) return;

            Debug.Log("MainBackpackWheelManager: 取消订阅主背包事件");

            _mainBackpackInventory.onContentChanged -= OnMainBackpackContentChanged;
            _isSubscribedToBackpack = false;

            Debug.Log("MainBackpackWheelManager: 已取消订阅主背包事件");
        }

        #endregion

        #region 背包事件处理

        /// <summary>
        /// 主背包内容变化事件处理器
        /// </summary>
        private void OnMainBackpackContentChanged(Inventory inventory, int changedSlot)
        {
            if (_isSubscribedToBackpack)
            {
                Debug.Log($"MainBackpackWheelManager: 主背包内容发生变化，槽位: {changedSlot}，刷新轮盘映射");

                // TODO: 实现增量更新优化
                // 当前使用全量刷新，后续可以根据changedSlot实现增量更新：
                // 1. 如果changedSlot在轮盘映射中，只更新该槽位
                // 2. 如果changedSlot不在映射中，检查是否需要插入新物品
                // 3. 处理物品移除时的映射更新
                // 4. 优化性能，避免不必要的全量刷新

                RefreshWheelMapping();
            }
        }

        #endregion

        #region 轮盘映射管理

        /// <summary>
        /// 初始化轮盘映射关系
        /// </summary>
        private void InitializeWheelMapping()
        {
            Debug.Log("MainBackpackWheelManager: 开始初始化轮盘映射关系");

            // 清空现有映射
            Array.Fill(_wheelToBackpackPositions, -1);
            _backpackToWheelPositions.Clear();

            // 清空轮盘显示
            _wheelLayoutManager.ClearAllCategories();

            if (_mainBackpackInventory == null)
            {
                Debug.LogWarning("MainBackpackWheelManager: 主背包为空，无法初始化映射");
                return;
            }

            int wheelPosition = 0;

            // 遍历主背包，按顺序建立映射
            for (int backpackPosition = 0; backpackPosition < _mainBackpackInventory.Content.Count; backpackPosition++)
            {
                if (wheelPosition >= 8) break; // 轮盘只有8个位置

                var item = _mainBackpackInventory.GetItemAt(backpackPosition);
                if (item != null && IsShortcutItem(item))
                {
                    // 建立映射关系
                    _wheelToBackpackPositions[wheelPosition] = backpackPosition;
                    _backpackToWheelPositions[backpackPosition] = wheelPosition;

                    // 添加到轮盘显示
                    var category = ItemCategorizer.CategorizeItem(item);
                    if (category != ItemCategory.None)
                    {
                        _wheelLayoutManager.AddItemToCategory(category, item);
                        Debug.Log($"MainBackpackWheelManager: 建立映射 轮盘[{wheelPosition}] <-> 背包[{backpackPosition}] 物品: {item.DisplayName} ({category})");
                    }

                    wheelPosition++;
                }
            }

            Debug.Log($"MainBackpackWheelManager: 初始化完成，建立了 {wheelPosition} 个映射关系");
        }

        /// <summary>
        /// 刷新轮盘映射关系
        /// </summary>
        private void RefreshWheelMapping()
        {
            Debug.Log("MainBackpackWheelManager: 开始刷新轮盘映射");

            // 保存当前选中状态
            var currentSelections = new Dictionary<ItemCategory, Item>();
            foreach (ItemCategory category in Enum.GetValues(typeof(ItemCategory)))
            {
                if (category != ItemCategory.None)
                {
                    var selectedItem = _wheelLayoutManager.GetSelectedItem(category);
                    if (selectedItem != null)
                    {
                        currentSelections[category] = selectedItem;
                    }
                }
            }

            // 清空轮盘显示
            _wheelLayoutManager.ClearAllCategories();

            // 重新建立映射
            var newBackpackToWheel = new Dictionary<int, int>();
            var newWheelToBackpack = new int[8];
            Array.Fill(newWheelToBackpack, -1);

            int wheelPosition = 0;

            // 重新遍历主背包
            for (int backpackPosition = 0; backpackPosition < _mainBackpackInventory.Content.Count; backpackPosition++)
            {
                if (wheelPosition >= 8) break;

                var item = _mainBackpackInventory.GetItemAt(backpackPosition);
                if (item != null && IsShortcutItem(item))
                {
                    // 建立新映射
                    newWheelToBackpack[wheelPosition] = backpackPosition;
                    newBackpackToWheel[backpackPosition] = wheelPosition;

                    // 添加到轮盘显示
                    var category = ItemCategorizer.CategorizeItem(item);
                    if (category != ItemCategory.None)
                    {
                        _wheelLayoutManager.AddItemToCategory(category, item);
                    }

                    wheelPosition++;
                }
            }

            // 更新映射关系
            _wheelToBackpackPositions = newWheelToBackpack;
            _backpackToWheelPositions = newBackpackToWheel;

            Debug.Log($"MainBackpackWheelManager: 刷新完成，建立了 {wheelPosition} 个映射关系");
        }

        #endregion

        #region 智能插入算法

        /// <summary>
        /// 在指定背包位置插入物品到轮盘
        /// </summary>
        private void InsertItemAt(int backpackPosition, Item item)
        {
            // 第1步：找到新物品应该插入的轮盘位置
            int insertWheelPos = FindInsertWheelPosition(backpackPosition);

            if (insertWheelPos == -1)
            {
                Debug.LogWarning("MainBackpackWheelManager: 轮盘已满，无法插入新物品");
                return;
            }

            Debug.Log($"MainBackpackWheelManager: 在轮盘位置 {insertWheelPos} 插入物品 {item.DisplayName}");

            // 第2步：从插入位置开始，将后面的轮盘物品向后移动
            ShiftWheelItemsBackward(insertWheelPos);

            // 第3步：在插入位置放入新物品
            _wheelToBackpackPositions[insertWheelPos] = backpackPosition;
            _backpackToWheelPositions[backpackPosition] = insertWheelPos;

            // 第4步：更新轮盘UI
            var category = ItemCategorizer.CategorizeItem(item);
            if (category != ItemCategory.None)
            {
                _wheelLayoutManager.AddItemToCategory(category, item);
            }
        }

        /// <summary>
        /// 查找新物品应该插入的轮盘位置
        /// </summary>
        private int FindInsertWheelPosition(int backpackPosition)
        {
            for (int wheelPos = 0; wheelPos < 8; wheelPos++)
            {
                int existingBackpackPos = _wheelToBackpackPositions[wheelPos];

                // 情况1：轮盘位置为空，可以插入
                if (existingBackpackPos == -1) return wheelPos;

                // 情况2：现有物品的背包位置大于新物品位置，应该插入在此位置之前
                if (existingBackpackPos > backpackPosition) return wheelPos;
            }

            return -1; // 轮盘已满
        }

        /// <summary>
        /// 向后移动轮盘物品
        /// </summary>
        private void ShiftWheelItemsBackward(int fromWheelPosition)
        {
            // 从后向前，将轮盘物品向后移动一位
            for (int wheelPos = 7; wheelPos > fromWheelPosition; wheelPos--)
            {
                int currentBackpackPos = _wheelToBackpackPositions[wheelPos - 1];

                if (currentBackpackPos != -1)
                {
                    // 将物品从轮盘位置 wheelPos-1 移动到 wheelPos
                    _wheelToBackpackPositions[wheelPos] = currentBackpackPos;
                    _backpackToWheelPositions[currentBackpackPos] = wheelPos;

                    // 更新UI：移动对应的轮盘物品
                    var item = _mainBackpackInventory.GetItemAt(currentBackpackPos);
                    if (item != null)
                    {
                        var category = ItemCategorizer.CategorizeItem(item);
                        if (category != ItemCategory.None)
                        {
                            _wheelLayoutManager.AddItemToCategory(category, item);
                        }
                    }
                }
            }

            // 清空原始插入位置（将被新物品占用）
            _wheelToBackpackPositions[fromWheelPosition] = -1;
        }

        #endregion

        #region 位置同步功能

        /// <summary>
        /// 调整轮盘物品位置
        /// 在轮盘中调整物品位置时，同步调整背包中的位置
        /// </summary>
        public void AdjustWheelPosition(int fromWheelPos, int toWheelPos)
        {
            if (fromWheelPos < 0 || fromWheelPos >= 8 || toWheelPos < 0 || toWheelPos >= 8)
            {
                Debug.LogWarning($"MainBackpackWheelManager: 无效的轮盘位置: from={fromWheelPos}, to={toWheelPos}");
                return;
            }

            int fromBackpackPos = _wheelToBackpackPositions[fromWheelPos];
            int toBackpackPos = _wheelToBackpackPositions[toWheelPos];

            if (fromBackpackPos == -1)
            {
                Debug.LogWarning($"MainBackpackWheelManager: 源轮盘位置 {fromWheelPos} 为空");
                return;
            }

            var item = _mainBackpackInventory.GetItemAt(fromBackpackPos);
            if (item == null)
            {
                Debug.LogWarning($"MainBackpackWheelManager: 源背包位置 {fromBackpackPos} 无物品");
                return;
            }

            Debug.Log($"MainBackpackWheelManager: 调整轮盘位置 {fromWheelPos} -> {toWheelPos}，同步背包位置 {fromBackpackPos} -> {toBackpackPos}");

            try
            {
                // 交换背包中的物品位置
                if (toBackpackPos != -1)
                {
                    // 目标位置有物品，交换
                    var targetItem = _mainBackpackInventory.GetItemAt(toBackpackPos);
                    if (targetItem != null)
                    {
                        _mainBackpackInventory.AddAt(targetItem, fromBackpackPos);
                    }
                }

                _mainBackpackInventory.AddAt(item, toBackpackPos);

                // 更新映射关系
                _wheelToBackpackPositions[fromWheelPos] = toBackpackPos;
                _wheelToBackpackPositions[toWheelPos] = fromBackpackPos;

                if (toBackpackPos != -1)
                {
                    _backpackToWheelPositions[toBackpackPos] = fromWheelPos;
                }
                _backpackToWheelPositions[fromBackpackPos] = toWheelPos;

                Debug.Log("MainBackpackWheelManager: 位置同步完成");
            }
            catch (Exception ex)
            {
                Debug.LogError($"MainBackpackWheelManager: 位置同步失败: {ex.Message}");
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 检查物品是否为快捷键物品
        /// </summary>
        private bool IsShortcutItem(Item item)
        {
            if (item == null) return false;

            var category = ItemCategorizer.CategorizeItem(item);
            return category != ItemCategory.None;
        }

        /// <summary>
        /// 获取轮盘位置对应的背包位置
        /// </summary>
        public int GetBackpackPosition(int wheelPosition)
        {
            if (wheelPosition < 0 || wheelPosition >= 8) return -1;
            return _wheelToBackpackPositions[wheelPosition];
        }

        /// <summary>
        /// 获取背包位置对应的轮盘位置
        /// </summary>
        public int GetWheelPosition(int backpackPosition)
        {
            return _backpackToWheelPositions.TryGetValue(backpackPosition, out int wheelPos) ? wheelPos : -1;
        }

        #endregion
    }
}