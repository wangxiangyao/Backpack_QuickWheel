using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using ItemStatsSystem;
using Great_backpack.ShortcutSystem.Data;

namespace Great_backpack.ShortcutSystem
{
    /// <summary>
    /// 轮盘布局持久化管理器
    /// 负责序列化/反序列化轮盘布局数据
    /// </summary>
    public static class WheelLayoutPersistence
    {
        private static string GetSavePath()
        {
            string modDataPath = Path.Combine(Application.persistentDataPath, "GreatBackpack");
            if (!Directory.Exists(modDataPath))
            {
                Directory.CreateDirectory(modDataPath);
            }
            return Path.Combine(modDataPath, "wheel_layout.json");
        }

        /// <summary>
        /// 将轮盘布局转换为可序列化的数据格式
        /// </summary>
        public static WheelLayoutData ConvertToData(Dictionary<ItemCategory, List<Item>> wheelLayouts)
        {
            var data = new WheelLayoutData();
            var categoriesList = new List<CategoryLayout>();

            Debug.Log("[WheelLayoutPersistence] ════════════════════════════════════════");
            Debug.Log("[WheelLayoutPersistence] 开始转换轮盘布局为数据格式");
            Debug.Log($"[WheelLayoutPersistence] wheelLayouts == null? {wheelLayouts == null}");
            Debug.Log($"[WheelLayoutPersistence] 要转换的分类数: {wheelLayouts.Count}");

            if (wheelLayouts.Count == 0)
            {
                Debug.LogWarning("[WheelLayoutPersistence] ✗ wheelLayouts 为空！返回空数据对象");
                Debug.Log("[WheelLayoutPersistence] ════════════════════════════════════════");
                return data;
            }

            foreach (var category in wheelLayouts.Keys)
            {
                Debug.Log($"[WheelLayoutPersistence] 开始处理分类: {category}");
                var layoutList = wheelLayouts[category];
                var categoryLayout = new CategoryLayout(category.ToString());
                var itemLocationsList = new List<ItemLocation>();

                Debug.Log($"[WheelLayoutPersistence] 分类: {category}, 物品数: {layoutList.Count}");

                foreach (var item in layoutList)
                {
                    if (item == null)
                    {
                        // 用 -1, -1 标记空位
                        itemLocationsList.Add(new ItemLocation(-1, -1));
                        Debug.Log($"[WheelLayoutPersistence]   - 空位 (-1, -1)");
                    }
                    else
                    {
                        var location = GetItemLocation(item);
                        if (location != null)
                        {
                            itemLocationsList.Add(location);
                            Debug.Log($"[WheelLayoutPersistence]   - {item.DisplayName}: 配件槽位{location.attachmentSlotIndex}, 物品槽位{location.itemSlotIndex}");
                        }
                        else
                        {
                            // 位置记录失败，也用 -1 标记
                            itemLocationsList.Add(new ItemLocation(-1, -1));
                            Debug.LogWarning($"[WheelLayoutPersistence]   - {item.DisplayName}: 位置记录失败，标记为空位");
                        }
                    }
                }

                // 将列表转换为数组
                categoryLayout.ConvertListToArray(itemLocationsList);
                categoriesList.Add(categoryLayout);
            }

            // 将列表转换为数组
            data.categories = categoriesList.ToArray();

            Debug.Log($"[WheelLayoutPersistence] ✓ 转换完成，包含 {data.categories.Length} 个分类");
            Debug.Log("[WheelLayoutPersistence] ════════════════════════════════════════");
            return data;
        }

