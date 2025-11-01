using System.Reflection;
using Duckov.UI;
using HarmonyLib;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using UnityEngine;
using UnityEngine.UI;

namespace Backpack_QuickWheel.AttachmentUI.Patches
{
    /// <summary>
    /// 物品indicator布局优化Patch - 处理多行显示
    ///
    /// 问题：物品插槽超过8个（如行军背包的10个插槽）时，
    ///       单行布局导致indicator圆孔被严重压扁
    ///
    /// 解决方案：
    /// - 8个及以下：保持官方原样的单行布局
    /// - 9个及以上：改用GridLayoutGroup，每行最多7个，自动换行
    ///
    /// 布局效果：
    /// - 10个插槽 → 第1行7个 + 第2行3个
    /// - 14个插槽 → 第1行7个 + 第2行7个
    /// - 17个插槽 → 第1行7个 + 第2行7个 + 第3行3个
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
                // 🔧 优化：嘎嘎战术包等配件也需要使用网格布局
                // 当插槽数等于5时（嘎嘎战术包）也启用网格布局来测试7个每行效果
                if (slotCount < 5) return; // 少于5个保持原样，5个及以上使用网格布局

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

                // 关键配置：每行最多7个indicator
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = 7;

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
