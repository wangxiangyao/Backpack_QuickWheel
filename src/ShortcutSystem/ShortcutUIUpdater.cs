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
            UpdateShortcutUI(index, null);
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
    }
}
