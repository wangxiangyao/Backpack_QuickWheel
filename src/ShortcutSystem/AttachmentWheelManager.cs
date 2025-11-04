using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using Duckov;
using Duckov.Utilities;

namespace Backpack_QuickWheel.ShortcutSystem
{
    /// <summary>
    /// 配件系统轮盘数据管理器
    /// 完整封装现有的配件系统逻辑，支持主背包 + 配件的混合数据源
    /// 实现完整的三层事件订阅架构
    /// </summary>
    public class AttachmentWheelManager : IWheelDataManager
    {
        private readonly WheelLayoutManager _wheelLayoutManager;

        #region 配件系统数据结构 (完全复制自BackpackShortcutManager)

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

        // 事件订阅管理
        private HashSet<Item> _subscribedItems = new HashSet<Item>();

        #endregion

        #region 构造和初始化

        public AttachmentWheelManager(WheelLayoutManager wheelLayoutManager)
        {
            _wheelLayoutManager = wheelLayoutManager ?? throw new ArgumentNullException(nameof(wheelLayoutManager));
        }

        public void Initialize()
        {
            // 初始化数据结构
            _attachmentCategories = new Dictionary<Item, HashSet<ItemCategory>>();
            _subscribedAttachments = new List<Item>();
            _subscribedAttachmentSlots = new HashSet<Slot>();
            _slotContentHistory = new Dictionary<Slot, Item>();
            _subscribedItems = new HashSet<Item>();

            // 🆕 检查当前是否有装备的背包，如果有则重新收集配件物品
            Debug.Log($"[AttachmentWheelManager] 初始化检查：当前背包 = {_currentBackpack?.DisplayName ?? "null"}");
            if (_currentBackpack != null && IsBackpackSupported(_currentBackpack))
            {
                Debug.Log("[AttachmentWheelManager] 初始化时发现当前背包，重新收集配件物品");
                HandleSupportedBackpack(_currentBackpack);
            }
            else
            {
                Debug.Log("[AttachmentWheelManager] 初始化时没有符合条件的背包，跳过配件收集");
            }
        }

        public void Shutdown()
        {
            // 取消所有事件订阅
            UnsubscribeFromAllAttachmentEvents();

            // 清空数据
            _attachmentCategories?.Clear();
            _subscribedAttachments?.Clear();
            _subscribedAttachmentSlots?.Clear();
            _slotContentHistory?.Clear();
            _subscribedItems?.Clear();
            _currentBackpack = null;
        }

        #endregion

        #region IWheelDataManager 实现

        public void HandleBackpackChange(Item backpack)
        {

            // 🔧 无论什么情况，先清理当前状态
            UnsubscribeFromBackpackChanges();

            // 更新当前背包
            _currentBackpack = backpack;

            // 🆕 智能激活机制：检查新背包是否支持配件系统
            bool isSupportedBackpack = IsBackpackSupported(backpack);

            if (isSupportedBackpack)
            {
                HandleSupportedBackpack(backpack);
            }
            else
            {
                HandleUnsupportedBackpack();
            }
        }

        public void OnGameStart()
        {
            Debug.Log("AttachmentWheelManager: 游戏开始处理");

            if (_currentBackpack != null && IsBackpackSupported(_currentBackpack))
            {
                // 重新收集配件数据
                RefreshItemsWithNewArchitecture();
            }
        }

        public string GetManagerType()
        {
            return "AttachmentWheelManager";
        }

        #endregion

        #region 背包支持检查 (完全复制BackpackShortcutManager逻辑)

        /// <summary>
        /// 🆕 检查背包是否支持配件系统
        /// </summary>
        private bool IsBackpackSupported(Item backpack)
        {
            if (backpack == null)
            {
                Debug.Log("[AttachmentWheelManager] 🎯 背包为null，不支持配件系统");
                return false;
            }

            // 🔧 修复：使用与BackpackShortcutManager相同的配置检查
            bool isSupported = BackpackModConfig.BackpackTypeIDs.Contains(backpack.TypeID);
            Debug.Log($"[AttachmentWheelManager] 🎯 背包 {backpack.DisplayName} (TypeID: {backpack.TypeID}) 支持状态: {isSupported}");

            if (isSupported)
            {
                Debug.Log($"[AttachmentWheelManager] ✅ 背包 {backpack.DisplayName} 支持配件系统 - TypeID {backpack.TypeID} 在支持列表中");
            }
            else
            {
                Debug.Log($"[AttachmentWheelManager] ❌ 背包 {backpack.DisplayName} 不支持配件系统 - TypeID {backpack.TypeID} 不在支持列表 [{string.Join(", ", BackpackModConfig.BackpackTypeIDs)}] 中");
            }

            return isSupported;
        }