        /// <summary>
        /// 获取物品的位置信息
        /// 返回：配件在背包的槽位索引 + 物品在配件的槽位索引
        /// </summary>
        private static ItemLocation GetItemLocation(Item item)
        {
            if (item == null) return null;

            // 获取物品所在的槽位
            var itemSlot = item.PluggedIntoSlot;
            if (itemSlot == null)
            {
                Debug.LogWarning($"[WheelLayoutPersistence] 物品 {item.DisplayName} 没有插入槽位，无法记录位置");
                return null;
            }

            // 配件 = 槽位的主物品
            var attachment = itemSlot.Master;
            if (attachment == null)
            {
                Debug.LogWarning($"[WheelLayoutPersistence] 物品 {item.DisplayName} 所在的槽位没有主物品");
                return null;
            }

            // 背包 = 配件的父物品
            var backpack = attachment.ParentItem;
            if (backpack == null)
            {
                Debug.LogWarning($"[WheelLayoutPersistence] 配件 {attachment.DisplayName} 没有父物品（背包）");
                return null;
            }

            // 找配件在背包中的槽位索引
            int attachmentSlotIndex = -1;
            if (backpack.Slots != null)
            {
                for (int i = 0; i < backpack.Slots.Count; i++)
                {
                    if (backpack.Slots[i]?.Content == attachment)
                    {
                        attachmentSlotIndex = i;
                        break;
                    }
                }
            }

            if (attachmentSlotIndex < 0)
            {
                Debug.LogWarning($"[WheelLayoutPersistence] 找不到配件 {attachment.DisplayName} 在背包中的槽位索引");
                return null;
            }

            // 找物品在配件中的槽位索引
            int itemSlotIndex = -1;
            if (attachment.Slots != null)
            {
                for (int i = 0; i < attachment.Slots.Count; i++)
                {
                    if (attachment.Slots[i]?.Content == item)
                    {
                        itemSlotIndex = i;
                        break;
                    }
                }
            }

            if (itemSlotIndex < 0)
            {
                Debug.LogWarning($"[WheelLayoutPersistence] 找不到物品 {item.DisplayName} 在配件中的槽位索引");
                return null;
            }

            return new ItemLocation(attachmentSlotIndex, itemSlotIndex);
        }

        /// <summary>
        /// 从保存的数据恢复轮盘布局
        /// 如果验证失败，返回null并弃用布局
        /// </summary>
        public static Dictionary<ItemCategory, List<Item>> RestoreFromData(
            WheelLayoutData data,
            Item currentBackpack,
            Dictionary<ItemCategory, List<Item>> categorizedItems)
        {
            if (data == null)
            {
                Debug.LogWarning("[WheelLayoutPersistence] data 为 null，无法恢复");
                return null;
            }

            Debug.Log("[WheelLayoutPersistence] ════════════════════════════════════════");
            Debug.Log("[WheelLayoutPersistence] 开始恢复轮盘布局...");
            Debug.Log($"[WheelLayoutPersistence] 要恢复的分类数: {data.categories.Length}");

            var restoredLayouts = new Dictionary<ItemCategory, List<Item>>();

            foreach (var categoryLayout in data.categories)
            {
                if (!Enum.TryParse<ItemCategory>(categoryLayout.categoryName, out var category))
                {
                    Debug.LogWarning($"[WheelLayoutPersistence] 无法识别分类: {categoryLayout.categoryName}");
                    continue;
                }

                var locationList = categoryLayout.itemLocations;
                var restoredList = new List<Item>();

                Debug.Log($"[WheelLayoutPersistence] 分类 {category}: {locationList.Length} 个位置记录");

                // 遍历保存的位置列表
                for (int i = 0; i < locationList.Length; i++)
                {
                    var location = locationList[i];

                    // 检查是否为 null 位置标记 (-1, -1)
                    if (location.IsNull())
                    {
                        restoredList.Add(null); // 空位
                        Debug.Log($"[WheelLayoutPersistence]   [{i}] 空位");
                    }
                    else
                    {
                        // 根据位置找到物品
                        Item restoredItem = FindItemByLocation(currentBackpack, location);

                        if (restoredItem == null)
                        {
                            Debug.LogError($"[WheelLayoutPersistence]   [{i}] 位置查询失败 (配件槽{location.attachmentSlotIndex}, 物品槽{location.itemSlotIndex})，弃用整个布局");
                            Debug.Log("[WheelLayoutPersistence] ════════════════════════════════════════");
                            return null; // 任何物品找不到，就弃用整个布局
                        }

                        // 验证物品是否在该分类中
                        if (!categorizedItems[category].Contains(restoredItem))
                        {
                            Debug.LogError($"[WheelLayoutPersistence]   [{i}] 物品 {restoredItem.DisplayName} 不在分类 {category} 中，弃用整个布局");
                            Debug.Log("[WheelLayoutPersistence] ════════════════════════════════════════");
                            return null; // 物品分类不匹配，弃用整个布局
                        }

                        restoredList.Add(restoredItem);
                        Debug.Log($"[WheelLayoutPersistence]   [{i}] ✓ {restoredItem.DisplayName}");
                    }
                }

                restoredLayouts[category] = restoredList;
            }

            Debug.Log("[WheelLayoutPersistence] ✓ 轮盘布局恢复成功");
            Debug.Log("[WheelLayoutPersistence] ════════════════════════════════════════");
            return restoredLayouts;
        }

