using System.Reflection;
using Duckov.UI;
using HarmonyLib;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using UnityEngine;
using UnityEngine.UI;

namespace Great_backpack.AttachmentUI.Patches
{
    /// <summary>
    /// 物品indicator布局优化Patch - 处理多行显示
    ///
    /// 问题：物品插槽超过8个（如行军背包的10个插槽）时，
    ///       单行布局导致indicator圆孔被严重压扁
    ///
    /// 解决方案：
    /// - 8个及以下：保持官方原样的单行布局
    /// - 9个及以上：改用GridLayoutGroup，每行最多8个，自动换行
    ///
    /// 布局效果：
    /// - 10个插槽 → 第1行8个 + 第2行2个
    /// - 16个插槽 → 第1行8个 + 第2行8个
    /// - 17个插槽 → 第1行8个 + 第2行8个 + 第3行1个
    /// </summary>
    [HarmonyPatch(typeof(ItemDisplay), "Setup")]
    public class ItemDisplayIndicatorLayoutPatch
    {
        // 反射缓存：获取ItemDisplay.slotIndicatorContainer字段
        private static FieldInfo _slotIndicatorContainerField;

        static ItemDisplayIndicatorLayoutPatch()
        {
            _slotIndicatorContainerField = typeof(ItemDisplay).GetField("slotIndicatorContainer",
                BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [HarmonyPostfix]
        public static void Setup_Postfix(ItemDisplay __instance, Item target)
        {
            try
            {
                if (target?.Slots == null) return;

                int slotCount = target.Slots.Count;
                if (slotCount <= 8) return; // 8个及以下保持原样

                // 通过反射获取slotIndicatorContainer
                if (_slotIndicatorContainerField == null) return;

                GameObject container = _slotIndicatorContainerField.GetValue(__instance) as GameObject;
                if (container == null) return;

                // 检查container是否有RectTransform（必需的布局前提）
                if (container.GetComponent<RectTransform>() == null) return;

                // 获取或移除旧的布局组件
                var oldLayout = container.GetComponent<LayoutGroup>();
                if (oldLayout != null)
                {
                    // 使用DestroyImmediate确保同步删除
                    Object.DestroyImmediate(oldLayout);
                }

                // 添加GridLayoutGroup实现多行布局
                var gridLayout = container.AddComponent<GridLayoutGroup>();

                // 防守性检查
                if (gridLayout == null) return;

                // 关键配置：每行最多6个indicator
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = 6;

                // 布局参数
                gridLayout.cellSize = new Vector2(12, 12); // 圆孔大小
                gridLayout.spacing = new Vector2(2, 2); // indicator之间的间距
                gridLayout.childAlignment = TextAnchor.UpperLeft; // 从左上角开始排列
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ItemDisplayIndicatorLayoutPatch] 错误: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