        /// <summary>
        /// 🆕 处理支持配件系统的背包
        /// </summary>
        private void HandleSupportedBackpack(Item backpack)
        {
            if (backpack == null) return;

            Debug.Log($"[AttachmentWheelManager] 更新当前背包为: {backpack.DisplayName}");

            // 🧹 装备支持背包前，先清理官方快捷键UI显示
            Debug.Log("[AttachmentWheelManager] 🧹 装备支持背包前，清理官方快捷键UI显示");
            for (int i = 0; i < 4; i++)
            {
                ShortcutUIUpdater.ClearShortcutUI(i);
            }
            Debug.Log("[AttachmentWheelManager] ✓ 已清空官方快捷键UI显示，准备启动配件系统");

            // 🔧 注册背包类型到ItemTypeRegistry
            ItemTypeRegistry.RegisterBackpack(backpack.TypeID);

            SubscribeToBackpackChanges(backpack);
            Debug.Log("[AttachmentWheelManager] 已订阅新背包变化事件");

            // 启用完整的配件系统
            // 🆕 使用新架构刷新物品
            Debug.Log("[AttachmentWheelManager] 开始刷新物品...");
            RefreshItemsWithNewArchitecture();
        }

        /// <summary>
        /// 🆕 处理不支持配件系统的背包
        /// </summary>
        private void HandleUnsupportedBackpack()
        {
            Debug.Log($"[AttachmentWheelManager] 🔧 处理不支持的背包");

            // 清空轮盘显示
            _wheelLayoutManager.ClearAllCategories();

            if (_currentBackpack != null)
            {
                // 注册背包类型但不订阅配件事件
                ItemTypeRegistry.RegisterBackpack(_currentBackpack.TypeID);
            }
        }

        /// <summary>
        /// 取消订阅所有配件的事件 (无参数版本，用于Shutdown)
        /// </summary>
        private void UnsubscribeFromAllAttachmentEvents()
        {
            Debug.Log("[AttachmentWheelManager] 取消订阅所有配件事件");

            // 取消订阅配件本身的拔出事件
            foreach (var attachment in _subscribedAttachments.ToList())
            {
                if (attachment != null)
                {
                    UnsubscribeFromAttachmentEvents(attachment);
                }
            }
        }

        #endregion

        #region 配件事件订阅 (完整三层架构)

