using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using Duckov.Utilities;
using Backpack_QuickWheel.ShortcutSystem.Data;

namespace Backpack_QuickWheel.ShortcutSystem
{
    public class BackpackShortcutManager : MonoBehaviour
    {
        private static BackpackShortcutManager _instance;
        private CharacterEquipmentController _equipmentController;

        // ════ 新架构：统一的轮盘布局管理器 ════
        // 替代双数组设计的混乱架构，统一管理轮盘格子和物品
        private WheelLayoutManager _wheelLayoutManager;

        // 🔧 修复：当前选择的物品引用，建立绝对对应关系
        private Dictionary<ItemCategory, Item> _currentSelectedItem = new Dictionary<ItemCategory, Item>();

        // 用于监听背包内容变化
        private Item _currentBackpack;
        private List<Item> _subscribedAttachments = new List<Item>();

        // 防止递归刷新的标志
        private bool _isRefreshing = false;

        // 配件内容变化的去抖：当短时间内多个物品被放入/移除时，只执行一次更新
        private Coroutine _pendingAttachmentUpdateCoroutine = null;
        private const float ATTACHMENT_UPDATE_DEBOUNCE_TIME = 0.02f; // 🔧 优化：减少去抖延迟从0.1s到0.02s

        // 用于监听技能释放事件
        private HashSet<SkillBase> _monitoredSkills = new HashSet<SkillBase>();

        // 初始化状态标记
        private bool _isInitializing = true;

        // 临时变量：用于在协程间传递新类别信息
        private HashSet<ItemCategory> _tempNewCategories = null;

        // 🎯 当前订阅销毁事件的物品实例
        private Item _currentSubscribedItem = null;

        // 🔧 配件slot订阅管理
        private HashSet<Slot> _subscribedAttachmentSlots = new HashSet<Slot>();

        // 🔧 配件slot内容跟踪：用于在移除时识别被移除的物品类型
        private Dictionary<Slot, Item> _slotContentHistory = new Dictionary<Slot, Item>();

        // 🔧 配件包含的物品类别跟踪：用于精确清理配件影响的类别
        private Dictionary<Item, HashSet<ItemCategory>> _attachmentCategories = new Dictionary<Item, HashSet<ItemCategory>>();

        // 🔧 物品拔出事件订阅管理
        private HashSet<Item> _subscribedItems = new HashSet<Item>();

        public static bool IsShortcutSystemEnabled { get; private set; }
        public static event System.Action<bool> OnShortcutSystemStateChanged;

        public static BackpackShortcutManager Instance => _instance;


        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(this);

            // 初始化新的轮盘布局管理器
            _wheelLayoutManager = new WheelLayoutManager();

            // 🏗️ 重构：移除轮盘UI事件绑定，轮盘系统只管理数据，不触发UI更新
            // UI更新由BackpackShortcutManager统一协调
            // _wheelLayoutManager.OnLayoutChanged += OnWheelLayoutChanged;

            // 🏗️ 设置UI系统的委托，让UI系统具备自我管理能力
            // 🔧 修复：通过BackpackShortcutManager获取当前选中，确保选中状态管理正确
            ShortcutUIUpdater.GetCurrentSelectionDelegate = GetCurrentSelectionByCategory;

            // 🔧 修复：初始化选中物品记录
            foreach (ItemCategory category in System.Enum.GetValues(typeof(ItemCategory)))
            {
                if (category != ItemCategory.None)
                {
                    _currentSelectedItem[category] = null; // 初始无选中物品
                }
            }

            // 设置初始化为进行中状态
            _isInitializing = true;
            Debug.Log("[BackpackShortcutManager] Awake完成，初始化状态设置为: true");
        }

        public static void Initialize(CharacterEquipmentController equipmentController)
        {
            if (_instance == null)
            {
                GameObject obj = new GameObject("BackpackShortcutManager");
                _instance = obj.AddComponent<BackpackShortcutManager>();
            }

            _instance._equipmentController = equipmentController;
            _instance.StartListening();
        }

        /// <summary>
        /// 设置初始化状态
        /// </summary>
        public static void SetInitializing(bool isInitializing)
        {
            if (_instance != null)
            {
                _instance._isInitializing = isInitializing;
                Debug.Log($"[BackpackShortcutManager] 初始化状态设置为: {isInitializing}");

                // 初始化完成时，启用系统
                if (!isInitializing)
                {
                    _instance.EnableSystemAfterInit();
                }
            }
        }

        /// <summary>
        /// 获取初始化状态
        /// </summary>
        public static bool IsInitializing()
        {
            return _instance != null && _instance._isInitializing;
        }

        /// <summary>
        /// 开始监听各种事件
        /// </summary>
        private void StartListening()
        {
            // 监听快捷键输入事件
            UIInputManager.OnShortcutInput += OnUIShortcutInput;
            Debug.Log("[BackpackShortcutManager] 已订阅 UIInputManager.OnShortcutInput 事件");

            // 监听背包装备变化
            if (_equipmentController != null)
            {
                // 这里需要通过Harmony补丁来监听，因为backpackSlot是private
                // 补丁会调用OnBackpackChanged方法
            }

            // 🎯 正确方案：监听物品的onDestroy和onSetStackCount事件
            // 这确保在物品真正被消耗后才触发UI更新
            // 注意：这些是实例事件，需要在收集物品时为每个物品实例订阅
            Debug.Log("[BackpackShortcutManager] 物品消耗事件监听已准备（将在物品收集时为每个实例订阅）");
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        private void OnDestroy()
        {
            // 停止待处理的更新协程
            if (_pendingAttachmentUpdateCoroutine != null)
            {
                StopCoroutine(_pendingAttachmentUpdateCoroutine);
            }

            // 取消所有事件订阅
            UnsubscribeFromBackpackChanges();

            // 🎯 取消订阅选中物品的销毁事件
            UnsubscribeFromPreviousSelection();

            UIInputManager.OnShortcutInput -= OnUIShortcutInput;

            Debug.Log("[BackpackShortcutManager] 已清理所有事件订阅");
        }

        /// <summary>
        /// 获取指定类别的轮盘布局
        /// 返回轮盘上的排列顺序，包含 null 占位符
        /// 如果尚未保存布局，返回纯物品列表
        /// </summary>
        public List<Item> GetItemsForCategory(ItemCategory category)
        {
            if (!IsShortcutSystemEnabled)
            {
                return new List<Item>();
            }

            // 🆕 新架构：通过WheelLayoutManager获取布局数据
            var layoutItems = _wheelLayoutManager.GetLayoutForUI(category);
            return layoutItems ?? new List<Item>();
        }

        /// <summary>
        /// 🔧 修复：获取指定类别的所有物品（用于轮盘显示）
        /// 直接返回收集到的所有物品，不使用轮盘布局
        /// 确保轮盘能显示所有收集到的物品实例
        /// </summary>
        public List<Item> GetAllItemsForCategory(ItemCategory category)
        {
            if (!IsShortcutSystemEnabled)
            {
                return new List<Item>();
            }

            Debug.Log($"[BackpackShortcutManager] GetAllItemsForCategory: 查询类别 {category}");

            // 🆕 新架构：通过WheelLayoutManager获取有效物品
            var validItems = _wheelLayoutManager.GetValidItems(category);
            Debug.Log($"[BackpackShortcutManager] GetAllItemsForCategory: 找到 {validItems.Count} 个有效物品");

            // 详细日志
            for (int i = 0; i < validItems.Count; i++)
            {
                var item = validItems[i];
                Debug.Log($"[BackpackShortcutManager]   物品{i+1}: {item.DisplayName} (实例ID: {item.GetHashCode()})");
            }

            return validItems;
        }

        /// <summary>
        /// 将类别转换为快捷键索引
        /// </summary>
        private int CategoryToIndex(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Medical: return 0;
                case ItemCategory.Stim: return 1;
                case ItemCategory.Food: return 2;
                case ItemCategory.Explosive: return 3;
                default: return -1;
            }
        }

        /// <summary>
        /// 🆕 新架构：增量更新轮盘布局
        /// 使用WheelLayoutManager统一管理轮盘格子和物品，替代双数组架构
        /// </summary>
        private System.Collections.IEnumerator IncrementalUpdateCategorizedItems()
        {
            // 初始化临时变量用于存储新类别
            _tempNewCategories = new HashSet<ItemCategory>();

            yield return null;  // 等待一帧，确保物品状态已更新

            if (_currentBackpack == null) yield break;

            // 🆕 新架构：获取当前背包中的所有物品（按类别分类）
            var currentBackpackItems = new HashSet<Item>();
            CollectAllItemsFromBackpack(_currentBackpack, currentBackpackItems);

            // 按类别收集物品
            var categorizedItems = new Dictionary<ItemCategory, List<Item>>();
            foreach (var item in currentBackpackItems)
            {
                var category = ItemCategorizer.CategorizeItem(item);
                if (category != ItemCategory.None)  // 跳过无效类别
                {
                    if (!categorizedItems.ContainsKey(category))
                    {
                        categorizedItems[category] = new List<Item>();
                    }
                    categorizedItems[category].Add(item);
                }
            }

            // 🆕 使用新架构更新轮盘布局
            foreach (var kvp in categorizedItems)
            {
                var category = kvp.Key;
                var items = kvp.Value;

                // 通过WheelLayoutManager更新布局
                _wheelLayoutManager.UpdateFromCollectedItems(category, items);

                Debug.Log($"[BackpackShortcutManager] 新架构更新 {category}: {items.Count} 个物品");
            }

            // 跳过旧的复杂逻辑，直接结束
            Debug.Log("[BackpackShortcutManager] 新架构：轮盘布局更新完成");
        }

