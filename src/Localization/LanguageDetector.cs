using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Backpack_QuickWheel.Localization
{
    public static class LanguageDetector
    {
        // 开发时使用：导出游戏支持的语言列表
        public static void ExportSupportedLanguages()
        {
            string localizationPath = Path.Combine(Application.streamingAssetsPath, "Localization");

            if (!Directory.Exists(localizationPath))
            {
                Debug.LogError($"本地化目录不存在: {localizationPath}");
                return;
            }

            var csvFiles = Directory.GetFiles(localizationPath, "*.csv");
            var languages = csvFiles.Select(file =>
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                if (System.Enum.TryParse<SystemLanguage>(fileName, out var language))
                {
                    return new { FileName = fileName, Language = language };
                }
                return null;
            }).Where(x => x != null).ToList();

            // 输出到文件
            string outputPath = Path.Combine(Application.persistentDataPath, "SupportedLanguages.txt");
            using (StreamWriter writer = new StreamWriter(outputPath))
            {
                writer.WriteLine("游戏支持的本地化语言文件:");
                foreach (var lang in languages)
                {
                    writer.WriteLine($"文件: {lang.FileName}.csv -> 语言: {lang.Language}");
                }
                writer.WriteLine($"\n总共支持 {languages.Count} 种语言");
            }

            Debug.Log($"已导出支持的语言列表到: {outputPath}");
        }

        // 获取游戏实际支持的语言列表
        public static List<SystemLanguage> GetSupportedLanguages()
        {
            var supportedLanguages = new List<SystemLanguage>();
            string localizationPath = Path.Combine(Application.streamingAssetsPath, "Localization");

            if (!Directory.Exists(localizationPath))
                return supportedLanguages;

            var csvFiles = Directory.GetFiles(localizationPath, "*.csv");
            foreach (var file in csvFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                if (System.Enum.TryParse<SystemLanguage>(fileName, out var language))
                {
                    supportedLanguages.Add(language);
                }
            }

            return supportedLanguages;
        }

        // 将 SystemLanguage 转换为语言代码（如 zh-CN, en-US）
        public static string ToLanguageCode(SystemLanguage language)
        {
            switch (language)
            {
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                    return "zh-CN";
                case SystemLanguage.ChineseTraditional:
                    return "zh-TW";
                case SystemLanguage.English:
                    return "en-US";
                case SystemLanguage.Japanese:
                    return "ja-JP";
                case SystemLanguage.Korean:
                    return "ko-KR";
                case SystemLanguage.Russian:
                    return "ru-RU";
                case SystemLanguage.German:
                    return "de-DE";
                case SystemLanguage.French:
                    return "fr-FR";
                case SystemLanguage.Spanish:
                    return "es-ES";
                default:
                    return language.ToString();
            }
        }
    }
}