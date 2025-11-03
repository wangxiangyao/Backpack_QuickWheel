using HarmonyLib;
using ItemStatsSystem;
using System.Reflection;
using UnityEngine;

namespace Backpack_QuickWheel.Patches
{
    /// <summary>
    /// 修补 ItemDetailsDisplay.Awake() 方法
    ///
    /// 问题背景：
    /// ItemDetailsDisplay的Awake()中只订阅了SlotCollectionDisplay.onElementDoubleClicked事件
    /// 没有订阅onElementClicked事件
    /// 这导致单击配件插槽中的物品时，没有任何反应
    ///
    /// 解决方案：
    /// 在Awake()执行后，额外订阅onElementClicked事件
    /// 当用户单击插槽中的物品时，改变Selection来显示该物品的详情
    ///
    /// 注意：此功能存在已知问题，第二排配件详情可能会自动关闭
    /// 详见 KNOWN_ISSUES.md
    /// </summary>
    [HarmonyPatch(typeof(ItemDetailsDisplay), "Awake")]
    public class ItemDetailsDisplaySlotClickPatch
    {
        static void Postfix(ItemDetailsDisplay __instance)
        {
            // 通过反射获取slotCollectionDisplay
            FieldInfo slotCollectionDisplayField = typeof(ItemDetailsDisplay).GetField(
                "slotCollectionDisplay",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (slotCollectionDisplayField == null)
            {
                Debug.LogError("[ItemDetailsDisplaySlotClickPatch] 无法找到slotCollectionDisplay字段");
                return;
            }

            var slotCollectionDisplay = slotCollectionDisplayField.GetValue(__instance) as ItemSlotCollectionDisplay;

            if (slotCollectionDisplay == null)
            {
                Debug.LogError("[ItemDetailsDisplaySlotClickPatch] slotCollectionDisplay为null");
                return;
            }

            // 订阅onElementClicked事件
            // 当用户单击插槽中的物品时，显示该物品的详情
            slotCollectionDisplay.onElementClicked += (collectionDisplay, slotDisplay) =>
            {
                if (slotDisplay == null)
                {
                    return;
                }

                Item item = slotDisplay.GetItem();
                if (item == null)
                {
                    return;
                }

                // 通过SlotDisplay获取该物品对应的ItemDisplay
                var itemDisplayField = typeof(SlotDisplay).GetField("itemDisplay",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                ItemDisplay itemDisplay = null;
                if (itemDisplayField != null)
                {
                    itemDisplay = itemDisplayField.GetValue(slotDisplay) as ItemDisplay;
                }

                // 使用Select更新全局Selection
                if (itemDisplay != null && itemDisplay.Target == item)
                {
                    Debug.Log($"[ItemDetailsDisplaySlotClickPatch] 点击了配件: {item.DisplayName}");

                    // 简单的Select调用，不添加复杂的保护逻辑
                    ItemUIUtilities.Select(itemDisplay);
                }
            };
        }
    }
}