        /// <summary>
        /// 🔧 单类别精确更新 - 配件内部物品变化时同时更新轮盘和UI
        /// 根据统一事件订阅架构，物品变化应该同时通知轮盘系统和UI系统
        /// </summary>
        private System.Collections.IEnumerator SingleCategoryUpdate(ItemCategory category)
        {
            Debug.Log($"[BackpackShortcutManager] 开始单类别精确更新: {category}");

            yield return null; // 等待一帧，确保物品状态已更新

            if (_currentBackpack == null) yield break;

            // 🔧 只收集指定类别的物品，避免全背包扫描
            var categoryItems = new List<Item>();
            CollectCategoryItemsFromBackpack(_currentBackpack, category, categoryItems);

            Debug.Log($"[BackpackShortcutManager] 单类别 {category} 收集到 {categoryItems.Count} 个物品");

            // 🔧 1. 更新轮盘布局
            _wheelLayoutManager.UpdateFromCollectedItems(category, categoryItems);

            // 🔧 2. 通知UI系统类别发生变化（通过委托模式）
            ShortcutUIUpdater.UpdateCategoryUI(category);

            Debug.Log($"[BackpackShortcutManager] 单类别 {category} 更新完成");
        }

  
        /// <summary>
        /// 🔧 只收集指定类别的物品，性能优化版本
        /// 避免收集所有物品，只收集目标类别
        /// 🔧 修复：收集所有实际的物品实例，不按类型去重
        /// </summary>
        private void CollectCategoryItemsFromBackpack(Item backpack, ItemCategory targetCategory, List<Item> result)
        {
            if (backpack == null || backpack.Slots == null) return;

            foreach (var slot in backpack.Slots)
            {
                if (slot != null && slot.Content != null)
                {
                    var item = slot.Content;

                    // 🔧 只收集目标类别的物品，收集所有实例
                    if (ItemCategorizer.CategorizeItem(item) == targetCategory)
                    {
                        result.Add(item);
                        Debug.Log($"[BackpackShortcutManager] 收集到目标类别物品: {item.DisplayName} (TypeID: {item.TypeID}, 引用: {item.GetHashCode()}, 类别: {targetCategory})");
                    }

                    // 🔧 递归检查配件中的物品，但只收集目标类别
                    if (item.Slots != null && item.Slots.Count > 0)
                    {
                        CollectCategoryItemsFromBackpackRecursive(item, targetCategory, result);
                    }
                }
            }
        }

        /// <summary>
        /// 🔧 递归收集配件中的物品，收集所有实例
        /// 🔧 修复：收集所有实际的物品实例，不按类型去重
        /// </summary>
        private void CollectCategoryItemsFromBackpackRecursive(Item container, ItemCategory targetCategory, List<Item> result)
        {
            if (container == null || container.Slots == null) return;

            foreach (var slot in container.Slots)
            {
                if (slot != null && slot.Content != null)
                {
                    var item = slot.Content;

                    // 🔧 只收集目标类别的物品，收集所有实例
                    if (ItemCategorizer.CategorizeItem(item) == targetCategory)
                    {
                        result.Add(item);
                        Debug.Log($"[BackpackShortcutManager] 递归收集到目标类别物品: {item.DisplayName} (TypeID: {item.TypeID}, 引用: {item.GetHashCode()}, 容器: {container.DisplayName})");
                    }

                    // 🔧 递归检查嵌套容器
                    if (item.Slots != null && item.Slots.Count > 0)
                    {
                        CollectCategoryItemsFromBackpackRecursive(item, targetCategory, result);
                    }
                }
            }
        }

        /// <summary>
        /// 递归收集背包和所有配件中的所有物品
        /// </summary>
        private void CollectAllItemsFromBackpack(Item backpack, HashSet<Item> result)
        {
            if (backpack == null || backpack.Slots == null) return;

            foreach (var slot in backpack.Slots)
            {
                if (slot != null && slot.Content != null)
                {
                    var item = slot.Content;
                    result.Add(item);

                    // 递归检查配件中的物品
                    if (item.Slots != null && item.Slots.Count > 0)
                    {
                        CollectAllItemsFromBackpack(item, result);
                    }
                }
            }
        }

        /// <summary>
        /// 🎯 为选中的物品订阅相关事件
        /// 监听物品销毁、移动、父级变化等退出情况
        /// </summary>
        private void SubscribeToSelectedItem(Item item)
        {
            if (item == null) return;

            // 先取消之前选中物品的订阅
            UnsubscribeFromPreviousSelection();

            // 为新的选中物品订阅所有相关事件
            item.onDestroy += OnSelectedItemDestroyed;
            item.onParentChanged += OnSelectedItemParentChanged;
            item.onUnpluggedFromSlot += OnSelectedItemUnplugged;
            item.onSlotTreeChanged += OnSelectedItemSlotTreeChanged;

            _currentSubscribedItem = item;

            Debug.Log($"[BackpackShortcutManager] 已为选中物品 {item.DisplayName} 订阅所有生命周期事件");
        }

        /// <summary>
        /// 🎯 取消之前选中物品的事件订阅
        /// 在选择新物品或物品退出系统时调用
        /// </summary>
        private void UnsubscribeFromPreviousSelection()
        {
            if (_currentSubscribedItem != null)
            {
                // 取消所有事件订阅
                _currentSubscribedItem.onDestroy -= OnSelectedItemDestroyed;
                _currentSubscribedItem.onParentChanged -= OnSelectedItemParentChanged;
                _currentSubscribedItem.onUnpluggedFromSlot -= OnSelectedItemUnplugged;
                _currentSubscribedItem.onSlotTreeChanged -= OnSelectedItemSlotTreeChanged;

                Debug.Log($"[BackpackShortcutManager] 已取消之前选中物品 {_currentSubscribedItem.DisplayName} 的所有事件订阅");
                _currentSubscribedItem = null;
            }
        }

        /// <summary>
        /// 🎯 选中物品销毁事件处理 - 当选中的物品被完全消耗时触发
        /// 例如：可乐喝完、医疗用品用完等
        /// </summary>
        private void OnSelectedItemDestroyed(Item destroyedItem)
        {
            if (!IsShortcutSystemEnabled || _isRefreshing) return;

            Debug.Log($"[BackpackShortcutManager] 选中物品被销毁: {destroyedItem?.DisplayName}");

            if (destroyedItem == null) return;

            // 清理订阅关系
            if (_currentSubscribedItem == destroyedItem)
            {
                _currentSubscribedItem = null;
            }

            // 处理物品消耗后的UI更新
            HandleItemConsumption(destroyedItem);
        }

        /// <summary>
        /// 🎯 选中物品父级变化事件处理 - 当物品移动到不同的容器时触发
        /// 例如：物品从背包移动到地面，或从配件移动到背包
        /// </summary>
        private void OnSelectedItemParentChanged(Item item)
        {
            if (!IsShortcutSystemEnabled || _isRefreshing) return;
            if (item == null || item != _currentSubscribedItem) return;

            Debug.Log($"[BackpackShortcutManager] 选中物品父级变化: {item.DisplayName}");

            // 检查物品是否还在我们的系统范围内（在主背包或其配件中）
            if (!IsItemInOurSystem(item))
            {
                Debug.Log($"[BackpackShortcutManager] 选中物品已离开我们的系统范围: {item.DisplayName}");
                HandleItemExitSystem(item);
            }
        }

        /// <summary>
        /// 🎯 选中物品从插槽拔出事件处理
        /// </summary>
        private void OnSelectedItemUnplugged(Item item)
        {
            if (!IsShortcutSystemEnabled || _isRefreshing) return;
            if (item == null || item != _currentSubscribedItem) return;

            Debug.Log($"[BackpackShortcutManager] 选中物品从插槽拔出: {item.DisplayName}");

            // 物品从插槽拔出通常意味着离开了系统
            HandleItemExitSystem(item);
        }

        /// <summary>
        /// 🎯 选中物品插槽树结构变化事件处理
        /// </summary>
        private void OnSelectedItemSlotTreeChanged(Item item)
        {
            if (!IsShortcutSystemEnabled || _isRefreshing) return;
            if (item == null || item != _currentSubscribedItem) return;

            Debug.Log($"[BackpackShortcutManager] 选中物品插槽树结构变化: {item.DisplayName}");

            // 延迟检查，因为插槽树变化可能是暂时的
            StartCoroutine(CheckItemInSystemDelayed(item, 0.5f));
        }

