using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Duckov.UI;
using Backpack_QuickWheel.AttachmentUI.Patches;
using ItemStatsSystem;
using UnityEngine;

namespace Backpack_QuickWheel.AttachmentUI
{
    /// <summary>
    /// 圆孔拖拽高亮缓存管理器
    ///
    /// 设计思路：
    /// 1. Setup阶段：indicator计算其能接收的物品规则 → 注册到Manager
    /// 2. 拖拽开始：Manager查表规则 → 快速匹配draggedItem → 高亮对应indicator
    /// 3. 拖拽结束：Manager取消所有高亮
    ///
    /// 优势：
    /// - 事件订阅从O(40) → O(1)（只Manager订阅）
    /// - 分帧按规则匹配，而非按indicator遍历
    /// - Setup时完成预计算，拖拽时纯查表
    /// </summary>
    public static class SlotIndicatorCacheManager
    {
        // 按规则key分组indicator
        // key: "require: [tag1|tag2], exclusive: [tag3]" 或 "require_or: [tag1|tag2], exclusive: [tag3]"
        // value: 符合该规则的所有indicator列表
        private static Dictionary<string, List<SlotIndicator>> _indicatorsByRule
            = new Dictionary<string, List<SlotIndicator>>();

        // 跟踪所有已注册的indicator（用于快速查找和卸载）
        private static Dictionary<SlotIndicator, HashSet<string>> _indicatorToRules
            = new Dictionary<SlotIndicator, HashSet<string>>();

        // 当前被高亮的indicator集合
        private static HashSet<SlotIndicator> _highlightedIndicators
            = new HashSet<SlotIndicator>();

        // Coroutine宿主
        private static GameObject _coroutineHost;
        private static Coroutine _dragCheckCoroutine;

        /// <summary>
        /// 初始化，订阅全局拖拽事件
        /// </summary>
        public static void Initialize()
        {
            // 创建Coroutine宿主
            _coroutineHost = new GameObject("[SlotIndicatorCacheManager] CoroutineHost");
            _coroutineHost.AddComponent<CoroutineRunner>();
            GameObject.DontDestroyOnLoad(_coroutineHost);

            // 订阅拖拽事件（一次性）
            IItemDragSource.OnStartDragItem += OnDragStarted;
            IItemDragSource.OnEndDragItem += OnDragEnded;

            Debug.Log("[SlotIndicatorCacheManager] 已初始化");
        }

        /// <summary>
        /// 反初始化
        /// </summary>
        public static void Uninitialize()
        {
            IItemDragSource.OnStartDragItem -= OnDragStarted;
            IItemDragSource.OnEndDragItem -= OnDragEnded;

            _indicatorsByRule.Clear();
            _indicatorToRules.Clear();
            _highlightedIndicators.Clear();

            if (_dragCheckCoroutine != null && _coroutineHost != null)
            {
                _coroutineHost.GetComponent<CoroutineRunner>().StopCoroutine(_dragCheckCoroutine);
            }

            Debug.Log("[SlotIndicatorCacheManager] 已反初始化");
        }

        /// <summary>
        /// 简单的Coroutine执行器
        /// </summary>
        private class CoroutineRunner : MonoBehaviour { }

        /// <summary>
        /// 注册indicator到缓存（在Setup时调用）
        /// </summary>
        public static void RegisterIndicator(SlotIndicator indicator, string ruleKey)
        {
            if (indicator == null || string.IsNullOrEmpty(ruleKey))
                return;

            try
            {
                // 添加到规则对应的列表
                if (!_indicatorsByRule.ContainsKey(ruleKey))
                {
                    _indicatorsByRule[ruleKey] = new List<SlotIndicator>();
                    Debug.Log($"[RegisterIndicator] 创建新规则: {ruleKey}");
                }
                _indicatorsByRule[ruleKey].Add(indicator);

                // 记录indicator对应的所有规则
                if (!_indicatorToRules.ContainsKey(indicator))
                {
                    _indicatorToRules[indicator] = new HashSet<string>();
                }
                _indicatorToRules[indicator].Add(ruleKey);

                Debug.Log($"[RegisterIndicator] 注册indicator到规则: {ruleKey} (当前该规则下有{_indicatorsByRule[ruleKey].Count}个indicator)");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SlotIndicatorCacheManager] 注册indicator失败: {ex}");
            }
        }

