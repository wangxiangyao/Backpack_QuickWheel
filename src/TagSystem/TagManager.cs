using Duckov.Utilities;
using UnityEngine;
using System.Collections.Generic;

namespace Backpack_QuickWheel
{
    public class TagManager
    {
        public Dictionary<string, Tag> CreatedTags { get; private set; }

        public TagManager()
        {
            CreatedTags = new Dictionary<string, Tag>();
        }

        public void CreateRequiredTags()
        {
            Debug.Log("开始创建插槽所需的Tag...");

            // 创建插槽类型Tag
            foreach (var slotConfig in BackpackModConfig.SlotConfigs.Values)
            {
                CreateTagForSlot(slotConfig.Key, slotConfig.DisplayName);
            }

            // 创建配件插槽的显示Tag
            foreach (var attachmentConfig in BackpackModConfig.AttachmentItemConfigs)
            {
                foreach (var slotConfig in attachmentConfig.SlotConfigs)
                {
                    CreateTagForSlot(slotConfig.Key, slotConfig.DisplayName);
                }
            }

            Debug.Log($"共创建了{CreatedTags.Count}个Tag");
        }

        private void CreateTagForSlot(string tagName, string displayName)
        {
            try
            {
                if (CreatedTags.ContainsKey(tagName)) return;

                Tag newTag = ScriptableObject.CreateInstance<Tag>();
                newTag.name = tagName;

                SetTagLocalization(tagName, displayName);

                CreatedTags[tagName] = newTag;
                Debug.Log($"成功创建Tag: {tagName} -> {displayName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"创建Tag {tagName} 时出错: {e.Message}");
            }
        }

        private void SetTagLocalization(string tagKey, string displayName)
        {
            try
            {
                string localizationKey = "Tag_" + tagKey;
                SodaCraft.Localizations.LocalizationManager.SetOverrideText(localizationKey, displayName);
                Debug.Log($"设置本地化: {localizationKey} -> {displayName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"设置Tag本地化时出错: {e.Message}");
            }
        }
    }
}