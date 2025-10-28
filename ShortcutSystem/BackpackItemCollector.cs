using System.Collections.Generic;
using System.Linq;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using Duckov.Utilities;

namespace Great_backpack.ShortcutSystem
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

            // 初始化所有分类（包括None，但不使用它）
            foreach (ItemCategory category in System.Enum.GetValues(typeof(ItemCategory)))
            {
                if (category != ItemCategory.None) // 跳过None分类
                {
                    categorizedItems[category] = new List<Item>();
                }
            }

            if (backpack == null) return categorizedItems;

            // 从背包的所有配件中收集物品
            CollectFromSlots(backpack.Slots, categorizedItems);
            CollectFromInventory(backpack.Inventory, categorizedItems);

            return categorizedItems;
        }

        private static void CollectFromSlots(SlotCollection slots, Dictionary<ItemCategory, List<Item>> categorizedItems)
        {
            if (slots == null) return;

            foreach (Slot slot in slots)
            {
                if (slot?.Content != null)
                {
                    // 递归收集配件中的物品
                    CollectItemsRecursive(slot.Content, categorizedItems);
                }
            }
        }

        private static void CollectFromInventory(Inventory inventory, Dictionary<ItemCategory, List<Item>> categorizedItems)
        {
            if (inventory == null) return;

            foreach (Item item in inventory)
            {
                if (item != null)
                {
                    CollectItemsRecursive(item, categorizedItems);
                }
            }
        }

        private static void CollectItemsRecursive(Item item, Dictionary<ItemCategory, List<Item>> categorizedItems)
        {
            // 检查当前物品是否是我们需要收集的类型
            if (IsCollectibleItem(item))
            {
                var category = ItemCategorizer.CategorizeItem(item);

                // 只收集有效分类的物品
                if (category != ItemCategory.None && categorizedItems.ContainsKey(category))
                {
                    if (!categorizedItems[category].Contains(item))
                    {
                        categorizedItems[category].Add(item);
                    }
                }
            }

            // 递归收集子物品（无论当前物品是否被收集，都要检查其内容）
            if (item.Slots != null)
            {
                foreach (Slot slot in item.Slots)
                {
                    if (slot?.Content != null)
                    {
                        CollectItemsRecursive(slot.Content, categorizedItems);
                    }
                }
            }

            if (item.Inventory != null)
            {
                foreach (Item childItem in item.Inventory)
                {
                    if (childItem != null)
                    {
                        CollectItemsRecursive(childItem, categorizedItems);
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
            if (item.IsBeingDestroyed || item.ParentItem == null)
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