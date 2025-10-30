using System;
using System.Collections.Generic;
using System.Text;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using UnityEngine;
using SlotCollection = ItemStatsSystem.Items.SlotCollection;

namespace Backpack_QuickWheel.AttachmentUI
{
    /// <summary>
    /// 配件UI辅助类 - 提供配件相关的UI显示信息
    /// </summary>
    public static class AttachmentUIHelper
    {
        /// <summary>
        /// 检查物品是否是配件（含有插槽的容器）
        /// </summary>
        public static bool IsAttachment(Item item)
        {
            if (item == null) return false;

            SlotCollection slots = item.Slots;
            if (slots == null) return false;

            return slots.Count > 0;
        }

        /// <summary>
        /// 获取配件的槽位简化信息用于显示
        /// 格式: 槽位名称: 内容 (非空槽位)
        ///      <绿色>槽位名称</绿色>: (标签列表) (空槽位)
        /// </summary>
        public static string GetAttachmentSlotsTooltip(Item attachmentItem)
        {
            if (!IsAttachment(attachmentItem))
            {
                return string.Empty;
            }

            SlotCollection slots = attachmentItem.Slots;
            if (slots == null || slots.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            int slotIndex = 0;

            foreach (Slot slot in slots)
            {
                if (slot == null) continue;

                // 槽位名称
                string slotName = slot.DisplayName ?? $"Slot {slotIndex + 1}";

                // 槽位内容或可接收标签
                if (slot.Content != null)
                {
                    // 显示物品及数量 - 非空槽位，槽位名称正常显示
                    Item content = slot.Content;
                    string contentName = content.DisplayName;
                    sb.Append(slotName).Append(": ");

                    if (content.Stackable && content.StackCount > 1)
                    {
                        sb.Append(contentName).Append(" x").Append(content.StackCount);
                    }
                    else
                    {
                        sb.Append(contentName);
                    }
                }
                else
                {
                    // 空槽位 - 槽位名称着绿色，简化显示为(tag1、tag2、...)
                    sb.Append("<color=green>").Append(slotName).Append("</color>: (");

                    bool hasLabel = false;
                    if (slot.requireTags != null && slot.requireTags.Count > 1)
                    {
                        // 从第二个tag开始显示（第一个tag用作DisplayName，已经显示过了）
                        for (int j = 1; j < slot.requireTags.Count; j++)
                        {
                            if (j > 1) sb.Append("、");
                            sb.Append(slot.requireTags[j].DisplayName);
                            hasLabel = true;
                        }
                    }

                    if (!hasLabel)
                    {
                        sb.Append("任意");
                    }
                    sb.Append(")");
                }

                slotIndex++;
                // 换行（除了最后一个）
                if (slotIndex < slots.Count)
                {
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 获取配件的槽位使用率文本
        /// 格式: "2/4"
        /// </summary>
        public static string GetAttachmentSlotUsage(Item attachmentItem)
        {
            if (!IsAttachment(attachmentItem))
            {
                return string.Empty;
            }

            SlotCollection slots = attachmentItem.Slots;
            if (slots == null || slots.Count == 0)
            {
                return string.Empty;
            }

            int filledCount = 0;
            int totalCount = slots.Count;

            foreach (Slot slot in slots)
            {
                if (slot != null && slot.Content != null)
                {
                    filledCount++;
                }
            }

            return $"{filledCount}/{totalCount}";
        }

        /// <summary>
        /// 获取配件的完整描述文本（包括槽位数和已使用数）
        /// </summary>
        public static string GetAttachmentFullInfo(Item attachmentItem)
        {
            if (!IsAttachment(attachmentItem))
            {
                return string.Empty;
            }

            string usage = GetAttachmentSlotUsage(attachmentItem);
            if (string.IsNullOrEmpty(usage))
            {
                return string.Empty;
            }

            return $"{attachmentItem.DisplayName} [{usage}]";
        }
    }
}
