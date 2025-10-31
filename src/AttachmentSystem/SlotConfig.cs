using System.Collections.Generic;

namespace Backpack_QuickWheel.AttachmentSystem
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

        public SlotConfig(string displayName, string key, string description, List<string> restrictTags)
        {
            DisplayName = displayName;
            Key = key;
            Description = description;
            RestrictTags = restrictTags ?? new List<string>();
        }
    }
}