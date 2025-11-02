using ItemStatsSystem;
using UnityEngine;
using System;
using System.Reflection;

namespace Backpack_QuickWheel.ShortcutSystem
{
    /// <summary>
    /// 快捷键 UI 更新器
    /// 由于游戏原版的 ItemShortcut 只支持主背包中的物品，
    /// 我们需要通过 Harmony 补丁或其他方式来更新 UI
    /// </summary>
    public static class ShortcutUIUpdater
    {
        /// <summary>
        /// 委托：获取指定类别的当前选中物品
        /// 由BackpackShortcutManager设置，用于UI自我管理
        /// </summary>
        public static Func<ItemCategory, Item> GetCurrentSelectionDelegate { get; set; }
        /// <summary>
        /// 尝试更新快捷键 UI
        /// </summary>
        /// <param name="index">快捷键索引 (0-3)</param>
        /// <param name="item">要显示的物品</param>
        /// <returns>是否成功更新</returns>
        public static bool TryUpdateShortcutUI(int index, Item item)
        {
            if (item == null)
            {
                Debug.LogWarning($"尝试设置快捷键 {index} 为 null");
                return false;
            }

            // 尝试使用游戏原版的 ItemShortcut.Set
            // 如果物品在主背包中，这会成功
            bool success = Duckov.ItemShortcut.Set(index, item);

            if (success)
            {
                Debug.Log($"✓ 快捷键 {index} UI 已更新: {item.DisplayName}");
                return true;
            }
            else
            {
                // 如果失败，说明物品不在主背包中（在背包配件里）
                // 我们需要手动触发 OnSetItem 事件来刷新 UI
                Debug.Log($"○ 物品不在主背包中，手动触发 UI 刷新: {item.DisplayName}");

                // 🔧 新增：检查事件触发是否成功
                bool eventSuccess = TriggerOnSetItemEvent(index);

                if (eventSuccess)
                {
                    Debug.Log($"✓ 快捷键 {index} UI 已通过事件刷新");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"⚠️ 快捷键 {index} UI 刷新失败，可能游戏系统未完全初始化");
                    return false;
                }
            }
        }

        /// <summary>
        /// 获取快捷键对应的按键名称
        /// </summary>
        private static string GetKeyName(int index)
        {
            switch (index)
            {
                case 0: return "4 (医疗)";
                case 1: return "5 (兴奋剂)";
                case 2: return "Q (食物)";
                case 3: return "G (爆炸物)";
                default: return $"索引{index}";
            }
        }