        /// <summary>
        /// 🎯 检查物品是否在我们的系统范围内
        /// </summary>
        private bool IsItemInOurSystem(Item item)
        {
            if (item == null) return false;

            // 检查物品是否在主背包中
            if (_currentBackpack != null && IsItemInContainer(item, _currentBackpack))
            {
                return true;
            }

            // 检查物品是否在配件中
            foreach (var attachment in _subscribedAttachments)
            {
                if (IsItemInContainer(item, attachment))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 🎯 检查物品是否在指定容器中
        /// </summary>
        private bool IsItemInContainer(Item item, Item container)
        {
            if (item == null || container == null || container.Slots == null) return false;

            // 直接检查
            foreach (var slot in container.Slots)
            {
                if (slot != null && slot.Content == item)
                {
                    return true;
                }
            }

            // 递归检查子容器
            foreach (var slot in container.Slots)
            {
                if (slot != null && slot.Content != null)
                {
                    if (IsItemInContainer(item, slot.Content))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 🎯 处理物品退出系统
        /// 当物品离开我们的系统范围时调用
        /// </summary>
        private void HandleItemExitSystem(Item item)
        {
            if (item == null) return;

            Debug.Log($"[BackpackShortcutManager] 处理物品退出系统: {item.DisplayName}");

            // 取消订阅（如果还没取消的话）
            if (_currentSubscribedItem == item)
            {
                UnsubscribeFromPreviousSelection();
            }

            // 确定物品的类别
            var category = ItemCategorizer.CategorizeItem(item);
            if (category == ItemCategory.None) return;

            // 从轮盘布局中移除该物品
            _wheelLayoutManager.RemoveItem(category, item);

            // 更新UI（选择下一个物品或清空）
            UpdateShortcutUI(category);
        }

        /// <summary>
        /// 🎯 从轮盘布局中移除单个物品的集中签名方法
        /// 统一管理物品移除逻辑，便于维护和扩展
        /// </summary>
        private void RemoveItemFromWheelLayout(ItemCategory category, Item item)
        {
            if (item == null || category == ItemCategory.None)
            {
                Debug.LogWarning("[BackpackShortcutManager] RemoveItemFromWheelLayout: 物品或类别无效");
                return;
            }

            Debug.Log($"[BackpackShortcutManager] 从轮盘移除物品: {item.DisplayName} (类别: {category})");
            _wheelLayoutManager.RemoveItem(category, item);
        }

        /// <summary>
        /// 🎯 从快捷键UI中移除单个物品的集中签名方法
        /// 统一管理快捷键UI更新逻辑，便于维护和扩展
        /// </summary>
        private void RemoveItemFromShortcutUI(ItemCategory category)
        {
            if (category == ItemCategory.None)
            {
                Debug.LogWarning("[BackpackShortcutManager] RemoveItemFromShortcutUI: 类别无效");
                return;
            }

            Debug.Log($"[BackpackShortcutManager] 更新快捷键UI移除: 类别 {category}");
            UpdateShortcutUI(category);
        }

        /// <summary>
        /// 🎯 延迟检查物品是否还在系统中
        /// 避免临时变化导致的误判
        /// </summary>
        private IEnumerator CheckItemInSystemDelayed(Item item, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (item == null || _currentSubscribedItem != item)
            {
                yield break; // 物品已经不是当前选中物品了
            }

            if (!IsItemInOurSystem(item))
            {
                Debug.Log($"[BackpackShortcutManager] 延迟检查确认物品已离开系统: {item.DisplayName}");
                HandleItemExitSystem(item);
            }
            else
            {
                Debug.Log($"[BackpackShortcutManager] 延迟检查确认物品仍在系统中: {item.DisplayName}");
            }
        }


        /// <summary>
        /// 🎯 统一的物品消耗处理逻辑
        /// 在物品真正被消耗后调用，确保UI更新的时机正确
        /// </summary>
        private void HandleItemConsumption(Item consumedItem)
        {
            var category = ItemCategorizer.CategorizeItem(consumedItem);
            if (category == ItemCategory.None) return;

            Debug.Log($"[BackpackShortcutManager] 处理物品消耗: {consumedItem.DisplayName}, 类别: {category}");

            // 通过WheelLayoutManager更新格子状态（标记为已使用）
            _wheelLayoutManager.UpdateSlotAfterItemUsage(consumedItem);

            // 查找当前类别的所有有效物品
            var validItems = _wheelLayoutManager.GetValidItems(category);
            Debug.Log($"[BackpackShortcutManager] 类别 {category} 剩余有效物品数量: {validItems.Count}");

            if (validItems.Count > 0)
            {
                // 还有剩余物品，选择下一个
                var currentItem = _wheelLayoutManager.GetCurrentSelection(category);
                int currentIndex = currentItem != null ? validItems.IndexOf(currentItem) : -1;

                // 选择下一个物品（循环选择）
                int nextIndex = currentIndex >= 0 ? (currentIndex + 1) % validItems.Count : 0;
                var nextItem = validItems[nextIndex];

                Debug.Log($"[BackpackShortcutManager] 切换到下一个物品: {nextItem.DisplayName} (索引: {nextIndex})");

                // 🔧 修复：更新选择为物品引用
                _currentSelectedItem[category] = nextItem;

                // 🎯 为新的选中物品订阅销毁事件
                SubscribeToSelectedItem(nextItem);

                // 立即更新UI显示下一个物品
                var shortcutIndex = CategoryToIndex(category);
                if (shortcutIndex >= 0)
                {
                    bool success = ShortcutUIUpdater.TryUpdateShortcutUI(shortcutIndex, nextItem);
                    if (!success)
                    {
                        Debug.LogWarning($"[BackpackShortcutManager] 更新快捷键UI失败: {nextItem.DisplayName}");
                    }
                }

                // 事件会在WheelLayoutManager内部自动触发
            }
            else
            {
                // 没有剩余物品，清空UI
                Debug.Log($"[BackpackShortcutManager] 类别 {category} 无剩余物品，清空UI");

                // 🎯 取消选中物品的订阅（因为没有下一个物品了）
                UnsubscribeFromPreviousSelection();

                var shortcutIndex = CategoryToIndex(category);
                if (shortcutIndex >= 0)
                {
                    ShortcutUIUpdater.ClearShortcutUI(shortcutIndex);
                }

                // 事件会在WheelLayoutManager内部自动触发
            }
        }

        /// <summary>
        /// 背包变化处理（由Harmony补丁调用）
        /// </summary>
        public void OnBackpackChanged(Slot backpackSlot)
        {
            Debug.Log($"[BackpackShortcutManager] OnBackpackChanged 被调用，背包: {backpackSlot?.Content?.DisplayName ?? "null"}");
            Debug.Log($"[BackpackShortcutManager] 当前系统启用状态: {IsShortcutSystemEnabled}");

            // 🔧 暂时移除系统启用检查，允许处理背包事件
            // if (!IsShortcutSystemEnabled) return;

            Debug.Log("[BackpackShortcutManager] 背包发生变化，开始更新物品列表");

            var newBackpack = backpackSlot?.Content;
            Debug.Log($"[BackpackShortcutManager] 新背包: {newBackpack?.DisplayName ?? "null"}, 当前背包: {_currentBackpack?.DisplayName ?? "null"}");

            if (newBackpack != _currentBackpack)
            {
                // 取消旧背包的监听
                UnsubscribeFromBackpackChanges();

                // 更新当前背包
                _currentBackpack = newBackpack;
                Debug.Log($"[BackpackShortcutManager] 更新当前背包为: {_currentBackpack?.DisplayName ?? "null"}");

                // 订阅新背包的变化
                if (_currentBackpack != null)
                {
                    // 🔧 注册背包类型到ItemTypeRegistry
                    ItemTypeRegistry.RegisterBackpack(_currentBackpack.TypeID);

                    SubscribeToBackpackChanges(_currentBackpack);
                    Debug.Log("[BackpackShortcutManager] 已订阅新背包变化事件");
                }

                // 启用系统（如果有背包）
                bool shouldEnable = _currentBackpack != null;
                Debug.Log($"[BackpackShortcutManager] 应该启用系统: {shouldEnable}, 当前启用状态: {IsShortcutSystemEnabled}");

                if (shouldEnable != IsShortcutSystemEnabled)
                {
                    Debug.Log("[BackpackShortcutManager] 调用 SetShortcutSystemEnabled");
                    SetShortcutSystemEnabled(shouldEnable);
                }

                // 🆕 使用新架构刷新物品
                Debug.Log("[BackpackShortcutManager] 开始刷新物品...");
                RefreshItemsWithNewArchitecture();
            }
            else
            {
                Debug.Log("[BackpackShortcutManager] 背包未变化，跳过处理");
            }
        }

        /// <summary>
        /// 🆕 新架构：刷新物品列表
        /// 使用WheelLayoutManager替代双数组更新
        /// </summary>
        private void RefreshItemsWithNewArchitecture()
        {
            if (_currentBackpack == null)
            {
                Debug.Log("[BackpackShortcutManager] 背包为空，清空所有物品");

                // 清空所有类别
                foreach (ItemCategory category in System.Enum.GetValues(typeof(ItemCategory)))
                {
                    if (category != ItemCategory.None)
                    {
                        _wheelLayoutManager.ClearCategory(category);
                    }
                }
                return;
            }

            Debug.Log("[BackpackShortcutManager] 开始收集背包物品");

            // 收集所有物品
            var allItems = new HashSet<Item>();
            CollectAllItemsFromBackpack(_currentBackpack, allItems);

            // 按类别分类
            var categorizedItems = new Dictionary<ItemCategory, List<Item>>();
            foreach (var item in allItems)
            {
                var category = ItemCategorizer.CategorizeItem(item);
                if (category != ItemCategory.None)  // 跳过无效类别
                {
                    if (!categorizedItems.ContainsKey(category))
                    {
                        categorizedItems[category] = new List<Item>();
                    }
                    categorizedItems[category].Add(item);
                }
            }

            // 🆕 通过WheelLayoutManager更新所有类别
            foreach (var kvp in categorizedItems)
            {
                var category = kvp.Key;
                var items = kvp.Value;

                _wheelLayoutManager.UpdateFromCollectedItems(category, items);
                Debug.Log($"[BackpackShortcutManager] 更新类别 {category}: {items.Count} 个物品");
            }

            // 🆕 触发UI更新（通过统一的类别通知机制）
            foreach (var category in categorizedItems.Keys)
            {
                ShortcutUIUpdater.UpdateCategoryUI(category);
            }

            Debug.Log("[BackpackShortcutManager] 新架构物品刷新完成");
        }

  
    
        /// <summary>
        /// 设置快捷键系统启用状态
        /// </summary>
        private void SetShortcutSystemEnabled(bool enabled)
        {
            if (IsShortcutSystemEnabled != enabled)
            {
                IsShortcutSystemEnabled = enabled;

                if (!enabled)
                {
                    // 🚨 系统关闭时执行完整的数据清理
                    Debug.Log("[BackpackShortcutManager] 开始系统关闭数据清理...");
                    PerformSystemResetCleanup();
                }

                OnShortcutSystemStateChanged?.Invoke(enabled);
                Debug.Log($"[BackpackShortcutManager] 快捷键系统已{(enabled ? "启用" : "禁用")}");
            }
        }

        /// <summary>
        /// 🚨 执行系统重置时的数据清理
        /// 简化版本：清理核心数据和状态
        /// </summary>
        private void PerformSystemResetCleanup()
        {
            Debug.Log("[BackpackShortcutManager] 🔧 开始执行系统重置清理...");

            // 1. 清空所有快捷键UI
            for (int i = 0; i < 6; i++)
            {
                ShortcutUIUpdater.ClearShortcutUI(i);
            }

            // 2. 清空轮盘布局
            if (_wheelLayoutManager != null)
            {
                foreach (ItemCategory category in System.Enum.GetValues(typeof(ItemCategory)))
                {
                    if (category != ItemCategory.None)
                    {
                        _wheelLayoutManager.ClearCategory(category);
                    }
                }
            }

            // 3. 清理数据结构
            _currentSelectedItem?.Clear();
            _subscribedAttachments?.Clear();
            _subscribedAttachmentSlots?.Clear();
            _slotContentHistory?.Clear();
            _attachmentCategories?.Clear();

            // 4. 停止协程
            if (_pendingAttachmentUpdateCoroutine != null)
            {
                StopCoroutine(_pendingAttachmentUpdateCoroutine);
                _pendingAttachmentUpdateCoroutine = null;
            }

            // 5. 重置状态
            _isRefreshing = false;

            // 6. 清理类型缓存
            ItemTypeRegistry.ClearAll();

            Debug.Log("[BackpackShortcutManager] 🔧 系统重置清理完成");
        }

        /// <summary>
        /// 🆕 批量更新所有类别的UI（性能优化）
        /// </summary>
        private System.Collections.IEnumerator UpdateAllCategoriesUIAsync()
        {
            Debug.Log("[BackpackShortcutManager] 开始批量更新UI");

            // 获取所有有物品的类别
            var categoriesToUpdate = new List<ItemCategory>();
            foreach (ItemCategory category in System.Enum.GetValues(typeof(ItemCategory)))
            {
                if (category != ItemCategory.None)
                {
                    var items = _wheelLayoutManager.GetValidItems(category);
                    if (items.Count > 0)
                    {
                        categoriesToUpdate.Add(category);
                    }
                }
            }

            // 🔧 新增：跟踪失败的UI更新，用于重试
            var failedCategories = new List<ItemCategory>();

            // 分帧更新UI，避免卡顿
            foreach (var category in categoriesToUpdate)
            {
                bool success = UpdateShortcutUIWithRetry(category);
                if (!success)
                {
                    failedCategories.Add(category);
                    Debug.LogWarning($"[BackpackShortcutManager] 类别 {category} UI更新失败，将重试");
                }
                yield return null; // 等待一帧
            }

            // 🔧 新增：对失败的类别进行重试
            if (failedCategories.Count > 0)
            {
                Debug.Log($"[BackpackShortcutManager] 开始重试 {failedCategories.Count} 个失败的UI更新");
                yield return new WaitForSeconds(0.5f); // 等待0.5秒后重试

                var retrySuccess = new List<ItemCategory>();
                foreach (var category in failedCategories)
                {
                    bool success = UpdateShortcutUIWithRetry(category);
                    if (success)
                    {
                        retrySuccess.Add(category);
                        Debug.Log($"[BackpackShortcutManager] 类别 {category} 重试成功");
                    }
                    else
                    {
                        Debug.LogError($"[BackpackShortcutManager] 类别 {category} 重试仍然失败，可能游戏系统未就绪");
                    }
                    yield return null; // 等待一帧
                }

                // 移除重试成功的类别
                foreach (var successCategory in retrySuccess)
                {
                    failedCategories.Remove(successCategory);
                }
            }

            int successCount = categoriesToUpdate.Count - failedCategories.Count;
            Debug.Log($"[BackpackShortcutManager] 批量UI更新完成：成功 {successCount} 个，失败 {failedCategories.Count} 个");
        }

        /// <summary>
        /// 初始化完成后启用系统（混合方案版本）
        /// </summary>
        private void EnableSystemAfterInit()
        {
            Debug.Log("[BackpackShortcutManager] 初始化完成，系统就绪，等待背包装备事件...");

            // 启用系统框架
            if (!IsShortcutSystemEnabled)
            {
                SetShortcutSystemEnabled(true);
                Debug.Log("[BackpackShortcutManager] 系统框架已启用，等待背包装备事件或主动检查...");
            }

            // 🔧 混合方案：不再需要主动检查和延迟刷新
            // 系统会通过OnBackpackChanged事件或ModBehaviour的CheckCurrentBackpackStatus来激活
        }

        /// <summary>
        /// 取消订阅背包变化事件
        /// </summary>
        private void UnsubscribeFromBackpackChanges()
        {
            // 取消背包内容变化监听
            if (_currentBackpack != null)
            {
                // 🚨 移除冗余事件处理：OnBackpackContentChanged已完全移除
                // _currentBackpack.onSlotContentChanged -= OnBackpackContentChanged;

                // 🔧 注销背包类型
                ItemTypeRegistry.UnregisterBackpack(_currentBackpack.TypeID);

                // 取消所有配件的事件监听并注销类型
                foreach (var attachment in _subscribedAttachments)
                {
                    if (attachment != null)
                    {
                        // 🔧 修复：取消配件内部所有slot的事件订阅
                        if (attachment.Slots != null)
                        {
                            foreach (var internalSlot in attachment.Slots)
                            {
                                if (internalSlot != null)
                                {
                                    internalSlot.onSlotContentChanged -= OnAttachmentInternalSlotChanged;
                                }
                            }
                        }
                        // 🔧 注销配件类型
                        ItemTypeRegistry.UnregisterAttachment(attachment.TypeID);
                    }
                }
                _subscribedAttachments.Clear();

                // 🔧 取消所有配件slot的事件监听
                foreach (var attachmentSlot in _subscribedAttachmentSlots)
                {
                    if (attachmentSlot != null)
                    {
                        attachmentSlot.onSlotContentChanged -= OnAttachmentSlotContentChanged;
                    }
                }
                _subscribedAttachmentSlots.Clear();

                // 🔧 清理slot内容历史记录
                _slotContentHistory.Clear();

                // 🔧 取消所有物品的拔出事件监听
                foreach (var item in _subscribedItems)
                {
                    if (item != null)
                    {
                        item.onUnpluggedFromSlot -= OnItemUnpluggedFromSlot;
                    }
                }
                _subscribedItems.Clear();
            }

            Debug.Log("[BackpackShortcutManager] 已取消背包变化事件订阅");
        }

        /// <summary>
        /// 订阅背包变化事件
        /// </summary>
        private void SubscribeToBackpackChanges(Item backpack)
        {
            if (backpack == null) return;

            // 订阅背包内容变化
            // 🚨 移除冗余事件处理：OnBackpackContentChanged与OnAttachmentSlotContentChanged功能重复
            // 配件slot的变化完全由OnAttachmentSlotContentChanged处理，无需重复监听
            // backpack.onSlotContentChanged += OnBackpackContentChanged;
            Debug.Log("[BackpackShortcutManager] 已订阅背包内容变化事件");

            // 🚨 修复：正确订阅背包的配件slot变化事件
            if (backpack.Slots != null)
            {
                var attachmentSlotIndices = GetAttachmentSlotIndices(backpack);
                Debug.Log($"[BackpackShortcutManager] 🎯 开始订阅背包配件slot，总slot数: {backpack.Slots.Count}，配件slot索引: [{string.Join(", ", attachmentSlotIndices)}]");

                for (int i = 0; i < backpack.Slots.Count; i++)
                {
                    var slot = backpack.Slots[i];
                    if (attachmentSlotIndices.Contains(i))
                    {
                        // ✅ 正确：订阅背包的配件slot（如SidePocket_Large）
                        slot.onSlotContentChanged += OnAttachmentSlotContentChanged;
                        _subscribedAttachmentSlots.Add(slot);
                        Debug.Log($"[BackpackShortcutManager] ✅ 订阅背包配件slot变化: {slot?.Key} (索引: {i})");

                        // 如果slot已有内容，注册配件类型并建立历史记录
                        if (slot != null && slot.Content != null)
                        {
                            var attachment = slot.Content;
                            ItemTypeRegistry.RegisterAttachment(attachment.TypeID);

                            // 🔧 修复：建立初始历史记录，解决游戏重启后历史记录缺失问题
                            _slotContentHistory[slot] = attachment;
                            Debug.Log($"[BackpackShortcutManager] 注册已存在配件类型: {attachment.DisplayName} (TypeID: {attachment.TypeID})");
                            Debug.Log($"[BackpackShortcutManager] 📝 建立初始历史记录: {attachment.DisplayName} 在 slot {slot.Key}");

                            // 🔧 新增：建立初始配件类别记录，解决配件类别记录缺失问题
                            BuildAttachmentCategories(attachment);

                            // 🔧 新增：订阅已存在配件的内部物品拔出事件，解决初始化事件订阅缺失问题
                            SubscribeToExistingItemsInAttachment(attachment);
                            Debug.Log($"[BackpackShortcutManager] 🔧 订阅已存在配件 {attachment.DisplayName} 的内部物品事件");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 🔧 建立配件类别记录的抽象方法
        /// 收集配件内部所有物品的类别信息并记录到_attachmentCategories字典
        /// </summary>
        private void BuildAttachmentCategories(Item attachment)
        {
            if (attachment == null || attachment.Slots == null) return;

            Debug.Log($"[BackpackShortcutManager] 🔧 建立配件类别记录: {attachment.DisplayName}");

            // 记录配件包含的物品类别
            var attachmentCategories = new HashSet<ItemCategory>();

            foreach (var internalSlot in attachment.Slots)
            {
                if (internalSlot?.Content != null)
                {
                    Item internalItem = internalSlot.Content;
                    ItemCategory category = ItemCategorizer.CategorizeItem(internalItem);

                    if (category != ItemCategory.None)
                    {
                        attachmentCategories.Add(category);
                    }
                }
            }

            // 保存配件包含的类别信息
            _attachmentCategories[attachment] = attachmentCategories;
            Debug.Log($"[BackpackShortcutManager] 📝 记录配件 {attachment.DisplayName} 包含的类别: {string.Join(", ", attachmentCategories)}");
        }

        /// <summary>
        /// 🎯 根据背包配置获取配件slot的索引
        /// </summary>
        private List<int> GetAttachmentSlotIndices(Item backpack)
        {
            var attachmentIndices = new List<int>();

            if (backpack?.TypeID == null) return attachmentIndices;

            // 查找当前背包类型的配置
            if (BackpackModConfig.BackpackSlotConfigs.TryGetValue(backpack.TypeID, out var slotConfigs))
            {
                // 获取所有配件slot的名称
                var attachmentSlotNames = new HashSet<string>(slotConfigs);
                Debug.Log($"[BackpackShortcutManager] 背包 {backpack.DisplayName} (TypeID: {backpack.TypeID}) 配件slot: {string.Join(", ", attachmentSlotNames)}");

                // 根据slot名称匹配实际slot的requireTags
                for (int i = 0; i < backpack.Slots.Count; i++)
                {
                    var slot = backpack.Slots[i];
                    if (slot?.requireTags != null)
                    {
                        foreach (var requireTag in slot.requireTags)
                        {
                            if (requireTag != null && attachmentSlotNames.Contains(requireTag.name))
                            {
                                attachmentIndices.Add(i);
                                Debug.Log($"[BackpackShortcutManager] 找到配件slot: {i} (requireTag: {requireTag.name})");
                                break;
                            }
                        }
                    }
                }
            }

            return attachmentIndices;
        }

        /// <summary>
        /// 🚨 已移除：OnBackpackContentChanged方法
        /// 原因：与OnAttachmentSlotContentChanged功能完全重复
        /// 配件slot的变化完全由OnAttachmentSlotContentChanged处理，无需重复监听
        /// ItemTypeRegistry注册功能已合并到OnAttachmentSlotContentChanged中
        /// </summary>

    
        /// <summary>
        /// 快捷键输入处理
        /// </summary>
        private void OnUIShortcutInput(UIInputEventData eventData, int index)
        {
            if (!IsShortcutSystemEnabled) return;

            var category = IndexToCategory(index);
            if (category != ItemCategory.None)
            {
                HandleShortcutInput(category);
            }
        }

        /// <summary>
        /// 将快捷键索引转换为类别
        /// </summary>
        public static ItemCategory IndexToCategory(int index)
        {
            switch (index)
            {
                case 0: return ItemCategory.Medical;
                case 1: return ItemCategory.Stim;
                case 2: return ItemCategory.Food;
                case 3: return ItemCategory.Explosive;
                default: return ItemCategory.None;
            }
        }

        /// <summary>
        /// 处理快捷键输入 - 只负责使用物品，UI更新由OnItemUsedStatic处理
        /// </summary>
        public void HandleShortcutInput(ItemCategory category)
        {
            if (!IsShortcutSystemEnabled) return;

            // 🆕 新架构：从WheelLayoutManager获取当前选中的物品
            var currentItem = _wheelLayoutManager.GetCurrentSelection(category);
            if (currentItem != null)
            {
                Debug.Log($"[BackpackShortcutManager] 使用物品: {currentItem.DisplayName}");

                // 只负责使用物品，不处理任何UI更新
                // UI更新会在物品使用完成后通过OnItemUsedStatic自动处理
                ItemUsageHandler.UseItem(currentItem, category);
            }
            else
            {
                Debug.Log($"[BackpackShortcutManager] 类别 {category} 没有可用物品");
            }
        }

    
        /// <summary>
        /// 检查是否为背包快捷键索引
        /// </summary>
        public static bool IsBackpackShortcutIndex(int index)
        {
            return index >= 0 && index <= 3; // 0-3: 医疗, 兴奋剂, 食物, 爆炸物
        }

        /// <summary>
        /// 获取当前选中的物品（委托方法）
        /// 🔧 新增：简单判断当前选中物品是否有效并返回
        /// </summary>
        private Item GetCurrentSelectionByCategory(ItemCategory category)
        {
            if (_currentSelectedItem.TryGetValue(category, out Item selectedItem) &&
                selectedItem != null && !selectedItem.IsBeingDestroyed)
            {
                Debug.Log($"[BackpackShortcutManager] 获取当前选中 {category}: {selectedItem.DisplayName}");
                return selectedItem; // 直接返回选中的物品
            }

            Debug.Log($"[BackpackShortcutManager] 无有效选中物品 {category}");
            return null; // 没有选中或选中无效
        }

        /// <summary>
        /// 获取当前选中的物品
        /// </summary>
        public Item GetCurrentItem(ItemCategory category)
        {
            if (_instance == null) return null;
            return _instance.GetCurrentSelectionByCategory(category);
        }

        /// <summary>
        /// 保存轮盘布局
        /// </summary>
        public void SaveWheelLayout(ItemCategory category, List<Item> arrangedItems)
        {
            Debug.Log($"[BackpackShortcutManager] 保存轮盘布局: {category}");
            // 使用新架构保存
            WheelLayoutPersistence.SaveWheelSlots(_wheelLayoutManager);
        }

        
        /// <summary>
        /// 更新快捷键UI
        /// </summary>
        private void UpdateShortcutUI(ItemCategory category)
        {
            var index = CategoryToIndex(category);
            if (index < 0) return;

            // 🆕 新架构：从WheelLayoutManager获取当前物品
            var currentItem = _wheelLayoutManager.GetCurrentSelection(category);

            // 更新UI
            if (currentItem != null)
            {
                ShortcutUIUpdater.TryUpdateShortcutUI(index, currentItem);

                // 🎯 为当前选中的物品订阅销毁事件
                SubscribeToSelectedItem(currentItem);
            }
            else
            {
                ShortcutUIUpdater.ClearShortcutUI(index);

                // 🎯 没有选中物品时取消订阅
                UnsubscribeFromPreviousSelection();
            }
        }

        /// <summary>
        /// 🔧 新增：带重试机制的UI更新
        /// </summary>
        /// <param name="category">物品类别</param>
        /// <returns>是否更新成功</returns>
        private bool UpdateShortcutUIWithRetry(ItemCategory category)
        {
            var index = CategoryToIndex(category);
            if (index < 0) return false;

            // 🆕 新架构：从WheelLayoutManager获取当前物品
            var currentItem = _wheelLayoutManager.GetCurrentSelection(category);

            // 更新UI
            if (currentItem != null)
            {
                bool success = ShortcutUIUpdater.TryUpdateShortcutUI(index, currentItem);
                if (success)
                {
                    Debug.Log($"[BackpackShortcutManager] ✓ 类别 {category} UI更新成功: {currentItem.DisplayName}");
                }
                else
                {
                    Debug.LogWarning($"[BackpackShortcutManager] ✗ 类别 {category} UI更新失败: {currentItem.DisplayName}");
                }
                return success;
            }
            else
            {
                // 清空UI通常不会失败
                ShortcutUIUpdater.ClearShortcutUI(index);
                Debug.Log($"[BackpackShortcutManager] ✓ 类别 {category} UI已清空（无物品）");
                return true;
            }
        }

      
        /// <summary>
        /// 持久化轮盘布局
        /// </summary>
        public void PersistWheelLayouts()
        {
            Debug.Log("[BackpackShortcutManager] 持久化轮盘布局");
            WheelLayoutPersistence.SaveWheelSlots(_wheelLayoutManager);
        }

        /// <summary>
        /// 设置当前选择
        /// 🔧 修复：直接设置选中记录，维护本层数据源
        /// </summary>
        public void SetCurrentSelection(ItemCategory category, Item selectedItem)
        {
            if (selectedItem == null || selectedItem.IsBeingDestroyed)
            {
                Debug.LogWarning($"[BackpackShortcutManager] 尝试设置无效选中物品: {selectedItem?.DisplayName ?? "null"}");
                return;
            }

            // 验证物品是否在类别中
            var validItems = _wheelLayoutManager.GetValidItems(category);
            if (validItems.Contains(selectedItem))
            {
                _currentSelectedItem[category] = selectedItem;
                Debug.Log($"[BackpackShortcutManager] 设置 {category} 选中物品: {selectedItem.DisplayName}");
            }
            else
            {
                Debug.LogWarning($"[BackpackShortcutManager] 物品 {selectedItem.DisplayName} 不在类别 {category} 中");
            }
        }

        #region 🔧 选中状态调整方法

        /// <summary>
        /// 🔧 处理物品添加时的选中状态调整
        /// 当物品被添加到类别中时，调整选中状态
        /// </summary>
        private void AdjustSelectionForItemAddition(ItemCategory category, Item addedItem)
        {
            Debug.Log($"[BackpackShortcutManager] 🔧 调整选中状态 - 物品添加: {addedItem.DisplayName} (类别: {category})");

            // 获取当前选中物品
            Item currentSelection = GetCurrentSelectionByCategory(category);

            if (currentSelection == null)
            {
                // 情况1：当前无选中物品，自动选中新添加的物品
                _currentSelectedItem[category] = addedItem;
                Debug.Log($"[BackpackShortcutManager] ✓ 自动选中新物品: {addedItem.DisplayName}");
            }
            else
            {
                // 情况2：已有选中物品，保持当前选中不变
                Debug.Log($"[BackpackShortcutManager] ✓ 保持当前选中: {currentSelection.DisplayName}");
            }
        }

        /// <summary>
        /// 🔧 处理物品移除时的选中状态调整
        /// 当物品从类别中被移除时，调整选中状态
        /// </summary>
        private void AdjustSelectionForItemRemoval(ItemCategory category, Item removedItem)
        {
            Debug.Log($"[BackpackShortcutManager] 🔧 调整选中状态 - 物品移除: {removedItem.DisplayName} (类别: {category})");

            // 获取当前选中物品
            Item currentSelection = GetCurrentSelectionByCategory(category);

            if (currentSelection == null)
            {
                // 情况1：当前无选中物品，无需处理
                Debug.Log($"[BackpackShortcutManager] 当前无选中物品，无需调整");
                return;
            }

            // 检查被移除的是否是当前选中物品
            if (currentSelection == removedItem)
            {
                // 情况2：移除的是当前选中物品，需要自动选择下一个

                // 2.1 首先尝试从轮盘布局中选择下一个
                Item nextSelection = _wheelLayoutManager.GetNextItemByLayout(category, removedItem);

                if (nextSelection != null)
                {
                    _currentSelectedItem[category] = nextSelection;
                    Debug.Log($"[BackpackShortcutManager] ✓ 轮盘布局中选择下一个物品: {nextSelection.DisplayName}");
                    return;
                }

                // 2.2 轮盘布局中没有找到，检查manager中是否有其他物品
                var allValidItems = _wheelLayoutManager.GetValidItems(category);
                if (allValidItems.Count > 0)
                {
                    // 选择第一个有效物品
                    Item fallbackSelection = allValidItems[0];
                    _currentSelectedItem[category] = fallbackSelection;
                    Debug.Log($"[BackpackShortcutManager] ✓ 备选方案：选择第一个有效物品: {fallbackSelection.DisplayName}");
                }
                else
                {
                    // 完全没有其他物品了，清空选中
                    _currentSelectedItem[category] = null;
                    Debug.Log($"[BackpackShortcutManager] ✓ 类别无其他物品，清空选中");
                }
            }
            else
            {
                // 情况3：移除的不是当前选中物品，选中状态不变
                Debug.Log($"[BackpackShortcutManager] ✓ 移除的不是选中物品，保持选中: {currentSelection.DisplayName}");
            }
        }

        #endregion

        #region 🔧 配件移动检测机制

        /// <summary>
        /// 🔧 配件槽位内容变化事件处理 - 处理配件本身的放入/取出
        /// 参数确认：Slot slot - 配件槽位本身（来自源码Slot.cs:144）
        /// </summary>
        private void OnAttachmentSlotContentChanged(Slot slot)
        {
            if (!IsShortcutSystemEnabled || _isRefreshing) return;

            Item attachmentItem = slot?.Content;  // 这里是配件本身（如工具箱）
            Debug.Log($"[BackpackShortcutManager] 配件slot内容变化: {attachmentItem?.DisplayName ?? "null"}, slot: {slot?.Key}");

            if (attachmentItem != null)
            {
                // 🔧 关键修复：防止把背包当作配件处理
                if (ItemTypeRegistry.IsBackpack(attachmentItem))
                {
                    Debug.LogWarning($"[BackpackShortcutManager] ❌ 错误：背包 {attachmentItem.DisplayName} 被当作配件处理，跳过配件事件处理");
                    Debug.LogWarning($"[BackpackShortcutManager] 这个问题通常意味着背包被错误地放入了配件槽位");

                    // 🔧 记录slot内容历史（即使是背包也要记录，以便正确处理移除）
                    _slotContentHistory[slot] = attachmentItem;
                    return; // 跳过处理，防止把背包当作配件
                }

                // 🔧 记录slot内容历史，用于后续移除时的类型识别
                _slotContentHistory[slot] = attachmentItem;

                // 🚨 新增：将OnBackpackContentChanged的有用功能合并过来
                // 注册配件类型到ItemTypeRegistry
                ItemTypeRegistry.RegisterAttachment(attachmentItem.TypeID);
                Debug.Log($"[BackpackShortcutManager] 配件类型注册: {attachmentItem.DisplayName} (TypeID: {attachmentItem.TypeID})");

                // 🔧 配件被放入背包槽
                HandleAttachmentPutIntoSlot(attachmentItem, slot);

                // 🔧 修复流程B：订阅配件所有内部slot的onSlotContentChanged事件，而不是配件Item的事件
                if (!_subscribedAttachments.Contains(attachmentItem))
                {
                    _subscribedAttachments.Add(attachmentItem);
                    Debug.Log($"[BackpackShortcutManager] 订阅配件: {attachmentItem.DisplayName} (TypeID: {attachmentItem.TypeID})");

                    // 订阅配件所有内部slot的事件（处理物品插入）
                    if (attachmentItem.Slots != null)
                    {
                        foreach (var internalSlot in attachmentItem.Slots)
                        {
                            internalSlot.onSlotContentChanged += OnAttachmentInternalSlotChanged;
                            _subscribedAttachmentSlots.Add(internalSlot);
                            Debug.Log($"[BackpackShortcutManager] ✅ 订阅新配件内部slot变化: {internalSlot?.Key} (配件: {attachmentItem.DisplayName})");
                        }
                    }

                    // 🔧 立即订阅配件内部已有物品的拔出事件
                    SubscribeToExistingItemsInAttachment(attachmentItem);
                }
            }
            else
            {
                // 🔧 配件被取出，但先检查被移除的是什么类型
                HandleAttachmentRemovedFromSlot(slot);
            }
        }

        /// <summary>
        /// 🔧 物品拔出事件处理 - 专门处理物品**拖出**事件
        /// 这个事件能获取被拖出物品的精确引用，用于精确的单类别更新
        /// </summary>
        private void OnItemUnpluggedFromSlot(Item unpluggedItem)
        {
            if (!IsShortcutSystemEnabled || _isRefreshing) return;

            if (unpluggedItem == null) return;

            Debug.Log($"[BackpackShortcutManager] 🔧 物品拔出事件: {unpluggedItem.DisplayName} (TypeID: {unpluggedItem.TypeID})");

            // 确定被拔出物品的类别
            ItemCategory affectedCategory = ItemCategorizer.CategorizeItem(unpluggedItem);
            Debug.Log($"[BackpackShortcutManager] 物品拔出，影响类别: {affectedCategory} ({unpluggedItem.DisplayName})");

            // 🏗️ 重构：Manager统一协调所有系统更新，避免事件循环
            Debug.Log($"[BackpackShortcutManager] Manager统一处理物品移除: {unpluggedItem.DisplayName} (类别: {affectedCategory})");

            if (affectedCategory != ItemCategory.None)
            {
                // 🏗️ 重构：Manager调用各系统的自我管理方法
                // 1. 先更新轮盘系统的数据
                _wheelLayoutManager.RemoveItem(affectedCategory, unpluggedItem);

                // 2. 🔧 新增：调整选中状态
                AdjustSelectionForItemRemoval(affectedCategory, unpluggedItem);

                // 3. 再通知UI系统自我更新
                ShortcutUIUpdater.HandleItemRemoved(affectedCategory, unpluggedItem);

                Debug.Log($"[BackpackShortcutManager] ✓ 已通知各系统移除物品: {unpluggedItem.DisplayName}");
            }
            else
            {
                Debug.Log($"[BackpackShortcutManager] 被拔出的物品 {unpluggedItem.DisplayName} 无有效类别，跳过更新");
            }
        }

        /// <summary>
        /// 🔧 处理物品放入slot的事件
        /// </summary>
        private void HandleItemPutIntoSlot(Item item, Slot slot)
        {
            Debug.Log($"[BackpackShortcutManager] 🔧 物品放入: {item.DisplayName} (引用: {item.GetHashCode()}) 到 slot: {slot?.Key}");

            // 检查物品是否是配件类型（通过ItemTypeRegistry）
            if (ItemTypeRegistry.IsAttachment(item))
            {
                Debug.Log($"[BackpackShortcutManager] 检测到配件放入配件slot: {item.DisplayName} (TypeID: {item.TypeID})");
            }
            else
            {
                Debug.Log($"[BackpackShortcutManager] 检测到普通物品放入配件slot: {item.DisplayName} (TypeID: {item.TypeID})");
            }

            // 确定物品类别并触发单类别更新
            ItemCategory affectedCategory = ItemCategorizer.CategorizeItem(item);
            Debug.Log($"[BackpackShortcutManager] 物品放入，影响类别: {affectedCategory} ({item.DisplayName})");

            if (_pendingAttachmentUpdateCoroutine != null)
            {
                StopCoroutine(_pendingAttachmentUpdateCoroutine);
            }

            if (affectedCategory != ItemCategory.None)
            {
                // 🎯 简单方案：直接调用各系统更新，避免复杂的事件链
                Debug.Log($"[BackpackShortcutManager] 直接调用各系统更新: {item.DisplayName} (类别: {affectedCategory})");

                // 直接调用各系统的更新方法
                UpdateWheelLayout(affectedCategory, item);
                UpdateShortcutUI(affectedCategory, item);
            }
            else
            {
                Debug.Log($"[BackpackShortcutManager] 放入的物品 {item.DisplayName} 无有效类别，跳过更新");
            }
        }

        /// <summary>
        /// 🆕 更新轮盘布局 - 简单直接的方式
        /// 直接操作轮盘系统，不通过复杂的事件链
        /// </summary>
        private void UpdateWheelLayout(ItemCategory category, Item newItem)
        {
            if (_wheelLayoutManager == null)
            {
                Debug.LogError("[BackpackShortcutManager] WheelLayoutManager 为空，无法更新轮盘布局");
                return;
            }

            Debug.Log($"[BackpackShortcutManager] 更新轮盘布局: {category} + {newItem.DisplayName}");

            // 直接添加物品到轮盘
            _wheelLayoutManager.AddItemToCategory(category, newItem);
        }

        /// <summary>
        /// 🆕 更新快捷键UI - 简单直接的方式
        /// 直接操作UI系统，不通过复杂的事件链
        /// </summary>
        private void UpdateShortcutUI(ItemCategory category, Item item)
        {
            int shortcutIndex = CategoryToIndex(category);
            if (shortcutIndex < 0)
            {
                Debug.LogWarning($"[BackpackShortcutManager] 类别 {category} 无对应快捷键索引");
                return;
            }

            Debug.Log($"[BackpackShortcutManager] 更新快捷键UI: {item.DisplayName} → 快捷键{shortcutIndex}");

            // 直接更新快捷键UI
            bool success = ShortcutUIUpdater.TryUpdateShortcutUI(shortcutIndex, item);
            if (!success)
            {
                Debug.LogWarning($"[BackpackShortcutManager] 快捷键UI更新失败: {item.DisplayName}");
            }
        }

        /// <summary>
        /// 🔧 配件内部槽位变化事件处理 - 处理配件内部物品的放入/取出
        /// 重写说明：Slot.onSlotContentChanged事件只提供Slot参数，专注处理slot内容变化
        /// </summary>
        private void OnAttachmentInternalSlotChanged(Slot internalSlot)
        {
            if (!IsShortcutSystemEnabled || _isRefreshing) return;

            Item internalItem = internalSlot?.Content;  // 配件内部的物品（如可乐）
            Debug.Log($"[BackpackShortcutManager] 配件内部槽位变化: {internalItem?.DisplayName ?? "null"}, slot: {internalSlot?.Key}");

            if (internalItem != null)
            {
                // 🔧 物品被放入配件内部槽位
                HandleItemPutIntoAttachmentSlot(internalItem, internalSlot);

                // 🔧 订阅物品的拔出事件
                if (!_subscribedItems.Contains(internalItem))
                {
                    internalItem.onUnpluggedFromSlot += OnItemUnpluggedFromSlot;
                    _subscribedItems.Add(internalItem);
                    Debug.Log($"[BackpackShortcutManager] 订阅配件内部物品拔出事件: {internalItem.DisplayName} (TypeID: {internalItem.TypeID})");
                }
            }
            else
            {
                // 🔧 物品从配件内部槽位取出，但物品拔出事件应该由OnItemUnpluggedFromSlot处理
                Debug.Log($"[BackpackShortcutManager] 配件内部槽位变空，等待物品拔出事件处理");
            }
        }

        /// <summary>
        /// 🔧 订阅配件内部已有物品的拔出事件
        /// 在配件被放入时立即调用，确保已有物品也被正确监听
        /// </summary>
        private void SubscribeToExistingItemsInAttachment(Item attachment)
        {
            if (attachment?.Slots == null) return;

            foreach (var slot in attachment.Slots)
            {
                // 🔧 修复流程A：订阅配件内部slot的onSlotContentChanged事件（处理物品插入）
                slot.onSlotContentChanged += OnAttachmentInternalSlotChanged;
                _subscribedAttachmentSlots.Add(slot);
                Debug.Log($"[BackpackShortcutManager] ✅ 订阅配件内部slot变化: {slot?.Key} (配件: {attachment.DisplayName})");

                // 同时订阅已存在物品的拔出事件（处理物品拔出）
                if (slot?.Content != null && !_subscribedItems.Contains(slot.Content))
                {
                    slot.Content.onUnpluggedFromSlot += OnItemUnpluggedFromSlot;
                    _subscribedItems.Add(slot.Content);
                    Debug.Log($"[BackpackShortcutManager] 订阅配件内已有物品拔出事件: {slot.Content.DisplayName} (TypeID: {slot.Content.TypeID})");
                }
            }
        }

        
        /// <summary>
        /// 🎯 配件变化后刷新所有分类 - 简化版本，直接触发增量更新
        /// </summary>
        private void RefreshCategoriesAfterAttachmentChange()
        {
            Debug.Log("[BackpackShortcutManager] 配件变化，刷新所有分类");

            // 停止之前的更新协程
            if (_pendingAttachmentUpdateCoroutine != null)
            {
                StopCoroutine(_pendingAttachmentUpdateCoroutine);
            }

            // 启动新的增量更新
            _pendingAttachmentUpdateCoroutine = StartCoroutine(IncrementalUpdateCategorizedItems());
        }

        #endregion

        /// <summary>
        /// 🔧 处理配件被放入背包槽的事件
        /// </summary>
        private void HandleAttachmentPutIntoSlot(Item attachment, Slot slot)
        {
            Debug.Log($"[BackpackShortcutManager] 🔧 配件放入: {attachment.DisplayName} (TypeID: {attachment.TypeID}) 到 slot: {slot?.Key}");

            // 🔧 配件被放入，需要收集配件内部的物品
            if (attachment.Slots != null && attachment.Slots.Count > 0)
            {
                // 收集配件内所有物品并按类别更新
                var itemsByCategory = new Dictionary<ItemCategory, List<Item>>();

                foreach (var internalSlot in attachment.Slots)
                {
                    if (internalSlot?.Content != null)
                    {
                        Item internalItem = internalSlot.Content;
                        ItemCategory category = ItemCategorizer.CategorizeItem(internalItem);

                        if (category != ItemCategory.None)
                        {
                            if (!itemsByCategory.ContainsKey(category))
                            {
                                itemsByCategory[category] = new List<Item>();
                            }
                            itemsByCategory[category].Add(internalItem);
                        }
                    }
                }

                // 🔧 使用抽象方法建立配件类别记录
                BuildAttachmentCategories(attachment);

                // 按类别更新轮盘
                foreach (var kvp in itemsByCategory)
                {
                    ItemCategory category = kvp.Key;
                    List<Item> items = kvp.Value;

                    Debug.Log($"[BackpackShortcutManager] 配件 {attachment.DisplayName} 贡献类别 {category}: {items.Count} 个物品");

                    // 收集该类别的所有物品（包括背包和其他配件中的）
                    var allCategoryItems = new List<Item>();
                    CollectCategoryItemsFromBackpack(_currentBackpack, category, allCategoryItems);

                    // 更新轮盘布局
                    _wheelLayoutManager.UpdateFromCollectedItems(category, allCategoryItems);
                }
            }
            else
            {
                Debug.Log($"[BackpackShortcutManager] 配件 {attachment.DisplayName} 无内部槽位");
            }
        }

        /// <summary>
        /// 🔧 处理配件从背包槽被移除的事件
        /// </summary>
        private void HandleAttachmentRemovedFromSlot(Slot slot)
        {
            Debug.Log($"[BackpackShortcutManager] 🔧 物品从slot移除: {slot?.Key}");

            // 🔧 关键修复：检查历史记录，确定被移除的物品类型
            if (_slotContentHistory.TryGetValue(slot, out Item removedItem))
            {
                Debug.Log($"[BackpackShortcutManager] 🔍 从历史记录识别被移除物品: {removedItem.DisplayName} (TypeID: {removedItem.TypeID})");

                // 🔧 检查被移除物品的类型
                if (ItemTypeRegistry.IsBackpack(removedItem))
                {
                    Debug.LogWarning($"[BackpackShortcutManager] ⚠️ 被移除的是背包: {removedItem.DisplayName}，不当作配件移除处理");
                    Debug.LogWarning($"[BackpackShortcutManager] 这应该由背包变化事件处理，而不是配件移除事件");

                    // 🔧 清理历史记录
                    _slotContentHistory.Remove(slot);
                    return; // 不处理背包移除，留给背包事件系统
                }
                else
                {
                    Debug.Log($"[BackpackShortcutManager] ✅ 确认被移除的是配件: {removedItem.DisplayName}，执行配件移除逻辑");

                    // 🔧 配件移除时清空所有常见类别（原有逻辑）
                    ClearAllCategoriesForAttachment(removedItem);

                    // 🔧 取消配件的事件订阅
                    UnsubscribeFromAttachmentEvents(removedItem);
                }

                // 🔧 清理历史记录
                _slotContentHistory.Remove(slot);
            }
            else
            {
                // 🔴 严重错误：历史记录绝对不能丢失，丢失就是系统bug
                Debug.LogError($"[BackpackShortcutManager] 🚨 严重错误：slot {slot?.Key} 的历史记录丢失！这是系统bug，需要修复历史记录管理逻辑");

                // 💥 抛出异常强制修复，而不是用备用方案掩盖问题
                throw new System.Exception($"历史记录丢失：slot {slot?.Key} 的历史记录在 _slotContentHistory 中不存在");
            }
        }

        /// <summary>
        /// 🔧 处理物品被放入配件内部槽位的事件
        /// </summary>
        private void HandleItemPutIntoAttachmentSlot(Item item, Slot internalSlot)
        {
            Debug.Log($"[BackpackShortcutManager] 🔧 物品放入配件内部: {item.DisplayName} (TypeID: {item.TypeID}) 到 slot: {internalSlot?.Key}");

            // 确定物品类别并触发单物品更新
            ItemCategory affectedCategory = ItemCategorizer.CategorizeItem(item);
            Debug.Log($"[BackpackShortcutManager] 物品放入配件内部，影响类别: {affectedCategory} ({item.DisplayName})");

            if (affectedCategory != ItemCategory.None)
            {
                // 🔧 修复：使用单物品级别通知，与物品移除逻辑保持一致
                // 1. 更新轮盘系统（增量添加）
                _wheelLayoutManager.AddItemToCategory(affectedCategory, item);

                // 2. 🔧 新增：调整选中状态
                AdjustSelectionForItemAddition(affectedCategory, item);

                // 3. 通知UI系统（单物品级别）
                ShortcutUIUpdater.HandleItemAdded(affectedCategory, item);

                Debug.Log($"[BackpackShortcutManager] ✓ 已通知各系统添加物品: {item.DisplayName} (类别: {affectedCategory})");
            }
            else
            {
                Debug.Log($"[BackpackShortcutManager] 放入配件内部的物品 {item.DisplayName} 无有效类别，跳过更新");
            }
        }

  
        /// <summary>
        /// 🔧 清空指定配件可能影响的所有类别
        /// 配件可能包含多种类别的物品，需要全部清空
        /// </summary>
        private void ClearAllCategoriesForAttachment(Item attachment)
        {
            if (attachment == null) return;

            Debug.Log($"[BackpackShortcutManager] 开始清空配件 {attachment.DisplayName} 影响的类别");

            // 🔧 修复：只清空配件实际包含的物品类别，而不是清空所有类别
            if (_attachmentCategories.TryGetValue(attachment, out HashSet<ItemCategory> categoriesToClear))
            {
                Debug.Log($"[BackpackShortcutManager] 配件 {attachment.DisplayName} 包含的类别: {string.Join(", ", categoriesToClear)}");

                if (categoriesToClear.Count > 0)
                {
                    foreach (var category in categoriesToClear)
                    {
                        Debug.Log($"[BackpackShortcutManager] 清空类别: {category}");
                        _wheelLayoutManager.ClearCategory(category);
                    }
                }
                else
                {
                    Debug.Log($"[BackpackShortcutManager] 配件 {attachment.DisplayName} 不包含任何物品类别，无需清空");
                }

                // 🔧 清理类别记录
                _attachmentCategories.Remove(attachment);
            }
            else
            {
                // 🔧 如果没有记录，说明配件是在添加类别记录功能之前放入的，使用后备方案
                Debug.LogWarning($"[BackpackShortcutManager] ⚠️ 没有找到配件 {attachment.DisplayName} 的类别记录，使用后备清空方案");

                // 后备方案：清空所有常见类别（保持原有逻辑）
                var fallbackCategories = new ItemCategory[]
                {
                    ItemCategory.Food,
                    ItemCategory.Medical,
                    ItemCategory.Stim,
                    ItemCategory.Explosive,
                    ItemCategory.Melee
                };

                foreach (var category in fallbackCategories)
                {
                    Debug.Log($"[BackpackShortcutManager] 后备清空类别: {category}");
                    _wheelLayoutManager.ClearCategory(category);
                }
            }
        }

    
        /// <summary>
        /// 🔧 取消配件的事件订阅
        /// 当配件被移除时，清理所有相关的事件订阅
        /// </summary>
        private void UnsubscribeFromAttachmentEvents(Item attachment)
        {
            if (attachment == null) return;

            Debug.Log($"[BackpackShortcutManager] 取消配件 {attachment.DisplayName} 的事件订阅");

            // 🔧 取消配件内部槽位变化事件
            if (_subscribedAttachments.Contains(attachment))
            {
                // 遍历配件的所有内部slot，取消事件订阅
                if (attachment.Slots != null)
                {
                    foreach (var internalSlot in attachment.Slots)
                    {
                        internalSlot.onSlotContentChanged -= OnAttachmentInternalSlotChanged;
                        _subscribedAttachmentSlots.Remove(internalSlot);
                    }
                }
                _subscribedAttachments.Remove(attachment);
                Debug.Log($"[BackpackShortcutManager] 已取消配件内部槽位变化事件: {attachment.DisplayName}");
            }

            // 🔧 递归取消配件内部物品的订阅
            UnsubscribeFromItemsInAttachment(attachment);
        }

        /// <summary>
        /// 🔧 递归取消配件内部所有物品的事件订阅
        /// </summary>
        private void UnsubscribeFromItemsInAttachment(Item attachment)
        {
            if (attachment == null || attachment.Slots == null) return;

            foreach (var slot in attachment.Slots)
            {
                if (slot != null && slot.Content != null)
                {
                    var item = slot.Content;

                    // 取消物品的拔出事件订阅
                    if (_subscribedItems.Contains(item))
                    {
                        item.onUnpluggedFromSlot -= OnItemUnpluggedFromSlot;
                        _subscribedItems.Remove(item);
                        Debug.Log($"[BackpackShortcutManager] 已取消物品拔出事件: {item.DisplayName} (来自配件 {attachment.DisplayName})");
                    }

                    // 递归检查嵌套容器
                    if (item.Slots != null && item.Slots.Count > 0)
                    {
                        UnsubscribeFromItemsInAttachment(item);
                    }
                }
            }
        }
    }
}