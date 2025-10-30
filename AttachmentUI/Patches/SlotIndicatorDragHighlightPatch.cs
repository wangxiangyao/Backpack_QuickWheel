using System;
using System.Collections;
using System.Collections.Generic;
using Duckov.UI;
using HarmonyLib;
using ItemStatsSystem;
using UnityEngine;
using UnityEngine.UI;

namespace Great_backpack.AttachmentUI.Patches
{
    /// <summary>
    /// 圆孔拖拽高亮Patch - 每个SlotIndicator独立处理
    ///
    /// 原理：
    /// 1. 在SlotIndicator.Setup时，仅为主背包中的指示器订阅IItemDragSource的全局拖拽事件
    /// 2. 在SlotIndicator.OnDisable时，取消订阅事件
    /// 3. 当拖拽开始时，通过多帧分散Coroutine检查每个SlotIndicator是否可以接收该物品，能则高亮
    /// 4. 当拖拽结束时，停止Coroutine并恢复所有圆孔为原色
    ///
    /// 性能优化：
    /// - 使用Coroutine多帧分散处理：避免在一帧内调用40个CanPlug()导致卡顿
    /// - 每帧处理2个indicator，40个指标分散到约20帧（~333ms）
    /// - 主背包过滤：只订阅主背包中的indicator，减少不必要的事件处理
    /// </summary>
    [HarmonyPatch(typeof(SlotIndicator))]
    public class SlotIndicatorDragHighlightPatch
    {
        // 存储每个 SlotIndicator 实例的事件处理代理，用于取消订阅
        private static Dictionary<SlotIndicator, (Action<Item> onStart, Action<Item> onEnd)> _subscribedIndicators
            = new Dictionary<SlotIndicator, (Action<Item>, Action<Item>)>();

        // 存储高亮状态下被激活的 contentIndicator，以便稍后恢复状态
        private static HashSet<SlotIndicator> _highlightedIndicators
            = new HashSet<SlotIndicator>();

        // 缓存每个SlotIndicator的contentIndicator GameObject和Graphic组件，避免重复反射和GetComponent
        private static Dictionary<SlotIndicator, (GameObject contentIndicator, Graphic graphic)> _cachedComponents
            = new Dictionary<SlotIndicator, (GameObject, Graphic)>();

        // 反射字段缓存
        private static System.Reflection.FieldInfo _contentIndicatorField;

        // Coroutine宿主 - 用于执行多帧分散处理
        private static GameObject _coroutineHost;

        // 当前正在运行的拖拽检查Coroutine
        private static Coroutine _dragCheckCoroutine;

