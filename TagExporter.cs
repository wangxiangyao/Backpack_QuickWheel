using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Duckov.Utilities;
using UnityEngine;

namespace Great_backpack
{
    public class TagExporter
    {
        public static void ExportAllTagsToFile(string filePath = null)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    // 默认保存到桌面
                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    filePath = Path.Combine(desktopPath, "Duckov_Tags_Export.csv");
                }

                // 获取所有Tag
                Tag[] allTags = Resources.FindObjectsOfTypeAll<Tag>();

                Debug.Log($"找到 {allTags.Length} 个Tag");

                // 创建CSV内容
                StringBuilder csvContent = new StringBuilder();
                csvContent.AppendLine("Tag名称,显示名称,描述,是否显示,是否显示描述,优先级,颜色哈希");

                foreach (Tag tag in allTags.OrderBy(t => t.name))
                {
                    string colorHex = ColorUtility.ToHtmlStringRGB(tag.Color);

                    csvContent.AppendLine($"\"{tag.name}\",\"{tag.DisplayName}\",\"{tag.Description}\",{tag.Show},{tag.ShowDescription},{tag.Priority},\"{colorHex}\"");
                }

                // 写入文件
                File.WriteAllText(filePath, csvContent.ToString(), Encoding.UTF8);

                Debug.Log($"成功导出 {allTags.Length} 个Tag到文件: {filePath}");

                // 同时在控制台输出一些统计信息
                OutputTagStatistics(allTags);
            }
            catch (Exception e)
            {
                Debug.LogError($"导出Tag时出错: {e.Message}");
            }
        }

        private static void OutputTagStatistics(Tag[] allTags)
        {
            // 按类别分组统计
            var tagGroups = allTags
                .GroupBy(t => GetTagCategory(t.name))
                .OrderBy(g => g.Key);

            Debug.Log("=== Tag统计信息 ===");
            foreach (var group in tagGroups)
            {
                Debug.Log($"{group.Key}: {group.Count()}个");
            }

            // 输出一些常用的物品相关Tag作为参考
            var itemTags = allTags.Where(t =>
                t.name.Contains("Food") ||
                t.name.Contains("Weapon") ||
                t.name.Contains("Tool") ||
                t.name.Contains("Medical") ||
                t.name.Contains("Ammo") ||
                t.name.Contains("Resource") ||
                t.name.Contains("Luxury") ||
                t.name.Contains("Electric") ||
                t.name.Contains("Daily"))
                .OrderBy(t => t.name)
                .Take(20); // 只显示前20个

            Debug.Log("=== 常用物品Tag示例 ===");
            foreach (var tag in itemTags)
            {
                Debug.Log($"  {tag.name} -> {tag.DisplayName}");
            }
        }

        private static string GetTagCategory(string tagName)
        {
            if (tagName.Contains("Food") || tagName.Contains("Drink") || tagName.Contains("Water"))
                return "食物饮料";
            if (tagName.Contains("Weapon") || tagName.Contains("Gun") || tagName.Contains("Ammo"))
                return "武器弹药";
            if (tagName.Contains("Tool") || tagName.Contains("Resource") || tagName.Contains("Material"))
                return "工具材料";
            if (tagName.Contains("Medical") || tagName.Contains("Health") || tagName.Contains("Drug"))
                return "医疗药品";
            if (tagName.Contains("Clothing") || tagName.Contains("Armor") || tagName.Contains("Wear"))
                return "服装护甲";
            if (tagName.Contains("Electric") || tagName.Contains("Electronic") || tagName.Contains("Battery"))
                return "电子设备";
            if (tagName.Contains("Luxury") || tagName.Contains("Valuable") || tagName.Contains("Jewelry"))
                return "奢侈品";
            if (tagName.Contains("Container") || tagName.Contains("Bag") || tagName.Contains("Storage"))
                return "容器存储";
            if (tagName.Contains("Key") || tagName.Contains("Document") || tagName.Contains("Paper"))
                return "钥匙文件";
            if (tagName.Contains("Animal") || tagName.Contains("Plant") || tagName.Contains("Organic"))
                return "动植物";

            return "其他";
        }

        // 在 TagExporter 类中添加一个方法来检查特定Tag
        public static void CheckSpecificTags()
        {
            try
            {
                Tag[] allTags = Resources.FindObjectsOfTypeAll<Tag>();

                // 我们要查找的Tag列表
                string[] tagsToFind = {
                    "key", "SpecialKey", "Injector", "Healing", "Drink", "Food",
                    "Explosive", "MeleeWeapon", "Magazine"
                };

                Debug.Log("=== 查找特定Tag ===");

                foreach (string tagName in tagsToFind)
                {
                    var foundTags = allTags.Where(t =>
                        t.name.Contains(tagName, StringComparison.OrdinalIgnoreCase) ||
                        t.DisplayName.Contains(tagName, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (foundTags.Any())
                    {
                        Debug.Log($"找到与 '{tagName}' 相关的Tag:");
                        foreach (var tag in foundTags)
                        {
                            Debug.Log($"  - 名称: '{tag.name}', 显示名: '{tag.DisplayName}'");
                        }
                    }
                    else
                    {
                        Debug.Log($"未找到与 '{tagName}' 相关的Tag");
                    }
                }

                // 同时输出所有Tag的前10个，看看命名模式
                Debug.Log("=== Tag命名模式示例 ===");
                foreach (var tag in allTags.Take(10))
                {
                    Debug.Log($"Tag: '{tag.name}' -> '{tag.DisplayName}'");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"检查Tag时出错: {e.Message}");
            }
        }
    }
}