        /// <summary>
        /// 根据位置信息找到对应的物品
        /// </summary>
        private static Item FindItemByLocation(Item backpack, ItemLocation location)
        {
            if (backpack == null || backpack.Slots == null)
                return null;

            // 步骤1：从背包找到配件
            if (location.attachmentSlotIndex < 0 || location.attachmentSlotIndex >= backpack.Slots.Count)
            {
                Debug.LogWarning($"[WheelLayoutPersistence] 配件槽位索引越界: {location.attachmentSlotIndex}");
                return null;
            }

            var attachmentSlot = backpack.Slots[location.attachmentSlotIndex];
            var attachment = attachmentSlot?.Content;

            if (attachment == null)
            {
                Debug.LogWarning($"[WheelLayoutPersistence] 背包槽位 {location.attachmentSlotIndex} 没有配件");
                return null;
            }

            // 步骤2：从配件找到物品
            if (attachment.Slots == null)
            {
                Debug.LogWarning($"[WheelLayoutPersistence] 配件 {attachment.DisplayName} 没有槽位");
                return null;
            }

            if (location.itemSlotIndex < 0 || location.itemSlotIndex >= attachment.Slots.Count)
            {
                Debug.LogWarning($"[WheelLayoutPersistence] 物品槽位索引越界: {location.itemSlotIndex}");
                return null;
            }

            var itemSlot = attachment.Slots[location.itemSlotIndex];
            var item = itemSlot?.Content;

            if (item == null)
            {
                Debug.LogWarning($"[WheelLayoutPersistence] 配件 {attachment.DisplayName} 的槽位 {location.itemSlotIndex} 为空");
                return null;
            }

            return item;
        }

