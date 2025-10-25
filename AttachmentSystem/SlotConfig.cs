using System.Collections.Generic;

namespace Great_backpack.AttachmentSystem
{
    public class SlotConfig
    {
        public string Key { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public List<string> RestrictTags { get; set; }

        public SlotConfig(string displayName, string key, string description)
        {
            DisplayName = displayName;
            Key = key;
            Description = description;
            RestrictTags = new List<string>();
        }

        public SlotConfig(string key, string displayName, List<string> restrictTags)
        {
            Key = key;
            DisplayName = displayName;
            RestrictTags = restrictTags;
        }
    }
}