        // 静态构造函数，初始化反射字段和Coroutine宿主
        static SlotIndicatorDragHighlightPatch()
        {
            _contentIndicatorField = typeof(SlotIndicator).GetField("contentIndicator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // 创建Coroutine宿主
            _coroutineHost = new GameObject("[SlotIndicatorDragHighlight] CoroutineHost");
            _coroutineHost.AddComponent<CoroutineRunner>();
            GameObject.DontDestroyOnLoad(_coroutineHost);
        }

        /// <summary>
        /// 简单的Coroutine执行器
        /// </summary>
        private class CoroutineRunner : MonoBehaviour { }

        /// <summary>
        /// 获取Coroutine执行器
        /// </summary>
        private static CoroutineRunner GetCoroutineRunner()
        {
            if (_coroutineHost == null || _coroutineHost.GetComponent<CoroutineRunner>() == null)
            {
                _coroutineHost = new GameObject("[SlotIndicatorDragHighlight] CoroutineHost");
                _coroutineHost.AddComponent<CoroutineRunner>();
                GameObject.DontDestroyOnLoad(_coroutineHost);
            }
            return _coroutineHost.GetComponent<CoroutineRunner>();
        }

        /// <summary>
        /// Patch Setup - 订阅拖拽事件（仅对主背包中的物品）
        /// </summary>
        [HarmonyPatch("Setup")]
        [HarmonyPostfix]
        public static void Setup_Postfix(SlotIndicator __instance)
        {
            if (__instance == null || __instance.Target == null)
                return;

            try
            {
                var parentItem = __instance.Target.Master;

                // 检查槽位所属的物品是否在主背包中
                // 只有在主背包中的indicator才需要订阅拖拽事件，避免不必要的事件处理
                if (!IsItemInMainBackpack(parentItem))
                    return;

                // 缓存contentIndicator和Graphic组件（一次性，避免之后每次都反射和GetComponent）
                if (_contentIndicatorField != null)
                {
                    var contentIndicatorGO = _contentIndicatorField.GetValue(__instance) as GameObject;
                    if (contentIndicatorGO != null)
                    {
                        var graphic = contentIndicatorGO.GetComponent<Graphic>();
                        if (graphic != null)
                        {
                            _cachedComponents[__instance] = (contentIndicatorGO, graphic);
                        }
                    }
                }

                // 创建事件处理方法，使用闭包捕获当前的SlotIndicator实例
                Action<Item> onStartHandler = (draggedItem) => OnDragStarted(__instance, draggedItem);
                Action<Item> onEndHandler = (draggedItem) => OnDragEnded(__instance, draggedItem);

                // 订阅全局拖拽事件（只有在主背包的indicator才会订阅）
                IItemDragSource.OnStartDragItem += onStartHandler;
                IItemDragSource.OnEndDragItem += onEndHandler;

                // 保存代理，以便OnDisable时取消订阅
                _subscribedIndicators[__instance] = (onStartHandler, onEndHandler);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SlotIndicatorDragHighlight] Setup订阅失败: {ex}");
            }
        }

        /// <summary>
        /// 判断物品是否在主背包中
        /// 先检查 PluggedIntoSlot 作为快速路径，再用 GameObject 父子关系进行完整查找
        /// </summary>
        private static bool IsItemInMainBackpack(Item item)
        {
            if (item == null)
                return false;

            // 快速路径：如果 PluggedIntoSlot == null 且 Inventory != null，说明这是主背包
            if (item.PluggedIntoSlot == null && item.Inventory != null)
                return true;

            // 慢速路径：物品被插入到了槽位中，需要通过 GameObject 父子关系向上查找
            while (item != null)
            {
                // 获取 GameObject 的父节点
                Transform parentTransform = item.gameObject.transform.parent;
                if (parentTransform == null)
                    return false;

                // 在父节点上查找 Item 组件
                Item parentItem = parentTransform.GetComponent<Item>();
                if (parentItem == null)
                    return false;

                // 检查父物品的 Inventory
                if (parentItem.Inventory != null)
                    return true;

                // 继续向上查找
                item = parentItem;
            }

            return false;
        }

        /// <summary>
        /// Patch OnDisable - 取消订阅拖拽事件
        /// </summary>
        [HarmonyPatch("OnDisable")]
        [HarmonyPostfix]
        public static void OnDisable_Postfix(SlotIndicator __instance)
        {
            if (__instance == null)
                return;

            try
            {
                // 查找并移除该SlotIndicator的事件订阅
                if (_subscribedIndicators.TryGetValue(__instance, out var handlers))
                {
                    IItemDragSource.OnStartDragItem -= handlers.onStart;
                    IItemDragSource.OnEndDragItem -= handlers.onEnd;
                    _subscribedIndicators.Remove(__instance);

                    // 清理缓存
                    _cachedComponents.Remove(__instance);
                    _highlightedIndicators.Remove(__instance);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SlotIndicatorDragHighlight] OnDisable取消订阅失败: {ex}");
            }
        }

        /// <summary>
        /// 处理拖拽开始事件 - 启动多帧分散处理Coroutine
        /// 在这一帧就立即取消前一个拖拽的高亮，然后异步检查新拖拽的CanPlug结果
        /// </summary>
        private static void OnDragStarted(SlotIndicator slotIndicator, Item draggedItem)
        {
            // 停止上一个拖拽的Coroutine（如果有）
            if (_dragCheckCoroutine != null)
            {
                GetCoroutineRunner().StopCoroutine(_dragCheckCoroutine);
            }

            // 立即取消所有高亮（不需要等待Coroutine）
            var toUnhighlight = new List<SlotIndicator>(_highlightedIndicators);
            foreach (var indicator in toUnhighlight)
            {
                UnhighlightSlot(indicator);
            }

            // 启动新的Coroutine来分散处理所有indicator的CanPlug检查
            _dragCheckCoroutine = GetCoroutineRunner().StartCoroutine(
                CheckAllIndicatorsCoroutine(draggedItem)
            );
        }

