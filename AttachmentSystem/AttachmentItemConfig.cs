using System.Collections.Generic;

namespace Great_backpack.AttachmentSystem
{
    public class AttachmentItemConfig
    {
        public string ItemName { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public int TypeID { get; set; }
        public float Weight { get; set; }
        public int Value { get; set; }
        public string RequiredTag { get; set; }
        public List<SlotConfig> SlotConfigs { get; set; }

        public AttachmentItemConfig(string itemName, string displayName, string description,
                                   int typeID, float weight, int value, string requiredTag,
                                   List<SlotConfig> slotConfigs)
        {
            ItemName = itemName;
            DisplayName = displayName;
            Description = description;
            TypeID = typeID;
            Weight = weight;
            Value = value;
            RequiredTag = requiredTag;
            SlotConfigs = slotConfigs;
        }
    }
}