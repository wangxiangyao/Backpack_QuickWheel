using ItemStatsSystem;
using ItemStatsSystem.Items;
using UnityEngine;
using System.Collections.Generic;
using Backpack_QuickWheel.AttachmentSystem;
using Duckov.Utilities;

namespace Backpack_QuickWheel.BackpackSystem
{
    public class BackpackModifier
    {
        private Dictionary<string, Tag> createdTags;

        public BackpackModifier(Dictionary<string, Tag> tags)
        {
            createdTags = tags;
        }

        public void ModifyAllBackpacks()
        {
            foreach (int backpackTypeID in BackpackModConfig.BackpackTypeIDs)
            {
                Item backpackPrefab = ItemAssetsCollection.GetPrefab(backpackTypeID);
                if (backpackPrefab != null)
                {
                    AddAttachmentSlotsToBackpack(backpackPrefab);
                    Debug.Log($"已为TypeID {backpackTypeID} 的背包添加插槽");
                }
                else
                {
                    Debug.LogWarning($"未找到TypeID {backpackTypeID} 的背包预制体");
                }
            }
        }

        private void AddAttachmentSlotsToBackpack(Item backpack)
        {
            if (backpack.Slots == null)
            {
                backpack.CreateSlotsComponent();
                Debug.Log($"为背包创建了SlotCollection组件");
            }

            AddSlotsViaCollection(backpack);
        }

        private void AddSlotsViaCollection(Item backpack)
        {
            try
            {
                SlotCollection slotCollection = backpack.Slots;
                if (slotCollection == null) return;

                int originalSlotCount = slotCollection.Count;

                if (BackpackModConfig.BackpackSlotConfigs.TryGetValue(backpack.TypeID, out List<string> slotTypes))
                {
                    for (int i = 0; i < slotTypes.Count; i++)
                    {
                        string slotType = slotTypes[i];
                        string slotKey = $"{slotType}_{i + 1}";
                        AddAttachmentSlot(slotCollection, slotKey, slotType, i + 1);
                    }

                    Debug.Log($"成功为背包[{backpack.TypeID}]添加了{slotTypes.Count}个插槽，现在共有{slotCollection.Count}个插槽");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"添加插槽时出错: {e.Message}");
            }
        }

        private void AddAttachmentSlot(SlotCollection slotCollection, string slotKey, string slotType, int slotIndex)
        {
            try
            {
                if (!BackpackModConfig.SlotConfigs.TryGetValue(slotType, out SlotConfig config))
                {
                    Debug.LogWarning($"未找到插槽类型[{slotType}]的配置");
                    return;
                }

                Slot newSlot = new Slot(slotKey);
                newSlot.Initialize(slotCollection);

                // 设置显示名称Tag
                if (createdTags.TryGetValue(slotType, out Tag slotTag))
                {
                    if (newSlot.requireTags == null)
                        newSlot.requireTags = new List<Tag>();

                    newSlot.requireTags.Add(slotTag);
                }

                // 设置插槽的内容限制
                if (config.RestrictTags != null && config.RestrictTags.Count > 0)
                {
                    foreach (string restrictTagName in config.RestrictTags)
                    {
                        if (createdTags.TryGetValue(restrictTagName, out Tag restrictTag))
                        {
                            if (newSlot.requireTags == null)
                                newSlot.requireTags = new List<Tag>();
                            newSlot.requireTags.Add(restrictTag);
                        }
                    }
                }

                slotCollection.Add(newSlot);
                Debug.Log($"成功创建插槽[{slotIndex}]: {config.DisplayName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"创建插槽 {slotKey} 时出错: {e.Message}");
            }
        }
    }
}