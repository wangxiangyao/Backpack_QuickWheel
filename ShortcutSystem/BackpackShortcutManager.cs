using System.Collections.Generic;
using UnityEngine;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using Duckov.Utilities;

namespace Great_backpack.ShortcutSystem
{
    public class BackpackShortcutManager : MonoBehaviour
    {
        private static BackpackShortcutManager _instance;
        private CharacterEquipmentController _equipmentController;
        private Dictionary<ItemCategory, List<Item>> _categorizedItems = new Dictionary<ItemCategory, List<Item>>();
        private Dictionary<ItemCategory, int> _currentSelection = new Dictionary<ItemCategory, int>();

        // 用于监听背包内容变化
        private Item _currentBackpack;
        private List<Item> _subscribedAttachments = new List<Item>();

        // 防止递归刷新的标志
        private bool _isRefreshing = false;

        // 用于监听技能释放事件
        private HashSet<SkillBase> _monitoredSkills = new HashSet<SkillBase>();

        // 初始化状态标记
        private bool _isInitializing = true;

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

            // 初始化选择索引
            foreach (ItemCategory category in System.Enum.GetValues(typeof(ItemCategory)))
            {
                _currentSelection[category] = 0;
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
                    // 订阅配件的内容变化（配件内的物品变化会触发 onChildChanged）
                    slot.Content.onChildChanged += OnAttachmentContentChanged;
                    _subscribedAttachments.Add(slot.Content);
                    Debug.Log($"[BackpackShortcutManager] 已订阅配件 {slot.Content.DisplayName} 的内容变化事件");
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
        /// </summary>
        private void OnBackpackContentChanged(Item item, Slot slot)
        {
            Debug.Log($"[BackpackShortcutManager] 背包内容变化：slot={slot?.Key}");

            // 重新收集物品
            RefreshItems();
        }

        /// <summary>
        /// 配件内容变化回调（添加/移除物品）
        /// </summary>
        private void OnAttachmentContentChanged(Item item)
        {
            Debug.Log($"[BackpackShortcutManager] 配件 {item.DisplayName} 内容变化");

            // 延迟重新收集物品，避免在事件处理过程中立即刷新
            StartCoroutine(DelayedRefresh());
        }

        private System.Collections.IEnumerator DelayedRefresh()
        {
            // 等待一帧，让事件处理完成
            yield return null;
            RefreshItems();
        }

        /// <summary>
        /// 重新收集物品并更新 UI
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

                // 重新收集物品
                _categorizedItems = BackpackItemCollector.CollectItemsFromBackpack(_currentBackpack);

                // 重新订阅配件的事件
                SubscribeToAttachmentsChanges(_currentBackpack);

                // 更新 UI
                UpdateShortcutUI();

                // 通知官方快捷键系统验证所有已注册物品
                // 这样可以清除不在库存中的物品（如配件中的物品）
                NotifyOfficialShortcutSystemToValidate();

                Debug.Log("[BackpackShortcutManager] 物品刷新完成");
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
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("[BackpackShortcutManager] OnBackpackChanged 被调用");
            Debug.Log($"[BackpackShortcutManager] 背包槽: {(backpackSlot != null ? "存在" : "null")}");
            Debug.Log($"[BackpackShortcutManager] 背包内容: {(backpackSlot?.Content != null ? backpackSlot.Content.DisplayName : "null")}");
            Debug.Log($"[BackpackShortcutManager] 当前背包: {(_currentBackpack != null ? _currentBackpack.DisplayName : "null")}");

            // 检查是否是相同的背包（避免重复处理）
            if (_currentBackpack != null && backpackSlot?.Content != null &&
                _currentBackpack == backpackSlot.Content)
            {
                Debug.Log("[BackpackShortcutManager] 背包没有变化，跳过处理");
                Debug.Log("═══════════════════════════════════════");
                return;
            }

            // 检查是否是空背包切换（从有背包到无背包）
            if (_currentBackpack != null && backpackSlot?.Content == null)
            {
                Debug.Log("[BackpackShortcutManager] 背包被卸下，正常处理");
            }
            else if (_currentBackpack == null && backpackSlot?.Content != null)
            {
                Debug.Log("[BackpackShortcutManager] 新背包装备，正常处理");
            }
            else if (_currentBackpack != null && backpackSlot?.Content != null)
            {
                Debug.Log($"[BackpackShortcutManager] 背包切换: {_currentBackpack?.DisplayName} -> {backpackSlot.Content.DisplayName}");
            }

            // 根据背包槽是否有内容,判断我们自己的快捷键系统是否启用
            bool hadBackpack = IsShortcutSystemEnabled;
            IsShortcutSystemEnabled = backpackSlot?.Content != null;

            Debug.Log($"[BackpackShortcutManager] 之前系统状态: {hadBackpack}");
            Debug.Log($"[BackpackShortcutManager] 现在系统状态: {IsShortcutSystemEnabled}");

            // 如果是否启用的状态发生变化，触发事件
            if (hadBackpack != IsShortcutSystemEnabled)
            {
                Debug.Log($"[BackpackShortcutManager] 系统状态发生变化，触发事件");
                OnShortcutSystemStateChanged?.Invoke(IsShortcutSystemEnabled);
            }

            // 先取消之前的订阅
            UnsubscribeFromBackpackChanges();

            if (IsShortcutSystemEnabled)
            {
                Debug.Log("[BackpackShortcutManager] 快捷键系统已启用，开始收集物品");

                // 收集物品
                _categorizedItems = BackpackItemCollector.CollectItemsFromBackpack(backpackSlot.Content);

                // 订阅背包和配件的内容变化事件
                SubscribeToBackpackChanges(backpackSlot.Content);

                // 开始监听技能释放事件
                StartMonitoringSkills();

                // 更新快捷键 UI 显示各类别的第一个物品
                UpdateShortcutUI();
            }
            else
            {
                Debug.Log("[BackpackShortcutManager] 快捷键系统已停用，清空物品");
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

            Debug.Log("[BackpackShortcutManager] OnBackpackChanged 执行完成");
            Debug.Log("═══════════════════════════════════════");
        }

        public Item GetCurrentItem(ItemCategory category)
        {
            if (!IsShortcutSystemEnabled || !_categorizedItems.ContainsKey(category) || _categorizedItems[category].Count == 0)
                return null;

            int index = _currentSelection[category];
            if (index < 0 || index >= _categorizedItems[category].Count)
                index = 0;

            return _categorizedItems[category][index];
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
        /// 更新快捷键 UI，显示各类别的第一个物品
        /// </summary>
        private void UpdateShortcutUI()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("开始更新背包快捷键 UI...");

            int successCount = 0;
            int failCount = 0;

            // 遍历我们的物品分类，将每个分类的第一个物品设置到对应的快捷键槽位
            for (int index = 0; index < 4; index++)
            {
                var category = IndexToCategory(index);
                Item itemToShow = null;

                // 获取该分类的第一个物品
                if (_categorizedItems.ContainsKey(category) && _categorizedItems[category].Count > 0)
                {
                    itemToShow = _categorizedItems[category][0];
                }

                // 尝试更新 UI
                if (itemToShow != null)
                {
                    bool success = ShortcutUIUpdater.TryUpdateShortcutUI(index, itemToShow);
                    if (success) successCount++;
                    else failCount++;
                }
                else
                {
                    Debug.Log($"○ 快捷键 {index} ({category}) - 无物品，清空快捷栏");
                    // 当没有物品时，也需要清空快捷栏
                    ShortcutUIUpdater.TriggerShortcutRefresh(index);
                }
            }

            Debug.Log($"快捷键 UI 更新完成: {successCount} 成功, {failCount} 失败（物品在配件中）");
            Debug.Log($"物品统计: 医疗×{_categorizedItems.GetValueOrDefault(ItemCategory.Medical)?.Count ?? 0}, " +
                      $"兴奋剂×{_categorizedItems.GetValueOrDefault(ItemCategory.Stim)?.Count ?? 0}, " +
                      $"食物×{_categorizedItems.GetValueOrDefault(ItemCategory.Food)?.Count ?? 0}, " +
                      $"爆炸物×{_categorizedItems.GetValueOrDefault(ItemCategory.Explosive)?.Count ?? 0}");
            Debug.Log("═══════════════════════════════════════");
        }

        /// <summary>
        /// 清空所有快捷键 UI
        /// </summary>
        private void ClearShortcutUI()
        {
            Debug.Log("背包已卸下，开始清空快捷键 UI...");

            // 触发所有快捷键索引的刷新事件
            // 这会让 ItemShortcutButton 和 ItemShortcutEditorEntry 都刷新
            // 由于系统已被禁用，ItemShortcutGetPatch 会返回 null，UI 会显示为空
            for (int i = 0; i < 4; i++)
            {
                ShortcutUIUpdater.TriggerShortcutRefresh(i);
            }

            Debug.Log("快捷键 UI 已清空");
        }

        /// <summary>
        /// 开始监听技能释放事件
        /// </summary>
        private void StartMonitoringSkills()
        {
            if (_currentBackpack == null) return;

            Debug.Log("[BackpackShortcutManager] 开始监听技能释放事件");

            // 查找背包中所有有技能的物品
            FindAndMonitorSkillsRecursive(_currentBackpack);
        }

        /// <summary>
        /// 停止监听技能释放事件
        /// </summary>
        private void StopMonitoringSkills()
        {
            Debug.Log("[BackpackShortcutManager] 停止监听技能释放事件");

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
                Debug.Log($"[BackpackShortcutManager] 已监听物品 {item.DisplayName} 的技能释放事件");
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

            Debug.Log("[BackpackShortcutManager] 检测到技能释放事件");

            // 延迟刷新，确保物品状态已更新
            StartCoroutine(DelayedRefresh());
        }
    }
}