        /// <summary>
        /// 多帧分散处理：每帧检查2个indicator的CanPlug
        /// 平衡速度和流畅性：40个indicator分散到20帧（~333ms），每帧CPU占用低
        /// </summary>
        private static IEnumerator CheckAllIndicatorsCoroutine(Item draggedItem)
        {
            if (draggedItem == null)
                yield break;

            int processedCount = 0;
            const int itemsPerFrame = 2; // 每帧处理2个indicator

            foreach (var kvp in _subscribedIndicators)
            {
                SlotIndicator indicator = kvp.Key;
                if (indicator == null || indicator.Target == null)
                    continue;

                // 检查槽位是否已有物品
                if (indicator.Target.Content != null)
                {
                    // 槽位已有物品，不能高亮
                    continue;
                }

                // 检查物品是否可以插入
                if (indicator.Target.CanPlug(draggedItem))
                {
                    HighlightSlot(indicator);
                }

                // 每处理2个indicator就让出控制权，等待下一帧
                processedCount++;
                if (processedCount % itemsPerFrame == 0)
                {
                    yield return null;
                }
            }
        }

        /// <summary>
        /// 处理拖拽结束事件 - 停止Coroutine并立即取消所有高亮
        /// </summary>
        private static void OnDragEnded(SlotIndicator slotIndicator, Item draggedItem)
        {
            // 停止Coroutine
            if (_dragCheckCoroutine != null)
            {
                GetCoroutineRunner().StopCoroutine(_dragCheckCoroutine);
                _dragCheckCoroutine = null;
            }

            // 立即取消所有高亮
            var toUnhighlight = new List<SlotIndicator>(_highlightedIndicators);
            foreach (var indicator in toUnhighlight)
            {
                UnhighlightSlot(indicator);
            }
        }

        /// <summary>
        /// 高亮SlotIndicator的圆孔为绿色
        /// 使用缓存的组件避免重复反射和GetComponent调用
        /// </summary>
        private static void HighlightSlot(SlotIndicator slotIndicator)
        {
            if (!_cachedComponents.TryGetValue(slotIndicator, out var cached))
                return;

            var contentIndicatorGO = cached.contentIndicator;
            var graphic = cached.graphic;

            // 如果槽位原本是空的，contentIndicator 会是 inactive，需要临时激活来显示高亮
            if (!contentIndicatorGO.activeSelf)
            {
                contentIndicatorGO.SetActive(true);
                _highlightedIndicators.Add(slotIndicator);
            }

            // 设置颜色为绿色（只改颜色，不改布局）
            if (graphic != null)
            {
                graphic.color = new Color(0f, 1f, 0f, 1f); // 绿色
                // 只更新材质，不重新布局
                graphic.SetMaterialDirty();
            }
        }

        /// <summary>
        /// 恢复SlotIndicator的圆孔为白色，并恢复激活状态
        /// 使用缓存的组件避免重复反射和GetComponent调用
        /// </summary>
        private static void UnhighlightSlot(SlotIndicator slotIndicator)
        {
            if (!_cachedComponents.TryGetValue(slotIndicator, out var cached))
                return;

            var contentIndicatorGO = cached.contentIndicator;
            var graphic = cached.graphic;

            // 恢复颜色为白色（只改颜色，不改布局）
            if (graphic != null)
            {
                graphic.color = Color.white;
                // 只更新材质，不重新布局
                graphic.SetMaterialDirty();
            }

            // 如果这个indicator是我们激活的（在_highlightedIndicators中），就停用它
            if (_highlightedIndicators.Contains(slotIndicator))
            {
                contentIndicatorGO.SetActive(false);
                _highlightedIndicators.Remove(slotIndicator);
            }
        }
    }
}
