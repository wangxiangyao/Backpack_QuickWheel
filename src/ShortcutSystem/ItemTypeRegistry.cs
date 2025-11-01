using System.Collections.Generic;
using UnityEngine;
using ItemStatsSystem;
using ItemStatsSystem.Items;

namespace Backpack_QuickWheel.ShortcutSystem
{
    /// <summary>
    /// 物品类型注册表
    /// 在初始化时记录类型，避免运行时的复杂判断
    /// 背包和配件的初始化入口完全不同，直接在初始化时标记类型
    /// </summary>
    public static class ItemTypeRegistry
    {
        private static readonly HashSet<int> BackpackTypeIDs = new HashSet<int>();
        private static readonly HashSet<int> AttachmentTypeIDs = new HashSet<int>();

        /// <summary>
        /// 注册背包类型ID
        /// 在背包初始化时调用
        /// </summary>
        public static void RegisterBackpack(int typeID)
        {
            if (BackpackTypeIDs.Add(typeID))
            {
                Debug.Log($"[ItemTypeRegistry] 注册背包类型: {typeID}");
            }
            else
            {
                Debug.Log($"[ItemTypeRegistry] 背包类型 {typeID} 已存在");
            }
        }

        /// <summary>
        /// 注册配件类型ID
        /// 在配件初始化时调用
        /// </summary>
        public static void RegisterAttachment(int typeID)
        {
            if (AttachmentTypeIDs.Add(typeID))
            {
                Debug.Log($"[ItemTypeRegistry] 注册配件类型: {typeID}");
            }
            else
            {
                Debug.Log($"[ItemTypeRegistry] 配件类型 {typeID} 已存在");
            }
        }

        /// <summary>
        /// 判断是否为背包
        /// </summary>
        public static bool IsBackpack(Item item)
        {
            return item != null && BackpackTypeIDs.Contains(item.TypeID);
        }

        /// <summary>
        /// 判断是否为配件
        /// </summary>
        public static bool IsAttachment(Item item)
        {
            return item != null && AttachmentTypeIDs.Contains(item.TypeID);
        }

        /// <summary>
        /// 判断是否为物品（非背包非配件）
        /// </summary>
        public static bool IsNormalItem(Item item)
        {
            return item != null && !IsBackpack(item) && !IsAttachment(item);
        }

        /// <summary>
        /// 获取物品类型描述（用于调试）
        /// </summary>
        public static string GetItemTypeDescription(Item item)
        {
            if (item == null) return "null";

            if (IsBackpack(item)) return $"背包(TypeID:{item.TypeID})";
            if (IsAttachment(item)) return $"配件(TypeID:{item.TypeID})";
            return $"物品(TypeID:{item.TypeID})";
        }

        /// <summary>
        /// 注销单个背包类型
        /// 当背包卸下时调用
        /// </summary>
        public static void UnregisterBackpack(int typeID)
        {
            if (BackpackTypeIDs.Remove(typeID))
            {
                Debug.Log($"[ItemTypeRegistry] 注销背包类型: {typeID}");
            }
            else
            {
                Debug.LogWarning($"[ItemTypeRegistry] 尝试注销不存在的背包类型: {typeID}");
            }
        }

        /// <summary>
        /// 注销单个配件类型
        /// 当配件从背包移除时调用
        /// </summary>
        public static void UnregisterAttachment(int typeID)
        {
            if (AttachmentTypeIDs.Remove(typeID))
            {
                Debug.Log($"[ItemTypeRegistry] 注销配件类型: {typeID}");
            }
            else
            {
                Debug.LogWarning($"[ItemTypeRegistry] 尝试注销不存在的配件类型: {typeID}");
            }
        }

        /// <summary>
        /// 清除所有注册信息
        /// 主要用于系统重置或测试
        /// </summary>
        public static void ClearAll()
        {
            int backpackCount = BackpackTypeIDs.Count;
            int attachmentCount = AttachmentTypeIDs.Count;

            BackpackTypeIDs.Clear();
            AttachmentTypeIDs.Clear();

            Debug.Log($"[ItemTypeRegistry] 已清除所有注册信息: 背包{backpackCount}个, 配件{attachmentCount}个");
        }

        /// <summary>
        /// 获取统计信息（用于调试）
        /// </summary>
        public static string GetStatistics()
        {
            return $"[ItemTypeRegistry] 当前注册状态: 背包类型{BackpackTypeIDs.Count}个, 配件类型{AttachmentTypeIDs.Count}个";
        }
    }
}