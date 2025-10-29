using HarmonyLib;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using System;

namespace Great_backpack.Patches
{
    /// <summary>
    /// Patch：修复 InteractableBase.TryGetRequiredItem() 不检查配件Slots中的物品
    ///
    /// 问题：钥匙放在配件的插槽中时，系统无法识别，显示"需要钥匙"
    /// 原因：原始方法只检查背包Inventory中的直接物品和它们的Slots，
    ///       但配件放在背包的Slots中时，不会递归检查配件Slots中的物品
    ///
    /// 解决：递归检查所有物品的Slots，包括嵌套配件的Slots
    /// </summary>
    [HarmonyPatch(typeof(InteractableBase), "TryGetRequiredItem")]
    public class InteractableTryGetRequiredItemPatch
    {
        static void Postfix(InteractableBase __instance, CharacterMainControl fromCharacter, ref ValueTuple<bool, Item> __result)
        {
            // 如果已经找到了，就不需要继续查找
            if (__result.Item1)
                return;

            if (!fromCharacter)
                return;

            // 直接访问public字段
            bool requireItem = __instance.requireItem;
            int requireItemId = __instance.requireItemId;

            if (!requireItem)
                return;

            // 检查角色装备的Slots中的物品（包括已装备的背包）
            if (fromCharacter.CharacterItem.Slots == null || fromCharacter.CharacterItem.Slots.Count == 0)
                return;

            // 遍历角色装备的Slots
            foreach (Slot slot in fromCharacter.CharacterItem.Slots)
            {
                if (slot.Content == null)
                    continue;

                // 递归检查这个物品的Slots及其嵌套Slots（包括背包和配件）
                ValueTuple<bool, Item> found = SearchInSlots(slot.Content, requireItemId);
                if (found.Item1)
                {
                    __result = found;
                    return;
                }
            }

            // 同时也检查背包Inventory中的物品（未装备的物品）
            if (fromCharacter.CharacterItem.Inventory != null)
            {
                foreach (Item item in fromCharacter.CharacterItem.Inventory)
                {
                    if (item == null) continue;

                    if (item.TypeID == requireItemId)
                    {
                        __result = new ValueTuple<bool, Item>(true, item);
                        return;
                    }

                    // 检查物品的Slots
                    ValueTuple<bool, Item> found = SearchInSlots(item, requireItemId);
                    if (found.Item1)
                    {
                        __result = found;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// 递归搜索物品的Slots中的目标物品
        /// 包括嵌套配件的Slots（配件中的配件）
        /// </summary>
        private static ValueTuple<bool, Item> SearchInSlots(Item item, int requireItemId)
        {
            if (item == null || item.Slots == null || item.Slots.Count == 0)
                return new ValueTuple<bool, Item>(false, null);

            // 遍历物品的所有Slots
            foreach (Slot slot in item.Slots)
            {
                if (slot.Content == null)
                    continue;

                // 检查Slot中的物品是否是目标物品
                if (slot.Content.TypeID == requireItemId)
                    return new ValueTuple<bool, Item>(true, slot.Content);

                // ★ 递归：如果Slot中的物品也有Slots，继续搜索（配件中的配件）
                if (slot.Content.Slots != null && slot.Content.Slots.Count > 0)
                {
                    ValueTuple<bool, Item> foundInNestedSlots = SearchInSlots(slot.Content, requireItemId);
                    if (foundInNestedSlots.Item1)
                        return foundInNestedSlots;
                }
            }

            return new ValueTuple<bool, Item>(false, null);
        }
    }
}
