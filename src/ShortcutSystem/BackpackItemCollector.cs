using System.Collections.Generic;
using System.Linq;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using Duckov.Utilities;

namespace Backpack_QuickWheel.ShortcutSystem
{
    public static class BackpackItemCollector
    {
        // 需要排除的配件Tag（这些是容器本身，不是可使用的物品）
        private static readonly HashSet<string> ExcludedContainerTags = new HashSet<string>
        {
            "TacticalPouch_Small", "TacticalPouch_Large", "SidePocket_Small", "SidePocket_Large",
            "LockBuckle", "ShoulderStrap", "ShoulderPouch", "AmmoPouch"
        };

        public static Dictionary<ItemCategory, List<Item>> CollectItemsFromBackpack(Item backpack)
        {
            var categorizedItems = new Dictionary<ItemCategory, List<Item>>();

            // 🔧 修复：使用物品实例进行去重，避免同一物品被多次收集
            // 注意：使用对象引用而不是TypeID，因为TypeID是类型ID不是实例ID
            var collectedItems = new System.Collections.Generic.HashSet<Item>();

            // 初始化所有分类（包括None，但不使用它）
            foreach (ItemCategory category in System.Enum.GetValues(typeof(ItemCategory)))
            {
                if (category != ItemCategory.None) // 跳过None分类
                {
                    categorizedItems[category] = new List<Item>();
                }
            }

            if (backpack == null)
            {
                return categorizedItems;
            }

            // 🔍 调试：详细追踪收集过程
            UnityEngine.Debug.Log($"[BackpackItemCollector] ══════════════════════════════════════");
            UnityEngine.Debug.Log($"[BackpackItemCollector] 开始收集物品，背包: {backpack.DisplayName}");
            UnityEngine.Debug.Log($"[BackpackItemCollector] 背包槽位数: {(backpack.Slots?.Count ?? 0)}");
            int inventoryCount = 0;
            if (backpack.Inventory != null)
            {
                foreach (var invItem in backpack.Inventory)
                {
                    if (invItem != null) inventoryCount++;
                }
            }
            UnityEngine.Debug.Log($"[BackpackItemCollector] 背包库存数: {inventoryCount}");

            // 🔧 修复：先从配件槽收集，再从背包库存收集
            // 这样配件中的物品有优先权，避免重复
            CollectFromSlots(backpack.Slots, categorizedItems, collectedItems);
            CollectFromInventory(backpack.Inventory, categorizedItems, collectedItems);

            // 🔍 调试：详细输出收集结果
            int totalItems = categorizedItems.Values.Sum(list => list.Count);
            UnityEngine.Debug.Log($"[BackpackItemCollector] 物品收集完成: 总 {totalItems} 项");
            UnityEngine.Debug.Log($"[BackpackItemCollector] 已收集实例数: {collectedItems.Count}");

            foreach (var kvp in categorizedItems)
            {
                if (kvp.Value.Count > 0)
                {
                    var itemDetails = new System.Collections.Generic.List<string>();
                    for (int i = 0; i < kvp.Value.Count; i++)
                    {
                        var item = kvp.Value[i];
                        itemDetails.Add($"{item.DisplayName}(ID:{item.GetHashCode()})");
                    }
                    UnityEngine.Debug.Log($"[BackpackItemCollector]   {kvp.Key}: {kvp.Value.Count} 项 - {string.Join(", ", itemDetails)}");
                }
            }

            // 🔍 特别检查可乐物品
            if (categorizedItems.ContainsKey(ItemCategory.Food))
            {
                var foodItems = categorizedItems[ItemCategory.Food];
                var colaItems = foodItems.FindAll(item => item.DisplayName.Contains("可乐") || item.DisplayName.ToLower().Contains("cola"));
                if (colaItems.Count > 0)
                {
                    UnityEngine.Debug.Log($"[BackpackItemCollector] 🥤 检测到可乐: {colaItems.Count} 个");
                    for (int i = 0; i < colaItems.Count; i++)
                    {
                        var cola = colaItems[i];
                        UnityEngine.Debug.Log($"[BackpackItemCollector]   可乐{i+1}: {cola.DisplayName} (实例ID: {cola.GetHashCode()}, TypeID: {cola.TypeID})");
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[BackpackItemCollector] 🥤 未检测到可乐物品！");
                    UnityEngine.Debug.Log($"[BackpackItemCollector] 食物类别物品: {string.Join(", ", foodItems.ConvertAll(i => i.DisplayName))}");
                }
            }

            UnityEngine.Debug.Log($"[BackpackItemCollector] ══════════════════════════════════════");
            return categorizedItems;
        }

        private static void CollectFromSlots(SlotCollection slots, Dictionary<ItemCategory, List<Item>> categorizedItems, System.Collections.Generic.HashSet<Item> collectedItems)
        {
            if (slots == null) return;

            UnityEngine.Debug.Log($"[BackpackItemCollector] 开始收集槽位物品，槽位数: {slots.Count}");
            int slotIndex = 0;
            foreach (Slot slot in slots)
            {
                if (slot?.Content != null)
                {
                    UnityEngine.Debug.Log($"[BackpackItemCollector] 槽位{slotIndex}: {slot.Content.DisplayName} (Key: {slot.Key})");
                    // 递归收集配件中的物品
                    CollectItemsRecursive(slot.Content, categorizedItems, collectedItems);
                }
                else
                {
                    UnityEngine.Debug.Log($"[BackpackItemCollector] 槽位{slotIndex}: 空槽位 (Key: {slot.Key})");
                }
                slotIndex++;
            }
        }

        private static void CollectFromInventory(Inventory inventory, Dictionary<ItemCategory, List<Item>> categorizedItems, System.Collections.Generic.HashSet<Item> collectedItems)
        {
            if (inventory == null) return;

            // 计算库存物品数量
            int inventoryCount = 0;
            foreach (var invItem in inventory)
            {
                if (invItem != null) inventoryCount++;
            }

            UnityEngine.Debug.Log($"[BackpackItemCollector] 开始收集库存物品，物品数: {inventoryCount}");
            int invIndex = 0;
            foreach (Item item in inventory)
            {
                if (item != null)
                {
                    UnityEngine.Debug.Log($"[BackpackItemCollector] 库存{invIndex}: {item.DisplayName} (实例ID: {item.GetHashCode()})");
                    CollectItemsRecursive(item, categorizedItems, collectedItems);
                }
                else
                {
                    UnityEngine.Debug.Log($"[BackpackItemCollector] 库存{invIndex}: null物品");
                }
                invIndex++;
            }
        }

        private static void CollectItemsRecursive(Item item, Dictionary<ItemCategory, List<Item>> categorizedItems, System.Collections.Generic.HashSet<Item> collectedItems)
        {
            // 检查当前物品是否是我们需要收集的类型
            if (IsCollectibleItem(item))
            {
                var category = ItemCategorizer.CategorizeItem(item);

                // 调试：追踪物品分类过程
                if (category != ItemCategory.None)
                {
                    UnityEngine.Debug.Log($"[BackpackItemCollector] 收集: {item.DisplayName} (TypeID: {item.TypeID}) -> 分类: {category}");
                }
                else
                {
                    var tagNames = new System.Collections.Generic.List<string>();
                    foreach (Tag tag in item.Tags)
                    {
                        tagNames.Add(tag.name);
                    }
                    UnityEngine.Debug.LogWarning($"[BackpackItemCollector] ⚠ {item.DisplayName} 未被分类 (Tags: {string.Join(", ", tagNames)})");
                }

                // 🔧 修复：使用物品实例进行去重
                // 只收集有效分类的物品，且该物品实例还没有被收集过
                if (category != ItemCategory.None && categorizedItems.ContainsKey(category))
                {
                    if (!collectedItems.Contains(item))
                    {
                        categorizedItems[category].Add(item);
                        collectedItems.Add(item);
                        UnityEngine.Debug.Log($"[BackpackItemCollector] ✓ 新增物品: {item.DisplayName} (实例ID: {item.GetHashCode()})");
                    }
                    else
                    {
                        UnityEngine.Debug.Log($"[BackpackItemCollector] ⚠ 跳过重复物品: {item.DisplayName} (实例ID: {item.GetHashCode()})");
                    }
                }
            }
            else
            {
                // 调试：为什么这个物品被排除？
                var isDestroyed = item.IsBeingDestroyed;
                var hasExcludedTag = false;
                var tagNames = new System.Collections.Generic.List<string>();
                foreach (Tag tag in item.Tags)
                {
                    tagNames.Add(tag.name);
                    if (ExcludedContainerTags.Contains(tag.name))
                    {
                        hasExcludedTag = true;
                    }
                }
                var isShortcutItem = ItemCategorizer.IsShortcutItem(item);

                if (!isDestroyed && !hasExcludedTag && isShortcutItem)
                {
                    // 理论上应该被收集，但被排除了？
                    UnityEngine.Debug.LogError($"[BackpackItemCollector] ❌ {item.DisplayName} 应该被收集但被排除！ (IsDestroyed={isDestroyed}, HasExcludedTag={hasExcludedTag}, IsShortcutItem={isShortcutItem}, Tags={string.Join(", ", tagNames)})");
                }
            }

            // 递归收集子物品（无论当前物品是否被收集，都要检查其内容）
            if (item.Slots != null && item.Slots.Count > 0)
            {
                foreach (Slot slot in item.Slots)
                {
                    if (slot?.Content != null)
                    {
                        CollectItemsRecursive(slot.Content, categorizedItems, collectedItems);
                    }
                }
            }

            if (item.Inventory != null)
            {
                foreach (Item childItem in item.Inventory)
                {
                    if (childItem != null)
                    {
                        CollectItemsRecursive(childItem, categorizedItems, collectedItems);
                    }
                }
            }
        }

        /// <summary>
        /// 判断物品是否应该被收集
        /// </summary>
        private static bool IsCollectibleItem(Item item)
        {
            if (item == null) return false;

            // 检查物品是否已被销毁或无效
            if (item.IsBeingDestroyed)
            {
                return false;
            }

            // 检查是否是配件容器本身（应该排除）
            foreach (Tag tag in item.Tags)
            {
                if (ExcludedContainerTags.Contains(tag.name))
                {
                    return false; // 这是容器配件，不收集
                }
            }

            // 检查是否属于快捷键系统可收集的类型
            return ItemCategorizer.IsShortcutItem(item);
        }
    }
}