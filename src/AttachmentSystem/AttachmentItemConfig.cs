using Backpack_QuickWheel.Localization;
using System.Collections.Generic;

namespace Backpack_QuickWheel.AttachmentSystem
{
    public class AttachmentItemConfig
    {
        public string ItemName { get; set; }
        public string DisplayName { get; set; }  // 本地化键
        public string Description { get; set; }  // 物品描述
        public int TypeID { get; set; }
        public float Weight { get; set; }
        public int Value { get; set; }
        public int Quality { get; set; }  // 品质值
        public ItemStatsSystem.DisplayQuality DisplayQuality { get; set; }  // 品质等级显示
        public string RequiredTag { get; set; }
        public List<SlotConfig> SlotConfigs { get; set; }
        public string EmbeddedSpritePath { get; set; }

        // 新增：本地化数据引用
        public LocalizationData Localization { get; set; }

        public AttachmentItemConfig(string itemName, string DisplayName, string description,
                                   int typeID, float weight, int value, string requiredTag,
                                   List<SlotConfig> slotConfigs, string embeddedSpritePath = null)
        {
            ItemName = itemName;
            this.DisplayName = DisplayName;
            Description = description;
            TypeID = typeID;
            Weight = weight;
            Value = value;
            RequiredTag = requiredTag;
            SlotConfigs = slotConfigs;
            EmbeddedSpritePath = embeddedSpritePath;
            Localization = new LocalizationData();
        }
    }
}