        /// <summary>
        /// 保存轮盘布局到文件
        /// </summary>
        public static void SaveToFile(WheelLayoutData data)
        {
            try
            {
                Debug.Log("[WheelLayoutPersistence] ════════════════════════════════════════");
                Debug.Log("[WheelLayoutPersistence] 准备保存轮盘布局...");
                Debug.Log($"[WheelLayoutPersistence] 数据对象中的分类数: {data.categories.Length}");

                foreach (var categoryLayout in data.categories)
                {
                    Debug.Log($"[WheelLayoutPersistence] 分类 {categoryLayout.categoryName}: {categoryLayout.itemLocations.Length} 个位置记录");
                }

                // 手工生成 JSON（因为 JsonUtility 不支持序列化数组）
                string json = GenerateJson(data);
                string path = GetSavePath();

                Debug.Log($"[WheelLayoutPersistence] JSON 内容长度: {json.Length}");
                Debug.Log($"[WheelLayoutPersistence] JSON 内容:\n{json}");
                Debug.Log($"[WheelLayoutPersistence] 保存路径: {path}");

                File.WriteAllText(path, json);

                // 验证写入
                if (File.Exists(path))
                {
                    long fileSize = new System.IO.FileInfo(path).Length;
                    Debug.Log($"[WheelLayoutPersistence] ✓ 文件已保存成功，文件大小: {fileSize} 字节");
                }
                else
                {
                    Debug.LogError($"[WheelLayoutPersistence] ✗ 文件保存失败，文件不存在");
                }

                Debug.Log("[WheelLayoutPersistence] ════════════════════════════════════════");
            }
            catch (Exception e)
            {
                Debug.LogError($"[WheelLayoutPersistence] ✗ 保存轮盘布局失败: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 手工解析 JSON 字符串（避免 JsonUtility 的限制）
        /// 使用括号计数法处理嵌套的方括号和花括号
        /// </summary>
        private static WheelLayoutData ParseJson(string json)
        {
            try
            {
                var data = new WheelLayoutData();

                // 提取 savedTimestamp
                var timestampMatch = System.Text.RegularExpressions.Regex.Match(json, @"""savedTimestamp"":\s*(\d+)");
                if (timestampMatch.Success)
                {
                    data.savedTimestamp = long.Parse(timestampMatch.Groups[1].Value);
                    Debug.Log($"[WheelLayoutPersistence] 提取 savedTimestamp: {data.savedTimestamp}");
                }

                // 用括号计数法提取 categories 数组内容
                int categoriesStartIdx = json.IndexOf("\"categories\":");
                if (categoriesStartIdx < 0)
                {
                    Debug.LogWarning("[WheelLayoutPersistence] 找不到 'categories' 字段");
                    return data;
                }

                // 找到 [ 的位置
                int arrayStartIdx = json.IndexOf('[', categoriesStartIdx);
                if (arrayStartIdx < 0)
                {
                    Debug.LogWarning("[WheelLayoutPersistence] 找不到 categories 数组的 [");
                    return data;
                }

                // 用括号计数找到匹配的 ]
                int bracketCount = 0;
                int arrayEndIdx = -1;
                for (int i = arrayStartIdx; i < json.Length; i++)
                {
                    if (json[i] == '[') bracketCount++;
                    else if (json[i] == ']')
                    {
                        bracketCount--;
                        if (bracketCount == 0)
                        {
                            arrayEndIdx = i;
                            break;
                        }
                    }
                }

                if (arrayEndIdx < 0)
                {
                    Debug.LogWarning("[WheelLayoutPersistence] 找不到 categories 数组的匹配 ]");
                    return data;
                }

                // 提取数组内容
                string categoriesStr = json.Substring(arrayStartIdx + 1, arrayEndIdx - arrayStartIdx - 1);
                Debug.Log($"[WheelLayoutPersistence] 提取到 categories 字符串，长度: {categoriesStr.Length}");

                // 逐个解析 category 对象
                var categoriesList = new List<CategoryLayout>();
                int braceCount = 0;
                int currentObjStart = -1;

                for (int i = 0; i < categoriesStr.Length; i++)
                {
                    char c = categoriesStr[i];

                    if (c == '{')
                    {
                        if (braceCount == 0)
                        {
                            currentObjStart = i;
                        }
                        braceCount++;
                    }
                    else if (c == '}')
                    {
                        braceCount--;
                        if (braceCount == 0 && currentObjStart >= 0)
                        {
                            // 提取完整的 category 对象
                            string categoryObjStr = categoriesStr.Substring(currentObjStart, i - currentObjStart + 1);
                            ParseCategoryObject(categoryObjStr, categoriesList);
                        }
                    }
                }

                data.categories = categoriesList.ToArray();
                Debug.Log($"[WheelLayoutPersistence] 解析完成，共 {data.categories.Length} 个分类");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WheelLayoutPersistence] JSON 解析异常: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// 解析单个 category 对象
        /// </summary>
        private static void ParseCategoryObject(string categoryObjStr, List<CategoryLayout> categoriesList)
        {
            try
            {
                // 提取 categoryName
                var categoryNameMatch = System.Text.RegularExpressions.Regex.Match(categoryObjStr, @"""categoryName"":\s*""([^""]+)""");
                if (!categoryNameMatch.Success)
                {
                    Debug.LogWarning("[WheelLayoutPersistence] 找不到 categoryName");
                    return;
                }

                string categoryName = categoryNameMatch.Groups[1].Value;
                Debug.Log($"[WheelLayoutPersistence] 解析分类: {categoryName}");

                // 用括号计数法提取 itemLocations 数组
                int itemLocationsStartIdx = categoryObjStr.IndexOf("\"itemLocations\":");
                if (itemLocationsStartIdx < 0)
                {
                    Debug.LogWarning($"[WheelLayoutPersistence]   找不到 itemLocations");
                    return;
                }

                int arrayStartIdx = categoryObjStr.IndexOf('[', itemLocationsStartIdx);
                if (arrayStartIdx < 0)
                {
                    Debug.LogWarning($"[WheelLayoutPersistence]   找不到 itemLocations 数组的 [");
                    return;
                }

                // 找到匹配的 ]
                int bracketCount = 0;
                int arrayEndIdx = -1;
                for (int i = arrayStartIdx; i < categoryObjStr.Length; i++)
                {
                    if (categoryObjStr[i] == '[') bracketCount++;
                    else if (categoryObjStr[i] == ']')
                    {
                        bracketCount--;
                        if (bracketCount == 0)
                        {
                            arrayEndIdx = i;
                            break;
                        }
                    }
                }

                if (arrayEndIdx < 0)
                {
                    Debug.LogWarning($"[WheelLayoutPersistence]   找不到 itemLocations 数组的 ]");
                    return;
                }

                // 提取数组内容
                string itemLocationsStr = categoryObjStr.Substring(arrayStartIdx + 1, arrayEndIdx - arrayStartIdx - 1);

                // 解析 itemLocations
                var itemLocationsList = new List<ItemLocation>();
                var locationMatches = System.Text.RegularExpressions.Regex.Matches(
                    itemLocationsStr,
                    @"\{""attachmentSlotIndex"":\s*(-?\d+),\s*""itemSlotIndex"":\s*(-?\d+)\}"
                );

                foreach (System.Text.RegularExpressions.Match locMatch in locationMatches)
                {
                    int attachmentSlotIndex = int.Parse(locMatch.Groups[1].Value);
                    int itemSlotIndex = int.Parse(locMatch.Groups[2].Value);
                    itemLocationsList.Add(new ItemLocation(attachmentSlotIndex, itemSlotIndex));
                    Debug.Log($"[WheelLayoutPersistence]   - 位置: 配件槽{attachmentSlotIndex}, 物品槽{itemSlotIndex}");
                }

                var categoryLayout = new CategoryLayout(categoryName);
                categoryLayout.ConvertListToArray(itemLocationsList);
                categoriesList.Add(categoryLayout);

                Debug.Log($"[WheelLayoutPersistence] ✓ 分类 {categoryName} 解析完成，{itemLocationsList.Count} 个位置");
            }
            catch (Exception e)
            {
                Debug.LogError($"[WheelLayoutPersistence] 解析 category 对象异常: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 手工生成 JSON 字符串（避免 JsonUtility 的限制）
        /// </summary>
        private static string GenerateJson(WheelLayoutData data)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"savedTimestamp\": {data.savedTimestamp},");
            sb.AppendLine("  \"categories\": [");

            for (int i = 0; i < data.categories.Length; i++)
            {
                var category = data.categories[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"categoryName\": \"{category.categoryName}\",");
                sb.AppendLine("      \"itemLocations\": [");

                for (int j = 0; j < category.itemLocations.Length; j++)
                {
                    var location = category.itemLocations[j];
                    sb.Append("        {");
                    sb.Append($"\"attachmentSlotIndex\": {location.attachmentSlotIndex}, ");
                    sb.Append($"\"itemSlotIndex\": {location.itemSlotIndex}");
                    sb.Append("}");
                    if (j < category.itemLocations.Length - 1) sb.Append(",");
                    sb.AppendLine();
                }

                sb.AppendLine("      ]");
                sb.Append("    }");
                if (i < data.categories.Length - 1) sb.Append(",");
                sb.AppendLine();
            }

            sb.AppendLine("  ]");
            sb.Append("}");

            return sb.ToString();
        }

        /// <summary>
        /// 从文件加载轮盘布局
        /// </summary>
        public static WheelLayoutData LoadFromFile()
        {
            try
            {
                string path = GetSavePath();

                Debug.Log("[WheelLayoutPersistence] ════════════════════════════════════════");
                Debug.Log("[WheelLayoutPersistence] 准备加载轮盘布局...");
                Debug.Log($"[WheelLayoutPersistence] 文件路径: {path}");

                if (!File.Exists(path))
                {
                    Debug.Log("[WheelLayoutPersistence] ✗ 轮盘布局文件不存在");
                    Debug.Log("[WheelLayoutPersistence] ════════════════════════════════════════");
                    return null;
                }

                string json = File.ReadAllText(path);
                long fileSize = new System.IO.FileInfo(path).Length;

                Debug.Log($"[WheelLayoutPersistence] ✓ 文件已读取，大小: {fileSize} 字节");
                Debug.Log($"[WheelLayoutPersistence] JSON 内容长度: {json.Length}");
                Debug.Log($"[WheelLayoutPersistence] JSON 内容:\n{json}");

                // 手工解析 JSON
                WheelLayoutData data = ParseJson(json);

                if (data == null)
                {
                    Debug.LogError("[WheelLayoutPersistence] ✗ JSON 解析失败，data 为 null");
                    Debug.Log("[WheelLayoutPersistence] ════════════════════════════════════════");
                    return null;
                }

                Debug.Log($"[WheelLayoutPersistence] ✓ 解析成功");
                Debug.Log($"[WheelLayoutPersistence] 数据对象中的分类数: {data.categories.Length}");

                foreach (var categoryLayout in data.categories)
                {
                    Debug.Log($"[WheelLayoutPersistence] 分类 {categoryLayout.categoryName}: {categoryLayout.itemLocations.Length} 个位置记录");
                }

                Debug.Log("[WheelLayoutPersistence] ════════════════════════════════════════");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WheelLayoutPersistence] ✗ 加载轮盘布局失败: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }
    }
}
