using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Backpack_QuickWheel.Localization
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
                    // Debug.Log($"收集名称本地化: {itemConfig.DisplayName} -> {nameText}");
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
                    // Debug.Log($"收集描述本地化: {descKey} -> {descText}");
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

            // 调试：显示收集到的所有本地化数据
            foreach (var entry in _currentLanguageData)
            {
                Debug.Log($"本地化数据: 键='{entry.Key}', 值='{entry.Value}'");
            }
        }

        // 注册到游戏本地化系统
        private static void RegisterToGameLocalization()
        {
            foreach (var entry in _currentLanguageData)
            {
                // 尝试多种可能的键名格式
                RegisterLocalizationWithMultipleFormats(entry.Key, entry.Value);
            }
        }

        // 使用多种格式注册本地化，确保覆盖所有可能的键名格式
        private static void RegisterLocalizationWithMultipleFormats(string key, string value)
        {
            // 格式1: 原始键名
            SodaCraft.Localizations.LocalizationManager.SetOverrideText(key, value);
            Debug.Log($"注册本地化: {key} -> {value}");

            //// 格式2: 移除"ITEM_"前缀（如果存在）
            //if (key.StartsWith("ITEM_"))
            //{
            //    string simplifiedKey = key.Substring(5); // 移除"ITEM_"
            //    SodaCraft.Localizations.LocalizationManager.SetOverrideText(simplifiedKey, value);
            //    Debug.Log($"注册本地化(简化): {simplifiedKey} -> {value}");
            //}

            //// 格式3: 移除"_NAME"后缀（如果存在）
            //if (key.EndsWith("_NAME"))
            //{
            //    string nameOnlyKey = key.Substring(0, key.Length - 5);
            //    SodaCraft.Localizations.LocalizationManager.SetOverrideText(nameOnlyKey, value);
            //    Debug.Log($"注册本地化(名称): {nameOnlyKey} -> {value}");
            //}

            //// 格式4: 移除"ITEM_"前缀和"_NAME"后缀
            //if (key.StartsWith("ITEM_") && key.EndsWith("_NAME"))
            //{
            //    string cleanKey = key.Substring(5, key.Length - 10); // 移除"ITEM_"和"_NAME"
            //    SodaCraft.Localizations.LocalizationManager.SetOverrideText(cleanKey, value);
            //    Debug.Log($"注册本地化(清理): {cleanKey} -> {value}");
            //}

            //// 格式5: 添加额外的测试键名格式
            //if (key.StartsWith("ITEM_") && key.EndsWith("_NAME"))
            //{
            //    // 尝试游戏可能使用的其他格式
            //    string testKey1 = key.Replace("ITEM_", "").Replace("_NAME", "");
            //    SodaCraft.Localizations.LocalizationManager.SetOverrideText(testKey1, value);
            //    Debug.Log($"注册本地化(测试1): {testKey1} -> {value}");

            //    string testKey2 = key.ToLower();
            //    SodaCraft.Localizations.LocalizationManager.SetOverrideText(testKey2, value);
            //    Debug.Log($"注册本地化(测试2): {testKey2} -> {value}");
            //}
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
                    Debug.Log($"移除本地化: {key}");
                }
            }

            Initialize(newLanguage);
        }

        // 强制重新注册所有本地化（用于修复显示问题）
        public static void ForceReRegister()
        {
            if (_currentLanguageData == null || _currentLanguageData.Count == 0)
            {
                Debug.LogWarning("本地化数据为空，无法强制重新注册");
                return;
            }

            Debug.Log($"强制重新注册 {_currentLanguageData.Count} 个本地化条目");
            RegisterToGameLocalization();
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
