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

        // ════ 数据源分离 ════
        // _categorizedItems：纯物品列表，不包含 null（来自背包收集）
        private Dictionary<ItemCategory, List<Item>> _categorizedItems = new Dictionary<ItemCategory, List<Item>>();

        // _wheelLayouts：轮盘布局数据，包含 null 占位符（用户布局）
        // 用于保存用户在轮盘上交换后的排列顺序
        private Dictionary<ItemCategory, List<Item>> _wheelLayouts = new Dictionary<ItemCategory, List<Item>>();

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

            // 初始化选择索引和轮盘布局
            foreach (ItemCategory category in System.Enum.GetValues(typeof(ItemCategory)))
            {
                _currentSelection[category] = 0;
                _wheelLayouts[category] = new List<Item>();  // 初始化空布局
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
            }
        }

        /// <summary>
        /// 检查是否正在初始化
        /// </summary>
        public static bool IsInitializing()
        {
            return _instance != null && _instance._isInitializing;
        }

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

            // 监听物品使用事件
            Item.onUseStatic += OnItemUsed;
            UsageUtilities.OnItemUsedStaticEvent += OnItemUsedStatic;
            Debug.Log("[BackpackShortcutManager] 已订阅物品使用和销毁事件");
        }

        private void OnDestroy()
        {
            // 停止待处理的更新协程
            if (_pendingAttachmentUpdateCoroutine != null)
            {
                StopCoroutine(_pendingAttachmentUpdateCoroutine);
                _pendingAttachmentUpdateCoroutine = null;
            }

            // 取消订阅事件
            UIInputManager.OnShortcutInput -= OnUIShortcutInput;
            Item.onUseStatic -= OnItemUsed;
            UsageUtilities.OnItemUsedStaticEvent -= OnItemUsedStatic;
            Debug.Log("[BackpackShortcutManager] 已取消订阅所有事件");

            // 取消订阅背包和配件的事件
            UnsubscribeFromBackpackChanges();
        }

        /// <summary>
        /// 物品使用事件处理
        /// </summary>
        private void OnItemUsed(Item item, object user)
        {
            if (!IsShortcutSystemEnabled) return;

            // 检查是否是手雷或爆炸物
            if (IsExplosiveItem(item))
            {
                Debug.Log($"[BackpackShortcutManager] 检测到手雷被使用: {item.DisplayName}");
                // 延迟刷新，确保物品状态已更新
                StartCoroutine(DelayedRefresh());
            }
        }

        /// <summary>
        /// 物品使用静态事件处理（通过UsageUtilities触发）
        /// </summary>
        private void OnItemUsedStatic(Item item)
        {
            if (!IsShortcutSystemEnabled) return;

            // 检查是否是手雷或爆炸物
            if (IsExplosiveItem(item))
            {
                Debug.Log($"[BackpackShortcutManager] 检测到手雷被使用（静态事件）: {item.DisplayName}");
                // 延迟刷新，确保物品状态已更新
                StartCoroutine(DelayedRefresh());
            }
        }


        /// <summary>
        /// 检查物品是否是手雷或爆炸物
        /// </summary>
        private bool IsExplosiveItem(Item item)
        {
            if (item == null) return false;

            // 检查物品标签
            foreach (Tag tag in item.Tags)
            {
                if (tag.name == "Explosive" || tag.name == "Grenade" ||
                    tag.name.Contains("Grenade") || tag.name.Contains("Explosive"))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 订阅背包和配件的内容变化事件
        /// </summary>
        private void SubscribeToBackpackChanges(Item backpack)
        {
            if (backpack == null) return;

            _currentBackpack = backpack;

            // 订阅背包的内容变化（添加/移除配件）
            backpack.onSlotContentChanged += OnBackpackContentChanged;
            Debug.Log($"[BackpackShortcutManager] 已订阅背包 {backpack.DisplayName} 的内容变化事件");

            // 订阅每个配件的内容变化
            SubscribeToAttachmentsChanges(backpack);
        }

        /// <summary>
        /// 订阅所有配件的内容变化事件
        /// </summary>
        private void SubscribeToAttachmentsChanges(Item backpack)
        {
            if (backpack == null || backpack.Slots == null) return;

            // 遍历背包的所有插槽
            foreach (var slot in backpack.Slots)
            {
                if (slot != null && slot.Content != null)
                {
                    // 检查是否已经订阅过，避免重复订阅
                    if (!_subscribedAttachments.Contains(slot.Content))
                    {
                        // 订阅配件的内容变化（配件内的物品变化会触发 onChildChanged）
                        slot.Content.onChildChanged += OnAttachmentContentChanged;
                        _subscribedAttachments.Add(slot.Content);
                        Debug.Log($"[BackpackShortcutManager] 已订阅配件 {slot.Content.DisplayName} 的内容变化事件");
                    }
                    else
                    {
                        Debug.Log($"[BackpackShortcutManager] 配件 {slot.Content.DisplayName} 已订阅过，跳过重复订阅");
                    }
                }
            }
        }

        /// <summary>
        /// 取消订阅所有背包和配件的事件
        /// </summary>
        private void UnsubscribeFromBackpackChanges()
        {
            // 取消订阅背包
            if (_currentBackpack != null)
            {
                _currentBackpack.onSlotContentChanged -= OnBackpackContentChanged;
                Debug.Log($"[BackpackShortcutManager] 已取消订阅背包的内容变化事件");
                _currentBackpack = null;
            }

            // 取消订阅所有配件
            foreach (var attachment in _subscribedAttachments)
            {
                if (attachment != null)
                {
                    attachment.onChildChanged -= OnAttachmentContentChanged;
                }
            }
            _subscribedAttachments.Clear();
            Debug.Log($"[BackpackShortcutManager] 已取消订阅所有配件的内容变化事件");
        }

        /// <summary>
        /// 背包内容变化回调（添加/移除配件）
        /// 区分：配件结构改变 vs 配件内物品改变
        /// - 配件被添加/移除 → 需要 RefreshItems（重新收集）
        /// - 配件内的物品添加/移除 → 由 OnAttachmentContentChanged 处理（只更新UI）
        /// </summary>
        private void OnBackpackContentChanged(Item item, Slot slot)
        {
            Debug.Log($"[BackpackShortcutManager] 背包内容变化：item={item?.DisplayName}, slot={slot?.Key}");

            // 检查是否是新配件被添加
            if (item != null && slot != null)
            {
                // 检查这是否真的是配件（容器类型），而不是配件内的物品
                if (item.Slots != null)
                {
                    Debug.Log($"[BackpackShortcutManager] 新配件添加: {item.DisplayName}，需要重新收集物品");

                    // 订阅新配件的内容变化
                    if (!_subscribedAttachments.Contains(item))
                    {
                        item.onChildChanged += OnAttachmentContentChanged;
                        _subscribedAttachments.Add(item);
                        Debug.Log($"[BackpackShortcutManager] 新配件已订阅: {item.DisplayName}");
                    }

                    // 重新收集物品
                    RefreshItems();
                }
            }
            else if (item == null && slot != null)
            {
                // 配件被移除
                Debug.Log($"[BackpackShortcutManager] 配件被移除: {slot.Key}，需要重新收集物品");
                RefreshItems();
            }
        }

        /// <summary>
        /// 配件内容变化回调（添加/移除物品）
        /// 使用去抖机制：当短时间内多个物品变化时，只执行一次更新
        /// </summary>
        private void OnAttachmentContentChanged(Item item)
        {
            Debug.Log($"[BackpackShortcutManager] 配件 {item.DisplayName} 内容变化（触发去抖更新）");

            // 如果已有待处理的更新，停止它（重新开始计时）
            if (_pendingAttachmentUpdateCoroutine != null)
            {
                StopCoroutine(_pendingAttachmentUpdateCoroutine);
            }

            // 启动新的延迟更新协程
            _pendingAttachmentUpdateCoroutine = StartCoroutine(DelayedIncementalUpdate(item));
        }

        private System.Collections.IEnumerator DelayedIncementalUpdate(Item changedItem)
        {
            // 等待一定时间，合并短时间内的多个事件
            // 例如：用户一次性放入多个物品，事件会快速连续触发
            // 通过这个延迟，所有这些事件都会被合并成一次更新
            yield return new WaitForSeconds(ATTACHMENT_UPDATE_DEBOUNCE_TIME);

            Debug.Log($"[BackpackShortcutManager] 去抖完成，执行延迟更新（分帧处理）");

            // 增量更新分类数据（_categorizedItems）- 使用分帧处理
            _tempNewCategories = null;  // 清空临时变量
            var incrementalCoroutine = IncrementalUpdateCategorizedItemsFrameDistributed();
            yield return StartCoroutine(incrementalCoroutine);

            Debug.Log($"[BackpackShortcutManager] 配件内物品变化，_categorizedItems 已更新");

            // 🔧 优化：异步化UI更新，减少卡顿
            yield return StartCoroutine(UpdateShortcutUIAsync(changedItem));

            // 清除待处理协程的引用
            _pendingAttachmentUpdateCoroutine = null;
        }

        /// <summary>
        /// 🔧 优化：异步UI更新，减少拖拽drop时的卡顿
        /// 将UI更新分帧执行，避免主线程阻塞
        /// </summary>
        private System.Collections.IEnumerator UpdateShortcutUIAsync(Item changedItem)
        {
            if (changedItem != null)
            {
                var changedCategory = ItemCategorizer.CategorizeItem(changedItem);
                Debug.Log($"[BackpackShortcutManager] 异步更新受影响的类别: {changedCategory}");

                // 分帧更新：先更新变化物品的类别
                UpdateShortcutUI(changedCategory);
                yield return null; // 等待一帧，让UI有机会渲染

                // 然后更新新增类别（如果有的话）
                if (_tempNewCategories != null && _tempNewCategories.Count > 0)
                {
                    Debug.Log($"[BackpackShortcutManager] 异步更新 {_tempNewCategories.Count} 个新类别");
                    foreach (var category in _tempNewCategories)
                    {
                        UpdateShortcutUI(category);
                        yield return null; // 每个类别更新后等待一帧
                    }
                }
            }
            else
            {
                // 如果无法确定具体物品，则全量更新（兜底方案）
                Debug.Log("[BackpackShortcutManager] 异步全量UI更新");
                UpdateShortcutUI();
                yield return null; // 等待一帧
            }
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
        /// 增量更新分类物品（不重新收集所有物品）
        /// 当配件内物品添加/移除时调用
        /// 只移除已删除的物品，添加新物品，保留其他物品的顺序和轮盘布局
        /// 新类别会被设置到 _tempNewCategories 成员变量中
        /// </summary>
        private System.Collections.IEnumerator IncrementalUpdateCategorizedItems()
        {
            // 初始化临时变量用于存储新类别
            _tempNewCategories = new HashSet<ItemCategory>();

            yield return null;  // 等待一帧，确保物品状态已更新

            if (_currentBackpack == null) yield break;

            // 获取当前背包中的所有物品（快速收集，不做分类）
            var currentBackpackItems = new HashSet<Item>();
            CollectAllItemsFromBackpack(_currentBackpack, currentBackpackItems);

            // 步骤 1：增量更新 _categorizedItems（保持干净，不包含 null）
            foreach (var category in new List<ItemCategory>(_categorizedItems.Keys))
            {
                var oldItems = _categorizedItems[category];
                var newItems = new List<Item>();

                // 保留所有仍然存在于背包中的物品（保持原有顺序，但过滤掉 null）
                foreach (var item in oldItems)
                {
                    if (item != null && currentBackpackItems.Contains(item))
                    {
                        newItems.Add(item);
                        // 移除详细日志以提升性能
                    }
                    // 移除详细日志以提升性能
                }

                // 添加任何新的物品（背包中有但列表中没有的）
                var oldItemsSet = new HashSet<Item>(oldItems);
                foreach (var item in currentBackpackItems)
                {
                    // 检查这个物品是否属于这个类别且不在旧列表中
                    if (!oldItemsSet.Contains(item) && ItemCategorizer.CategorizeItem(item) == category)
                    {
                        newItems.Add(item);
                        // 移除详细日志以提升性能
                    }
                }

                // 更新列表 - 只打印汇总信息
                if (newItems.Count != oldItems.Count || !newItems.SequenceEqual(oldItems))
                {
                    _categorizedItems[category] = newItems;
                    Debug.Log($"[BackpackShortcutManager] 更新 {category}: {oldItems.Count} → {newItems.Count} 个物品");

                    // 检测"从无到有"的类别变化，也需要触发UI更新
                    if (oldItems.Count == 0 && newItems.Count > 0)
                    {
                        _tempNewCategories.Add(category);
                        Debug.Log($"[BackpackShortcutManager] 检测到类别从无到有: {category}，需要UI更新");
                    }
                }
            }

            // 步骤 2：检测并添加新类别（如果背包中出现了新类别的物品）
            foreach (var item in currentBackpackItems)
            {
                var category = ItemCategorizer.CategorizeItem(item);

                // 如果这是一个新类别（之前没有出现过）
                if (!_categorizedItems.ContainsKey(category))
                {
                    _tempNewCategories.Add(category);
                }
            }

            // 为所有新类别初始化空列表和布局
            foreach (var category in _tempNewCategories)
            {
                _categorizedItems[category] = new List<Item>();
                _wheelLayouts[category] = new List<Item>();
                Debug.Log($"[BackpackShortcutManager] 检测到新类别: {category}，初始化数据结构");
            }

            // 为新类别添加所有相应的物品
            if (_tempNewCategories.Count > 0)
            {
                foreach (var item in currentBackpackItems)
                {
                    var category = ItemCategorizer.CategorizeItem(item);
                    if (_tempNewCategories.Contains(category))
                    {
                        _categorizedItems[category].Add(item);
                        _wheelLayouts[category].Add(item);
                    }
                }
            }

            // 步骤 3：同步更新 _wheelLayouts（保留 null 占位符）
            foreach (var category in new List<ItemCategory>(_wheelLayouts.Keys))
            {
                var oldLayout = _wheelLayouts[category];
                var newLayout = new List<Item>();

                // 遍历旧布局，保留 null 占位符和仍然存在的物品
                foreach (var item in oldLayout)
                {
                    if (item == null)
                    {
                        // 保留 null 占位符
                        newLayout.Add(null);
                        // 移除详细日志以提升性能
                    }
                    else if (currentBackpackItems.Contains(item))
                    {
                        // 保留仍然存在的物品
                        newLayout.Add(item);
                        // 移除详细日志以提升性能
                    }
                    else
                    {
                        // 移除已删除的物品
                        // 移除详细日志以提升性能
                    }
                }

                _wheelLayouts[category] = newLayout;
            }

            Debug.Log("[BackpackShortcutManager] 增量更新分类物品和轮盘布局完成");
        }

        /// <summary>
        /// 增量更新分类物品（分帧处理版本）
        /// 当配件内物品添加/移除时调用，使用分帧处理避免卡顿
        /// 只移除已删除的物品，添加新物品，保留其他物品的顺序和轮盘布局
        /// 新类别会被设置到 _tempNewCategories 成员变量中
        /// </summary>
        private System.Collections.IEnumerator IncrementalUpdateCategorizedItemsFrameDistributed()
        {
            // 初始化临时变量用于存储新类别
            _tempNewCategories = new HashSet<ItemCategory>();

            yield return null;  // 等待一帧，确保物品状态已更新

            if (_currentBackpack == null) yield break;

            // 步骤 1：分帧收集所有物品
            var currentBackpackItems = new HashSet<Item>();
            var collectCoroutine = CollectAllItemsFromBackpackFrameDistributed(_currentBackpack, currentBackpackItems);
            yield return StartCoroutine(collectCoroutine);

            // 步骤 2：分帧处理每个类别
            var categories = new List<ItemCategory>(_categorizedItems.Keys);
            for (int i = 0; i < categories.Count; i++)
            {
                var category = categories[i];
                var oldItems = _categorizedItems[category];
                var newItems = new List<Item>();

                // 保留所有仍然存在于背包中的物品（保持原有顺序，但过滤掉 null）
                foreach (var item in oldItems)
                {
                    if (item != null && currentBackpackItems.Contains(item))
                    {
                        newItems.Add(item);
                    }
                }

                // 添加任何新的物品（背包中有但列表中没有的）
                var oldItemsSet = new HashSet<Item>(oldItems);
                foreach (var item in currentBackpackItems)
                {
                    // 检查这个物品是否属于这个类别且不在旧列表中
                    if (!oldItemsSet.Contains(item) && ItemCategorizer.CategorizeItem(item) == category)
                    {
                        newItems.Add(item);
                    }
                }

                // 更新列表 - 只打印汇总信息
                if (newItems.Count != oldItems.Count || !newItems.SequenceEqual(oldItems))
                {
                    _categorizedItems[category] = newItems;
                    Debug.Log($"[BackpackShortcutManager] 更新 {category}: {oldItems.Count} → {newItems.Count} 个物品");

                    // 检测"从无到有"的类别变化，也需要触发UI更新
                    if (oldItems.Count == 0 && newItems.Count > 0)
                    {
                        _tempNewCategories.Add(category);
                        Debug.Log($"[BackpackShortcutManager] 检测到类别从无到有: {category}，需要UI更新");
                    }
                }

                // 每处理完一个类别就等待一帧，避免连续处理造成卡顿
                if (i % 2 == 0) // 每两个类别等待一帧，平衡性能和响应速度
                {
                    yield return null;
                }
            }

            // 步骤 3：分帧检测新类别
            foreach (var item in currentBackpackItems)
            {
                var category = ItemCategorizer.CategorizeItem(item);

                // 如果这是一个新类别（之前没有出现过）
                if (!_categorizedItems.ContainsKey(category))
                {
                    _tempNewCategories.Add(category);
                }

                // 每检测5个物品等待一帧
                if (currentBackpackItems.Count % 5 == 0)
                {
                    yield return null;
                }
            }

            // 步骤 4：为新类别初始化数据结构
            foreach (var category in _tempNewCategories)
            {
                if (!_categorizedItems.ContainsKey(category))
                {
                    _categorizedItems[category] = new List<Item>();
                    _wheelLayouts[category] = new List<Item>();
                    Debug.Log($"[BackpackShortcutManager] 检测到新类别: {category}，初始化数据结构");
                }

                // 为新类别添加所有相应的物品
                foreach (var item in currentBackpackItems)
                {
                    var itemCategory = ItemCategorizer.CategorizeItem(item);
                    if (itemCategory == category)
                    {
                        _categorizedItems[category].Add(item);
                        _wheelLayouts[category].Add(item);
                    }
                }
            }

            Debug.Log("[BackpackShortcutManager] 分帧增量更新分类物品和轮盘布局完成");
        }

        /// <summary>
        /// 递归收集背包和所有配件中的所有物品（分帧处理版本）
        /// </summary>
        private System.Collections.IEnumerator CollectAllItemsFromBackpackFrameDistributed(Item backpack, HashSet<Item> result)
        {
            if (backpack == null || backpack.Slots == null) yield break;

            int processedCount = 0;
            foreach (var slot in backpack.Slots)
            {
                if (slot != null && slot.Content != null)
                {
                    var item = slot.Content;
                    result.Add(item);

                    // 递归检查配件中的物品
                    if (item.Slots != null && item.Slots.Count > 0)
                    {
                        var subCoroutine = CollectAllItemsFromBackpackFrameDistributed(item, result);
                        yield return StartCoroutine(subCoroutine);
                    }
                }

                processedCount++;
                // 每处理5个slot等待一帧，避免一次性处理过多
                if (processedCount % 5 == 0)
                {
                    yield return null;
                }
            }
        }

        /// <summary>
        /// 递归收集背包和所有配件中的所有物品（不分类）
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
                    if (item.Slots != null)
                    {
                        CollectAllItemsFromBackpack(item, result);
                    }
                }
            }

            // 检查库存中的物品
            if (backpack.Inventory != null)
            {
                foreach (var item in backpack.Inventory)
                {
                    if (item != null)
                    {
                        result.Add(item);
                    }
                }
            }
        }

        private System.Collections.IEnumerator DelayedRefresh()
        {
            // 等待一帧，让事件处理完成
            yield return null;
            RefreshItems();
        }

        /// <summary>
        /// 重新收集物品并更新 UI
        /// 保持现有的轮盘布局顺序，使用增量更新策略
        /// 当配件结构改变时调用（配件添加/移除）
        /// </summary>
        private void RefreshItems()
        {
            // 防止递归调用
            if (_isRefreshing)
            {
                Debug.LogWarning("[BackpackShortcutManager] 正在刷新中，跳过此次调用避免递归");
                return;
            }

            if (_currentBackpack == null)
            {
                Debug.LogWarning("[BackpackShortcutManager] 无法刷新：背包为 null");
                return;
            }

            _isRefreshing = true;
            try
            {
                Debug.Log("[BackpackShortcutManager] 开始重新收集物品...");

                // 先取消订阅所有配件的事件（避免重复订阅）
                foreach (var attachment in _subscribedAttachments)
                {
                    if (attachment != null)
                    {
                        attachment.onChildChanged -= OnAttachmentContentChanged;
                    }
                }
                _subscribedAttachments.Clear();

                // 重新收集所有背包中的物品（不分类）
                var currentBackpackItems = new HashSet<Item>();
                CollectAllItemsFromBackpack(_currentBackpack, currentBackpackItems);

                // 使用增量更新策略：保留现有物品的顺序（保持轮盘布局）
                var newCategorizedItems = new Dictionary<ItemCategory, List<Item>>();

                // 初始化所有分类
                foreach (ItemCategory category in System.Enum.GetValues(typeof(ItemCategory)))
                {
                    if (category != ItemCategory.None)
                    {
                        newCategorizedItems[category] = new List<Item>();
                    }
                }

                // 收集纯物品列表（不包含 null）
                foreach (var category in new List<ItemCategory>(_categorizedItems.Keys))
                {
                    var newItems = new List<Item>();

                    // 只添加背包中存在的物品
                    foreach (var item in currentBackpackItems)
                    {
                        if (ItemCategorizer.CategorizeItem(item) == category)
                        {
                            newItems.Add(item);
                            Debug.Log($"[BackpackShortcutManager] 物品收集: {category} - {item.DisplayName}");
                        }
                    }

                    newCategorizedItems[category] = newItems;
                }

                // 同步更新轮盘布局：删除已经不存在的物品，保留 null 占位符
                foreach (var category in new List<ItemCategory>(_wheelLayouts.Keys))
                {
                    var oldLayout = _wheelLayouts[category];
                    var newLayout = new List<Item>();

                    // 遍历旧布局，保留 null 占位符和仍然存在的物品
                    foreach (var item in oldLayout)
                    {
                        if (item == null)
                        {
                            // 保留 null 占位符
                            newLayout.Add(null);
                            Debug.Log($"[BackpackShortcutManager] 布局保留: {category} - <null占位符>");
                        }
                        else if (currentBackpackItems.Contains(item))
                        {
                            // 保留仍然存在的物品
                            newLayout.Add(item);
                            Debug.Log($"[BackpackShortcutManager] 布局保留: {category} - {item.DisplayName}");
                        }
                        else
                        {
                            // 移除已删除的物品
                            Debug.Log($"[BackpackShortcutManager] 布局移除: {category} - {item.DisplayName}（不在背包中）");
                        }
                    }

                    _wheelLayouts[category] = newLayout;
                }

                // 更新 _categorizedItems
                _categorizedItems = newCategorizedItems;

                // 重新订阅配件的事件
                SubscribeToAttachmentsChanges(_currentBackpack);

                // 更新 UI（现在会使用保留的顺序）
                UpdateShortcutUI();

                // 通知官方快捷键系统验证所有已注册物品
                // 这样可以清除不在库存中的物品（如配件中的物品）
                NotifyOfficialShortcutSystemToValidate();

                Debug.Log("[BackpackShortcutManager] 物品刷新完成（保持轮盘布局顺序）");
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        /// <summary>
        /// 通知官方快捷键系统验证所有已注册物品的有效性
        /// 这样可以清除在配件中的物品（当配件被卸下时）
        /// </summary>
        private void NotifyOfficialShortcutSystemToValidate()
        {
            try
            {
                // 获取官方快捷键系统的 OnSetItem 静态事件
                var itemShortcutType = typeof(Duckov.ItemShortcut);

                // OnSetItem 是 static event，值为 Action<int>
                // 我们需要调用它来通知官方快捷键系统刷新显示
                var onSetItemProperty = itemShortcutType.GetProperty("OnSetItem",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                if (onSetItemProperty == null)
                {
                    // 尝试直接获取字段
                    var delegateType = typeof(System.Action<>).MakeGenericType(typeof(int));
                    var eventField = itemShortcutType.GetField("OnSetItem",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                    if (eventField != null)
                    {
                        var eventDelegate = eventField.GetValue(null) as System.Delegate;
                        if (eventDelegate != null)
                        {
                            // 触发事件来刷新所有快捷栏位
                            for (int i = 0; i < Duckov.ItemShortcut.MaxIndex; i++)
                            {
                                eventDelegate.DynamicInvoke(i);
                                Debug.Log($"[BackpackShortcutManager] 通知官方快捷键系统验证索引 {i}");
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BackpackShortcutManager] 通知官方快捷键系统时出错: {ex.Message}");
            }
        }

        private void OnUIShortcutInput(UIInputEventData eventData, int index)
        {
            Debug.Log($"[BackpackShortcutManager] OnUIShortcutInput 事件触发，index = {index}");
            Debug.Log($"[BackpackShortcutManager] 快捷键系统启用状态: {IsShortcutSystemEnabled}");
            Debug.Log($"[BackpackShortcutManager] 索引是否在范围内: {IsBackpackShortcutIndex(index)}");

            // 如果我们的快捷键系统启用，并且索引在我们的范围内，就处理
            if (IsShortcutSystemEnabled && IsBackpackShortcutIndex(index))
            {
                Debug.Log($"[BackpackShortcutManager] ✓ 处理快捷键 {index}");

                // 标记事件为已使用，阻止游戏原版处理
                eventData.Use();

                var category = IndexToCategory(index);
                HandleShortcutInput(category);
            }
            else
            {
                Debug.Log($"[BackpackShortcutManager] ✗ 不处理该快捷键，让游戏原版处理");
            }
        }

        public void OnBackpackChanged(Slot backpackSlot)
        {
            // 检查是否是相同的背包（避免重复处理）
            if (_currentBackpack != null && backpackSlot?.Content != null &&
                _currentBackpack == backpackSlot.Content)
            {
                return;
            }

            // 根据背包槽是否有内容，判断快捷键系统是否启用
            bool hadBackpack = IsShortcutSystemEnabled;
            IsShortcutSystemEnabled = backpackSlot?.Content != null;

            // 如果状态发生变化，触发事件
            if (hadBackpack != IsShortcutSystemEnabled)
            {
                OnShortcutSystemStateChanged?.Invoke(IsShortcutSystemEnabled);
            }

            // 先取消之前的订阅
            UnsubscribeFromBackpackChanges();

            if (IsShortcutSystemEnabled)
            {
                Debug.Log($"[BackpackShortcutManager] 装备背包: {backpackSlot.Content.DisplayName}");

                // 收集物品
                _categorizedItems = BackpackItemCollector.CollectItemsFromBackpack(backpackSlot.Content);

                // 订阅背包和配件的内容变化事件
                SubscribeToBackpackChanges(backpackSlot.Content);

                // 【轮盘布局持久化】加载之前保存的轮盘布局
                LoadPersistedWheelLayouts();

                // 开始监听技能释放事件
                StartMonitoringSkills();

                // 更新快捷键 UI 显示各类别的第一个物品
                UpdateShortcutUI();
            }
            else
            {
                Debug.Log("[BackpackShortcutManager] 卸下背包");
                _categorizedItems.Clear();

                // 停止监听技能释放事件
                StopMonitoringSkills();

                // 清空快捷键 UI
                ClearShortcutUI();
            }

            // 重置选择索引
            foreach (ItemCategory category in System.Enum.GetValues(typeof(ItemCategory)))
            {
                _currentSelection[category] = 0;
            }
        }

        public Item GetCurrentItem(ItemCategory category)
        {
            if (!IsShortcutSystemEnabled)
                return null;

            // 从轮盘布局中获取物品（与SetCurrentSelection()保持一致）
            var wheelLayout = GetItemsForCategory(category);
            if (wheelLayout.Count == 0)
                return null;

            int index = _currentSelection[category];
            if (index < 0 || index >= wheelLayout.Count)
                index = 0;

            var selectedItem = wheelLayout[index];

            // 如果选择的是 null 占位符，自动寻找下一个有效物品
            if (selectedItem == null)
            {
                Debug.Log($"[BackpackShortcutManager] GetCurrentItem: 选择索引 {index} 是占位符，自动寻找下一个有效物品...");

                // 向前搜索
                for (int i = index + 1; i < wheelLayout.Count; i++)
                {
                    if (wheelLayout[i] != null)
                    {
                        selectedItem = wheelLayout[i];
                        Debug.Log($"[BackpackShortcutManager] 找到下一个有效物品在索引 {i}: {selectedItem.DisplayName}");
                        break;
                    }
                }

                // 向后搜索
                if (selectedItem == null)
                {
                    for (int i = index - 1; i >= 0; i--)
                    {
                        if (wheelLayout[i] != null)
                        {
                            selectedItem = wheelLayout[i];
                            Debug.Log($"[BackpackShortcutManager] 向后找到有效物品在索引 {i}: {selectedItem.DisplayName}");
                            break;
                        }
                    }
                }
            }

            return selectedItem;
        }

        public void HandleShortcutInput(ItemCategory category)
        {
            Debug.Log($"[BackpackShortcutManager] HandleShortcutInput 被调用，类别: {category}");
            Debug.Log($"[BackpackShortcutManager] 系统启用状态: {IsShortcutSystemEnabled}");

            if (!IsShortcutSystemEnabled)
            {
                Debug.LogWarning($"[BackpackShortcutManager] 快捷键系统未启用，忽略输入");
                return;
            }

            var item = GetCurrentItem(category);
            Debug.Log($"[BackpackShortcutManager] 获取到的物品: {(item != null ? item.DisplayName : "null")}");

            if (item != null)
            {
                Debug.Log($"[BackpackShortcutManager] ✓ 找到物品，准备使用: {item.DisplayName}");
                ItemUsageHandler.UseItem(item, category);
            }
            else
            {
                Debug.LogWarning($"[BackpackShortcutManager] ✗ 该类别 {category} 没有可用物品");
            }
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

            // 如果存在轮盘布局，优先返回布局（包含 null 占位符）
            if (_wheelLayouts.ContainsKey(category) && _wheelLayouts[category].Count > 0)
            {
                return new List<Item>(_wheelLayouts[category]);
            }

            // 如果没有保存的布局，使用纯物品列表
            if (_categorizedItems.ContainsKey(category))
            {
                return new List<Item>(_categorizedItems[category]);
            }

            return new List<Item>();
        }

        /// <summary>
        /// 设置指定类别的当前选择
        /// 用于轮盘选择器选中物品后更新选择
        /// 只更新该类别的快捷键UI，不影响其他类别
        /// </summary>
        public void SetCurrentSelection(ItemCategory category, Item selectedItem)
        {
            if (!IsShortcutSystemEnabled) return;

            // 获取实际的轮盘布局（可能包含 null 占位符）
            var wheelLayout = GetItemsForCategory(category);
            if (wheelLayout.Count == 0)
            {
                Debug.LogWarning($"[BackpackShortcutManager] 类别 {category} 没有物品布局");
                return;
            }

            // 在轮盘布局中查找物品的索引（考虑 null 占位符）
            int index = -1;
            if (selectedItem == null)
            {
                // 如果选中的是 null，找到第一个 null 的索引
                index = wheelLayout.IndexOf(null);
            }
            else
            {
                index = wheelLayout.IndexOf(selectedItem);
            }

            if (index < 0)
            {
                Debug.LogWarning($"[BackpackShortcutManager] 物品 {(selectedItem != null ? selectedItem.DisplayName : "null")} 不在类别 {category} 的轮盘布局中");
                return;
            }

            _currentSelection[category] = index;
            Debug.Log($"[BackpackShortcutManager] 更新选择: {category} -> 索引 {index} ({(selectedItem != null ? selectedItem.DisplayName : "<null占位符>")})");

            // 只更新该类别对应的快捷键UI（增量更新）
            UpdateShortcutUI(category);
        }

        // 判断快捷键索引是否属于我们的背包快捷键系统
        public static bool IsBackpackShortcutIndex(int index)
        {
            // 我们的快捷键分类只有4个，索引0-3对应Medical, Stim, Food, Explosive
            return index >= 0 && index < 4;
        }

        // 将快捷键索引转换为ItemCategory
        public static ItemCategory IndexToCategory(int index)
        {
            switch (index)
            {
                case 0: return ItemCategory.Medical;
                case 1: return ItemCategory.Stim;
                case 2: return ItemCategory.Food;
                case 3: return ItemCategory.Explosive;
                default: return ItemCategory.Medical;
            }
        }

        // 处理快捷键输入
        public static void HandleShortcutInput(int index)
        {
            if (!IsBackpackShortcutIndex(index))
                return;

            var category = IndexToCategory(index);
            Instance.HandleShortcutInput(category);
        }

        /// <summary>
        /// 更新快捷键 UI，显示各类别的当前选中物品（全量更新）
        /// 优先使用 _currentSelection 中记录的选择索引，保留轮盘布局
        /// 更新所有4个快捷键
        /// </summary>
        private void UpdateShortcutUI()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("开始更新背包快捷键 UI（全量更新）...");

            int successCount = 0;
            int failCount = 0;

            // 遍历我们的物品分类，将每个分类的当前选中物品设置到对应的快捷键槽位
            for (int index = 0; index < 4; index++)
            {
                var category = IndexToCategory(index);
                UpdateShortcutUIForCategory(index, category, ref successCount, ref failCount);
            }

            Debug.Log($"快捷键 UI 全量更新完成: {successCount} 成功, {failCount} 失败（物品在配件中）");
            Debug.Log($"物品统计: 医疗×{_categorizedItems.GetValueOrDefault(ItemCategory.Medical)?.Count ?? 0}, " +
                      $"兴奋剂×{_categorizedItems.GetValueOrDefault(ItemCategory.Stim)?.Count ?? 0}, " +
                      $"食物×{_categorizedItems.GetValueOrDefault(ItemCategory.Food)?.Count ?? 0}, " +
                      $"爆炸物×{_categorizedItems.GetValueOrDefault(ItemCategory.Explosive)?.Count ?? 0}");
            Debug.Log("═══════════════════════════════════════");
        }

        /// <summary>
        /// 增量更新快捷键 UI，只更新指定类别的快捷键（性能优化）
        /// 当轮盘选择某个物品后调用，避免重新计算所有4个快捷键
        /// </summary>
        private void UpdateShortcutUI(ItemCategory category)
        {
            int index = CategoryToIndex(category);
            if (index < 0 || index >= 4)
            {
                Debug.LogWarning($"[UpdateShortcutUI] 类别 {category} 无效，无法映射到快捷键索引");
                return;
            }

            int successCount = 0;
            int failCount = 0;

            Debug.Log($"[UpdateShortcutUI] 增量更新快捷键 {index} ({category})...");
            UpdateShortcutUIForCategory(index, category, ref successCount, ref failCount);

            if (successCount > 0)
            {
                Debug.Log($"[UpdateShortcutUI] ✓ 快捷键 {index} ({category}) 已更新");
            }
            else if (failCount > 0)
            {
                Debug.Log($"[UpdateShortcutUI] 快捷键 {index} ({category}) 更新失败（物品在配件中）");
            }
        }

        /// <summary>
        /// 为指定快捷键索引和类别更新UI
        /// 内部辅助方法，被两个UpdateShortcutUI重载调用
        /// </summary>
        private void UpdateShortcutUIForCategory(int index, ItemCategory category, ref int successCount, ref int failCount)
        {
            // 获取轮盘布局（包含 null 占位符）
            var wheelLayout = GetItemsForCategory(category);

            if (wheelLayout.Count == 0)
            {
                ShortcutUIUpdater.TriggerShortcutRefresh(index);
                return;
            }

            // 使用 _currentSelection 中记录的选择索引
            int selectionIndex = _currentSelection[category];

            // 如果选择索引无效，重置为 0
            if (selectionIndex < 0 || selectionIndex >= wheelLayout.Count)
            {
                selectionIndex = 0;
                _currentSelection[category] = 0;
            }

            // 获取选择位置的物品
            var selectedItem = wheelLayout[selectionIndex];

            // 如果选择的是 null 占位符，自动寻找下一个有效物品
            if (selectedItem == null)
            {
                // 向前搜索
                for (int i = selectionIndex + 1; i < wheelLayout.Count; i++)
                {
                    if (wheelLayout[i] != null)
                    {
                        selectedItem = wheelLayout[i];
                        break;
                    }
                }

                // 如果向前没有找到，向后搜索
                if (selectedItem == null)
                {
                    for (int i = selectionIndex - 1; i >= 0; i--)
                    {
                        if (wheelLayout[i] != null)
                        {
                            selectedItem = wheelLayout[i];
                            break;
                        }
                    }
                }
            }

            // 更新 UI
            if (selectedItem != null)
            {
                bool success = ShortcutUIUpdater.TryUpdateShortcutUI(index, selectedItem);
                if (success) successCount++;
                else failCount++;
            }
            else
            {
                ShortcutUIUpdater.TriggerShortcutRefresh(index);
            }
        }

        /// <summary>
        /// 清空所有快捷键 UI
        /// </summary>
        private void ClearShortcutUI()
        {
            // 触发所有快捷键索引的刷新事件
            for (int i = 0; i < 4; i++)
            {
                ShortcutUIUpdater.TriggerShortcutRefresh(i);
            }
        }

        /// <summary>
        /// 开始监听技能释放事件
        /// </summary>
        private void StartMonitoringSkills()
        {
            if (_currentBackpack == null) return;

            // 查找背包中所有有技能的物品
            FindAndMonitorSkillsRecursive(_currentBackpack);
        }

        /// <summary>
        /// 停止监听技能释放事件
        /// </summary>
        private void StopMonitoringSkills()
        {
            // 取消订阅所有技能的事件
            foreach (var skill in _monitoredSkills)
            {
                if (skill != null)
                {
                    skill.OnSkillReleasedEvent -= OnSkillReleased;
                }
            }
            _monitoredSkills.Clear();
        }

        /// <summary>
        /// 递归查找并监听所有有技能的物品
        /// </summary>
        private void FindAndMonitorSkillsRecursive(Item item)
        {
            if (item == null) return;

            // 检查当前物品是否有技能设置
            var skillSetting = item.GetComponent<ItemSetting_Skill>();
            if (skillSetting != null && skillSetting.Skill != null)
            {
                // 监听技能释放事件
                skillSetting.Skill.OnSkillReleasedEvent += OnSkillReleased;
                _monitoredSkills.Add(skillSetting.Skill);
            }

            // 递归检查子物品
            if (item.Slots != null)
            {
                foreach (Slot slot in item.Slots)
                {
                    if (slot?.Content != null)
                    {
                        FindAndMonitorSkillsRecursive(slot.Content);
                    }
                }
            }

            if (item.Inventory != null)
            {
                foreach (Item childItem in item.Inventory)
                {
                    if (childItem != null)
                    {
                        FindAndMonitorSkillsRecursive(childItem);
                    }
                }
            }
        }

        /// <summary>
        /// 技能释放事件处理
        /// </summary>
        private void OnSkillReleased()
        {
            if (!IsShortcutSystemEnabled) return;

            // 延迟刷新，确保物品状态已更新
            StartCoroutine(DelayedRefresh());
        }

        /// <summary>
        /// 保存轮盘布局（当物品在轮盘上被交换时调用）
        /// 使用物品的实例ID来唯一标识，支持多个相同TypeID的物品
        /// 同时更新 _categorizedItems 的顺序，使其与轮盘调整一致
        /// </summary>
        public void SaveWheelLayout(ItemCategory category, List<Item> arrangedItems)
        {
            if (!IsShortcutSystemEnabled || arrangedItems == null || arrangedItems.Count == 0)
            {
                return;
            }

            // 保存到 _wheelLayouts（轮盘布局独立数据源）
            if (!_wheelLayouts.ContainsKey(category))
            {
                _wheelLayouts[category] = new List<Item>();
            }

            // 新布局就是 arrangedItems（直接保存，包含 null 占位符）
            _wheelLayouts[category] = new List<Item>(arrangedItems);
        }

        /// <summary>
        /// 持久化轮盘布局到文件
        /// 在轮盘关闭时调用
        /// </summary>
        public void PersistWheelLayouts()
        {
            // 将当前的轮盘布局转换为可序列化的数据格式
            var data = WheelLayoutPersistence.ConvertToData(_wheelLayouts);

            // 保存到文件
            WheelLayoutPersistence.SaveToFile(data);
        }

        /// <summary>
        /// 从文件加载轮盘布局
        /// 在背包装备时调用
        /// </summary>
        private void LoadPersistedWheelLayouts()
        {
            if (_currentBackpack == null)
            {
                return;
            }

            // 从文件加载布局数据
            var data = WheelLayoutPersistence.LoadFromFile();
            if (data == null)
            {
                return;
            }

            // 尝试恢复布局（仅恢复，不验证）
            var restoredLayouts = WheelLayoutPersistence.RestoreFromData(data, _currentBackpack);

            if (restoredLayouts == null)
            {
                Debug.Log("[BackpackShortcutManager] 轮盘布局恢复失败，使用默认布局");
                return;
            }

            // 验证布局的完整性（物品必须与收集的物品一一对应）
            if (!WheelLayoutPersistence.ValidateLayoutIntegrity(restoredLayouts, _categorizedItems))
            {
                Debug.Log("[BackpackShortcutManager] 轮盘布局完整性检查失败，使用默认布局");
                return;
            }

            // 验证通过，替换当前的轮盘布局
            _wheelLayouts = restoredLayouts;
            Debug.Log("[BackpackShortcutManager] 轮盘布局已加载");
        }

    }
}