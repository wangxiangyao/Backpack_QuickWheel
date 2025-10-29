using HarmonyLib;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using System;
using System.Reflection;
using UnityEngine;

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
            {
                Debug.Log("[TryGetRequiredItem-Patch] 原始方法已找到物品，不需要继续搜索");
                return;
            }

            if (!fromCharacter)
            {
                Debug.Log("[TryGetRequiredItem-Patch] fromCharacter为null");
                return;
            }

            // 直接访问public字段
            bool requireItem = __instance.requireItem;
            int requireItemId = __instance.requireItemId;

            Debug.Log($"[TryGetRequiredItem-Patch] 进入Postfix，requireItem={requireItem}, requireItemId={requireItemId}");

            if (!requireItem)
            {
                Debug.Log("[TryGetRequiredItem-Patch] requireItem=false，无需查找物品");
                return;
            }

            // 检查角色装备的Slots中的物品（包括已装备的背包）
            if (fromCharacter.CharacterItem.Slots == null || fromCharacter.CharacterItem.Slots.Count == 0)
            {
                Debug.Log("[TryGetRequiredItem-Patch] 角色Slots为null或为空");
                return;
            }

            Debug.Log($"[TryGetRequiredItem-Patch] 角色有 {fromCharacter.CharacterItem.Slots.Count} 个装备槽位");

            // 遍历角色装备的Slots
            foreach (Slot slot in fromCharacter.CharacterItem.Slots)
            {
                if (slot.Content == null)
                {
                    Debug.Log("[TryGetRequiredItem-Patch]   装备槽位为空");
                    continue;
                }

                Debug.Log($"[TryGetRequiredItem-Patch]   检查装备: {slot.Content.DisplayName} (TypeID={slot.Content.TypeID})");

                // 递归检查这个物品的Slots及其嵌套Slots（包括背包和配件）
                ValueTuple<bool, Item> found = SearchInSlots(slot.Content, requireItemId);
                if (found.Item1)
                {
                    Debug.Log($"[TryGetRequiredItem-Patch] ✓ 找到了！");
                    __result = found;
                    return;
                }
            }

            // 同时也检查背包Inventory中的物品（未装备的物品）
            if (fromCharacter.CharacterItem.Inventory != null)
            {
                Debug.Log($"[TryGetRequiredItem-Patch] 同时检查背包Inventory中未装备的物品...");
                foreach (Item item in fromCharacter.CharacterItem.Inventory)
                {
                    if (item == null) continue;

                    Debug.Log($"[TryGetRequiredItem-Patch]   检查物品: {item.DisplayName} (TypeID={item.TypeID})");

                    if (item.TypeID == requireItemId)
                    {
                        Debug.Log($"[TryGetRequiredItem-Patch] ✓ 在Inventory中找到了！");
                        __result = new ValueTuple<bool, Item>(true, item);
                        return;
                    }

                    // 检查物品的Slots
                    ValueTuple<bool, Item> found = SearchInSlots(item, requireItemId);
                    if (found.Item1)
                    {
                        Debug.Log($"[TryGetRequiredItem-Patch] ✓ 在Inventory物品的Slots中找到了！");
                        __result = found;
                        return;
                    }
                }
            }

            Debug.Log($"[TryGetRequiredItem-Patch] 未找到TypeID={requireItemId}的物品");
        }

        /// <summary>
        /// 递归搜索物品的Slots中的目标物品
        /// 包括嵌套配件的Slots（配件中的配件）
        /// </summary>
        private static ValueTuple<bool, Item> SearchInSlots(Item item, int requireItemId)
        {
            if (item == null)
            {
                Debug.Log("[TryGetRequiredItem-Patch] SearchInSlots: item为null");
                return new ValueTuple<bool, Item>(false, null);
            }

            if (item.Slots == null)
            {
                Debug.Log($"[TryGetRequiredItem-Patch] SearchInSlots: {item.DisplayName} 的Slots为null");
                return new ValueTuple<bool, Item>(false, null);
            }

            if (item.Slots.Count == 0)
            {
                Debug.Log($"[TryGetRequiredItem-Patch] SearchInSlots: {item.DisplayName} 的Slots为空");
                return new ValueTuple<bool, Item>(false, null);
            }

            Debug.Log($"[TryGetRequiredItem-Patch]     {item.DisplayName} 有 {item.Slots.Count} 个Slot，开始检查内容...");

            // 遍历物品的所有Slots
            foreach (Slot slot in item.Slots)
            {
                if (slot.Content == null)
                {
                    Debug.Log($"[TryGetRequiredItem-Patch]       Slot为空");
                    continue;
                }

                Debug.Log($"[TryGetRequiredItem-Patch]       Slot内容: {slot.Content.DisplayName} (TypeID={slot.Content.TypeID})");

                // 检查Slot中的物品是否是目标物品
                if (slot.Content.TypeID == requireItemId)
                {
                    Debug.Log($"[TryGetRequiredItem-Patch] ✓✓✓ 在 {item.DisplayName} 的插槽中找到所需物品: {slot.Content.DisplayName}");
                    return new ValueTuple<bool, Item>(true, slot.Content);
                }

                // ★ 递归：如果Slot中的物品也有Slots，继续搜索（配件中的配件）
                if (slot.Content.Slots != null && slot.Content.Slots.Count > 0)
                {
                    Debug.Log($"[TryGetRequiredItem-Patch]       继续递归检查 {slot.Content.DisplayName}...");
                    ValueTuple<bool, Item> foundInNestedSlots = SearchInSlots(slot.Content, requireItemId);
                    if (foundInNestedSlots.Item1)
                    {
                        return foundInNestedSlots;
                    }
                }
            }

            return new ValueTuple<bool, Item>(false, null);
        }
    }
}