        /// <summary>
        /// 反注册indicator（在OnDisable时调用）
        /// </summary>
        public static void UnregisterIndicator(SlotIndicator indicator)
        {
            if (indicator == null)
                return;

            try
            {
                // 从所有规则中移除
                if (_indicatorToRules.TryGetValue(indicator, out var rules))
                {
                    foreach (var rule in rules)
                    {
                        if (_indicatorsByRule.ContainsKey(rule))
                        {
                            _indicatorsByRule[rule].Remove(indicator);
                            if (_indicatorsByRule[rule].Count == 0)
                            {
                                _indicatorsByRule.Remove(rule);
                            }
                        }
                    }
                    _indicatorToRules.Remove(indicator);
                }

                // 如果已高亮，则取消高亮
                if (_highlightedIndicators.Contains(indicator))
                {
                    UnhighlightSlot(indicator);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SlotIndicatorCacheManager] 反注册indicator失败: {ex}");
            }
        }

        /// <summary>
        /// 拖拽开始：分帧匹配规则，高亮对应indicator
        /// </summary>
        private static void OnDragStarted(Item draggedItem)
        {
            if (draggedItem == null)
                return;

            // 停止上一个Coroutine
            if (_dragCheckCoroutine != null && _coroutineHost != null)
            {
                _coroutineHost.GetComponent<CoroutineRunner>().StopCoroutine(_dragCheckCoroutine);
            }

            // 立即取消所有旧高亮
            var toUnhighlight = new List<SlotIndicator>(_highlightedIndicators);
            foreach (var indicator in toUnhighlight)
            {
                UnhighlightSlot(indicator);
            }

            // 启动Coroutine分帧匹配规则
            if (_coroutineHost != null)
            {
                _dragCheckCoroutine = _coroutineHost.GetComponent<CoroutineRunner>().StartCoroutine(
                    CheckRulesCoroutine(draggedItem)
                );
            }
        }

        /// <summary>
        /// 多帧分散处理：每帧检查2-3个规则
        /// </summary>
        private static IEnumerator CheckRulesCoroutine(Item draggedItem)
        {
            if (draggedItem == null)
                yield break;

            int processedCount = 0;
            const int rulesPerFrame = 2; // 每帧处理2个规则

            foreach (var kvp in _indicatorsByRule)
            {
                string ruleKey = kvp.Key;
                List<SlotIndicator> indicators = kvp.Value;

                // 检查draggedItem是否符合该规则
                if (MatchRule(draggedItem, ruleKey))
                {
                    // 符合规则：高亮所有该规则下的indicator
                    foreach (var indicator in indicators)
                    {
                        if (indicator != null && indicator.Target != null)
                        {
                            // 仅当槽位为空时才高亮
                            if (indicator.Target.Content == null)
                            {
                                HighlightSlot(indicator);
                            }
                        }
                    }
                }

                // 每处理2个规则让出控制权
                processedCount++;
                if (processedCount % rulesPerFrame == 0)
                {
                    yield return null;
                }
            }
        }

