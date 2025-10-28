using System.Linq;
using ItemStatsSystem;

namespace Great_backpack.ShortcutSystem
{
    // 定义收集品类型
    public enum ItemCategory
    {
        None,       // 未分类/不收集
        Medical,    // 治疗物品
        Stim,       // 针剂
        Food,       // 食物
        Explosive,  // 爆炸物
        Melee       // 近战武器（使用官方系统）
    }

    public static class ItemCategorizer
    {
        // 定义收集品类型 - Tag的映射关系
        private static readonly System.Collections.Generic.Dictionary<string, ItemCategory> _tagMappings = new System.Collections.Generic.Dictionary<string, ItemCategory>
        {
            // 治疗物品 - 使用官方Tag
            { "Healing", ItemCategory.Medical },
            
            // 针剂 - 使用官方Tag
            { "Injector", ItemCategory.Stim },
            
            // 食物 - 使用官方Tag（已包含饮品）
            { "Food", ItemCategory.Food },
            
            // 爆炸物 - 使用官方Tag
            { "Explosive", ItemCategory.Explosive },
            
            // 近战武器 - 使用官方Tag
            { "MeleeWeapon", ItemCategory.Melee }
        };

        public static ItemCategory CategorizeItem(Item item)
        {
            if (item == null) return ItemCategory.None;

            // 检查物品是否有我们关心的Tag
            foreach (var mapping in _tagMappings)
            {
                if (item.Tags.Any(tag => tag.name == mapping.Key))
                {
                    return mapping.Value;
                }
            }

            // 没有匹配的Tag，返回None表示不收集
            return ItemCategory.None;
        }

        public static bool IsShortcutItem(Item item)
        {
            if (item == null) return false;

            foreach (var mapping in _tagMappings)
            {
                if (item.Tags.Any(tag => tag.name == mapping.Key))
                {
                    return true;
                }
            }

            return false;
        }
    }
}