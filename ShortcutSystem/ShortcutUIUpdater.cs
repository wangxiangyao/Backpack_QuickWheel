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
                TriggerOnSetItemEvent(index);
                Debug.Log($"✓ 快捷键 {index} UI 已通过事件刷新");
                return true;
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
        /// 清空指定快捷键槽位
        /// </summary>
        /// <param name="index">快捷键索引</param>
        public static void ClearShortcutUI(int index)
        {
            // 游戏的 ItemShortcut.Set 不接受 null
            // UI 会自动检测物品有效性并清空显示
            Debug.Log($"清空快捷键 {index}");
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
        private static void TriggerOnSetItemEvent(int index)
        {
            // 防止递归触发事件
            if (_isTriggeringEvent)
            {
                Debug.LogWarning($"[ShortcutUIUpdater] 正在触发事件中，跳过避免递归");
                return;
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
                    }
                    else
                    {
                        Debug.LogWarning($"[ShortcutUIUpdater] OnSetItem 事件没有订阅者");
                    }
                }
                else
                {
                    Debug.LogError($"[ShortcutUIUpdater] 无法通过反射找到 OnSetItem 事件");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ShortcutUIUpdater] 触发 OnSetItem 事件时出错: {ex.Message}");
            }
            finally
            {
                _isTriggeringEvent = false;
            }
        }
    }
}
