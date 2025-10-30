using System.Reflection;
using Duckov.UI;
using ItemStatsSystem;
using TMPro;

namespace Backpack_QuickWheel.AttachmentUI
{
    /// <summary>
    /// 配件Hover信息管理器
    /// 订阅ItemHoveringUI的onSetupItem事件，为配件物品的hover面板追加槽位信息
    /// </summary>
    public static class AttachmentHoveringUIManager
    {
        private static FieldInfo itemDescriptionField;

        /// <summary>
        /// 初始化，订阅hover事件
        /// </summary>
        public static void Initialize()
        {
            // 缓存itemDescription字段以提高性能
            itemDescriptionField = typeof(ItemHoveringUI).GetField("itemDescription",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // 订阅ItemHoveringUI的onSetupItem事件
            ItemHoveringUI.onSetupItem += OnItemHoveringSetup;
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        public static void Uninitialize()
        {
            ItemHoveringUI.onSetupItem -= OnItemHoveringSetup;
        }

        /// <summary>
        /// 当ItemHoveringUI设置物品时触发
        /// </summary>
        private static void OnItemHoveringSetup(ItemHoveringUI hoveringUI, Item item)
        {
            if (item == null)
            {
                return;
            }

            // 检查是否是配件
            if (!AttachmentUIHelper.IsAttachment(item))
            {
                return;
            }

            // 获取槽位信息
            string slotInfo = AttachmentUIHelper.GetAttachmentSlotsTooltip(item);
            if (string.IsNullOrEmpty(slotInfo))
            {
                return;
            }

            try
            {
                // 通过反射获取itemDescription文本UI
                if (itemDescriptionField == null)
                {
                    itemDescriptionField = typeof(ItemHoveringUI).GetField("itemDescription",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                }

                if (itemDescriptionField != null)
                {
                    var itemDesc = itemDescriptionField.GetValue(hoveringUI) as TextMeshProUGUI;
                    if (itemDesc != null)
                    {
                        // 追加槽位信息
                        string slotUsage = AttachmentUIHelper.GetAttachmentSlotUsage(item);
                        string header = string.IsNullOrEmpty(slotUsage)
                            ? "[配件槽位]"
                            : $"[配件槽位 {slotUsage}]";

                        itemDesc.text += "\n\n" + header + "\n" + slotInfo;
                    }
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[AttachmentHoveringUIManager] 追加槽位信息时出错: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