        /// <summary>
        /// 订阅背包变化事件 (复制BackpackShortcutManager.SubscribeToBackpackChanges)
        /// </summary>
        private void SubscribeToBackpackChanges(Item backpack)
        {
            if (backpack == null) return;

            Debug.Log("[AttachmentWheelManager] 已订阅背包内容变化事件");

            // 🚨 修复：正确订阅背包的配件slot变化事件
            if (backpack.Slots != null)
            {
                var attachmentSlotIndices = GetAttachmentSlotIndices(backpack);
                Debug.Log($"[AttachmentWheelManager] 🎯 开始订阅背包配件slot，总slot数: {backpack.Slots.Count}，配件slot索引: [{string.Join(", ", attachmentSlotIndices)}]");

                for (int i = 0; i < backpack.Slots.Count; i++)
                {
                    var slot = backpack.Slots[i];
                    if (attachmentSlotIndices.Contains(i))
                    {
                        // ✅ 正确：订阅背包的配件slot（如SidePocket_Large）
                        slot.onSlotContentChanged += OnBackpackSlotContentChanged;
                        _subscribedAttachmentSlots.Add(slot);
                        Debug.Log($"[AttachmentWheelManager] ✅ 订阅背包配件slot变化: {slot?.Key} (索引: {i})");

                        // 如果slot已有内容，注册配件类型并建立历史记录
                        if (slot != null && slot.Content != null)
                        {
                            var attachment = slot.Content;
                            ItemTypeRegistry.RegisterAttachment(attachment.TypeID);

                            // 🔧 修复：建立初始历史记录，解决游戏重启后历史记录缺失问题
                            _slotContentHistory[slot] = attachment;
                            Debug.Log($"[AttachmentWheelManager] 注册已存在配件类型: {attachment.DisplayName} (TypeID: {attachment.TypeID})");
                            Debug.Log($"[AttachmentWheelManager] 📝 建立初始历史记录: {attachment.DisplayName} 在 slot {slot.Key}");

                            // 🔧 新增：建立初始配件类别记录，解决配件类别记录缺失问题
                            BuildAttachmentCategories(attachment);

                            // 🏗️ 三层架构：订阅已存在配件的事件（包括内部物品）
                            SubscribeToAttachmentEvents(attachment);
                            Debug.Log($"[AttachmentWheelManager] 🔧 订阅已存在配件 {attachment.DisplayName} 的三层事件");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 取消订阅背包变化事件 (复制BackpackShortcutManager.UnsubscribeFromBackpackChanges)
        /// </summary>
        private void UnsubscribeFromBackpackChanges()
        {
            // 取消背包内容变化监听
            if (_currentBackpack != null)
            {
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

            Debug.Log("[AttachmentWheelManager] 已取消背包变化事件订阅");
        }

        /// <summary>
        /// 🏗️ 三层事件订阅架构 (完整复制BackpackShortcutManager.SubscribeToAttachmentEvents)
        /// </summary>
        private void SubscribeToAttachmentEvents(Item attachmentItem)
        {
            if (attachmentItem == null || _subscribedAttachments.Contains(attachmentItem))
                return;

            Debug.Log($"[AttachmentWheelManager] 🏗️ 开始三层架构订阅 - 第二层：配件 {attachmentItem.DisplayName}");

            // 第二层：订阅配件本身的拔出事件（关键修复：之前缺失！）
            attachmentItem.onUnpluggedFromSlot += OnAttachmentUnplugged;
            _subscribedAttachments.Add(attachmentItem);
            Debug.Log($"[AttachmentWheelManager] ✅ 第二层完成：订阅配件拔出事件 - {attachmentItem.DisplayName}");

            // 第二层：订阅配件内部slot的内容变化事件
            if (attachmentItem.Slots != null)
            {
                Debug.Log($"[AttachmentWheelManager] 🏗️ 第二层：订阅配件内部slot内容变化 - {attachmentItem.Slots.Count} 个slot");

                for (int i = 0; i < attachmentItem.Slots.Count; i++)
                {
                    var internalSlot = attachmentItem.Slots[i];
                    Debug.Log($"[AttachmentWheelManager] 🔍 检查slot[{i}]: {internalSlot?.Key} (null: {internalSlot == null})");

                    if (internalSlot != null)
                    {
                        internalSlot.onSlotContentChanged += OnAttachmentInternalSlotChanged;
                        _subscribedAttachmentSlots.Add(internalSlot);
                        Debug.Log($"[AttachmentWheelManager] ✅ 第二层完成：订阅slot {internalSlot.Key} 内容变化事件");

                        // 🔧 修复：订阅slot事件后，立即检查是否有物品，有物品就订阅物品的unplug
                        if (internalSlot.Content != null)
                        {
                            Debug.Log($"[AttachmentWheelManager] 🏗️ 第三层：slot {internalSlot.Key} 当前有物品 {internalSlot.Content.DisplayName}，立即订阅其拔出事件");
                            SubscribeToItemUnpluggedEvent(internalSlot.Content);
                        }
                        else
                        {
                            Debug.Log($"[AttachmentWheelManager] 📝 slot {internalSlot.Key} 当前为空，等待后续物品放入");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[AttachmentWheelManager] ❌ slot[{i}] 为null，跳过订阅");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[AttachmentWheelManager] ❌ 配件 {attachmentItem.DisplayName} 的Slots为null，无法完成第二层订阅！");
            }

            Debug.Log($"[AttachmentWheelManager] 🏗️ 三层架构订阅完成 - 配件：{attachmentItem.DisplayName}");
        }

        /// <summary>
        /// 🔧 第三层：订阅单个物品的拔出事件 (复制BackpackShortcutManager.SubscribeToItemUnpluggedEvent)
        /// </summary>
        private void SubscribeToItemUnpluggedEvent(Item item)
        {
            if (item == null || _subscribedItems.Contains(item))
            {
                Debug.Log($"[AttachmentWheelManager] 🔄 物品 {item?.DisplayName ?? "null"} 无需订阅或已订阅");
                return;
            }

            // 第三层：订阅配件内物品的拔出事件
            item.onUnpluggedFromSlot += OnItemUnpluggedFromSlot;
            _subscribedItems.Add(item);
            Debug.Log($"[AttachmentWheelManager] ✅ 第三层完成：订阅物品 {item.DisplayName} (TypeID: {item.TypeID}) 拔出事件");
        }

        #endregion

        #region 事件处理器 (完整三层架构)

        /// <summary>
        /// 🔧 配件槽位内容变化事件处理 - 处理配件本身的放入/取出 (复制BackpackShortcutManager.OnBackpackSlotContentChanged)
        /// </summary>
        private void OnBackpackSlotContentChanged(Slot slot)
        {
            Item attachmentItem = slot?.Content;  // 这里是配件本身（如工具箱）
            Debug.Log($"[AttachmentWheelManager] 背包slot内容变化: {attachmentItem?.DisplayName ?? "null"}, slot: {slot?.Key}");

            if (attachmentItem != null)
            {
                // 配件移入

                // 🔧 关键修复：防止把背包当作配件处理
                if (ItemTypeRegistry.IsBackpack(attachmentItem))
                {
                    Debug.LogWarning($"[AttachmentWheelManager] ❌ 错误：背包 {attachmentItem.DisplayName} 被当作配件处理，跳过配件事件处理");
                    Debug.LogWarning($"[AttachmentWheelManager] 这个问题通常意味着背包被错误地放入了配件槽位");

                    // 🔧 记录slot内容历史（即使是背包也要记录，以便正确处理移除）
                    _slotContentHistory[slot] = attachmentItem;
                    return; // 跳过处理，防止把背包当作配件
                }

                // 🔧 记录slot内容历史，用于后续移除时的类型识别
                _slotContentHistory[slot] = attachmentItem;

                // 🚨 新增：将OnBackpackContentChanged的有用功能合并过来
                // 注册配件类型到ItemTypeRegistry
                ItemTypeRegistry.RegisterAttachment(attachmentItem.TypeID);
                Debug.Log($"[AttachmentWheelManager] 配件类型注册: {attachmentItem.DisplayName} (TypeID: {attachmentItem.TypeID})");

                // 🔧 配件被放入背包槽
                HandleAttachmentPutIntoSlot(attachmentItem, slot);

                // 🏗️ 三层架构：调用新的事件订阅方法
                SubscribeToAttachmentEvents(attachmentItem);
            }
        }

        /// <summary>
        /// 🔧 配件拔出事件处理 - 第二层关键补充 (复制BackpackShortcutManager.OnAttachmentUnplugged)
        /// </summary>
        private void OnAttachmentUnplugged(Item unpluggedAttachment)
        {
            Debug.Log($"[AttachmentWheelManager] 🔧 第二层事件：配件拔出 - {unpluggedAttachment.DisplayName} (TypeID: {unpluggedAttachment.TypeID})");

            // 取消订阅该配件的所有事件
            UnsubscribeFromAttachmentEvents(unpluggedAttachment);
            Debug.Log($"[AttachmentWheelManager] ✅ 已取消配件 {unpluggedAttachment.DisplayName} 的所有事件订阅");

            // 获取配件被拔出前影响的类别
            if (_attachmentCategories.TryGetValue(unpluggedAttachment, out HashSet<ItemCategory> affectedCategories))
            {
                Debug.Log($"[AttachmentWheelManager] 配件 {unpluggedAttachment.DisplayName} 影响类别: {string.Join(", ", affectedCategories)}");

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
                                    Debug.Log($"[AttachmentWheelManager] 🔧 准备移除配件内物品: {item.DisplayName} (类别: {category})");
                                }
                            }
                        }
                    }

                    // 批量移除物品
                    if (itemsToRemove.Count > 0)
                    {
                        Debug.Log($"[AttachmentWheelManager] 🔧 批量移除类别 {category} 的 {itemsToRemove.Count} 个物品");
                        _wheelLayoutManager.BatchRemoveItems(category, itemsToRemove);
                    }
                }

                // 清理配件类别记录
                _attachmentCategories.Remove(unpluggedAttachment);
            }
            else
            {
                Debug.LogWarning($"[AttachmentWheelManager] 配件 {unpluggedAttachment.DisplayName} 无类别记录，无法精确更新");
            }
        }

        /// <summary>
        /// 🔧 配件内部槽位变化事件处理 - 处理配件内部物品的放入/取出 (复制BackpackShortcutManager.OnAttachmentInternalSlotChanged)
        /// </summary>
        private void OnAttachmentInternalSlotChanged(Slot internalSlot)
        {
            Item internalItem = internalSlot?.Content;  // 配件内部的物品（如可乐）
            Debug.Log($"[AttachmentWheelManager] 配件内部槽位变化: {internalItem?.DisplayName ?? "null"}, slot: {internalSlot?.Key}");

            if (internalItem != null)
            {
                // 🔧 物品被放入配件内部槽位
                HandleItemPutIntoAttachmentSlot(internalItem, internalSlot);

                // 🔧 修复：有物品放入，立即订阅该物品的拔出事件
                Debug.Log($"[AttachmentWheelManager] 🏗️ 第三层：检测到物品 {internalItem.DisplayName} 放入，立即订阅其拔出事件");
                SubscribeToItemUnpluggedEvent(internalItem);
            }
            else
            {
                // 🔧 物品从配件内部槽位取出，但物品拔出事件应该由OnItemUnpluggedFromSlot处理
                Debug.Log($"[AttachmentWheelManager] 配件内部槽位变空，等待物品拔出事件处理");
                // 物品拔出事件会由OnItemUnpluggedFromSlot处理，这里不需要做额外处理
            }
        }

        /// <summary>
        /// 🔧 物品拔出事件处理 - 专门处理物品**拖出**事件 (复制BackpackShortcutManager.OnItemUnpluggedFromSlot)
        /// </summary>
        private void OnItemUnpluggedFromSlot(Item unpluggedItem)
        {
            if (unpluggedItem == null) return;

            Debug.Log($"[AttachmentWheelManager] 🔧 物品拔出事件: {unpluggedItem.DisplayName} (TypeID: {unpluggedItem.TypeID})");

            // 确定被拔出物品的类别
            ItemCategory affectedCategory = ItemCategorizer.CategorizeItem(unpluggedItem);
            Debug.Log($"[AttachmentWheelManager] 物品拔出，影响类别: {affectedCategory} ({unpluggedItem.DisplayName})");

            // 🏗️ 重构：Manager统一协调所有系统更新，避免事件循环
            Debug.Log($"[AttachmentWheelManager] Manager统一处理物品移除: {unpluggedItem.DisplayName} (类别: {affectedCategory})");

            if (affectedCategory != ItemCategory.None)
            {
                // 🏗️ 重构：Manager调用各系统的自我管理方法
                // 1. 先更新轮盘系统的数据
                _wheelLayoutManager.RemoveItem(affectedCategory, unpluggedItem);

                Debug.Log($"[AttachmentWheelManager] ✓ 已通知各系统移除物品: {unpluggedItem.DisplayName}");
            }
            else
            {
                Debug.Log($"[AttachmentWheelManager] 被拔出的物品 {unpluggedItem.DisplayName} 无有效类别，跳过更新");
            }
        }

        #endregion

        #region 配件处理方法 (完整复制BackpackShortcutManager逻辑)

        /// <summary>
        /// 🔧 建立配件类别记录的抽象方法 (复制BackpackShortcutManager.BuildAttachmentCategories)
        /// </summary>
        private void BuildAttachmentCategories(Item attachment)
        {
            if (attachment == null || attachment.Slots == null) return;

            Debug.Log($"[AttachmentWheelManager] 🔧 建立配件类别记录: {attachment.DisplayName}");

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
            Debug.Log($"[AttachmentWheelManager] 📝 记录配件 {attachment.DisplayName} 包含的类别: {string.Join(", ", attachmentCategories)}");
        }

        /// <summary>
        /// 🎯 根据背包配置获取配件slot的索引 (复制BackpackShortcutManager.GetAttachmentSlotIndices)
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
                Debug.Log($"[AttachmentWheelManager] 背包 {backpack.DisplayName} (TypeID: {backpack.TypeID}) 配件slot: {string.Join(", ", attachmentSlotNames)}");

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
                                Debug.Log($"[AttachmentWheelManager] 找到配件slot: {i} (requireTag: {requireTag.name})");
                                break;
                            }
                        }
                    }
                }
            }

            return attachmentIndices;
        }

        /// <summary>
        /// 🔧 处理配件被放入背包槽的事件 (复制BackpackShortcutManager.HandleAttachmentPutIntoSlot)
        /// </summary>
        private void HandleAttachmentPutIntoSlot(Item attachment, Slot slot)
        {
            Debug.Log($"[AttachmentWheelManager] 🔧 配件放入: {attachment.DisplayName} (TypeID: {attachment.TypeID}) 到 slot: {slot?.Key}");

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

                    Debug.Log($"[AttachmentWheelManager] 配件 {attachment.DisplayName} 贡献类别 {category}: {items.Count} 个物品");

                    // 🆕 增量更新：逐个添加物品到轮盘，避免全量刷新
                    foreach (var item in items)
                    {
                        Debug.Log($"[AttachmentWheelManager] 增量添加物品: {item.DisplayName} 到类别 {category}");
                        _wheelLayoutManager.AddItemToCategory(category, item);
                    }
                }
            }
            else
            {
                Debug.Log($"[AttachmentWheelManager] 配件 {attachment.DisplayName} 无内部槽位");
            }
        }

        /// <summary>
        /// 🔧 处理物品被放入配件内部槽位的事件 (复制BackpackShortcutManager.HandleItemPutIntoAttachmentSlot)
        /// </summary>
        private void HandleItemPutIntoAttachmentSlot(Item item, Slot internalSlot)
        {
            Debug.Log($"[AttachmentWheelManager] 🔧 物品放入配件内部: {item.DisplayName} (TypeID: {item.TypeID}) 到 slot: {internalSlot?.Key}");

            // 确定物品类别并触发单物品更新
            ItemCategory affectedCategory = ItemCategorizer.CategorizeItem(item);
            Debug.Log($"[AttachmentWheelManager] 物品放入配件内部，影响类别: {affectedCategory} ({item.DisplayName})");

            if (affectedCategory != ItemCategory.None)
            {
                // 🆕 新架构：只调用轮盘系统，UI更新由轮盘直接处理
                // 1. 更新轮盘系统，轮盘会自动处理选中状态并直接更新UI
                _wheelLayoutManager.AddItemToCategory(affectedCategory, item);

                Debug.Log($"[AttachmentWheelManager] ✓ 已通知各系统添加物品: {item.DisplayName} (类别: {affectedCategory})");
            }
            else
            {
                Debug.Log($"[AttachmentWheelManager] 放入配件内部的物品 {item.DisplayName} 无有效类别，跳过更新");
            }
        }

        /// <summary>
        /// 🔧 取消订阅配件的所有事件 (复制BackpackShortcutManager.UnsubscribeFromAttachmentEvents)
        /// </summary>
        private void UnsubscribeFromAttachmentEvents(Item attachment)
        {
            if (attachment == null) return;

            Debug.Log($"[AttachmentWheelManager] 🔧 开始取消订阅配件事件: {attachment.DisplayName}");

            // 取消订阅配件本身的拔出事件
            if (_subscribedAttachments.Contains(attachment))
            {
                attachment.onUnpluggedFromSlot -= OnAttachmentUnplugged;
                _subscribedAttachments.Remove(attachment);
                Debug.Log($"[AttachmentWheelManager] ✅ 已取消订阅配件拔出事件");
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
                        Debug.Log($"[AttachmentWheelManager] ✅ 已取消订阅slot {slot.Key} 内容变化事件");
                    }
                }

                // 取消订阅配件内物品的事件
                UnsubscribeFromItemsInAttachment(attachment);
            }

            Debug.Log($"[AttachmentWheelManager] ✅ 配件 {attachment.DisplayName} 所有事件订阅已清理");
        }

        /// <summary>
        /// 🔧 递归取消配件内部所有物品的事件订阅 (复制BackpackShortcutManager.UnsubscribeFromItemsInAttachment)
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
                        Debug.Log($"[AttachmentWheelManager] 已取消物品拔出事件: {item.DisplayName} (来自配件 {attachment.DisplayName})");
                    }

                    // 递归检查嵌套容器
                    if (item.Slots != null && item.Slots.Count > 0)
                    {
                        UnsubscribeFromItemsInAttachment(item);
                    }
                }
            }
        }

        #endregion

        #region 物品收集和刷新 (复制BackpackShortcutManager逻辑)

        /// <summary>
        /// 🆕 新架构：刷新物品列表 (复制BackpackShortcutManager.RefreshItemsWithNewArchitecture)
        /// </summary>
        private void RefreshItemsWithNewArchitecture()
        {
            if (_currentBackpack == null)
            {
                Debug.Log("[AttachmentWheelManager] 背包为空，清空所有物品");

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

            Debug.Log("[AttachmentWheelManager] 开始收集背包物品");

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
                Debug.Log($"[AttachmentWheelManager] 🔥 全量更新类别 {category}: {items.Count} 个物品");
            }

            Debug.Log("[AttachmentWheelManager] 事件驱动架构：物品刷新完成，等待UI更新事件");
        }

        /// <summary>
        /// 递归收集背包和所有配件中的所有物品 (复制BackpackShortcutManager.CollectAllItemsFromBackpack)
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

        #endregion
    }
}