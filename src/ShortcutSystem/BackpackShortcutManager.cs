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
        #region 核心字段

        private static BackpackShortcutManager _instance;
        private CharacterEquipmentController _equipmentController;
        private bool _hasEquipmentController = false;

        public static BackpackShortcutManager Instance => _instance;
        public static bool IsShortcutSystemEnabled { get; private set; }
        public static event System.Action<bool> OnShortcutSystemStateChanged;

        #endregion

        #region 布局管理

        // 统一的轮盘布局管理器 - 新架构核心
        private WheelLayoutManager _wheelLayoutManager;

        /// <summary>
        /// 公共属性：访问轮盘布局管理器
        /// </summary>
        public WheelLayoutManager WheelLayoutManager => _wheelLayoutManager;

        #endregion

        #region 背包配件

        // 当前装备的背包引用
        private Item _currentBackpack;

        // 订阅的配件列表
        private List<Item> _subscribedAttachments = new List<Item>();

        // 配件slot订阅管理
        private HashSet<Slot> _subscribedAttachmentSlots = new HashSet<Slot>();

        // 配件slot内容历史：用于在移除时识别被移除的物品类型
        private Dictionary<Slot, Item> _slotContentHistory = new Dictionary<Slot, Item>();

        // 配件包含的物品类别跟踪：用于精确清理配件影响的类别
        private Dictionary<Item, HashSet<ItemCategory>> _attachmentCategories = new Dictionary<Item, HashSet<ItemCategory>>();

        #endregion

        #region 事件订阅

        // 当前订阅销毁事件的物品实例
        private Item _currentSubscribedItem = null;

        // 物品拔出事件订阅管理
        private HashSet<Item> _subscribedItems = new HashSet<Item>();

        // 技能释放事件监听
        private HashSet<SkillBase> _monitoredSkills = new HashSet<SkillBase>();

        #endregion

        #region 状态缓存

        // 防止递归刷新的标志
        private bool _isRefreshing = false;

        // 配件内容变化的去抖协程
        private Coroutine _pendingAttachmentUpdateCoroutine = null;
        private const float ATTACHMENT_UPDATE_DEBOUNCE_TIME = 0.02f;

        // 🗑️ 已删除：_tempNewCategories字段
        // 原因：只用于已删除的IncrementalUpdateCategorizedItems方法，无其他用途

        #endregion
      #region 生命周期初始化

        /// <summary>
        /// Unity生命周期：单例初始化
        /// </summary>
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
            _wheelLayoutManager = WheelLayoutManager.Instance;

            // 确认轮盘可以直接更新UI
            _wheelLayoutManager.SetUIUpdater();

            // 初始化完成，但没有EquipmentController，系统还未就绪
            _hasEquipmentController = false;
            Debug.Log("[BackpackShortcutManager] Awake完成，等待EquipmentController连接");
        }

        /// <summary>
        /// 系统初始化：连接EquipmentController并启用系统
        /// </summary>
        public static void Initialize(CharacterEquipmentController equipmentController)
        {
            if (_instance == null)
            {
                GameObject obj = new GameObject("BackpackShortcutManager");
                _instance = obj.AddComponent<BackpackShortcutManager>();
            }

            _instance._equipmentController = equipmentController;
            _instance.StartListening();

            // 有了EquipmentController才算真正初始化完成
            _instance._hasEquipmentController = true;
            Debug.Log("[BackpackShortcutManager] Initialize完成，EquipmentController已连接，系统就绪");

            // 启用系统
            _instance.EnableSystemAfterInit();
        }

        /// <summary>
        /// 检查系统是否正在初始化中
        /// </summary>
        public static bool IsInitializing()
        {
            return _instance != null && !_instance._hasEquipmentController;
        }

        /// <summary>
        /// Unity生命周期：资源清理
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
            UnsubscribeFromCurrentItem();
            UIInputManager.OnShortcutInput -= OnUIShortcutInput;

            Debug.Log("[BackpackShortcutManager] 已清理所有事件订阅");
        }

        #endregion

    #region 事件监听

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

            // 监听物品的onDestroy和onSetStackCount事件
            // 这确保在物品真正被消耗后才触发UI更新
            // 注意：这些是实例事件，需要在收集物品时为每个物品实例订阅
            Debug.Log("[BackpackShortcutManager] 物品消耗事件监听已准备（将在物品收集时为每个实例订阅）");
        }

        #endregion

        #region 公共API

        /// <summary>
        /// 获取指定类别的轮盘布局
        /// 返回轮盘上的排列顺序，包含 null 占位符
        /// </summary>
        public List<Item> GetItemsForCategory(ItemCategory category)
        {
            if (!IsShortcutSystemEnabled)
            {
                return new List<Item>();
            }

            // 通过WheelLayoutManager获取布局数据
            var layoutItems = _wheelLayoutManager.GetLayoutForUI(category);
            return layoutItems ?? new List<Item>();
        }

        /// <summary>
        /// 获取指定类别的所有物品（用于轮盘显示）
        /// 直接返回收集到的所有物品，不使用轮盘布局
        /// </summary>
        public List<Item> GetAllItemsForCategory(ItemCategory category)
        {
            if (!IsShortcutSystemEnabled)
            {
                return new List<Item>();
            }

            Debug.Log($"[BackpackShortcutManager] GetAllItemsForCategory: 查询类别 {category}");

            // 通过WheelLayoutManager获取有效物品
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
        /// 处理快捷键输入
        /// </summary>
        public void HandleShortcutInput(ItemCategory category)
        {
            if (!IsShortcutSystemEnabled) return;

            // 使用权威的选中状态方法
            var currentItem = _wheelLayoutManager.GetSelectedItem(category);
            if (currentItem != null)
            {
                Debug.Log($"[BackpackShortcutManager] HandleShortcutInput - 准备使用物品: {currentItem.DisplayName}");

                // 只负责使用物品，UI更新由物品销毁事件自动处理
                ItemUsageHandler.UseItem(currentItem, category);
            }
            else
            {
                Debug.Log($"[BackpackShortcutManager] HandleShortcutInput - Category {category} 没有可用物品");
            }
        }

        // 🗑️ 已删除：SaveWheelLayout方法
        // 原因：功能与PersistWheelLayouts重复，忽略传入参数
        // 替代：直接使用PersistWheelLayouts()方法

        /// <summary>
        /// 持久化轮盘布局
        /// </summary>
        public void PersistWheelLayouts()
        {
            Debug.Log("[BackpackShortcutManager] 持久化轮盘布局");
            WheelLayoutPersistence.SaveWheelSlots(_wheelLayoutManager);
        }

        /// <summary>
        /// 检查是否为背包快捷键索引
        /// </summary>
        public static bool IsBackpackShortcutIndex(int index)
        {
            return index >= 0 && index <= 3; // 0-3: 医疗, 兴奋剂, 食物, 爆炸物
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

        #endregion

  #region 物品管理

        // 🗑️ 已删除：IncrementalUpdateCategorizedItems方法
        // 原因：完全未被调用的死代码，功能已被RefreshItemsWithNewArchitecture替代
        // 新架构直接通过WheelLayoutManager更新，无需协程延迟

        /// <summary>
        /// 收集指定类别的物品，性能优化版本
        /// </summary>
  
        #endregion

        // 🗑️ 已删除：TrySelectItemInWheel方法
        // 原因：违反架构，选中状态应该完全由WheelLayoutManager管理
        // 替代：直接调用WheelLayoutManager的公共方法

        // 🗑️ 已删除：EnsureSelectionConsistency方法
// 违规行为：BackpackShortcutManager不应该直接操作WheelLayoutManager的选中状态
// 选中状态管理完全由WheelLayoutManager负责

  
  
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
        /// <summary>
        /// 🎯 为指定物品订阅事件 - 单一职责
        /// </summary>
        private void SubscribeToItem(Item item)
        {
            if (item == null) return;

            item.onDestroy += OnSelectedItemDestroyed;
            item.onParentChanged += OnSelectedItemParentChanged;
            item.onUnpluggedFromSlot += OnSelectedItemUnplugged;
            item.onSlotTreeChanged += OnSelectedItemSlotTreeChanged;

            Debug.Log($"[BackpackShortcutManager] 已为物品订阅事件: {item.DisplayName}");
        }

        /// <summary>
        /// 🎯 切换选中物品订阅 - 外部逻辑使用
        /// </summary>
        private void SwitchSubscribedItem(Item newItem)
        {
            UnsubscribeFromCurrentItem();
            if (newItem != null)
            {
                SubscribeToItem(newItem);
                _currentSubscribedItem = newItem;
            }
        }

        /// <summary>
        /// 🎯 取消指定物品的事件订阅 - 单一职责
        /// </summary>
        private void UnsubscribeFromItem(Item item)
        {
            if (item == null) return;

            item.onDestroy -= OnSelectedItemDestroyed;
            item.onParentChanged -= OnSelectedItemParentChanged;
            item.onUnpluggedFromSlot -= OnSelectedItemUnplugged;
            item.onSlotTreeChanged -= OnSelectedItemSlotTreeChanged;

            Debug.Log($"[BackpackShortcutManager] 已取消物品事件订阅: {item.DisplayName}");
        }

        /// <summary>
        /// 🎯 取消当前选中物品的事件订阅 - 单一职责
        /// </summary>
        private void UnsubscribeFromCurrentItem()
        {
            if (_currentSubscribedItem != null)
            {
                UnsubscribeFromItem(_currentSubscribedItem);
                _currentSubscribedItem = null;
            }
        }

        /// <summary>
        /// 🗑️ 已删除：UnsubscribeFromPreviousSelection - 职责不单一
        /// 使用 UnsubscribeFromCurrentItem 和 SwitchSubscribedItem 替代
        /// </summary>

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
        /// 🆕 架构修复：不再直接调用UI更新，由WheelLayoutManager直接处理
        /// </summary>
        private void HandleItemExitSystem(Item item)
        {
            if (item == null) return;

            Debug.Log($"[BackpackShortcutManager] 处理物品退出系统: {item.DisplayName}");

            // 取消订阅（如果还没取消的话）
            if (_currentSubscribedItem == item)
            {
                UnsubscribeFromCurrentItem();
            }

            // 确定物品的类别
            var category = ItemCategorizer.CategorizeItem(item);
            if (category == ItemCategory.None) return;

            // 从轮盘布局中移除该物品
            // 🆕 WheelLayoutManager会自动处理选中状态和UI更新
            _wheelLayoutManager.RemoveItem(category, item);

            // 🗑️ 已删除：不再直接调用UI更新
            // UpdateShortcutUI(category); // 违规操作，已移除
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
        /// 🎯 处理物品消耗 - 简化版本
        /// </summary>
        private void HandleItemConsumption(Item consumedItem)
        {
            var category = ItemCategorizer.CategorizeItem(consumedItem);
            if (category == ItemCategory.None) return;

            Debug.Log($"[BackpackShortcutManager] 物品消耗: {consumedItem.DisplayName}");

            // 🏗️ WheelLayoutManager自动处理选中状态切换和UI更新
            _wheelLayoutManager.UpdateSlotAfterItemUsage(consumedItem);

            // 只需要处理物品订阅逻辑
            var newSelectedItem = _wheelLayoutManager.GetSelectedItem(category);
            if (newSelectedItem != null)
            {
                // SwitchSubscribedItem内部会自动取消旧订阅
                SwitchSubscribedItem(newSelectedItem);
            }
            else
            {
                // 没有选中物品时才需要手动取消订阅
                UnsubscribeFromCurrentItem();
            }
        }

        /// <summary>
        /// 背包变化处理 - 智能激活机制拦截
        /// </summary>
        public void OnBackpackChanged(Slot backpackSlot)
        {
            Debug.Log($"[BackpackShortcutManager] OnBackpackChanged 被调用，背包: {backpackSlot?.Content?.DisplayName ?? "null"}");
            Debug.Log($"[BackpackShortcutManager] 当前系统启用状态: {IsShortcutSystemEnabled}");

            var newBackpack = backpackSlot?.Content;
            Debug.Log($"[BackpackShortcutManager] 新背包: {newBackpack?.DisplayName ?? "null"}, 当前背包: {_currentBackpack?.DisplayName ?? "null"}");

            // 🔧 无论什么情况，先清理当前状态
            UnsubscribeFromBackpackChanges();
            Debug.Log("[BackpackShortcutManager] ✅ 已清理当前背包状态");

            // 🆕 智能激活机制：检查新背包是否支持配件系统
            bool isSupportedBackpack = IsBackpackSupported(newBackpack);
            Debug.Log($"[BackpackShortcutManager] 🎯 智能激活检查: {newBackpack?.DisplayName ?? "null"} 支持配件系统: {isSupportedBackpack}");

            if (isSupportedBackpack)
            {
                Debug.Log("[BackpackShortcutManager] ✅ 背包支持配件系统，启动完整功能");
                HandleSupportedBackpack(newBackpack);
            }
            else
            {
                Debug.Log($"[BackpackShortcutManager] ❌ 背包 {newBackpack?.DisplayName ?? "null"} 不支持配件系统，使用基础模式");
                HandleUnsupportedBackpack(newBackpack);
            }
        }

        /// <summary>
        /// 🆕 检查背包是否支持配件系统
        /// </summary>
        private bool IsBackpackSupported(Item backpack)
        {
            if (backpack == null)
            {
                Debug.Log("[BackpackShortcutManager] 🎯 背包为null，不支持配件系统");
                return false;
            }

            // 检查背包TypeID是否在支持列表中
            bool isSupported = BackpackModConfig.BackpackTypeIDs.Contains(backpack.TypeID);
            Debug.Log($"[BackpackShortcutManager] 🎯 背包 {backpack.DisplayName} (TypeID: {backpack.TypeID}) 支持状态: {isSupported}");

            if (isSupported)
            {
                Debug.Log($"[BackpackShortcutManager] ✅ 背包 {backpack.DisplayName} 支持配件系统 - TypeID {backpack.TypeID} 在支持列表中");
            }
            else
            {
                Debug.Log($"[BackpackShortcutManager] ❌ 背包 {backpack.DisplayName} 不支持配件系统 - TypeID {backpack.TypeID} 不在支持列表 [{string.Join(", ", BackpackModConfig.BackpackTypeIDs)}] 中");
            }

            return isSupported;
        }

        /// <summary>
        /// 🆕 处理支持配件系统的背包
        /// </summary>
        private void HandleSupportedBackpack(Item backpack)
        {
            // 更新当前背包
            _currentBackpack = backpack;
            Debug.Log($"[BackpackShortcutManager] 更新当前背包为: {_currentBackpack?.DisplayName ?? "null"}");

            if (_currentBackpack != null)
            {
                // 🧹 装备支持背包前，先清理官方快捷键UI显示
                // 避免与玩家之前设置的官方快捷键冲突
                Debug.Log("[BackpackShortcutManager] 🧹 装备支持背包前，清理官方快捷键UI显示");
                for (int i = 0; i < 4; i++)
                {
                    ShortcutUIUpdater.ClearShortcutUI(i);
                }
                Debug.Log("[BackpackShortcutManager] ✓ 已清空官方快捷键UI显示，准备启动配件系统");

                // 🔧 注册背包类型到ItemTypeRegistry
                ItemTypeRegistry.RegisterBackpack(_currentBackpack.TypeID);

                SubscribeToBackpackChanges(_currentBackpack);
                Debug.Log("[BackpackShortcutManager] 已订阅新背包变化事件");

                // 启用完整的配件系统
                if (!IsShortcutSystemEnabled)
                {
                    SetShortcutSystemEnabled(true);
                    Debug.Log("[BackpackShortcutManager] ✅ 已启用完整配件系统");
                }

                // 🆕 使用新架构刷新物品
                Debug.Log("[BackpackShortcutManager] 开始刷新物品...");
                RefreshItemsWithNewArchitecture();
            }
        }

        /// <summary>
        /// 🆕 处理不支持配件系统的背包
        /// </summary>
        private void HandleUnsupportedBackpack(Item backpack)
        {
            Debug.Log($"[BackpackShortcutManager] 🔧 处理不支持的背包: {backpack?.DisplayName ?? "null"}");

            // 更新当前背包（但不启用配件功能）
            _currentBackpack = backpack;

            if (backpack != null)
            {
                // 注册背包类型但不订阅配件事件
                ItemTypeRegistry.RegisterBackpack(backpack.TypeID);

                // 🚨 临时处理：暂时禁用系统
                // 后续会添加基础轮盘功能
                if (IsShortcutSystemEnabled)
                {
                    SetShortcutSystemEnabled(false);
                    Debug.Log("[BackpackShortcutManager] ❌ 已禁用配件系统（不支持的背包）");
                }
            }
            else
            {
                // 没有背包时禁用系统
                if (IsShortcutSystemEnabled)
                {
                    SetShortcutSystemEnabled(false);
                    Debug.Log("[BackpackShortcutManager] ❌ 无背包，禁用系统");
                }
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

                _wheelLayoutManager.BatchUpdateMultipleCategories(category, items);
                Debug.Log($"[BackpackShortcutManager] 🔥 全量更新类别 {category}: {items.Count} 个物品");
            }

            // 🏗️ 架构修复：移除直接UI调用，改为事件驱动
            // BatchUpdateMultipleCategories会触发选中状态变化，从而直接更新UI
            // 确保选中状态正确同步后再更新UI

            Debug.Log("[BackpackShortcutManager] 事件驱动架构：物品刷新完成，等待UI更新事件");
        }

        // 🗑️ 已删除：OnWheelLayoutChanged事件处理器
// 轮盘现在直接更新UI，不再需要通过事件中转


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

            // 🗑️ 已删除：直接UI清理 - 架构违规
            // 🏗️ 架构修复：WheelLayoutManager.ClearCategory会自动处理UI更新

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

            // 3. 清理数据结构            _subscribedAttachments?.Clear();
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

        // 🗑️ 已删除：UpdateAllCategoriesUIAsync 方法 - 架构违规
        // 🏗️ 架构修复：
        // 1. 该方法未被调用，是死代码
        // 2. 批量UI更新违反单一职责原则
        // 3. UI更新应由WheelLayoutManager在类别变化时自动处理
        // 4. BackpackShortcutManager不应直接操作UI

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
                        attachmentSlot.onSlotContentChanged -= OnBackpackSlotContentChanged;
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
                        slot.onSlotContentChanged += OnBackpackSlotContentChanged;
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

                            // 🏗️ 三层架构：订阅已存在配件的事件（包括内部物品）
                            SubscribeToAttachmentEvents(attachment);
                            Debug.Log($"[BackpackShortcutManager] 🔧 订阅已存在配件 {attachment.DisplayName} 的三层事件");
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

  
        // 🗑️ 已删除：GetSelectedItemForUI 委托方法
        // 🏗️ 架构优化：UI现在直接使用 WheelLayoutManager.Instance.GetSelectedItem，无需中间委托

    
        // 🗑️ 已删除：SetCurrentSelection方法
        // 原因：违反架构，造成重复事件订阅
        // 替代：InputInterceptor直接使用物品，无需通知BackpackShortcutManager
        // 物品进入系统时已经订阅了所有必要事件

        #region 选中状态

        // 🗑️ 已删除：AdjustSelectionForItemAddition方法
        // 原因：WheelLayoutManager.AddItemToCategory已自动处理选中状态
        // 选中状态管理完全由WheelLayoutManager负责

        // 🗑️ 已删除：AdjustSelectionForItemRemoval方法
        // 原因：WheelLayoutManager.RemoveItem已自动处理选中状态
        // 选中状态管理完全由WheelLayoutManager负责

        #endregion

        #region 🔧 配件移动检测机制

        /// <summary>
        /// 🔧 配件槽位内容变化事件处理 - 处理配件本身的放入/取出
        /// 参数确认：Slot slot - 配件槽位本身（来自源码Slot.cs:144）
        /// </summary>
        private void OnBackpackSlotContentChanged(Slot slot)
        {
            if (!IsShortcutSystemEnabled || _isRefreshing) return;

            Item attachmentItem = slot?.Content;  // 这里是配件本身（如工具箱）
            Debug.Log($"[BackpackShortcutManager] 背包slot内容变化: {attachmentItem?.DisplayName ?? "null"}, slot: {slot?.Key}");

            
            if (attachmentItem != null)
            {
                // 配件移入

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

                // 🏗️ 三层架构：调用新的事件订阅方法
                SubscribeToAttachmentEvents(attachmentItem);
            }
        }

        /// <summary>
        /// 🏗️ 三层事件订阅架构
        ///
        /// 设计理念：
        /// - 插入操作：通过 Slot.onSlotContentChanged 事件处理（item != null）
        /// - 拔出操作：通过 Item.onUnpluggedFromSlot 事件处理（精确获取被操作物品引用）
        ///
        /// 三层架构定义：
        /// 第一层：背包自身 → 监控背包槽位内容变化
        ///   - 处理：OnBackpackChanged → OnBackpackSlotContentChanged
        ///
        /// 第二层：配件物品 → 监控配件拔出事件 + 配件内部槽位变化
        ///   - 插入：OnAttachmentInternalSlotChanged (internalItem != null)
        ///   - 拔出：OnAttachmentUnplugged (配件拔出事件)
        ///
        /// 第三层：配件内物品 → 监控物品拔出事件
        ///   - 拔出：OnItemUnpluggedFromSlot (物品拔出事件)
        ///
        /// 核心优势：通过订阅物品拔出事件，可以精确获取当前操作的物品引用，
        /// 避免通过slot内容变化时的null值来推断被移除的物品
        /// </summary>
        private void SubscribeToAttachmentEvents(Item attachmentItem)
        {
            if (attachmentItem == null || _subscribedAttachments.Contains(attachmentItem))
                return;

            Debug.Log($"[BackpackShortcutManager] 🏗️ 开始三层架构订阅 - 第二层：配件 {attachmentItem.DisplayName}");

            // 第二层：订阅配件本身的拔出事件（关键修复：之前缺失！）
            attachmentItem.onUnpluggedFromSlot += OnAttachmentUnplugged;
            _subscribedAttachments.Add(attachmentItem);
            Debug.Log($"[BackpackShortcutManager] ✅ 第二层完成：订阅配件拔出事件 - {attachmentItem.DisplayName}");

            // 第二层：订阅配件内部slot的内容变化事件
            if (attachmentItem.Slots != null)
            {
                Debug.Log($"[BackpackShortcutManager] 🏗️ 第二层：订阅配件内部slot内容变化 - {attachmentItem.Slots.Count} 个slot");

                for (int i = 0; i < attachmentItem.Slots.Count; i++)
                {
                    var internalSlot = attachmentItem.Slots[i];
                    Debug.Log($"[BackpackShortcutManager] 🔍 检查slot[{i}]: {internalSlot?.Key} (null: {internalSlot == null})");

                    if (internalSlot != null)
                    {
                        internalSlot.onSlotContentChanged += OnAttachmentInternalSlotChanged;
                        _subscribedAttachmentSlots.Add(internalSlot);
                        Debug.Log($"[BackpackShortcutManager] ✅ 第二层完成：订阅slot {internalSlot.Key} 内容变化事件");

                        // 🔧 修复：订阅slot事件后，立即检查是否有物品，有物品就订阅物品的unplug
                        if (internalSlot.Content != null)
                        {
                            Debug.Log($"[BackpackShortcutManager] 🏗️ 第三层：slot {internalSlot.Key} 当前有物品 {internalSlot.Content.DisplayName}，立即订阅其拔出事件");
                            SubscribeToItemUnpluggedEvent(internalSlot.Content);
                        }
                        else
                        {
                            Debug.Log($"[BackpackShortcutManager] 📝 slot {internalSlot.Key} 当前为空，等待后续物品放入");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[BackpackShortcutManager] ❌ slot[{i}] 为null，跳过订阅");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[BackpackShortcutManager] ❌ 配件 {attachmentItem.DisplayName} 的Slots为null，无法完成第二层订阅！");
            }

            Debug.Log($"[BackpackShortcutManager] 🏗️ 三层架构订阅完成 - 配件：{attachmentItem.DisplayName}");
        }

        /// <summary>
        /// 🔧 配件拔出事件处理 - 第二层关键补充（之前缺失！）
        /// 处理整个配件从背包槽位中被拔出的事件
        /// </summary>
        private void OnAttachmentUnplugged(Item unpluggedAttachment)
        {
            if (!IsShortcutSystemEnabled || _isRefreshing) return;

            Debug.Log($"[BackpackShortcutManager] 🔧 第二层事件：配件拔出 - {unpluggedAttachment.DisplayName} (TypeID: {unpluggedAttachment.TypeID})");

            // 取消订阅该配件的所有事件
            UnsubscribeFromAttachmentEvents(unpluggedAttachment);
            Debug.Log($"[BackpackShortcutManager] ✅ 已取消配件 {unpluggedAttachment.DisplayName} 的所有事件订阅");

            // 获取配件被拔出前影响的类别
            if (_attachmentCategories.TryGetValue(unpluggedAttachment, out HashSet<ItemCategory> affectedCategories))
            {
                Debug.Log($"[BackpackShortcutManager] 配件 {unpluggedAttachment.DisplayName} 影响类别: {string.Join(", ", affectedCategories)}");

                // 🔧 修复：先批量移除配件内的所有物品
                foreach (var category in affectedCategories)
                {
                    var itemsToRemove = new List<Item>();

                    // 收集该配件中属于此类别的所有物品
                    if (unpluggedAttachment.Slots != null)
                    {
                        foreach (var slot in unpluggedAttachment.Slots)
                        {
                            if (slot?.Content != null)
                            {
                                var item = slot.Content;
                                var itemCategory = ItemCategorizer.CategorizeItem(item);
                                if (itemCategory == category)
                                {
                                    itemsToRemove.Add(item);
                                    Debug.Log($"[BackpackShortcutManager] 🔧 准备移除配件内物品: {item.DisplayName} (类别: {category})");
                                }
                            }
                        }
                    }

                    // 批量移除物品
                    if (itemsToRemove.Count > 0)
                    {
                        Debug.Log($"[BackpackShortcutManager] 🔧 批量移除类别 {category} 的 {itemsToRemove.Count} 个物品");
                        _wheelLayoutManager.BatchRemoveItems(category, itemsToRemove);
                    }
                }

                // 清理配件类别记录
                _attachmentCategories.Remove(unpluggedAttachment);
            }
            else
            {
                Debug.LogWarning($"[BackpackShortcutManager] 配件 {unpluggedAttachment.DisplayName} 无类别记录，无法精确更新");
            }
        }

        /// <summary>
        /// 🔧 取消订阅配件的所有事件
        /// </summary>
        private void UnsubscribeFromAttachmentEvents(Item attachment)
        {
            if (attachment == null) return;

            Debug.Log($"[BackpackShortcutManager] 🔧 开始取消订阅配件事件: {attachment.DisplayName}");

            // 取消订阅配件本身的拔出事件
            if (_subscribedAttachments.Contains(attachment))
            {
                attachment.onUnpluggedFromSlot -= OnAttachmentUnplugged;
                _subscribedAttachments.Remove(attachment);
                Debug.Log($"[BackpackShortcutManager] ✅ 已取消订阅配件拔出事件");
            }

            // 取消订阅配件内部slot的事件
            if (attachment.Slots != null)
            {
                foreach (var slot in attachment.Slots)
                {
                    if (slot != null && _subscribedAttachmentSlots.Contains(slot))
                    {
                        slot.onSlotContentChanged -= OnAttachmentInternalSlotChanged;
                        _subscribedAttachmentSlots.Remove(slot);
                        Debug.Log($"[BackpackShortcutManager] ✅ 已取消订阅slot {slot.Key} 内容变化事件");
                    }
                }

                // 取消订阅配件内物品的事件
                UnsubscribeFromItemsInAttachment(attachment);
            }

            Debug.Log($"[BackpackShortcutManager] ✅ 配件 {attachment.DisplayName} 所有事件订阅已清理");
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

                // 🆕 新架构：RemoveItem已自动处理选中状态和UI更新
                // AdjustSelectionForItemRemoval已删除，选中状态由WheelLayoutManager负责
                // ShortcutUIUpdater.HandleItemRemoved已删除，UI更新由WheelLayoutManager直接处理

                Debug.Log($"[BackpackShortcutManager] ✓ 已通知各系统移除物品: {unpluggedItem.DisplayName}");
            }
            else
            {
                Debug.Log($"[BackpackShortcutManager] 被拔出的物品 {unpluggedItem.DisplayName} 无有效类别，跳过更新");
            }
        }

      
      
        // 🗑️ 已删除：UpdateShortcutUI(ItemCategory, Item) 方法 - 架构违规
        // 🏗️ 架构修复：
        // 1. 该方法未被调用，是死代码
        // 2. 直接调用UI更新违反单一职责原则
        // 3. UI更新应由WheelLayoutManager在数据变化时自动处理
        // 4. BackpackShortcutManager不应直接操作UI系统

        /// <summary>
        /// 🔧 第三层：订阅单个物品的拔出事件
        /// 这是第三层架构的核心方法
        /// </summary>
        private void SubscribeToItemUnpluggedEvent(Item item)
        {
            if (item == null || _subscribedItems.Contains(item))
            {
                Debug.Log($"[BackpackShortcutManager] 🔄 物品 {item?.DisplayName ?? "null"} 无需订阅或已订阅");
                return;
            }

            // 第三层：订阅配件内物品的拔出事件
            item.onUnpluggedFromSlot += OnItemUnpluggedFromSlot;
            _subscribedItems.Add(item);
            Debug.Log($"[BackpackShortcutManager] ✅ 第三层完成：订阅物品 {item.DisplayName} (TypeID: {item.TypeID}) 拔出事件");
        }

        /// <summary>
        /// 🔧 配件内部槽位变化事件处理 - 处理配件内部物品的放入/取出
        /// 正确逻辑：订阅slot的content事件 → 检查有没有物品 → 有物品订阅物品的unplug
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

                // 🔧 修复：有物品放入，立即订阅该物品的拔出事件
                Debug.Log($"[BackpackShortcutManager] 🏗️ 第三层：检测到物品 {internalItem.DisplayName} 放入，立即订阅其拔出事件");
                SubscribeToItemUnpluggedEvent(internalItem);
            }
            else
            {
                // 🔧 物品从配件内部槽位取出，但物品拔出事件应该由OnItemUnpluggedFromSlot处理
                Debug.Log($"[BackpackShortcutManager] 配件内部槽位变空，等待物品拔出事件处理");
                // 物品拔出事件会由OnItemUnpluggedFromSlot处理，这里不需要做额外处理
            }
        }

          // 🗑️ 已删除：SubscribeToExistingItemsInAttachment方法
        // 原因：功能已整合到SubscribeToAttachmentEvents和OnAttachmentInternalSlotChanged中
        // 新架构：订阅slot事件 → 检查物品 → 有物品就订阅物品unplug事件

        
      
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

                // 按类别增量更新轮盘 - 修复全量检测问题
                foreach (var kvp in itemsByCategory)
                {
                    ItemCategory category = kvp.Key;
                    List<Item> items = kvp.Value;

                    Debug.Log($"[BackpackShortcutManager] 配件 {attachment.DisplayName} 贡献类别 {category}: {items.Count} 个物品");

                    // 🆕 增量更新：逐个添加物品到轮盘，避免全量刷新
                    foreach (var item in items)
                    {
                        Debug.Log($"[BackpackShortcutManager] 增量添加物品: {item.DisplayName} 到类别 {category}");
                        _wheelLayoutManager.AddItemToCategory(category, item);
                    }
                }
            }
            else
            {
                Debug.Log($"[BackpackShortcutManager] 配件 {attachment.DisplayName} 无内部槽位");
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
                // 🆕 新架构：只调用轮盘系统，UI更新由轮盘直接处理
                // 1. 更新轮盘系统，轮盘会自动处理选中状态并直接更新UI
                _wheelLayoutManager.AddItemToCategory(affectedCategory, item);

                // 🗑️ 移除：不再需要选中状态调整，轮盘会自动处理
                // AdjustSelectionForItemAddition(affectedCategory, item);

                // 🗑️ 移除：UI更新由轮盘直接处理
                // ShortcutUIUpdater.HandleItemAdded(affectedCategory, item);

                Debug.Log($"[BackpackShortcutManager] ✓ 已通知各系统添加物品: {item.DisplayName} (类别: {affectedCategory})");
            }
            else
            {
                Debug.Log($"[BackpackShortcutManager] 放入配件内部的物品 {item.DisplayName} 无有效类别，跳过更新");
            }
        }

  
        /// <summary>
        /// 🔧 移除配件中的所有物品
        /// 遍历配件的所有槽位，逐个移除物品，而不是清空整个类别
        /// 🆕 架构修复：使用正确的物品移除逻辑，不违反架构原则
        /// </summary>
        private void RemoveItemsFromAttachment(Item attachment)
        {
            if (attachment == null) return;

            Debug.Log($"[BackpackShortcutManager] 开始移除配件 {attachment.DisplayName} 中的所有物品");

            // 🔧 遍历配件的所有槽位，逐个移除物品
            if (attachment.Slots != null)
            {
                int removedCount = 0;

                foreach (var slot in attachment.Slots)
                {
                    if (slot?.Content != null)
                    {
                        Item item = slot.Content;
                        ItemCategory category = ItemCategorizer.CategorizeItem(item);

                        if (category != ItemCategory.None)
                        {
                            Debug.Log($"[BackpackShortcutManager] 从配件移除物品: {item.DisplayName} (类别: {category})");

                            // 🆕 只移除特定物品，不影响该类别的其他物品
                            // WheelLayoutManager会自动处理选中状态和UI更新
                            _wheelLayoutManager.RemoveItem(category, item);
                            removedCount++;
                        }
                        else
                        {
                            Debug.Log($"[BackpackShortcutManager] 物品 {item.DisplayName} 无有效类别，跳过移除");
                        }
                    }
                }

                Debug.Log($"[BackpackShortcutManager] 从配件 {attachment.DisplayName} 总共移除了 {removedCount} 个物品");
            }
            else
            {
                Debug.LogWarning($"[BackpackShortcutManager] 配件 {attachment.DisplayName} 没有槽位或槽位为空");
            }

            // 🔧 清理类别记录
            _attachmentCategories.Remove(attachment);
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
