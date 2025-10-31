using ItemStatsSystem;
using ItemStatsSystem.Items;
using Duckov.Utilities;
using Duckov.UI;
using SodaCraft.Localizations;
using UnityEngine;
using System.Linq;

namespace Backpack_QuickWheel.ShortcutSystem
{
    public static class ItemUsageHandler
    {
        public static void UseItem(Item item, ItemCategory category)
        {
            Debug.Log($"[ItemUsageHandler] UseItem 被调用");
            Debug.Log($"[ItemUsageHandler] 物品: {(item != null ? item.DisplayName : "null")}");
            Debug.Log($"[ItemUsageHandler] 类别: {category}");

            if (item == null)
            {
                Debug.LogError($"[ItemUsageHandler] 物品为 null，无法使用");
                return;
            }

            var mainCharacter = CharacterMainControl.Main;
            if (mainCharacter == null)
            {
                Debug.LogError($"[ItemUsageHandler] 主角色为 null，无法使用物品");
                return;
            }

            Debug.Log($"[ItemUsageHandler] 主角色已找到，开始处理物品使用");

            switch (category)
            {
                case ItemCategory.Medical:
                case ItemCategory.Stim:
                case ItemCategory.Food:
                    Debug.Log($"[ItemUsageHandler] 物品类型: 可直接使用 ({category})");
                    // 直接使用
                    TryUseItemDirectly(item, mainCharacter);
                    break;

                case ItemCategory.Explosive:
                case ItemCategory.Melee:
                    Debug.Log($"[ItemUsageHandler] 物品类型: 需要装备到手上 ({category})");
                    // 拿到手上
                    EquipItemToHand(item, mainCharacter);
                    break;

                default:
                    Debug.LogWarning($"[ItemUsageHandler] 未知的物品类别: {category}");
                    break;
            }
        }

        private static void TryUseItemDirectly(Item item, CharacterMainControl character)
        {
            Debug.Log($"[ItemUsageHandler] TryUseItemDirectly 被调用");
            Debug.Log($"[ItemUsageHandler] 检查物品 {item.DisplayName} 是否可直接使用");
            Debug.Log($"[ItemUsageHandler] UsageUtilities 是否存在: {item.UsageUtilities != null}");

            if (item.UsageUtilities != null && item.UsageUtilities.IsUsable(item, character))
            {
                Debug.Log($"[ItemUsageHandler] ✓ 物品可直接使用，调用 character.UseItem()");
                character.UseItem(item);
                Debug.Log($"[ItemUsageHandler] character.UseItem() 已调用完成");
            }
            else
            {
                Debug.LogWarning($"[ItemUsageHandler] ✗ 物品不可直接使用，保持在快捷栏");
                // 官方逻辑：物品不可使用时，显示"无法使用"的提示，物品保持在快捷栏
                NotificationText.Push("UI_Item_NotUsable".ToPlainText());
            }
        }

        private static void EquipItemToHand(Item item, CharacterMainControl character)
        {
            Debug.Log($"[ItemUsageHandler] EquipItemToHand 被调用");
            Debug.Log($"[ItemUsageHandler] 物品: {(item != null ? item.DisplayName : "null")}");

            if (item == null || character == null)
            {
                Debug.LogError($"[ItemUsageHandler] 物品或角色为 null，无法装备");
                return;
            }

            // 检查物品是否已经在手上
            if (IsItemEquipped(item))
            {
                Debug.Log($"[ItemUsageHandler] 物品已经在手上: {item.DisplayName}");
                return;
            }

            // 使用 ItemAgentHolder 的 ChangeHoldItem 方法
            var agentHolder = character.agentHolder;
            if (agentHolder == null)
            {
                Debug.LogError("[ItemUsageHandler] ItemAgentHolder is null, cannot equip item");
                return;
            }

            Debug.Log($"[ItemUsageHandler] 调用 agentHolder.ChangeHoldItem({item.DisplayName})");

            // 直接调用 ChangeHoldItem 方法
            var result = agentHolder.ChangeHoldItem(item);

            if (result != null)
            {
                Debug.Log($"[ItemUsageHandler] ✓ 成功装备物品到手上: {item.DisplayName}");

                // 对于爆炸物，装备后可能需要额外的处理
                if (item.Tags.Any(tag => tag.name == "Explosive"))
                {
                    Debug.Log($"[ItemUsageHandler] 检测到爆炸物，准备使用");
                    PrepareExplosiveForUse(item, character);
                }
            }
            else
            {
                Debug.LogWarning($"[ItemUsageHandler] ✗ 无法装备物品: {item.DisplayName} - 可能物品没有手持代理或创建失败");
                Debug.Log($"[ItemUsageHandler] 尝试直接使用该物品");

                // 如果装备失败，尝试直接使用
                TryUseItemDirectly(item, character);
            }
        }

        private static void PrepareExplosiveForUse(Item item, CharacterMainControl character)
        {
            // 对于爆炸物，装备后可能需要进入准备投掷的状态
            Debug.Log($"爆炸物 {item.DisplayName} 已装备，准备使用");

            // 这里可以根据需要添加额外的爆炸物准备逻辑
            // 例如设置技能状态等
        }

        /// <summary>
        /// 检查物品是否已经在手上
        /// </summary>
        public static bool IsItemEquipped(Item item)
        {
            var mainCharacter = CharacterMainControl.Main;
            if (mainCharacter?.CurrentHoldItemAgent == null) return false;

            return mainCharacter.CurrentHoldItemAgent.Item == item;
        }

        /// <summary>
        /// 放下当前手持物品
        /// </summary>
        public static void UnequipCurrentItem()
        {
            var mainCharacter = CharacterMainControl.Main;
            if (mainCharacter == null) return;

            // 通过传递 null 来放下当前手持物品
            var agentHolder = mainCharacter.agentHolder;
            if (agentHolder != null)
            {
                agentHolder.ChangeHoldItem(null);
            }
        }

        /// <summary>
        /// 检查物品是否可以创建手持代理
        /// </summary>
        public static bool CanCreateHandheldAgent(Item item)
        {
            if (item == null) return false;

            // 检查物品是否有手持代理预制体
            var handheldAgentPrefab = item.AgentUtilities.GetPrefab(ItemExtensions.HandheldHash);
            return handheldAgentPrefab != null;
        }
    }
}