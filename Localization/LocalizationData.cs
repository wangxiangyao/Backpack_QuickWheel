using System.Collections.Generic;

namespace Backpack_QuickWheel.Localization
{
    public class LocalizationData
    {
        // 存储所有语言的本地化数据
        // 键: 语言代码 (如 "zh-CN", "en-US")
        // 值: 该语言下的键值对映射
        public Dictionary<string, Dictionary<string, string>> LanguageMappings { get; set; }

        public LocalizationData()
        {
            LanguageMappings = new Dictionary<string, Dictionary<string, string>>();
        }

        // 添加语言数据
        public void AddLanguageData(string languageCode, Dictionary<string, string> mappings)
        {
            LanguageMappings[languageCode] = mappings;
        }

        // 获取指定语言的本地化文本
        public string GetText(string languageCode, string key)
        {
            if (LanguageMappings.TryGetValue(languageCode, out var languageData) &&
                languageData.TryGetValue(key, out var text))
            {
                return text;
            }
            return null;
        }
    }
}
