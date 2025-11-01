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

        // 当前选择的物品索引（在轮盘布局中）
        private Dictionary<ItemCategory, int> _currentSelection = new Dictionary<ItemCategory, int>();

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

            // 初始化选择索引
            foreach (ItemCategory category in System.Enum.GetValues(typeof(ItemCategory)))
            {
                if (category != ItemCategory.None)
                {
                    _currentSelection[category] = 0;
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

                // 更新选择
                _currentSelection[category] = nextIndex;

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
                    SubscribeToBackpackChanges(_currentBackpack);
                    SubscribeToAttachmentsChanges(_currentBackpack);
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

            // 🆕 触发UI更新（批量更新，避免多次刷新）
            StartCoroutine(UpdateAllCategoriesUIAsync());

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
        /// 🚨 执行系统重置时的完整数据清理
        /// 确保所有核心数据回到初始状态，避免脏数据干扰
        /// </summary>
        private void PerformSystemResetCleanup()
        {
            Debug.Log("[BackpackShortcutManager] 🔧 开始执行系统重置清理...");

            // 1. 清理UI显示 - 清空所有快捷键UI
            try
            {
                for (int i = 0; i < 6; i++) // 0-5 对应游戏中的3-8快捷键
                {
                    ShortcutUIUpdater.ClearShortcutUI(i);
                }
                Debug.Log("[BackpackShortcutManager] ✅ UI清理完成");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BackpackShortcutManager] UI清理失败: {e.Message}");
            }

            // 2. 清理轮盘布局管理器数据
            try
            {
                if (_wheelLayoutManager != null)
                {
                    // 清空所有类别的轮盘布局
                    foreach (ItemCategory category in System.Enum.GetValues(typeof(ItemCategory)))
                    {
                        if (category != ItemCategory.None)
                        {
                            _wheelLayoutManager.ClearCategory(category);
                        }
                    }
                    Debug.Log("[BackpackShortcutManager] ✅ 轮盘布局清理完成");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BackpackShortcutManager] 轮盘布局清理失败: {e.Message}");
            }

            // 3. 清理选中物品数据
            try
            {
                if (_currentSelection != null)
                {
                    _currentSelection.Clear();
                    Debug.Log("[BackpackShortcutManager] ✅ 当前选择清理完成");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BackpackShortcutManager] 选中物品清理失败: {e.Message}");
            }

            // 4. 清理配件订阅数据
            try
            {
                if (_subscribedAttachments != null)
                {
                    _subscribedAttachments.Clear();
                    Debug.Log("[BackpackShortcutManager] ✅ 配件订阅清理完成");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BackpackShortcutManager] 配件订阅清理失败: {e.Message}");
            }

            // 5. 清理状态标志
            _isRefreshing = false;
            Debug.Log("[BackpackShortcutManager] ✅ 状态标志重置完成");

            // 6. 停止所有协程
            try
            {
                if (_pendingAttachmentUpdateCoroutine != null)
                {
                    StopCoroutine(_pendingAttachmentUpdateCoroutine);
                    _pendingAttachmentUpdateCoroutine = null;
                    Debug.Log("[BackpackShortcutManager] ✅ 协程清理完成");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BackpackShortcutManager] 协程清理失败: {e.Message}");
            }

            // 7. 强制垃圾回收（可选，用于测试）
            // System.GC.Collect();

            Debug.Log("[BackpackShortcutManager] 🔧 系统重置清理完成！所有核心数据已回到初始状态。");
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
        /// 🔧 新增：延迟UI刷新安全网
        /// 处理游戏系统初始化特别慢的情况
        /// </summary>
        private System.Collections.IEnumerator DelayedUIRefreshFallback()
        {
            // 等待2秒，确保游戏系统完全初始化
            yield return new WaitForSeconds(2.0f);

            Debug.Log("[BackpackShortcutManager] 执行延迟UI刷新安全网检查");

            // 检查是否有物品但没有显示在UI中
            bool hasItems = false;
            bool needsUIRefresh = false;

            foreach (ItemCategory category in System.Enum.GetValues(typeof(ItemCategory)))
            {
                if (category != ItemCategory.None)
                {
                    var items = _wheelLayoutManager.GetValidItems(category);
                    if (items.Count > 0)
                    {
                        hasItems = true;
                        var index = CategoryToIndex(category);
                        if (index >= 0)
                        {
                            // 检查UI是否正确显示（简单的检查：如果有物品但UI更新失败）
                            bool success = ShortcutUIUpdater.TryUpdateShortcutUI(index, items[0]);
                            if (!success)
                            {
                                needsUIRefresh = true;
                                Debug.LogWarning($"[BackpackShortcutManager] 延迟检查发现类别 {category} UI仍然需要刷新");
                            }
                        }
                    }
                }
            }

            if (hasItems && needsUIRefresh)
            {
                Debug.Log("[BackpackShortcutManager] 执行延迟UI刷新");
                StartCoroutine(UpdateAllCategoriesUIAsync());
            }
            else if (hasItems)
            {
                Debug.Log("[BackpackShortcutManager] 延迟检查：UI已正确显示，无需刷新");
            }
            else
            {
                Debug.Log("[BackpackShortcutManager] 延迟检查：无物品需要显示");
            }
        }

        /// <summary>
        /// 🆕 检查当前背包状态并收集物品
        /// 解决系统初始化时机问题，确保不会错过已装备的背包
        /// </summary>
        private void CheckCurrentBackpackAndCollectItems()
        {
            Debug.Log("[BackpackShortcutManager] 开始检查当前背包状态...");

            if (_equipmentController == null)
            {
                Debug.Log("[BackpackShortcutManager] EquipmentController为空，无法检查背包");
                return;
            }

            // 检查玩家是否已装备背包
            var player = CharacterMainControl.Main;
            if (player?.CharacterItem != null)
            {
                Debug.Log("[BackpackShortcutManager] 检测到玩家存在，检查装备槽...");

                // 尝试通过EquipmentController获取背包信息
                try
                {
                    // 使用反射安全地获取背包槽位
                    var equipmentData = _equipmentController.GetType().GetField("equipmentData",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (equipmentData != null)
                    {
                        var data = equipmentData.GetValue(_equipmentController);
                        if (data != null)
                        {
                            var getSlotMethod = data.GetType().GetMethod("GetSlotByEquipmentType");
                            if (getSlotMethod != null)
                            {
                                var backpackSlot = getSlotMethod.Invoke(data, new object[] { 3 }); // Backpack = 3
                                var slot = backpackSlot as Slot;

                                if (slot?.Content != null)
                                {
                                    Debug.Log($"[BackpackShortcutManager] 发现已装备背包: {slot.Content.DisplayName}");

                                    // 设置当前背包并触发物品收集
                                    _currentBackpack = slot.Content;
                                    SubscribeToBackpackChanges(_currentBackpack);
                                    SubscribeToAttachmentsChanges(_currentBackpack);

                                    // 立即收集物品并更新UI
                                    RefreshItemsWithNewArchitecture();
                                    Debug.Log("[BackpackShortcutManager] 已完成物品收集和UI更新");
                                }
                                else
                                {
                                    Debug.Log("[BackpackShortcutManager] 当前未装备背包，等待用户装备");
                                }
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[BackpackShortcutManager] 检查背包状态时出错: {ex.Message}");
                }
            }
            else
            {
                Debug.Log("[BackpackShortcutManager] 玩家尚未完全加载，等待背包装备事件");
            }
        }

        /// <summary>
        /// 取消订阅背包变化事件
        /// </summary>
        private void UnsubscribeFromBackpackChanges()
        {
            // 取消背包内容变化监听
            if (_currentBackpack != null)
            {
                _currentBackpack.onSlotContentChanged -= OnBackpackContentChanged;

                // 取消所有配件的事件监听
                foreach (var attachment in _subscribedAttachments)
                {
                    if (attachment != null)
                    {
                        attachment.onParentChanged -= OnAttachmentParentChanged;
                    }
                }
                _subscribedAttachments.Clear();
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
            backpack.onSlotContentChanged += OnBackpackContentChanged;
            Debug.Log("[BackpackShortcutManager] 已订阅背包内容变化事件");
        }

        /// <summary>
        /// 订阅配件变化事件
        /// </summary>
        private void SubscribeToAttachmentsChanges(Item backpack)
        {
            if (backpack == null || backpack.Slots == null) return;

            // 🔧 关键修复：根据背包配置判断哪些slot应该放配件
            var attachmentSlotIndices = GetAttachmentSlotIndices(backpack);

            for (int i = 0; i < backpack.Slots.Count; i++)
            {
                var slot = backpack.Slots[i];
                if (slot != null && slot.Content != null && attachmentSlotIndices.Contains(i))
                {
                    var attachment = slot.Content;

                    Debug.Log($"[BackpackShortcutManager] 发现配件: {attachment.DisplayName} (在slot {i})");

                    // 🔧 简化：只订阅配件移动事件
            attachment.onParentChanged += OnAttachmentParentChanged;

                    _subscribedAttachments.Add(attachment);

                    // 递归检查配件中的配件
                    if (attachment.Slots != null && attachment.Slots.Count > 0)
                    {
                        SubscribeToAttachmentsChanges(attachment);
                    }
                }
                else if (slot != null && slot.Content != null && !attachmentSlotIndices.Contains(i))
                {
                    Debug.Log($"[BackpackShortcutManager] 跳过普通物品: {slot.Content.DisplayName} (在slot {i})");
                }
            }

            Debug.Log($"[BackpackShortcutManager] 已订阅 {_subscribedAttachments.Count} 个配件的变化事件");
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
        /// 背包内容变化处理
        /// </summary>
        private void OnBackpackContentChanged(Item item, Slot slot)
        {
            if (!IsShortcutSystemEnabled || _isRefreshing) return;

            Debug.Log($"[BackpackShortcutManager] 背包内容变化: {item?.DisplayName}");

            // 启动增量更新协程
            if (_pendingAttachmentUpdateCoroutine != null)
            {
                StopCoroutine(_pendingAttachmentUpdateCoroutine);
            }
            _pendingAttachmentUpdateCoroutine = StartCoroutine(IncrementalUpdateCategorizedItems());
        }

    
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
        /// 获取当前选中的物品
        /// </summary>
        public Item GetCurrentItem(ItemCategory category)
        {
            if (_instance == null) return null;
            return _instance._wheelLayoutManager.GetCurrentSelection(category);
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
        /// </summary>
        public void SetCurrentSelection(ItemCategory category, Item selectedItem)
        {
            if (_wheelLayoutManager == null) return;

            // 找到物品在布局中的位置
            var slots = _wheelLayoutManager.GetSlots(category);
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].HasValidItem() && slots[i].Item == selectedItem)
                {
                    _currentSelection[category] = i;
                    Debug.Log($"[BackpackShortcutManager] 设置 {category} 选择为位置 {i}: {selectedItem.DisplayName}");
                    return;
                }
            }
        }

        #region 🔧 配件移动检测机制

  
        /// <summary>
        /// 🎯 配件父级变化事件处理 - 检测配件是否从背包移出
        /// </summary>
        private void OnAttachmentParentChanged(Item item)
        {
            if (item == null) return;

            Debug.Log($"[BackpackShortcutManager] 配件父级变化: {item.DisplayName}");

            // 检查是否为已订阅的配件
            if (_subscribedAttachments.Contains(item))
            {
                // 检查配件是否还在我们的背包系统中
                if (!IsAttachmentStillInBackpack(item))
                {
                    Debug.Log($"[BackpackShortcutManager] 检测到配件已从背包移出: {item.DisplayName}");

                    // 移除配件订阅
                    _subscribedAttachments.Remove(item);

                    // 配件移出后，需要刷新所有分类，因为其中的物品也离开了系统
                    RefreshCategoriesAfterAttachmentChange();
                }
                else
                {
                    Debug.Log($"[BackpackShortcutManager] 配件仍在背包系统中: {item.DisplayName}");
                }
            }
        }

      
        /// <summary>
        /// 🎯 检查配件是否仍在背包中
        /// </summary>
        private bool IsAttachmentStillInBackpack(Item attachment)
        {
            if (attachment == null || _currentBackpack == null || _currentBackpack.Slots == null)
            {
                return false;
            }

            // 🔧 关键检测：检查配件是否直接在背包的插槽中
            foreach (var slot in _currentBackpack.Slots)
            {
                if (slot != null && slot.Content == attachment)
                {
                    Debug.Log($"[BackpackShortcutManager] 配件 {attachment.DisplayName} 仍在背包插槽中");
                    return true;
                }
            }

            // 🔧 递归检查：检查配件是否在背包的嵌套容器中
            foreach (var slot in _currentBackpack.Slots)
            {
                if (slot != null && slot.Content != null && slot.Content.Slots != null)
                {
                    if (IsItemInContainer(attachment, slot.Content))
                    {
                        Debug.Log($"[BackpackShortcutManager] 配件 {attachment.DisplayName} 在背包的嵌套容器中");
                        return true;
                    }
                }
            }

            Debug.Log($"[BackpackShortcutManager] 配件 {attachment.DisplayName} 已不在背包中");
            return false;
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
    }
}