        /// <summary>
        /// 更新快捷键UI（使用官方API）
        /// </summary>
        /// <param name="index">快捷键索引</param>
        /// <param name="item">要设置的物品（可为null）</param>
        public static void UpdateShortcutUI(int index, Item item)
        {
            Debug.Log($"[ShortcutUIUpdater] 更新快捷键 {index} 为 {item?.DisplayName ?? "null"}");

            try
            {
                // 直接使用官方API设置物品
                bool success = Duckov.ItemShortcut.Set(index, item);

                if (success)
                {
                    Debug.Log($"[ShortcutUIUpdater] ✓ 快捷键 {index} 已更新: {item?.DisplayName ?? "null"}");
                }
                else
                {
                    Debug.LogWarning($"[ShortcutUIUpdater] 快捷键 {index} 更新失败");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ShortcutUIUpdater] 更新快捷键 {index} 时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 清空指定快捷键槽位
        /// </summary>
        /// <param name="index">快捷键索引</param>
        public static void ClearShortcutUI(int index)
        {
            Debug.Log($"[ShortcutUIUpdater] 清空快捷键 {index}");

            try
            {
                // 🔧 修复：先尝试使用官方API清空快捷键
                bool success = Duckov.ItemShortcut.Set(index, null);

                if (success)
                {
                    Debug.Log($"[ShortcutUIUpdater] ✓ 快捷键 {index} 已通过官方API清空");
                }
                else
                {
                    Debug.Log($"[ShortcutUIUpdater] 官方API清空失败，使用事件触发方式");
                }

                // 🔧 修复：无论官方API是否成功，都触发UI刷新事件确保UI更新
                bool eventSuccess = TriggerOnSetItemEvent(index);

                if (eventSuccess)
                {
                    Debug.Log($"[ShortcutUIUpdater] ✓ 快捷键 {index} UI刷新事件已触发");
                }
                else
                {
                    Debug.Log($"[ShortcutUIUpdater] 快捷键 {index} 事件触发失败，UI将由游戏系统自动刷新");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ShortcutUIUpdater] 清空快捷键 {index} 时出错: {ex.Message}");
                // 不重新抛出异常，避免影响系统清理流程
            }
        }

        /// <summary>
        /// 手动触发快捷键UI刷新
        /// 用于在背包卸下等场景强制刷新UI显示
        /// </summary>
        /// <param name="index">快捷键索引</param>
        public static void TriggerShortcutRefresh(int index)
        {
            TriggerOnSetItemEvent(index);
        }

        // 防止递归触发事件的标志
        [System.ThreadStatic]
        private static bool _isTriggeringEvent = false;

        /// <summary>
        /// 手动触发 ItemShortcut.OnSetItem 事件
        /// 用于当物品不在主背包中时，强制刷新 UI
        /// </summary>
        /// <param name="index">快捷键索引</param>
        /// <returns>是否成功触发事件</returns>
        private static bool TriggerOnSetItemEvent(int index)
        {
            // 防止递归触发事件
            if (_isTriggeringEvent)
            {
                Debug.LogWarning($"[ShortcutUIUpdater] 正在触发事件中，跳过避免递归");
                return false;
            }

            _isTriggeringEvent = true;
            try
            {
                // 通过反射获取 OnSetItem 事件的字段
                var eventField = typeof(Duckov.ItemShortcut).GetField("OnSetItem",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (eventField != null)
                {
                    // 获取事件委托
                    var eventDelegate = eventField.GetValue(null) as MulticastDelegate;

                    if (eventDelegate != null)
                    {
                        // 触发事件，通知所有监听者
                        foreach (var handler in eventDelegate.GetInvocationList())
                        {
                            handler.Method.Invoke(handler.Target, new object[] { index });
                        }
                        Debug.Log($"[ShortcutUIUpdater] 已手动触发 OnSetItem 事件，索引: {index}");
                        return true; // 成功触发事件
                    }
                    else
                    {
                        Debug.LogWarning($"[ShortcutUIUpdater] OnSetItem 事件没有订阅者");
                        return false; // 没有订阅者，触发失败
                    }
                }
                else
                {
                    Debug.LogError($"[ShortcutUIUpdater] 无法通过反射找到 OnSetItem 事件");
                    return false; // 找不到事件字段，触发失败
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ShortcutUIUpdater] 触发 OnSetItem 事件时出错: {ex.Message}");
                return false; // 异常处理，触发失败
            }
            finally
            {
                _isTriggeringEvent = false;
            }

            return false; // 默认失败（理论上不会执行到这里）
        }

        #region 🏗️ 自我管理方法 - 支持类别和物品级别的操作

        /// <summary>
        /// 🏗️ 获取类别对应的快捷键按键索引
        /// 将物品类别映射到游戏对应的快捷键（0=4键医疗，1=5键兴奋剂，2=Q键食物，3=G键爆炸物）
        /// </summary>
        private static int GetShortcutKeyIndex(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Medical: return 0;  // 4键 - 医疗物品
                case ItemCategory.Stim: return 1;     // 5键 - 兴奋剂
                case ItemCategory.Food: return 2;     // Q键 - 食物
                case ItemCategory.Explosive: return 3; // G键 - 爆炸物
                default: return -1;                 // 不支持的类别
            }
        }

        /// <summary>
        /// 🏗️ 更新指定类别的UI（自我管理方法）
        /// 根据当前选中物品自动判断是更新还是清空UI
        /// </summary>
        /// <param name="category">要更新的类别</param>
        public static void UpdateCategoryUI(ItemCategory category)
        {
            Debug.Log($"[ShortcutUIUpdater] 🏗️ 自我管理：更新类别 {category} 的UI");

            var index = GetShortcutKeyIndex(category);
            if (index < 0)
            {
                Debug.LogWarning($"[ShortcutUIUpdater] 类别 {category} 无对应快捷键");
                return;
            }

            // 通过委托获取当前选中物品
            var currentItem = GetCurrentSelectionDelegate?.Invoke(category);

            if (currentItem != null && !currentItem.IsBeingDestroyed)
            {
                Debug.Log($"[ShortcutUIUpdater] 🏗️ 类别 {category} 有选中物品: {currentItem.DisplayName}");
                TryUpdateShortcutUI(index, currentItem);
            }
            else
            {
                Debug.Log($"[ShortcutUIUpdater] 🏗️ 类别 {category} 无选中物品，清空UI");
                ClearShortcutUI(index);
            }
        }

        /// <summary>
        /// 🏗️ 处理物品移除的UI更新（自我管理方法）
        /// 当指定物品被移除时，更新对应类别的UI
        /// </summary>
        /// <param name="category">物品类别</param>
        /// <param name="removedItem">被移除的物品</param>
        public static void HandleItemRemoved(ItemCategory category, Item removedItem)
        {
            Debug.Log($"[ShortcutUIUpdater] 🏗️ 自我管理：处理物品移除 - {removedItem.DisplayName} (类别: {category})");

            // 直接更新类别的UI，系统会自动选择下一个物品或清空
            UpdateCategoryUI(category);
        }

        /// <summary>
        /// 🏗️ 处理物品添加的UI更新（自我管理方法）
        /// 当指定物品被添加时，更新对应类别的UI
        /// </summary>
        /// <param name="category">物品类别</param>
        /// <param name="addedItem">被添加的物品</param>
        public static void HandleItemAdded(ItemCategory category, Item addedItem)
        {
            Debug.Log($"[ShortcutUIUpdater] 🏗️ 自我管理：处理物品添加 - {addedItem.DisplayName} (类别: {category})");

            // 🔧 修复：检查选中状态是否发生了变化
            // 获取添加前的当前选中物品
            var currentSelection = GetCurrentSelectionDelegate?.Invoke(category);

            Debug.Log($"[ShortcutUIUpdater] 🏗️ 添加前选中物品: {currentSelection?.DisplayName ?? "null"}");

            // 根据选中逻辑规则判断：
            // 1. 如果之前没有选中物品，新物品应该成为选中 → 需要更新UI
            // 2. 如果之前有选中物品，保持当前选中 → 不需要更新UI
            bool shouldUpdateUI = (currentSelection == null || currentSelection.IsBeingDestroyed);

            if (shouldUpdateUI)
            {
                Debug.Log($"[ShortcutUIUpdater] 🏗️ 选中状态发生变化，更新UI");
                UpdateCategoryUI(category);
            }
            else
            {
                Debug.Log($"[ShortcutUIUpdater] 🏗️ 选中状态未变化，跳过UI更新");
            }
        }

        #endregion
    }
}
