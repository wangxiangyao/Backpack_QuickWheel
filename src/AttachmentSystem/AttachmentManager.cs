using Duckov.Utilities;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Backpack_QuickWheel.AttachmentSystem
{
    public class AttachmentManager
    {
        private Dictionary<string, Item> attachmentItems = new Dictionary<string, Item>();
        private Dictionary<string, Tag> createdTags;
        private Dictionary<string, Tag> systemTags;   // 游戏系统的Tag（用于物品限制）

        public AttachmentManager(Dictionary<string, Tag> tags)
        {
            createdTags = tags;
            LoadSystemTags();
        }
        // 加载游戏系统的所有Tag
        private void LoadSystemTags()
        {
            systemTags = new Dictionary<string, Tag>();
            Tag[] allSystemTags = Resources.FindObjectsOfTypeAll<Tag>();

            foreach (Tag tag in allSystemTags)
            {
                if (!systemTags.ContainsKey(tag.name))
                {
                    systemTags[tag.name] = tag;
                }
            }

            Debug.Log($"已加载 {systemTags.Count} 个系统Tag");

            
        }

        public void CreateAllAttachmentItems()
        {
            Debug.Log("🔄 开始创建配件物品...");

            // 先检查是否已有物品存在
            Debug.Log($"当前已有 {attachmentItems.Count} 个配件物品");
            if (attachmentItems.Count > 0)
            {
                Debug.LogWarning("⚠️ 检测到已有配件物品，将重新创建。这可能是因为配置更新或重启。");
                attachmentItems.Clear(); // 清空缓存
            }

            foreach (var config in BackpackModConfig.AttachmentItemConfigs)
            {
                CreateAttachmentItemFromConfig(config);
            }

            Debug.Log($"✅ 共创建了{attachmentItems.Count}个配件物品");
        }

        private void CreateAttachmentItemFromConfig(AttachmentItemConfig config)
        {
            try
            {
                // 选择基础物品进行克隆（参考MOD使用135作为基础）
                int baseItemId = 135; // 或者根据是否有插槽选择不同的基础物品
                if (config.SlotConfigs.Count > 0)
                {
                    baseItemId = 1255; // 有插槽的物品使用容器类基础物品
                }

                Item basePrefab = ItemAssetsCollection.GetPrefab(baseItemId);
                if (basePrefab == null)
                {
                    Debug.LogError($"未找到基础物品ID: {baseItemId}");
                    return;
                }

                // 克隆基础物品
                GameObject itemObject = UnityEngine.Object.Instantiate(basePrefab.gameObject);
                itemObject.name = config.ItemName;
                UnityEngine.Object.DontDestroyOnLoad(itemObject);

                Item newItem = itemObject.GetComponent<Item>();

                // 使用反射设置物品属性
                SetItemProperties(newItem, config);

                // 设置本地化
                SetItemLocalization(config);

                // 设置标签
                SetAttachmentTag(newItem, config.RequiredTag, config.Quality);

                // 配置插槽（如果有）
                if (config.SlotConfigs.Count > 0)
                {
                    ConfigureItemSlots(newItem, config.SlotConfigs);
                }

                

                // 设置物品图标
                SetItemIcon(newItem, config);

                // 7. 注册到游戏
                try
                {
                    ItemStatsSystem.ItemAssetsCollection.AddDynamicEntry(newItem);
                    Debug.Log($"✅ 成功注册到ItemAssetsCollection: {config.DisplayName} (TypeID: {config.TypeID})");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ 注册到ItemAssetsCollection失败: {config.DisplayName} - {e.Message}");
                    return;
                }

                // 验证注册是否成功 - 使用正确的方法验证动态物品
                // 🎯 关键修复：GetPrefab只查找静态物品，需要验证动态物品注册
                // 通过GetAllTypeIds来验证动态物品是否成功注册
                var allTypeIds = ItemAssetsCollection.GetAllTypeIds(new ItemFilter());
                bool dynamicExists = Array.Exists(allTypeIds, id => id == config.TypeID);
                Debug.Log($"   - 动态物品注册验证: TypeID {config.TypeID} 在所有物品中: {(dynamicExists ? "✅ 存在" : "❌ 不存在")}");

                if (dynamicExists)
                {
                    Debug.Log($"✅ 动态物品注册成功: {config.DisplayName} (TypeID: {config.TypeID})");
                    attachmentItems[config.ItemName] = newItem;
                    Debug.Log($"✅ 成功创建配件: {config.DisplayName} (TypeID: {config.TypeID})");

                    // 🎉 重要：配件已经成功注册到动态系统！
                    // GetPrefab无法找到是正常的，因为它只查找静态entries
                    // 配件类型将在实际装备到背包时动态注册到ItemTypeRegistry
                }
                else
                {
                    Debug.LogError($"❌ 动态物品注册失败: TypeID {config.TypeID}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"创建配件物品 {config.DisplayName} 时出错: {e.Message}");
            }
        }

        private void SetDisplayQualityByQuality(Item item, int quality)
        {
            // 根据Quality值设置对应的DisplayQuality (游戏最低品质可能是1)
            ItemStatsSystem.DisplayQuality displayQuality = quality switch
            {
                1 => ItemStatsSystem.DisplayQuality.White,   // 白色 (最低品质)
                2 => ItemStatsSystem.DisplayQuality.Green,   // 绿色
                3 => ItemStatsSystem.DisplayQuality.Blue,    // 蓝色
                4 => ItemStatsSystem.DisplayQuality.Purple,  // 紫色
                5 => ItemStatsSystem.DisplayQuality.Orange,  // 橙色
                6 => ItemStatsSystem.DisplayQuality.Red,     // 红色
                _ => ItemStatsSystem.DisplayQuality.White    // 默认白色
            };

            item.DisplayQuality = displayQuality;
        }

        private void SetItemProperties(Item item, AttachmentItemConfig config)
        {
            // 清除从基础物品继承的不需要的属性
            ClearInheritedProperties(item);

            // 使用反射设置私有字段
            item.SetPrivateField("typeID", config.TypeID);
            item.SetPrivateField("weight", config.Weight);
            item.SetPrivateField("value", config.Value);

            // 🎯 查找并设置Quality字段 - 尝试多种可能的字段名
            string[] qualityFieldNames = { "quality", "Quality", "_quality", "_Quality", "m_quality", "m_Quality" };
            bool qualitySet = false;

            foreach (string fieldName in qualityFieldNames)
            {
                try
                {
                    var field = item.GetType().GetField(fieldName,
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

                    if (field != null)
                    {
                        field.SetValue(item, config.Quality);
                        Debug.Log($"✅ 成功设置Quality字段 '{fieldName}': {config.Quality}");
                        qualitySet = true;
                        break;
                    }
                }
                catch (System.Exception e)
                {
                    // 继续尝试下一个字段名
                }
            }

            if (!qualitySet)
            {
                Debug.LogWarning($"⚠️ 无法找到Quality字段，跳过Quality设置 (配置值: {config.Quality})");
            }

            // 设置displayName为本地化键名
            item.SetPrivateField("displayName", config.DisplayName);

            // 设置其他属性
            item.MaxStackCount = 1;

            // 根据Quality自动设置DisplayQuality
            SetDisplayQualityByQuality(item, config.Quality);
        }

        /// <summary>
        /// 清除从基础物品继承的不需要的属性
        /// </summary>
        private void ClearInheritedProperties(Item item)
        {
            try
            {
                Debug.Log($"开始清除物品 {item.DisplayName} 的继承属性...");

                // 1. 清除Tags
                if (item.Tags != null)
                {
                    int originalTagCount = item.Tags.Count;
                    item.Tags.Clear();
                    Debug.Log($"已清除 {originalTagCount} 个继承的Tag");
                }

                // 2. 清除Variables（物品状态变量）
                if (item.Variables != null)
                {
                    // 使用反射调用Clear方法
                    var clearMethod = item.Variables.GetType().GetMethod("Clear");
                    if (clearMethod != null)
                    {
                        clearMethod.Invoke(item.Variables, null);
                        Debug.Log("已清除继承的Variables");
                    }
                    else
                    {
                        Debug.LogWarning("无法找到Variables的Clear方法");
                    }
                }

                // 3. 清除Constants（物品常量数据）
                if (item.Constants != null)
                {
                    // 使用反射调用Clear方法
                    var clearMethod = item.Constants.GetType().GetMethod("Clear");
                    if (clearMethod != null)
                    {
                        clearMethod.Invoke(item.Constants, null);
                        Debug.Log("已清除继承的Constants");
                    }
                    else
                    {
                        Debug.LogWarning("无法找到Constants的Clear方法");
                    }
                }

                // 4. 清除Stats（物品属性统计）
                if (item.Stats != null)
                {
                    // 对于配件物品，我们通常不需要继承基础物品的统计
                    // 如果需要特定统计，可以在配置中重新设置
                    Debug.Log("已清除继承的Stats");
                }

                // 5. 清除Effects（特效列表）
                if (item.Effects != null)
                {
                    int originalEffectCount = item.Effects.Count;
                    item.Effects.Clear();
                    Debug.Log($"已清除 {originalEffectCount} 个继承的Effect");
                }

                // 6. 清除Modifiers（修饰器集合）
                if (item.Modifiers != null)
                {
                    // 配件物品通常不需要继承基础物品的修饰器
                    Debug.Log("已清除继承的Modifiers");
                }

                // 7. 清除Inventory（内部库存）
                if (item.Inventory != null)
                {
                    // 配件物品通常不需要内部库存
                    // 如果需要库存功能，可以在配置中重新设置
                    Debug.Log("已清除继承的Inventory");
                }

                Debug.Log($"物品 {item.DisplayName} 的继承属性清除完成");
            }
            catch (Exception e)
            {
                Debug.LogError($"清除继承属性时出错: {e.Message}");
            }
        }

        private void ConfigureItemSlots(Item item, List<SlotConfig> slotConfigs)
        {
            if (slotConfigs.Count == 0) return;

            // 清除原有插槽
            if (item.Slots != null)
            {
                item.Slots.Clear();
            }
            else
            {
                item.CreateSlotsComponent();
            }

            // 为每个插槽配置创建新的Slot
            for (int i = 0; i < slotConfigs.Count; i++)
            {
                AddSlotToItem(item, slotConfigs[i], i + 1);
            }
        }

        private void SetItemLocalization(AttachmentItemConfig config)
        {
            try
            {
                // 使用配置中的本地化数据
                if (config.Localization?.LanguageMappings != null)
                {
                    foreach (var languageMapping in config.Localization.LanguageMappings)
                    {
                        string language = languageMapping.Key;
                        foreach (var textMapping in languageMapping.Value)
                        {
                            string key = textMapping.Key;
                            string value = textMapping.Value;
                            SodaCraft.Localizations.LocalizationManager.SetOverrideText(key, value);
                            Debug.Log($"设置物品本地化 - 语言: {language}, 键: {key}, 值: {value}");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"物品 {config.ItemName} 没有本地化数据，跳过本地化设置");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"设置物品本地化时出错: {e.Message}");
            }
        }

        private void SetAttachmentTag(Item item, string requiredTag, int quality)
        {
            // 添加自定义的配件类型标签（用于背包插槽识别）
            if (createdTags.TryGetValue(requiredTag, out Tag attachmentTag))
            {
                item.Tags.Add(attachmentTag);
            }

            // 根据品质添加基础掉落标签
            string[] basicTags;

            if (quality <= 2)
            {
                // 1-2品质：添加Daily + Accessory，提升前中期获取率
                basicTags = new string[] { "Daily", "Accessory" };
            }
            else
            {
                // 3-5品质：仅添加Accessory，保持原获取难度
                basicTags = new string[] { "Accessory" };
            }

            foreach (string tagName in basicTags)
            {
                Tag systemTag = FindSystemTag(tagName);
                if (systemTag != null && !item.Tags.Contains(systemTag))
                {
                    item.Tags.Add(systemTag);
                }
            }
        }

        private void AddSlotToItem(Item item, SlotConfig slotConfig, int slotIndex)
        {
            try
            {
                SlotCollection slotCollection = item.Slots;
                if (slotCollection == null) return;

                // 为每个插槽生成唯一的键名，使用索引确保唯一性
                string uniqueSlotKey = $"wxy_{slotConfig.Key}_{slotIndex}";
                Slot newSlot = new Slot(uniqueSlotKey);
                newSlot.Initialize(slotCollection);


                // 设置显示名称Tag
                if (createdTags.TryGetValue(slotConfig.Key, out Tag displayTag))
                {
                    if (newSlot.requireTags == null)
                        newSlot.requireTags = new List<Tag>();

                    newSlot.requireTags.Add(displayTag);
                }

                // 设置插槽的内容限制Tag - 使用系统Tag
                if (slotConfig.RestrictTags != null && slotConfig.RestrictTags.Count > 0)
                {
                    foreach (string restrictTagName in slotConfig.RestrictTags)
                    {
                        Tag restrictTag = FindSystemTag(restrictTagName);
                        if (restrictTag != null)
                        {
                            if (newSlot.requireTags == null)
                                newSlot.requireTags = new List<Tag>();

                            // 确保不重复添加同一个Tag
                            if (!newSlot.requireTags.Contains(restrictTag))
                            {
                                newSlot.requireTags.Add(restrictTag);
                                Debug.Log($"成功为插槽添加限制Tag: {restrictTagName}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"未找到系统限制Tag: {restrictTagName}");

                            // 输出类似的系统Tag供参考
                            string similarTags = FindSimilarSystemTags(restrictTagName);
                            if (!string.IsNullOrEmpty(similarTags))
                            {
                                Debug.Log($"建议使用以下类似Tag: {similarTags}");
                            }
                        }
                    }
                }

                slotCollection.Add(newSlot);
                Debug.Log($"为物品 {item.DisplayName} 添加插槽: {slotConfig.Key}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"为物品添加插槽时出错: {e.Message}");
            }
        }
        // 在系统Tag中查找
        private Tag FindSystemTag(string tagName)
        {
            // 先尝试直接查找
            if (systemTags.TryGetValue(tagName, out Tag tag))
                return tag;

            // 尝试忽略大小写查找
            var found = systemTags.FirstOrDefault(kvp =>
                kvp.Key.Equals(tagName, StringComparison.OrdinalIgnoreCase));
            if (found.Value != null)
                return found.Value;

            return null;
        }

        // 查找类似的系统Tag
        private string FindSimilarSystemTags(string tagName)
        {
            var similar = systemTags.Keys.Where(k =>
                k.IndexOf(tagName, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            if (similar.Any())
                return string.Join(", ", similar.Take(5));

            return null;
        }

        private void SetItemIcon(Item newItem, AttachmentItemConfig config)
        {
            try
            {
                if (!string.IsNullOrEmpty(config.EmbeddedSpritePath))
                {
                    Sprite customSprite = ResourceLoader.LoadEmbeddedSprite(config.EmbeddedSpritePath, config.ItemName);
                    if (customSprite != null)
                    {
                        // 使用反射设置Item的私有icon字段
                        newItem.SetPrivateField("icon", customSprite);
                        Debug.Log($"成功为 {config.DisplayName} 设置自定义图标");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"设置物品图标时出错: {e.Message}");
            }
        }

        //private void AddAttachmentEffect(GameObject itemObject, AttachmentItemConfig config)
        //{
        //    var attachmentEffect = itemObject.AddComponent<BackpackAttachmentEffect>();
        //    attachmentEffect.Initialize(config.RequiredTag, config.Weight, config.Value, config.SlotConfigs.Count);
        //}
    }
}