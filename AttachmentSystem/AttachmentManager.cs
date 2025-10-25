using Duckov.Utilities;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using UnityEngine;
using System.Collections.Generic;

namespace Great_backpack.AttachmentSystem
{
    public class AttachmentManager
    {
        private Dictionary<string, Item> attachmentItems = new Dictionary<string, Item>();
        private Dictionary<string, Tag> createdTags;

        public AttachmentManager(Dictionary<string, Tag> tags)
        {
            createdTags = tags;
        }

        public void CreateAllAttachmentItems()
        {
            Debug.Log("开始创建配件物品...");

            foreach (var config in BackpackModConfig.AttachmentItemConfigs)
            {
                CreateAttachmentItemFromConfig(config);
            }

            Debug.Log($"共创建了{attachmentItems.Count}个配件物品");
        }

        private void CreateAttachmentItemFromConfig(AttachmentItemConfig config)
        {
            try
            {
                GameObject itemObject = new GameObject(config.ItemName);
                Item newItem = itemObject.AddComponent<Item>();

                // 配置基础属性
                ConfigureItemProperties(newItem, config);

                // 设置配件Tag
                SetAttachmentTag(newItem, config.RequiredTag);

                // 创建插槽
                CreateItemSlots(newItem, config.SlotConfigs);

                // 添加效果组件
                AddAttachmentEffect(itemObject, config);

                // 注册到游戏
                ItemStatsSystem.ItemAssetsCollection.AddDynamicEntry(newItem);

                attachmentItems[config.ItemName] = newItem;
                Debug.Log($"成功创建配件: {config.DisplayName} (TypeID: {config.TypeID})");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"创建配件物品 {config.DisplayName} 时出错: {e.Message}");
            }
        }

        private void ConfigureItemProperties(Item item, AttachmentItemConfig config)
        {
            item.TypeID = config.TypeID;
            item.DisplayNameRaw = config.DisplayName;
            item.Weight = config.Weight;
            item.Value = config.Value;
            item.MaxStackCount = 1;
        }

        private void SetAttachmentTag(Item item, string requiredTag)
        {
            if (createdTags.TryGetValue(requiredTag, out Tag attachmentTag))
            {
                item.Tags.Add(attachmentTag);
            }
        }

        private void CreateItemSlots(Item item, List<SlotConfig> slotConfigs)
        {
            if (slotConfigs.Count == 0) return;

            if (item.Slots == null)
            {
                item.CreateSlotsComponent();
            }

            foreach (var slotConfig in slotConfigs)
            {
                AddSlotToItem(item, slotConfig);
            }
        }

        private void AddSlotToItem(Item item, SlotConfig slotConfig)
        {
            try
            {
                SlotCollection slotCollection = item.Slots;
                if (slotCollection == null) return;

                Slot newSlot = new Slot(slotConfig.Key);
                newSlot.Initialize(slotCollection);

                // 设置显示名称Tag
                if (createdTags.TryGetValue(slotConfig.Key, out Tag displayTag))
                {
                    if (newSlot.requireTags == null)
                        newSlot.requireTags = new List<Tag>();

                    newSlot.requireTags.Add(displayTag);
                }

                // TODO: 设置插槽的内容限制Tag
                // 这里需要等我们研究游戏中的物品Tag系统

                slotCollection.Add(newSlot);
                Debug.Log($"为物品 {item.DisplayName} 添加插槽: {slotConfig.Key}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"为物品添加插槽时出错: {e.Message}");
            }
        }

        private void AddAttachmentEffect(GameObject itemObject, AttachmentItemConfig config)
        {
            var attachmentEffect = itemObject.AddComponent<BackpackAttachmentEffect>();
            attachmentEffect.Initialize(config.RequiredTag, config.Weight, config.Value, config.SlotConfigs.Count);
        }
    }
}