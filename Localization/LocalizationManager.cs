using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Great_backpack.Localization
{
    public static class LocalizationManager
    {
        private static bool _isInitialized = false;
        private static Dictionary<string, string> _currentLanguageData;

        // 初始化本地化系统
        public static void Initialize(SystemLanguage language)
        {
            if (_isInitialized) return;

            string languageCode = LanguageDetector.ToLanguageCode(language);
            _currentLanguageData = new Dictionary<string, string>();

            // 收集所有配置中的本地化数据
            CollectAllLocalizationData(languageCode);

            // 注册到游戏本地化系统
            RegisterToGameLocalization();

            _isInitialized = true;
            Debug.Log($"本地化系统初始化完成，语言: {language} ({languageCode})");
        }

        // 收集所有配置项的本地化数据
        private static void CollectAllLocalizationData(string languageCode)
        {
            // 收集物品的本地化数据
            foreach (var itemConfig in BackpackModConfig.AttachmentItemConfigs)
            {
                // 添加空值检查
                if (itemConfig?.Localization == null)
                {
                    Debug.LogWarning($"物品配置或本地化数据为空: {itemConfig?.ItemName ?? "未知物品"}");
                    continue;
                }

                if (string.IsNullOrEmpty(itemConfig.DisplayName))
                {
                    Debug.LogError($"物品 DisplayName 为空: {itemConfig.ItemName}");
                    continue;
                }

                // 收集显示名称的本地化文本
                var nameText = itemConfig.Localization.GetText(languageCode, itemConfig.DisplayName);
                if (!string.IsNullOrEmpty(nameText))
                {
                    _currentLanguageData[itemConfig.DisplayName] = nameText;
                    Debug.Log($"收集名称本地化: {itemConfig.DisplayName} -> {nameText}");
                }
                else
                {
                    Debug.LogWarning($"未找到物品 '{itemConfig.ItemName}' 的名称本地化文本，键: {itemConfig.DisplayName}");
                }

                // 收集描述的本地化文本（描述键 = 名称键 + "_Desc"）
                string descKey = itemConfig.DisplayName + "_Desc";
                var descText = itemConfig.Localization.GetText(languageCode, descKey);
                if (!string.IsNullOrEmpty(descText))
                {
                    _currentLanguageData[descKey] = descText;
                    Debug.Log($"收集描述本地化: {descKey} -> {descText}");
                }
                else
                {
                    Debug.LogWarning($"未找到物品 '{itemConfig.ItemName}' 的描述本地化文本，键: {descKey}");
                }
            }



            // 收集插槽的本地化数据（如果需要）
            //foreach (var slotConfig in BackpackModConfig.UnifiedSlotTypes.Values)
            //{
            //    if (slotConfig.Localization != null && !string.IsNullOrEmpty(slotConfig.DisplayNameKey))
            //    {
            //        var text = slotConfig.Localization.GetText(languageCode, slotConfig.DisplayNameKey);
            //        if (!string.IsNullOrEmpty(text))
            //        {
            //            _currentLanguageData[slotConfig.DisplayNameKey] = text;
            //        }
            //    }
            //}

            Debug.Log($"成功收集到 {_currentLanguageData.Count} 个本地化条目（包含名称和描述）");
        }

        // 注册到游戏本地化系统
        private static void RegisterToGameLocalization()
        {
            foreach (var entry in _currentLanguageData)
            {
                SodaCraft.Localizations.LocalizationManager.SetOverrideText(entry.Key, entry.Value);
                Debug.Log($"注册本地化: {entry.Key} -> {entry.Value}");
            }
        }

        // 重新加载本地化（语言切换时调用）
        public static void Reload(SystemLanguage newLanguage)
        {
            _isInitialized = false;
            _currentLanguageData?.Clear();

            // 移除之前注册的覆盖文本
            if (_currentLanguageData != null)
            {
                foreach (var key in _currentLanguageData.Keys.ToList())
                {
                    SodaCraft.Localizations.LocalizationManager.RemoveOverrideText(key);
                }
            }

            Initialize(newLanguage);
        }

        // 获取当前语言的文本
        public static string GetText(string key)
        {
            if (_currentLanguageData != null && _currentLanguageData.TryGetValue(key, out var text))
            {
                return text;
            }
            return null;
        }
    }
}
