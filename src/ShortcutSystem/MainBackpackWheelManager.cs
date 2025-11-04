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

        // 🔧 性能优化：缓存枚举值，避免重复计算
        private static readonly ItemCategory[] AllCategories = Enum.GetValues(typeof(ItemCategory))
            .Cast<ItemCategory>()
            .Where(c => c != ItemCategory.None)
            .ToArray();

        #region 核心数据结构

        // 主角色引用
        private CharacterMainControl _mainCharacter;

        // 主背包库存引用
        private Inventory _mainBackpackInventory;

        // 🆕 按类别分别保存的轮盘映射关系（每个类别8个位置）
        private Dictionary<ItemCategory, int[]> _categoryToWheelPositions = new Dictionary<ItemCategory, int[]>();

        // 🆕 按类别分别保存的反向映射（背包位置 -> 轮盘位置）
        private Dictionary<ItemCategory, Dictionary<int, int>> _categoryToBackpackPositions = new Dictionary<ItemCategory, Dictionary<int, int>>();

  
        // 事件订阅标识
        private bool _isSubscribedToBackpack = false;

        // 🆕 交换过程中禁用事件监听标志
        private bool _isPerformingSwap = false;

        // 🆕 物品移动跟踪状态（用于简化算法）
        private Item _movingItem = null;
        private int _movingFromIndex = -1;
        private int _movingToIndex = -1;

        #endregion

        #region 构造和初始化

        public MainBackpackWheelManager(WheelLayoutManager wheelLayoutManager)
        {
            _wheelLayoutManager = wheelLayoutManager ?? throw new ArgumentNullException(nameof(wheelLayoutManager));

            // 🆕 初始化按类别的映射结构
            InitializeCategoryMappings();
        }

        /// <summary>
        /// 🆕 初始化按类别的映射结构
        /// 为每个类别创建独立的8位置轮盘映射
        /// </summary>
        private void InitializeCategoryMappings()
        {
            foreach (ItemCategory category in AllCategories)
            {
                // 为每个类别创建8个轮盘位置的映射，初始为-1（空）
                _categoryToWheelPositions[category] = new int[8];
                Array.Fill(_categoryToWheelPositions[category], -1);

                // 为每个类别创建反向映射
                _categoryToBackpackPositions[category] = new Dictionary<int, int>();
            }
            Debug.Log("MainBackpackWheelManager: 按类别映射结构初始化完成");
        }

        #region 🔧 工具方法

        /// <summary>
        /// 🔧 验证类别有效性
        /// </summary>
        private bool ValidateCategory(ItemCategory category, [System.Runtime.CompilerServices.CallerMemberName] string methodName = "")
        {
            if (category == ItemCategory.None)
            {
                Debug.LogWarning($"MainBackpackWheelManager: {methodName} - 无效的类别");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 🔧 获取指定类别的映射数据（统一验证）
        /// </summary>
        private bool GetCategoryMappings(ItemCategory category, out int[] wheelMapping, out Dictionary<int, int> backpackMapping)
        {
            wheelMapping = GetCategoryWheelMapping(category);
            backpackMapping = GetCategoryBackpackMapping(category);

            if (wheelMapping == null || backpackMapping == null)
            {
                Debug.LogWarning($"MainBackpackWheelManager: {category}类别的映射未初始化");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 🔧 验证轮盘位置有效性
        /// </summary>
        private bool ValidateWheelPosition(int position)
        {
            return position >= 0 && position < 8;
        }

        /// <summary>
        /// 🔧 验证背包位置有效性
        /// </summary>
        private bool ValidateBackpackPosition(int position)
        {
            if (_mainBackpackInventory == null) return false;
            return position >= 0 && position < _mainBackpackInventory.Content.Count;
        }

        /// <summary>
        /// 🔧 处理物品移除情况 - 查找并移除该物品在所有类别轮盘中的位置
        /// </summary>
        private void HandleItemRemoval(int changedSlot)
        {
            Debug.Log($"MainBackpackWheelManager: 检查物品移除情况，遍历所有类别");

            foreach (var category in AllCategories)
            {
                if (_categoryToBackpackPositions[category].TryGetValue(changedSlot, out int wheelPos))
                {
                    Debug.Log($"MainBackpackWheelManager: 从{category}类别移除轮盘槽位{wheelPos}的物品");
                    RemoveFromWheelSlot(category, wheelPos, changedSlot);
                    break; // 找到了就停止
                }
            }
        }

        /// <summary>
        /// 🔧 处理已存在轮盘槽位的更新
        /// </summary>
        private void HandleExistingSlotUpdate(int changedSlot, ItemCategory itemCategory, int existingWheelPos, Item newItem, bool newItemIsShortcut)
        {
            if (newItemIsShortcut && itemCategory != ItemCategory.None)
            {
                // 原有轮盘槽位现在有新的快捷物品（可能类别变化了）
                Debug.Log($"MainBackpackWheelManager: 更新{itemCategory}类别轮盘槽位{existingWheelPos}的物品为 {newItem.DisplayName}");

                // 🆕 简化逻辑：直接更新映射关系，不进行复杂的重排序
                var categoryWheelMapping = GetCategoryWheelMapping(itemCategory);
                var categoryBackpackMapping = GetCategoryBackpackMapping(itemCategory);

                if (categoryWheelMapping != null && categoryBackpackMapping != null)
                {
                    // 🔧 关键修复：先清理旧位置的映射关系
                    int oldBackpackPos = categoryWheelMapping[existingWheelPos];
                    if (oldBackpackPos != -1 && oldBackpackPos != changedSlot)
                    {
                        categoryBackpackMapping.Remove(oldBackpackPos);
                        Debug.Log($"MainBackpackWheelManager: 清理旧映射关系 轮盘{existingWheelPos} -> 背包{oldBackpackPos}");
                    }

                    // 🔧 更新为新的映射关系
                    categoryWheelMapping[existingWheelPos] = changedSlot;
                    categoryBackpackMapping[changedSlot] = existingWheelPos;

                    Debug.Log($"MainBackpackWheelManager: 直接更新映射关系 轮盘{existingWheelPos} -> 背包{changedSlot}");

                    // 🔧 关键修复：重新生成整个轮盘布局，确保物品顺序与映射一致
                    // 原来的UpdateWheelSlot使用AddItemToCategory会找空槽位插入，导致顺序错误
                    // RegenerateCategoryWheelFromMappings会根据映射重建_wheelSlots数组
                    RegenerateCategoryWheelFromMappings(itemCategory);
                    Debug.Log($"MainBackpackWheelManager: 已重新生成{itemCategory}类别轮盘布局");
                }
            }
            else
            {
                // 原有轮盘槽位现在没有快捷物品（物品被移除或替换为非快捷物品）
                Debug.Log($"MainBackpackWheelManager: 移除{itemCategory}类别轮盘槽位{existingWheelPos}的物品");
                RemoveFromWheelSlot(itemCategory, existingWheelPos, changedSlot);
            }
        }

        /// <summary>
        /// 🔧 处理新物品插入到轮盘
        /// </summary>
        private void HandleNewItemInsertion(int changedSlot, ItemCategory itemCategory, Item newItem, bool newItemIsShortcut)
        {
            if (newItemIsShortcut && itemCategory != ItemCategory.None)
            {
                // 🆕 简化逻辑：检查该物品是否已经在其他轮盘位置，如果已存在则只更新映射
                var categoryWheelMapping = GetCategoryWheelMapping(itemCategory);
                var categoryBackpackMapping = GetCategoryBackpackMapping(itemCategory);

                if (categoryWheelMapping != null && categoryBackpackMapping != null)
                {
                    // 🔧 检查该物品是否已经在当前类别的其他轮盘位置
                    for (int wheelPos = 0; wheelPos < 8; wheelPos++)
                    {
                        int existingBackpackPos = categoryWheelMapping[wheelPos];
                        if (existingBackpackPos != -1)
                        {
                            var existingItem = _mainBackpackInventory.GetItemAt(existingBackpackPos);
                            if (existingItem != null && existingItem.Equals(newItem))
                            {
                                // 物品已存在于轮盘中，直接更新映射关系即可
                                Debug.Log($"MainBackpackWheelManager: 物品 {newItem.DisplayName} 已存在于轮盘位置{wheelPos}，更新映射为背包位置{changedSlot}");

                                // 清理旧映射
                                categoryBackpackMapping.Remove(existingBackpackPos);

                                // 更新为新映射
                                categoryWheelMapping[wheelPos] = changedSlot;
                                categoryBackpackMapping[changedSlot] = wheelPos;

                                // 更新UI显示
                                UpdateWheelSlot(itemCategory, wheelPos, changedSlot, newItem);
                                return;
                            }
                        }
                    }

                    // 物品不在轮盘中，先插入到映射，然后重新生成整个轮盘布局
                    Debug.Log($"MainBackpackWheelManager: 物品 {newItem.DisplayName} 不在轮盘中，先建立映射再重新生成布局");

                    // 查找空轮盘位置
                    int emptyWheelPos = -1;
                    for (int i = 0; i < 8; i++)
                    {
                        if (categoryWheelMapping[i] == -1)
                        {
                            emptyWheelPos = i;
                            break;
                        }
                    }

                    if (emptyWheelPos != -1)
                    {
                        // 建立映射关系
                        categoryWheelMapping[emptyWheelPos] = changedSlot;
                        categoryBackpackMapping[changedSlot] = emptyWheelPos;

                        // 🔥 方案B：直接调用排序方法，按背包位置重新排列
                        // ResortCategoryWheel 会自动排序映射表并调用 SyncFromMapping 同步槽位
                        Debug.Log($"MainBackpackWheelManager: 物品插入后，触发{itemCategory}类别轮盘重新排序");
                        ResortCategoryWheel(itemCategory);
                    }
                    else
                    {
                        Debug.LogWarning($"MainBackpackWheelManager: {itemCategory}类别轮盘已满，无法插入物品 {newItem.DisplayName}");
                    }
                }
            }
            // 非轮盘槽位的非快捷物品变化，无需处理
        }

        /// <summary>
        /// 🔥 重新排序指定类别的轮盘（按背包位置从小到大）- 新架构
        /// 当物品背包位置发生变化时调用，确保轮盘显示顺序与背包位置一致
        /// </summary>
        private void ResortCategoryWheel(ItemCategory category)
        {
            if (!ValidateCategory(category)) return;

            if (!GetCategoryMappings(category, out var categoryWheelMapping, out var categoryBackpackMapping))
            {
                return;
            }

            // 收集当前类别的所有轮盘物品和对应的背包位置
            var wheelItems = new List<(int wheelPos, int backpackPos, Item item)>();

            for (int wheelPos = 0; wheelPos < 8; wheelPos++)
            {
                int backpackPos = categoryWheelMapping[wheelPos];
                if (backpackPos != -1)
                {
                    var item = _mainBackpackInventory.GetItemAt(backpackPos);
                    if (item != null && IsShortcutItem(item))
                    {
                        wheelItems.Add((wheelPos, backpackPos, item));
                    }
                }
            }

            if (wheelItems.Count == 0)
            {
                Debug.Log($"MainBackpackWheelManager: {category}类别轮盘为空，跳过重排序");
                return;
            }

            // 按背包位置从小到大排序
            wheelItems.Sort((a, b) => a.backpackPos.CompareTo(b.backpackPos));

            Debug.Log($"MainBackpackWheelManager: 🔄 重新排序{category}类别轮盘，按背包位置: {string.Join(", ", wheelItems.Select(x => $"{x.item.DisplayName}({x.backpackPos})"))}");

            // 清空当前映射
            Array.Fill(categoryWheelMapping, -1);
            categoryBackpackMapping.Clear();

            // 按新顺序重新建立映射
            for (int i = 0; i < wheelItems.Count && i < 8; i++)
            {
                var (oldWheelPos, backpackPos, item) = wheelItems[i];

                // 更新映射关系
                categoryWheelMapping[i] = backpackPos;
                categoryBackpackMapping[backpackPos] = i;

                Debug.Log($"MainBackpackWheelManager: 重新排序 - {item.DisplayName} 从轮盘位置{oldWheelPos}移动到位置{i}，背包位置{backpackPos}");
            }

            // 🔥 新架构：使用 SyncFromMapping 统一同步槽位数据
            // 这会自动标记Dirty Flag并更新UI
            _wheelLayoutManager.SyncFromMapping(
                category,
                categoryWheelMapping,
                (backpackPos) => _mainBackpackInventory.GetItemAt(backpackPos)
            );

            Debug.Log($"MainBackpackWheelManager: ✓ {category}类别轮盘重新排序完成");
        }

        #endregion

        /// <summary>
        /// 🆕 获取指定类别的轮盘到背包映射数组
        /// </summary>
        private int[] GetCategoryWheelMapping(ItemCategory category)
        {
            return _categoryToWheelPositions.TryGetValue(category, out var mapping) ? mapping : null;
        }

        /// <summary>
        /// 🆕 获取指定类别的背包到轮盘反向映射
        /// </summary>
        private Dictionary<int, int> GetCategoryBackpackMapping(ItemCategory category)
        {
            return _categoryToBackpackPositions.TryGetValue(category, out var mapping) ? mapping : null;
        }

        /// <summary>
        /// 🆕 根据背包位置获取物品类别
        /// 用于增量更新时确定需要更新哪个类别的映射
        /// </summary>
        /// <param name="backpackPosition">背包位置</param>
        /// <returns>物品类别，如果物品不存在或不是快捷物品则返回None</returns>
        private ItemCategory GetItemCategoryAtBackpackPosition(int backpackPosition)
        {
            var item = _mainBackpackInventory.GetItemAt(backpackPosition);
            if (item != null && IsShortcutItem(item))
            {
                return ItemCategorizer.CategorizeItem(item);
            }
            return ItemCategory.None;
        }

        public void Initialize()
        {
            Debug.Log("MainBackpackWheelManager: 初始化主背包管理器");

  
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

            // 🆕 订阅轮盘槽位交换事件
            WheelLayoutManager.OnSlotsSwapped += OnWheelSlotsSwapped;
            Debug.Log("MainBackpackWheelManager: 已订阅轮盘槽位交换事件");

            // 建立初始映射关系
            InitializeWheelMapping();

            Debug.Log("MainBackpackWheelManager: 主背包管理器初始化完成");
        }

        public void Shutdown()
        {
            Debug.Log("MainBackpackWheelManager: 关闭主背包管理器");

            // 取消事件订阅
            UnsubscribeFromBackpackEvents();

            // 🆕 取消订阅轮盘槽位交换事件
            WheelLayoutManager.OnSlotsSwapped -= OnWheelSlotsSwapped;
            Debug.Log("MainBackpackWheelManager: 已取消订阅轮盘槽位交换事件");

            // 清空轮盘显示
            _wheelLayoutManager.ClearAllCategories();

            // 🆕 清空所有类别的映射关系
            foreach (var category in AllCategories)
            {
                Array.Fill(_categoryToWheelPositions[category], -1);
                _categoryToBackpackPositions[category].Clear();
            }

            // 清空引用
            _mainBackpackInventory = null;
            _mainCharacter = null;

            Debug.Log("MainBackpackWheelManager: 主背包管理器已关闭");
        }

        #endregion

        #region IWheelDataManager 实现

        public void HandleBackpackChange(Item backpack)
        {
            // 🚨 醒目日志：追踪HandleBackpackChange的触发时机
            Debug.LogWarning($"🎒 HandleBackpackChange 被触发！新背包: {backpack?.DisplayName ?? "null"}");

            // 🆕 获取调用堆栈信息，帮助分析触发来源
            var stackTrace = System.Environment.StackTrace;
            Debug.LogWarning($"🎒 HandleBackpackChange 调用堆栈:\n{stackTrace}");

            // 🆕 检查是否已正确初始化
            if (_mainBackpackInventory == null)
            {
                Debug.LogWarning("MainBackpackWheelManager: 主背包引用为空，跳过背包变化处理");
                return;
            }

            // 主背包模式不关心背包变化，始终使用主角色背包
            // 重新初始化映射关系
            Debug.LogWarning($"🎒 即将调用 InitializeWheelMapping() - 当前时间: {System.DateTime.Now:HH:mm:ss.fff}");
            InitializeWheelMapping();
            Debug.LogWarning($"🎒 InitializeWheelMapping() 完成 - 当前时间: {System.DateTime.Now:HH:mm:ss.fff}");
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
                InitializeWheelMapping();
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
        /// 🆕 简化算法：直接处理物品移动，避免复杂的状态搜索
        /// </summary>
        private void OnMainBackpackContentChanged(Inventory inventory, int changedSlot)
        {
            // 🆕 在交换过程中跳过处理，避免逻辑冲突
            if (_isPerformingSwap)
            {
                Debug.Log($"MainBackpackWheelManager: 交换过程中，跳过槽位 {changedSlot} 的更新");
                return;
            }

            if (_isSubscribedToBackpack)
            {
                // 🔍 调试：详细记录事件触发情况
                var currentItem = _mainBackpackInventory.GetItemAt(changedSlot);
                Debug.LogWarning($"🚨 EVENT_TRIGGER - 槽位: {changedSlot}, 物品: {currentItem?.DisplayName ?? "空"}, 时间: {System.DateTime.Now:HH:mm:ss.fff}");
                Debug.Log($"MainBackpackWheelManager: 主背包内容发生变化，槽位: {changedSlot}，执行简化算法");
                Debug.Log($"MainBackpackWheelManager: 当前状态 - movingItem: {_movingItem?.DisplayName ?? "null"}, fromIndex: {_movingFromIndex}, toIndex: {_movingToIndex}");

                // 🆕 简化算法：跟踪物品移动状态
                TrackItemMovement(changedSlot);
            }
        }

        /// <summary>
        /// 🆕 简化：使用现有缓存直接处理物品移动
        /// 终点 = 事件触发的位置，起点 = 从缓存中查找物品的旧位置
        /// </summary>
        private void TrackItemMovement(int changedSlot)
        {
            var currentItem = _mainBackpackInventory.GetItemAt(changedSlot);
            Debug.LogWarning($"🎯 SIMPLE_TRACKING - 槽位: {changedSlot}, 物品: {currentItem?.DisplayName ?? "空"}");

            if (currentItem != null && IsShortcutItem(currentItem))
            {
                // 物品出现在新位置，查找旧位置
                int oldPos = FindItemInCache(currentItem);

                if (oldPos != -1)
                {
                    // 找到旧位置，这是移动
                    Debug.Log($"物品移动: {currentItem.DisplayName} 从{oldPos}到{changedSlot}");
                    var itemCategory = ItemCategorizer.CategorizeItem(currentItem);
                    // 调用现有的移动处理方法：模拟oldPos位置变空，changedSlot位置获得物品
                    HandleItemRemoval(oldPos);
                    HandleNewItemInsertion(changedSlot, itemCategory, currentItem, true);
                }
                else
                {
                    // 新物品，插入到轮盘
                    Debug.Log($"新物品: {currentItem.DisplayName} 在位置{changedSlot}");
                    // 调用现有的插入方法
                    HandleNewItemInsertion(changedSlot, ItemCategorizer.CategorizeItem(currentItem), currentItem, true);
                }
            }
            else
            {
                // 位置变空，处理物品移除
                Debug.Log($"位置{changedSlot}变空");
                HandleItemRemoval(changedSlot);
            }
        }

        /// <summary>
        /// 在缓存中查找物品的旧位置
        /// </summary>
        private int FindItemInCache(Item item)
        {
            foreach (var category in AllCategories)
            {
                var mapping = GetCategoryBackpackMapping(category);
                if (mapping != null)
                {
                    foreach (var kvp in mapping)
                    {
                        if (kvp.Value != -1)
                        {
                            var itemAtPos = _mainBackpackInventory.GetItemAt(kvp.Key);
                            if (itemAtPos != null && itemAtPos.Equals(item))
                            {
                                return kvp.Key; // 找到旧位置
                            }
                        }
                    }
                }
            }
            return -1; // 没找到
        }

        /// <summary>
        /// 🆕 简化算法核心：处理物品移动
        /// 根据用户提出的算法：缓存物品 → 分场景处理 → 更新轮盘
        /// </summary>
        private void ProcessItemMove()
        {
            if (_movingFromIndex == -1 || _movingToIndex == -1 || _movingItem == null)
            {
                Debug.LogWarning("MainBackpackWheelManager: 物品移动状态不完整，跳过处理");
                return;
            }

            // 获取目标位置的物品
            var itemAtTarget = _mainBackpackInventory.GetItemAt(_movingToIndex);

            Debug.Log($"MainBackpackWheelManager: 处理物品移动 - {_movingItem.DisplayName} 从位置{_movingFromIndex} 到位置{_movingToIndex}");
            Debug.Log($"MainBackpackWheelManager: 目标位置物品: {itemAtTarget?.DisplayName ?? "空"}");

            if (itemAtTarget == null)
            {
                // 场景1：移动到空格子
                Debug.Log("MainBackpackWheelManager: 场景1 - 移动到空格子");
                HandleMoveToEmptySlot(_movingItem, _movingFromIndex, _movingToIndex);
            }
            else
            {
                // 场景2：交换位置
                Debug.Log($"MainBackpackWheelManager: 场景2 - 交换位置，与物品 {itemAtTarget.DisplayName}");
                HandleItemSwap(_movingItem, _movingFromIndex, itemAtTarget, _movingToIndex);
            }
        }

        /// <summary>
        /// 🆕 场景1：处理移动到空格子
        /// </summary>
        private void HandleMoveToEmptySlot(Item item, int oldPos, int newPos)
        {
            var itemCategory = ItemCategorizer.CategorizeItem(item);

            if (itemCategory == ItemCategory.None || !IsShortcutItem(item))
            {
                Debug.Log($"MainBackpackWheelManager: 物品 {item.DisplayName} 不属于我们的分类，跳过处理");
                return;
            }

            Debug.Log($"MainBackpackWheelManager: 处理移动到空格子 - {item.DisplayName} ({itemCategory}) 从位置{oldPos} -> {newPos}");

            // 更新该物品对应类别的轮盘布局
            UpdateWheelLayoutForItem(item, oldPos, newPos, itemCategory);

            // 更新快捷键
            UpdateShortcutsForCategory(itemCategory);
        }

        /// <summary>
        /// 🆕 场景2：处理物品交换
        /// </summary>
        private void HandleItemSwap(Item item1, int pos1, Item item2, int pos2)
        {
            Debug.Log($"MainBackpackWheelManager: 处理物品交换 - {item1.DisplayName}(位置{pos1}) <-> {item2.DisplayName}(位置{pos2})");

            bool needUpdateShortcuts = false;

            // 处理item1
            var category1 = ItemCategorizer.CategorizeItem(item1);
            if (category1 != ItemCategory.None && IsShortcutItem(item1))
            {
                Debug.Log($"MainBackpackWheelManager: 更新item1 {item1.DisplayName} ({category1}) 从位置{pos1} -> {pos2}");
                UpdateWheelLayoutForItem(item1, pos1, pos2, category1);
                needUpdateShortcuts = true;
            }

            // 处理item2
            var category2 = ItemCategorizer.CategorizeItem(item2);
            if (category2 != ItemCategory.None && IsShortcutItem(item2))
            {
                Debug.Log($"MainBackpackWheelManager: 更新item2 {item2.DisplayName} ({category2}) 从位置{pos2} -> {pos1}");
                UpdateWheelLayoutForItem(item2, pos2, pos1, category2);
                needUpdateShortcuts = true;
            }

            // 更新受影响的类别的快捷键
            if (needUpdateShortcuts)
            {
                if (category1 != ItemCategory.None) UpdateShortcutsForCategory(category1);
                if (category2 != ItemCategory.None && category2 != category1) UpdateShortcutsForCategory(category2);
            }
        }

        /// <summary>
        /// 🆕 更新指定物品在轮盘布局中的位置
        /// </summary>
        private void UpdateWheelLayoutForItem(Item item, int oldPos, int newPos, ItemCategory category)
        {
            var categoryBackpackMapping = GetCategoryBackpackMapping(category);
            var categoryWheelMapping = GetCategoryWheelMapping(category);

            if (categoryBackpackMapping == null || categoryWheelMapping == null)
            {
                Debug.LogWarning($"MainBackpackWheelManager: {category}类别的映射未初始化");
                return;
            }

            // 查找该物品在轮盘中的位置
            if (categoryBackpackMapping.TryGetValue(oldPos, out int wheelPos))
            {
                // 找到了！更新映射关系
                Debug.Log($"MainBackpackWheelManager: 更新映射 - {category}类别 轮盘位置{wheelPos}: 背包位置{oldPos} -> {newPos}");

                // 更新映射关系
                categoryBackpackMapping.Remove(oldPos);
                categoryBackpackMapping[newPos] = wheelPos;
                categoryWheelMapping[wheelPos] = newPos;

                // 更新轮盘UI显示
                UpdateWheelSlot(category, wheelPos, newPos, item);
            }
            else
            {
                // 没找到，可能是新物品或需要插入
                Debug.Log($"MainBackpackWheelManager: 物品 {item.DisplayName} 在轮盘中未找到，执行插入操作");
                InsertNewItemToWheel(category, newPos, item);
            }
        }

        /// <summary>
        /// 🆕 更新指定类别的快捷键显示
        /// </summary>
        private void UpdateShortcutsForCategory(ItemCategory category)
        {
            // 这里可以调用现有的快捷键更新逻辑
            Debug.Log($"MainBackpackWheelManager: 更新{category}类别的快捷键显示");
            // 具体的快捷键更新逻辑可以根据现有代码调用
        }

        /// <summary>
        /// 🆕 增量更新轮盘映射关系
        /// 根据变化的背包槽位进行精确更新，避免全量刷新的性能开销
        /// </summary>
        /// <param name="changedSlot">发生变化的背包槽位</param>
        private void IncrementalUpdateWheelMapping(int changedSlot)
        {
            // 🚨 醒目日志：追踪增量更新的触发
            Debug.LogWarning($"🔧 IncrementalUpdateWheelMapping 触发 - 槽位: {changedSlot}, 时间: {System.DateTime.Now:HH:mm:ss.fff}");

            // 检查主背包引用
            if (_mainBackpackInventory == null)
            {
                Debug.LogWarning("MainBackpackWheelManager: 主背包引用为空，无法进行增量更新");
                return;
            }

            // 获取变化槽位的物品信息
            var newItem = _mainBackpackInventory.GetItemAt(changedSlot);
            bool newItemIsShortcut = newItem != null && IsShortcutItem(newItem);
            ItemCategory itemCategory = GetItemCategoryAtBackpackPosition(changedSlot);

            // 🔧 关键修复：检查是否是物品移动（先在所有类别中查找该物品）
            if (newItem != null && newItemIsShortcut)
            {
                bool foundInOtherSlot = FindAndMoveItem(changedSlot, itemCategory, newItem);
                if (foundInOtherSlot)
                {
                    Debug.Log($"MainBackpackWheelManager: 物品 {newItem.DisplayName} 已从其他位置移动到 {changedSlot}，映射关系已更新");
                    return; // 已处理，直接返回
                }
            }

            // 查找该槽位在对应类别轮盘中的位置
            var categoryBackpackMapping = GetCategoryBackpackMapping(itemCategory);
            int existingWheelPos = -1;
            bool slotInWheel = categoryBackpackMapping?.TryGetValue(changedSlot, out existingWheelPos) == true;

            Debug.Log($"MainBackpackWheelManager: 增量更新分析 - 槽位{changedSlot}, 类别:{itemCategory}, 在轮盘中: {slotInWheel}, 轮盘位置: {existingWheelPos}, 新物品: {newItem?.DisplayName ?? "null"}");

            // 根据不同情况进行处理
            if (itemCategory == ItemCategory.None && !newItemIsShortcut)
            {
                // 物品被移除或变为非快捷物品
                HandleItemRemoval(changedSlot);
            }
            else if (slotInWheel)
            {
                // 槽位在对应类别的轮盘映射中
                HandleExistingSlotUpdate(changedSlot, itemCategory, existingWheelPos, newItem, newItemIsShortcut);
            }
            else
            {
                // 槽位不在对应类别的轮盘映射中
                HandleNewItemInsertion(changedSlot, itemCategory, newItem, newItemIsShortcut);
            }

            Debug.Log("MainBackpackWheelManager: 增量更新完成");
        }

        /// <summary>
        /// 🔧 关键修复：查找物品是否在其他轮盘位置，如果是则更新映射关系
        /// 处理物品在背包中移动的情况（如从位置11移动到位置7）
        /// </summary>
        /// <param name="newBackpackPos">新的背包位置</param>
        /// <param name="itemCategory">物品类别</param>
        /// <param name="newItem">物品引用</param>
        /// <returns>是否找到并移动了物品</returns>
        private bool FindAndMoveItem(int newBackpackPos, ItemCategory itemCategory, Item newItem)
        {
            // 🆕 在所有类别中查找该物品
            foreach (var category in AllCategories)
            {
                var categoryWheelMapping = GetCategoryWheelMapping(category);
                var categoryBackpackMapping = GetCategoryBackpackMapping(category);

                if (categoryWheelMapping == null || categoryBackpackMapping == null) continue;

                // 遍历该类别的所有轮盘位置
                for (int wheelPos = 0; wheelPos < 8; wheelPos++)
                {
                    int oldBackpackPos = categoryWheelMapping[wheelPos];
                    if (oldBackpackPos != -1 && oldBackpackPos != newBackpackPos)
                    {
                        // 检查这个位置的物品是否是我们要找的物品
                        var itemAtOldPos = _mainBackpackInventory.GetItemAt(oldBackpackPos);
                        if (itemAtOldPos != null && itemAtOldPos.Equals(newItem))
                        {
                            // 找到了！物品从 oldBackpackPos 移动到了 newBackpackPos
                            Debug.Log($"MainBackpackWheelManager: 检测到物品移动: {newItem.DisplayName} 从背包位置{oldBackpackPos} 移动到 {newBackpackPos}");

                            // 检查类别是否发生了变化
                            if (category == itemCategory)
                            {
                                // 类别没变，只更新背包位置映射
                                Debug.Log($"MainBackpackWheelManager: 物品类别未变，更新映射 轮盘{wheelPos}: 背包{oldBackpackPos} -> 背包{newBackpackPos}");

                                // 更新映射关系
                                categoryWheelMapping[wheelPos] = newBackpackPos;
                                categoryBackpackMapping.Remove(oldBackpackPos);
                                categoryBackpackMapping[newBackpackPos] = wheelPos;

                                // 更新UI显示
                                UpdateWheelSlot(category, wheelPos, newBackpackPos, newItem);
                            }
                            else
                            {
                                // 类别发生了变化，需要从旧类别移除，添加到新类别
                                Debug.Log($"MainBackpackWheelManager: 物品类别变化: {category} -> {itemCategory}");

                                // 从旧类别移除
                                categoryWheelMapping[wheelPos] = -1;
                                categoryBackpackMapping.Remove(oldBackpackPos);
                                _wheelLayoutManager.RemoveItem(category, itemAtOldPos);

                                // 添加到新类别
                                InsertNewItemToWheel(itemCategory, newBackpackPos, newItem);
                            }

                            return true; // 已处理
                        }
                    }
                }
            }

            return false; // 未找到该物品在其他位置
        }

        /// <summary>
        /// 🆕 更新指定类别轮盘槽位的物品
        /// </summary>
        private void UpdateWheelSlot(ItemCategory category, int wheelPos, int backpackPos, Item newItem)
        {
            // 更新轮盘UI显示
            if (category != ItemCategory.None)
            {
                _wheelLayoutManager.AddItemToCategory(category, newItem);
                Debug.Log($"MainBackpackWheelManager: 已更新{category}类别轮盘显示 - 轮盘位置:{wheelPos}, 物品: {newItem.DisplayName}");
            }

            // 🆕 通知轮盘界面刷新 - 解决轮盘显示时数据过期问题
            _wheelLayoutManager.NotifyWheelRefresh(category);
        }

        /// <summary>
        /// 🆕 从指定类别轮盘槽位移除物品
        /// </summary>
        private void RemoveFromWheelSlot(ItemCategory category, int wheelPos, int backpackPos)
        {
            if (category == ItemCategory.None)
            {
                Debug.LogWarning("MainBackpackWheelManager: RemoveFromWheelSlot - 无效的类别");
                return;
            }

            // 获取指定类别的映射
            var categoryWheelMapping = GetCategoryWheelMapping(category);
            var categoryBackpackMapping = GetCategoryBackpackMapping(category);

            if (categoryWheelMapping == null || categoryBackpackMapping == null)
            {
                Debug.LogWarning($"MainBackpackWheelManager: {category}类别的映射未初始化");
                return;
            }

            // 🔧 获取要移除的物品（在更新映射之前）
            // 注意：此时物品可能已被拿起，所以GetItemAt可能返回null
            var itemToRemove = _mainBackpackInventory.GetItemAt(backpackPos);

            // 🔧 如果物品已被拿起，尝试从反向映射获取物品引用
            if (itemToRemove == null && categoryBackpackMapping.TryGetValue(backpackPos, out int mappedWheelPos))
            {
                // 从轮盘布局中查找该位置的物品
                var slots = _wheelLayoutManager.GetSlots(category);
                if (slots != null && mappedWheelPos >= 0 && mappedWheelPos < slots.Count)
                {
                    itemToRemove = slots[mappedWheelPos].Item;
                    Debug.Log($"MainBackpackWheelManager: 从轮盘布局中找到要移除的物品 {itemToRemove?.DisplayName ?? "null"}，位置{mappedWheelPos}");
                }
            }

            // 移除映射关系
            categoryWheelMapping[wheelPos] = -1;
            categoryBackpackMapping.Remove(backpackPos);

            // 🔧 精确移除指定物品，而不是清空整个类别
            if (itemToRemove != null)
            {
                _wheelLayoutManager.RemoveItem(category, itemToRemove);
                Debug.Log($"MainBackpackWheelManager: 已从{category}类别轮盘精确移除物品 {itemToRemove.DisplayName}，位置{wheelPos}");
            }
            else
            {
                Debug.LogWarning($"MainBackpackWheelManager: 无法找到要移除的物品，槽位{backpackPos}，轮盘位置{wheelPos}");

                // 🔧 如果找不到物品，使用原有的ClearCategory作为备选方案
                // 这里可以优化为更精确的操作，但现在先确保功能正常
                _wheelLayoutManager.ClearCategory(category);
                Debug.Log($"MainBackpackWheelManager: 使用备选方案清空{category}类别");
            }

            // 🆕 通知轮盘界面刷新 - 解决轮盘显示时数据过期问题
            _wheelLayoutManager.NotifyWheelRefresh(category);
        }

        /// <summary>
        /// 🆕 将新物品插入到指定类别的轮盘
        /// </summary>
        private void InsertNewItemToWheel(ItemCategory category, int backpackPos, Item newItem)
        {
            if (category == ItemCategory.None)
            {
                Debug.LogWarning("MainBackpackWheelManager: InsertNewItemToWheel - 无效的类别");
                return;
            }

            // 获取指定类别的映射
            var categoryWheelMapping = GetCategoryWheelMapping(category);
            var categoryBackpackMapping = GetCategoryBackpackMapping(category);

            if (categoryWheelMapping == null || categoryBackpackMapping == null)
            {
                Debug.LogWarning($"MainBackpackWheelManager: {category}类别的映射未初始化");
                return;
            }

            // 查找指定类别的空轮盘位置
            int emptyWheelPos = -1;
            for (int i = 0; i < 8; i++)
            {
                if (categoryWheelMapping[i] == -1)
                {
                    emptyWheelPos = i;
                    break;
                }
            }

            if (emptyWheelPos != -1)
            {
                // 先更新映射关系
                categoryWheelMapping[emptyWheelPos] = backpackPos;
                categoryBackpackMapping[backpackPos] = emptyWheelPos;

                // 🔧 尝试使用指定位置的方法，确保映射与UI一致
                _wheelLayoutManager.AddItemToCategoryAtPosition(category, newItem, emptyWheelPos);

                Debug.Log($"MainBackpackWheelManager: 已将新物品 {newItem.DisplayName} 插入{category}类别轮盘位置 {emptyWheelPos}");
            }
            else
            {
                // 轮盘已满，需要智能插入：将新物品插入到合适位置，后续物品后移
                Debug.Log($"MainBackpackWheelManager: {category}类别轮盘已满，执行智能插入算法");
                InsertItemAt(category, backpackPos, newItem);
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

            // 🆕 清空所有类别的映射
            foreach (var category in AllCategories)
            {
                Array.Fill(_categoryToWheelPositions[category], -1);
                _categoryToBackpackPositions[category].Clear();
            }

            // 清空轮盘显示
            _wheelLayoutManager.ClearAllCategories();

            if (_mainBackpackInventory == null)
            {
                Debug.LogWarning("MainBackpackWheelManager: 主背包为空，无法初始化映射");
                return;
            }

            // 🆕 按类别分组物品
            var categorizedItems = new Dictionary<ItemCategory, List<(int backpackPos, Item item)>>();
            foreach (ItemCategory category in Enum.GetValues(typeof(ItemCategory)))
            {
                if (category != ItemCategory.None)
                {
                    categorizedItems[category] = new List<(int, Item)>();
                }
            }

            // 遍历主背包，按类别收集物品
            for (int backpackPosition = 0; backpackPosition < _mainBackpackInventory.Content.Count; backpackPosition++)
            {
                var item = _mainBackpackInventory.GetItemAt(backpackPosition);
                if (item != null && IsShortcutItem(item))
                {
                    var category = ItemCategorizer.CategorizeItem(item);
                    if (category != ItemCategory.None)
                    {
                        categorizedItems[category].Add((backpackPosition, item));
                    }
                }
            }

            // 为每个类别建立映射（每个类别最多8个物品）
            foreach (var kvp in categorizedItems)
            {
                var category = kvp.Key;
                var items = kvp.Value.Take(8).ToList(); // 每个类别最多8个物品

                if (items.Count > 0)
                {
                    Debug.Log($"MainBackpackWheelManager: {category}类别有{items.Count}个物品");

                    var categoryWheelMapping = _categoryToWheelPositions[category];
                    var categoryBackpackMapping = _categoryToBackpackPositions[category];

                    for (int wheelPos = 0; wheelPos < items.Count; wheelPos++)
                    {
                        var (backpackPos, item) = items[wheelPos];

                        // 建立映射关系
                        categoryWheelMapping[wheelPos] = backpackPos;
                        categoryBackpackMapping[backpackPos] = wheelPos;

                        // 添加到轮盘显示
                        _wheelLayoutManager.AddItemToCategory(category, item);

                        Debug.Log($"MainBackpackWheelManager: {category}映射 轮盘[{wheelPos}] <-> 背包[{backpackPos}] 物品: {item.DisplayName}");
                    }
                }
            }

            Debug.Log("MainBackpackWheelManager: 按类别映射关系初始化完成");
        }

        /// <summary>
        /// 🔥 根据映射关系重新生成指定类别的轮盘布局（新架构）
        /// 使用 SyncFromMapping 确保槽位与映射表完全一致
        /// </summary>
        private void RegenerateCategoryWheelFromMappings(ItemCategory category)
        {
            if (!GetCategoryMappings(category, out var categoryWheelMapping, out var categoryBackpackMapping))
            {
                Debug.LogWarning($"MainBackpackWheelManager: 无法获取{category}类别的映射关系");
                return;
            }

            Debug.Log($"MainBackpackWheelManager: 🔄 开始重新生成{category}类别轮盘布局");

            // 🔥 新架构：使用 SyncFromMapping 统一同步
            // 直接传递映射表和获取物品的函数，让 WheelLayoutManager 处理所有同步逻辑
            _wheelLayoutManager.SyncFromMapping(
                category,
                categoryWheelMapping,
                (backpackPos) => {
                    var item = _mainBackpackInventory.GetItemAt(backpackPos);
                    // 验证物品有效性
                    if (item != null && IsShortcutItem(item))
                    {
                        return item;
                    }
                    else if (item == null || !IsShortcutItem(item))
                    {
                        // 清理无效映射
                        categoryWheelMapping[Array.IndexOf(categoryWheelMapping, backpackPos)] = -1;
                        categoryBackpackMapping.Remove(backpackPos);
                        Debug.LogWarning($"MainBackpackWheelManager: 清理无效映射 背包[{backpackPos}]");
                    }
                    return null;
                }
            );

            Debug.Log($"MainBackpackWheelManager: ✓ 重新生成{category}类别轮盘布局完成");
        }

        #endregion

        #region 智能插入算法

        /// <summary>
        /// 🆕 在指定背包位置插入物品到指定类别轮盘
        /// </summary>
        private void InsertItemAt(ItemCategory category, int backpackPosition, Item item)
        {
            if (category == ItemCategory.None)
            {
                Debug.LogWarning("MainBackpackWheelManager: InsertItemAt - 无效的类别");
                return;
            }

            // 获取指定类别的映射
            var categoryWheelMapping = GetCategoryWheelMapping(category);
            var categoryBackpackMapping = GetCategoryBackpackMapping(category);

            if (categoryWheelMapping == null || categoryBackpackMapping == null)
            {
                Debug.LogWarning($"MainBackpackWheelManager: {category}类别的映射未初始化");
                return;
            }

            // 第1步：找到新物品应该插入的轮盘位置
            int insertWheelPos = FindInsertWheelPosition(category, backpackPosition);

            if (insertWheelPos == -1)
            {
                Debug.LogWarning($"MainBackpackWheelManager: {category}类别轮盘已满，无法插入新物品");
                return;
            }

            Debug.Log($"MainBackpackWheelManager: 在{category}类别轮盘位置 {insertWheelPos} 插入物品 {item.DisplayName}");

            // 第2步：从插入位置开始，将后面的轮盘物品向后移动
            ShiftWheelItemsBackward(category, insertWheelPos);

            // 第3步：在插入位置放入新物品
            categoryWheelMapping[insertWheelPos] = backpackPosition;
            categoryBackpackMapping[backpackPosition] = insertWheelPos;

            // 第4步：更新轮盘UI
            _wheelLayoutManager.AddItemToCategory(category, item);
        }

        /// <summary>
        /// 🆕 查找新物品应该插入的指定类别轮盘位置
        /// </summary>
        private int FindInsertWheelPosition(ItemCategory category, int backpackPosition)
        {
            var categoryWheelMapping = GetCategoryWheelMapping(category);
            if (categoryWheelMapping == null)
            {
                Debug.LogWarning($"MainBackpackWheelManager: {category}类别的映射未初始化");
                return -1;
            }

            for (int wheelPos = 0; wheelPos < 8; wheelPos++)
            {
                int existingBackpackPos = categoryWheelMapping[wheelPos];

                // 情况1：轮盘位置为空，可以插入
                if (existingBackpackPos == -1) return wheelPos;

                // 情况2：现有物品的背包位置大于新物品位置，应该插入在此位置之前
                if (existingBackpackPos > backpackPosition) return wheelPos;
            }

            return -1; // 轮盘已满
        }

        /// <summary>
        /// 🆕 向后移动指定类别轮盘物品
        /// </summary>
        private void ShiftWheelItemsBackward(ItemCategory category, int fromWheelPosition)
        {
            var categoryWheelMapping = GetCategoryWheelMapping(category);
            var categoryBackpackMapping = GetCategoryBackpackMapping(category);

            if (categoryWheelMapping == null || categoryBackpackMapping == null)
            {
                Debug.LogWarning($"MainBackpackWheelManager: {category}类别的映射未初始化");
                return;
            }

            // 从后向前，将指定类别的轮盘物品向后移动一位
            for (int wheelPos = 7; wheelPos > fromWheelPosition; wheelPos--)
            {
                int currentBackpackPos = categoryWheelMapping[wheelPos - 1];

                if (currentBackpackPos != -1)
                {
                    // 将物品从轮盘位置 wheelPos-1 移动到 wheelPos
                    categoryWheelMapping[wheelPos] = currentBackpackPos;
                    categoryBackpackMapping[currentBackpackPos] = wheelPos;

                    // 更新UI：移动对应的轮盘物品
                    var item = _mainBackpackInventory.GetItemAt(currentBackpackPos);
                    if (item != null)
                    {
                        _wheelLayoutManager.AddItemToCategory(category, item);
                    }
                }
            }

            // 清空原始插入位置（将被新物品占用）
            categoryWheelMapping[fromWheelPosition] = -1;
        }

        #endregion

        #region 轮盘事件处理

        /// <summary>
        /// 🆕 处理轮盘槽位交换事件
        /// 当轮盘UI中的物品位置交换时，同步更新背包中的物品位置
        /// </summary>
        private void OnWheelSlotsSwapped(ItemCategory category, int fromWheelPos, int toWheelPos)
        {
            Debug.Log($"MainBackpackWheelManager: 收到轮盘槽位交换事件 - 类别: {category}, 索引: {fromWheelPos}<->{toWheelPos}");

            // 只有在主背包模式下才同步背包位置
            if (BackpackShortcutManager.Instance?.IsAttachmentMode == true)
            {
                Debug.Log("MainBackpackWheelManager: 当前为配件模式，跳过背包位置同步");
                return;
            }

            // 🆕 调用带类别信息的位置调整方法
            AdjustWheelPosition(category, fromWheelPos, toWheelPos);
        }

        #endregion

        #region 位置同步功能

        /// <summary>
        /// 🆕 调整轮盘物品位置（带类别信息）
        /// 在轮盘中调整物品位置时，同步调整背包中的位置
        /// </summary>
        public void AdjustWheelPosition(ItemCategory category, int fromWheelPos, int toWheelPos)
        {
            if (fromWheelPos < 0 || fromWheelPos >= 8 || toWheelPos < 0 || toWheelPos >= 8)
            {
                Debug.LogWarning($"MainBackpackWheelManager: 无效的轮盘位置: from={fromWheelPos}, to={toWheelPos}");
                return;
            }

            // 🆕 获取指定类别的映射
            var categoryWheelMapping = GetCategoryWheelMapping(category);
            var categoryBackpackMapping = GetCategoryBackpackMapping(category);

            if (categoryWheelMapping == null || categoryBackpackMapping == null)
            {
                Debug.LogWarning($"MainBackpackWheelManager: 类别 {category} 的映射未初始化");
                return;
            }

            int fromBackpackPos = categoryWheelMapping[fromWheelPos];
            int toBackpackPos = categoryWheelMapping[toWheelPos];

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

            Debug.Log($"MainBackpackWheelManager: 调整{category}轮盘位置 {fromWheelPos} -> {toWheelPos}，目标背包位置: {toBackpackPos}");

            // 🆕 输出指定类别的映射状态用于调试
            Debug.Log($"MainBackpackWheelManager: {category}类别轮盘映射状态:");
            for (int i = 0; i < 8; i++)
            {
                int bp = categoryWheelMapping[i];
                string itemName = (bp != -1) ? _mainBackpackInventory.GetItemAt(bp)?.DisplayName ?? "null" : "空";
                Debug.Log($"  {category}轮盘{i} -> 背包{bp} ({itemName})");
            }

            // 🆕 设置交换标志，防止增量更新干扰
            _isPerformingSwap = true;

            try
            {
                if (toBackpackPos != -1)
                {
                    // 情况1：目标位置有物品 - 需要交换背包中的位置
                    Debug.Log($"MainBackpackWheelManager: 目标位置有物品，执行背包位置交换");

                    var targetItem = _mainBackpackInventory.GetItemAt(toBackpackPos);
                    if (targetItem != null)
                    {
                        Debug.Log($"MainBackpackWheelManager: Detach目标物品 {targetItem.DisplayName}");
                        targetItem.Detach();
                    }

                    Debug.Log($"MainBackpackWheelManager: Detach源物品 {item.DisplayName}");
                    item.Detach();

                    // 重新放入到新位置
                    if (targetItem != null)
                    {
                        _mainBackpackInventory.AddAt(targetItem, fromBackpackPos);
                    }
                    _mainBackpackInventory.AddAt(item, toBackpackPos);

                    // 🆕 更新指定类别的映射关系（双向交换）
                    categoryWheelMapping[fromWheelPos] = toBackpackPos;
                    categoryWheelMapping[toWheelPos] = fromBackpackPos;
                    categoryBackpackMapping[toBackpackPos] = fromWheelPos;
                    categoryBackpackMapping[fromBackpackPos] = toWheelPos;

                    Debug.Log($"MainBackpackWheelManager: {category}类别背包位置交换完成");
                }
                else
                {
                    // 情况2：目标位置为空 - 只更新轮盘映射，不操作背包
                    Debug.Log($"MainBackpackWheelManager: 目标位置为空，仅更新轮盘映射关系");

                    // 🆕 更新指定类别的映射关系（单向移动）
                    categoryWheelMapping[fromWheelPos] = -1; // 清空原位置
                    categoryWheelMapping[toWheelPos] = fromBackpackPos; // 设置新位置
                    categoryBackpackMapping[fromBackpackPos] = toWheelPos; // 更新反向映射

                    Debug.Log($"MainBackpackWheelManager: {category}类别轮盘映射更新完成，背包位置保持不变");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"MainBackpackWheelManager: 位置同步失败: {ex.Message}");
            }
            finally
            {
                // 🆕 重置交换标志，恢复增量更新
                _isPerformingSwap = false;
                Debug.Log("MainBackpackWheelManager: 交换标志已重置，恢复增量更新");
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
        /// 🆕 获取指定类别轮盘位置对应的背包位置
        /// </summary>
        public int GetBackpackPosition(ItemCategory category, int wheelPosition)
        {
            if (wheelPosition < 0 || wheelPosition >= 8) return -1;

            var categoryMapping = GetCategoryWheelMapping(category);
            return categoryMapping?[wheelPosition] ?? -1;
        }

        /// <summary>
        /// 🆕 获取指定类别背包位置对应的轮盘位置
        /// </summary>
        public int GetWheelPosition(ItemCategory category, int backpackPosition)
        {
            var categoryMapping = GetCategoryBackpackMapping(category);
            return categoryMapping?.TryGetValue(backpackPosition, out int wheelPos) == true ? wheelPos : -1;
        }

        /// <summary>
        /// 🗑️ 已废弃：旧的GetBackpackPosition方法（缺少类别信息）
        /// 请使用 GetBackpackPosition(ItemCategory category, int wheelPosition)
        /// </summary>
        [Obsolete("请使用带类别参数的GetBackpackPosition方法")]
        public int GetBackpackPosition(int wheelPosition)
        {
            Debug.LogWarning("MainBackpackWheelManager: 使用了已废弃的GetBackpackPosition方法，请使用带类别参数的版本");
            return -1; // 强制返回-1，迫使调用方更新
        }

        /// <summary>
        /// 🗑️ 已废弃：旧的GetWheelPosition方法（缺少类别信息）
        /// 请使用 GetWheelPosition(ItemCategory category, int backpackPosition)
        /// </summary>
        [Obsolete("请使用带类别参数的GetWheelPosition方法")]
        public int GetWheelPosition(int backpackPosition)
        {
            Debug.LogWarning("MainBackpackWheelManager: 使用了已废弃的GetWheelPosition方法，请使用带类别参数的版本");
            return -1; // 强制返回-1，迫使调用方更新
        }

        #endregion
    }
}