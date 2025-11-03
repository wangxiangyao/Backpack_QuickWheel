using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using ItemStatsSystem;
using Backpack_QuickWheel.ShortcutSystem.Data;

namespace Backpack_QuickWheel.ShortcutSystem
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
        /// 🆕 新架构：将WheelSlot布局转换为可序列化的数据格式
        /// </summary>
        public static WheelLayoutData ConvertFromWheelSlots(WheelLayoutManager layoutManager)
        {
            var data = new WheelLayoutData();
            var categoriesList = new List<CategoryLayout>();

            // 获取所有类别的轮盘格子
            foreach (ItemCategory category in Enum.GetValues(typeof(ItemCategory)))
            {
                if (category == ItemCategory.None) continue;

                var slots = layoutManager.GetSlots(category);
                if (slots.Count == 0) continue;

                var categoryLayout = new CategoryLayout(category.ToString());
                var itemLocationsList = new List<ItemLocation>();

                foreach (var slot in slots)
                {
                    if (slot.HasValidItem) // Fixed: property access instead of method call
                    {
                        // 创建物品位置记录
                        var itemLocation = new ItemLocation
                        {
                            ItemID = slot.Item.GetInstanceID(), // 使用GetInstanceID替代UniqueID
                            TypeID = slot.Item.TypeID,
                            CustomName = slot.Item.DisplayName,
                            Position = slot.Index, // Fixed: use Index instead of OriginalIndex
                            IsEmpty = false,
                            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        };
                        itemLocationsList.Add(itemLocation);
                    }
                    else
                    {
                        // 记录空格子状态
                        var emptyLocation = new ItemLocation
                        {
                            ItemID = -1,
                            TypeID = -1,
                            CustomName = "",
                            Position = slot.Index, // Fixed: use Index instead of OriginalIndex
                            IsEmpty = true,
                            // State = slot.State.ToString(), // Removed: State doesn't exist in SimpleWheelSlot
                            // Timestamp = slot.CreatedTimestamp // Removed: CreatedTimestamp doesn't exist in SimpleWheelSlot
                        };
                        itemLocationsList.Add(emptyLocation);
                    }
                }

                categoryLayout.ConvertListToArray(itemLocationsList);
                categoriesList.Add(categoryLayout);
            }

            data.Categories = categoriesList;
            data.SaveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            data.Version = "2.0"; // 新架构版本

            return data;
        }

        /// <summary>
        /// 将轮盘布局转换为可序列化的数据格式（旧版本兼容）
        /// </summary>
        public static WheelLayoutData ConvertToData(Dictionary<ItemCategory, List<Item>> wheelLayouts)
        {
            var data = new WheelLayoutData();
            var categoriesList = new List<CategoryLayout>();

            if (wheelLayouts.Count == 0)
            {
                return data;
            }

            foreach (var category in wheelLayouts.Keys)
            {
                var layoutList = wheelLayouts[category];
                var categoryLayout = new CategoryLayout(category.ToString());
                var itemLocationsList = new List<ItemLocation>();

                foreach (var item in layoutList)
                {
                    if (item == null)
                    {
                        // 用 -1, -1 标记空位
                        itemLocationsList.Add(new ItemLocation(-1, -1));
                    }
                    else
                    {
                        var location = GetItemLocation(item);
                        if (location != null)
                        {
                            itemLocationsList.Add(location);
                        }
                        else
                        {
                            // 位置记录失败，也用 -1 标记
                            itemLocationsList.Add(new ItemLocation(-1, -1));
                        }
                    }
                }

                // 将列表转换为数组
                categoryLayout.ConvertListToArray(itemLocationsList);
                categoriesList.Add(categoryLayout);
            }

            // 将列表转换为数组
            data.categories = categoriesList.ToArray();
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
                return null;
            }

            // 配件 = 槽位的主物品
            var attachment = itemSlot.Master;
            if (attachment == null)
            {
                return null;
            }

            // 背包 = 配件的父物品
            var backpack = attachment.ParentItem;
            if (backpack == null)
            {
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
        /// 从保存的数据恢复轮盘布局（仅负责恢复，不做验证）
        /// 返回恢复的布局，可能包含null或不匹配的物品
        /// 调用者应该使用 ValidateLayoutIntegrity() 验证完整性
        /// </summary>
        public static Dictionary<ItemCategory, List<Item>> RestoreFromData(
            WheelLayoutData data,
            Item currentBackpack)
        {
            if (data == null)
            {
                return null;
            }

            var restoredLayouts = new Dictionary<ItemCategory, List<Item>>();

            foreach (var categoryLayout in data.categories)
            {
                if (!Enum.TryParse<ItemCategory>(categoryLayout.categoryName, out var category))
                {
                    continue;
                }

                var locationList = categoryLayout.itemLocations;
                var restoredList = new List<Item>();

                // 遍历保存的位置列表
                for (int i = 0; i < locationList.Length; i++)
                {
                    var location = locationList[i];

                    // 检查是否为 null 位置标记 (-1, -1)
                    if (location.IsNull())
                    {
                        restoredList.Add(null); // 空位
                    }
                    else
                    {
                        // 根据位置找到物品
                        Item restoredItem = FindItemByLocation(currentBackpack, location);
                        restoredList.Add(restoredItem); // 可能为null，由验证步骤处理
                    }
                }

                restoredLayouts[category] = restoredList;
            }

            return restoredLayouts;
        }

        /// <summary>
        /// 验证轮盘布局的完整性
        /// 规则：轮盘布局中的物品必须与收集的物品完全一一对应（不考虑顺序）
        /// 如果布局中有不在收集列表中的物品，或物品数量不匹配，返回false
        /// </summary>
        public static bool ValidateLayoutIntegrity(
            Dictionary<ItemCategory, List<Item>> restoredLayouts,
            Dictionary<ItemCategory, List<Item>> categorizedItems)
        {
            if (restoredLayouts == null || categorizedItems == null)
            {
                return false;
            }

            // 遍历每个分类，检查恢复的物品是否与收集的物品一一对应
            foreach (var category in categorizedItems.Keys)
            {
                var collected = categorizedItems[category];
                var restored = restoredLayouts.ContainsKey(category) ? restoredLayouts[category] : new List<Item>();

                // 提取恢复布局中的非null物品
                var restoredItems = new List<Item>();
                foreach (var item in restored)
                {
                    if (item != null)
                    {
                        restoredItems.Add(item);
                    }
                }

                // 🔧 修复：检查非null物品数量是否相等
                if (restoredItems.Count != collected.Count)
                {
                    Debug.LogWarning($"[WheelLayoutPersistence] 分类 {category} 物品数不匹配: 收集{collected.Count}项, 恢复{restoredItems.Count}项");
                    return false;
                }

                // 🔧 新增：检查布局长度是否合理
                // 布局长度不应该超过实际物品数量的2倍（防止过多的null占位符）
                if (restored.Count > collected.Count * 2)
                {
                    Debug.LogWarning($"[WheelLayoutPersistence] 分类 {category} 布局长度异常: 收集{collected.Count}项, 布局长度{restored.Count}");
                    return false;
                }

                // 🔧 新增：检查布局中null占比是否过高
                int nullCount = restored.Count - restoredItems.Count;
                float nullRatio = (float)nullCount / restored.Count;
                if (nullRatio > 0.5f) // null占比超过50%
                {
                    Debug.LogWarning($"[WheelLayoutPersistence] 分类 {category} null占比过高: {nullRatio:P} ({nullCount}/{restored.Count})");
                    return false;
                }

                // 检查每个恢复的物品是否都在收集列表中
                foreach (var item in restoredItems)
                {
                    if (!collected.Contains(item))
                    {
                        Debug.LogWarning($"[WheelLayoutPersistence] 分类 {category} 中的物品 {item.DisplayName} 不在收集列表中");
                        return false;
                    }
                }
            }

            return true;
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
                return null;
            }

            var attachmentSlot = backpack.Slots[location.attachmentSlotIndex];
            var attachment = attachmentSlot?.Content;

            if (attachment == null)
            {
                return null;
            }

            // 步骤2：从配件找到物品
            if (attachment.Slots == null)
            {
                return null;
            }

            if (location.itemSlotIndex < 0 || location.itemSlotIndex >= attachment.Slots.Count)
            {
                return null;
            }

            var itemSlot = attachment.Slots[location.itemSlotIndex];
            var item = itemSlot?.Content;

            if (item == null)
            {
                return null;
            }

            return item;
        }

        /// <summary>
        /// 🔧 从文件加载轮盘布局（新架构）
        /// </summary>
        public static WheelLayoutData LoadWheelSlots()
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
                Debug.Log($"[WheelLayoutPersistence] 文件内容长度: {json.Length} 字符");

                if (string.IsNullOrWhiteSpace(json))
                {
                    Debug.LogWarning("[WheelLayoutPersistence] 轮盘布局文件为空");
                    Debug.Log("[WheelLayoutPersistence] ════════════════════════════════════════");
                    return null;
                }

                // 使用JsonUtility解析JSON
                var data = JsonUtility.FromJson<WheelLayoutData>(json);
                if (data == null)
                {
                    Debug.LogWarning("[WheelLayoutPersistence] JsonUtility解析失败，尝试手工解析");
                    data = ParseJson(json);
                }

                if (data != null)
                {
                    Debug.Log($"[WheelLayoutPersistence] ✅ 成功加载轮盘布局，版本: {data.Version}");
                    Debug.Log($"[WheelLayoutPersistence] 类别数量: {data.categories?.Length ?? 0}");
                    Debug.Log("[WheelLayoutPersistence] ════════════════════════════════════════");
                    return data;
                }
                else
                {
                    Debug.LogWarning("[WheelLayoutPersistence] 解析轮盘布局数据失败");
                    Debug.Log("[WheelLayoutPersistence] ════════════════════════════════════════");
                    return null;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[WheelLayoutPersistence] ✗ 加载轮盘布局失败: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// 🆕 新架构：保存WheelSlot布局到文件
        /// </summary>
        public static void SaveWheelSlots(WheelLayoutManager layoutManager)
        {
            try
            {
                var data = ConvertFromWheelSlots(layoutManager);
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(GetSavePath(), json);
                Debug.Log($"[WheelLayoutPersistence] 已保存新架构轮盘布局到: {GetSavePath()}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[WheelLayoutPersistence] 保存新架构轮盘布局失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存轮盘布局到文件（旧版本兼容）
        /// </summary>
        public static void SaveToFile(WheelLayoutData data)
        {
            try
            {
                // 手工生成 JSON（因为 JsonUtility 不支持序列化数组）
                string json = GenerateJson(data);
                string path = GetSavePath();

                File.WriteAllText(path, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WheelLayoutPersistence] 保存轮盘布局失败: {e.Message}");
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

        /// <summary>
        /// 清理轮盘布局文件
        /// 当布局验证失败时调用，避免下次再次加载无效布局
        /// </summary>
        public static void ClearLayoutFile()
        {
            try
            {
                string filePath = GetSavePath();
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Log($"[WheelLayoutPersistence] 已删除轮盘布局文件: {filePath}");
                }
                else
                {
                    Debug.Log($"[WheelLayoutPersistence] 轮盘布局文件不存在，无需删除: {filePath}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[WheelLayoutPersistence] 删除轮盘布局文件失败: {ex.Message}");
                throw;
            }
        }
    }
}