        /// <summary>
        /// 拖拽结束：取消所有高亮
        /// </summary>
        private static void OnDragEnded(Item draggedItem)
        {
            // 停止Coroutine
            if (_dragCheckCoroutine != null && _coroutineHost != null)
            {
                _coroutineHost.GetComponent<CoroutineRunner>().StopCoroutine(_dragCheckCoroutine);
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
        /// 检查draggedItem是否符合规则
        /// </summary>
        private static bool MatchRule(Item draggedItem, string ruleKey)
        {
            try
            {
                // 解析规则key
                bool isOrLogic = ruleKey.Contains("require_or:");

                // 提取require tags（根据逻辑类型选择正确的前缀）
                string tagTypePrefix = isOrLogic ? "require_or" : "require";
                List<string> requireTags = ExtractTagsFromKey(ruleKey, tagTypePrefix);

                // 提取exclusive tags
                List<string> exclusiveTags = ExtractTagsFromKey(ruleKey, "exclusive");

                // 检查exclusiveTags（排除标签）
                foreach (var tagName in exclusiveTags)
                {
                    if (draggedItem.Tags.Any(t => t != null && t.name == tagName))
                    {
                        // 有排除标签，不符合
                        return false;
                    }
                }

                // 检查requireTags
                if (requireTags.Count == 0)
                {
                    // ⚠️ 重要：如果是自定义插槽但没有RestrictTags，说明配置错误
                    if (isOrLogic)
                    {
                        Debug.LogWarning($"[MatchRule] 自定义插槽规则为空，拒绝所有物品。规则: {ruleKey}");
                        return false; // 配置错误，拒绝
                    }
                    // 官方插槽且无requireTags，允许任何物品（这是官方逻辑）
                    return true;
                }

                if (isOrLogic)
                {
                    // OR逻辑：至少有一个required tag匹配
                    bool matches = requireTags.Any(tagName =>
                        draggedItem.Tags.Any(t => t != null && t.name == tagName));

                    return matches;
                }
                else
                {
                    // AND逻辑：所有required tags都要匹配
                    bool matches = requireTags.All(tagName =>
                        draggedItem.Tags.Any(t => t != null && t.name == tagName));

                    return matches;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SlotIndicatorCacheManager] 规则匹配失败 ({ruleKey}): {ex}");
                return false;
            }
        }

        /// <summary>
        /// 从规则key中提取tags
        /// 例如: "require: [tag1|tag2], exclusive: [tag3]" → require → ["tag1", "tag2"]
        /// </summary>
        private static List<string> ExtractTagsFromKey(string ruleKey, string tagType)
        {
            try
            {
                string prefix = $"{tagType}: [";
                int startIdx = ruleKey.IndexOf(prefix);
                if (startIdx == -1)
                {
                    return new List<string>();
                }

                startIdx += prefix.Length;
                int endIdx = ruleKey.IndexOf("]", startIdx);
                if (endIdx == -1)
                {
                    return new List<string>();
                }

                string tagsStr = ruleKey.Substring(startIdx, endIdx - startIdx);
                if (string.IsNullOrEmpty(tagsStr))
                {
                    return new List<string>();
                }

                return tagsStr.Split('|').Select(t => t.Trim()).ToList();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SlotIndicatorCacheManager] 提取tags失败: {ex}");
                return new List<string>();
            }
        }

        /// <summary>
        /// 高亮指定indicator的圆孔（委托给SlotIndicatorDragHighlightPatch）
        /// </summary>
        private static void HighlightSlot(SlotIndicator indicator)
        {
            if (indicator == null)
                return;

            try
            {
                // 调用SlotIndicatorDragHighlightPatch的高亮方法
                SlotIndicatorDragHighlightPatch.HighlightSlotPublic(indicator);
                _highlightedIndicators.Add(indicator);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SlotIndicatorCacheManager] 高亮失败: {ex}");
            }
        }

        /// <summary>
        /// 取消高亮指定indicator（委托给SlotIndicatorDragHighlightPatch）
        /// </summary>
        private static void UnhighlightSlot(SlotIndicator indicator)
        {
            if (indicator == null)
                return;

            try
            {
                // 调用SlotIndicatorDragHighlightPatch的取消高亮方法
                SlotIndicatorDragHighlightPatch.UnhighlightSlotPublic(indicator);
                _highlightedIndicators.Remove(indicator);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SlotIndicatorCacheManager] 取消高亮失败: {ex}");
            }
        }
    }
}
