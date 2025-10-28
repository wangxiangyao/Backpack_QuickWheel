using UnityEngine;
using ItemStatsSystem;
using Duckov.UI;

namespace Great_backpack.ShortcutSystem
{
    /// <summary>
    /// 轮盘格子适配器 - 封装ItemDisplay
    /// 禁用ItemDisplay的所有事件订阅，只保留UI显示
    /// </summary>
    public class WheelItemDisplayAdapter : MonoBehaviour
    {
        private ItemDisplay _itemDisplay;
        private Item _item;

        public void Initialize(ItemDisplay templateDisplay, Item item)
        {
            _item = item;
            _itemDisplay = templateDisplay;

            if (_itemDisplay != null && _item != null)
            {
                // 调用Setup来显示物品
                _itemDisplay.Setup(_item);
                Debug.Log($"[WheelItemDisplayAdapter] 初始化轮盘格子: {_item.DisplayName}");
            }
        }

        /// <summary>
        /// 清理ItemDisplay的事件订阅
        /// 通过禁用它的MonoBehaviour来防止事件处理
        /// </summary>
        public void CleanupItemDisplay()
        {
            if (_itemDisplay != null)
            {
                // 禁用ItemDisplay脚本以防止事件处理
                _itemDisplay.enabled = false;
            }
        }

        public Item GetItem()
        {
            return _item;
        }
    }
}
