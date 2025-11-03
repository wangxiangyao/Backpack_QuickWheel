using UnityEngine;
using ItemStatsSystem;

namespace Backpack_QuickWheel.ShortcutSystem
{
    /// <summary>
    /// 拦截和处理快捷键输入，检测长按与短按
    /// 长按显示轮盘选择器，短按直接使用物品
    ///
    /// 逻辑：
    /// - 按键按下(started) -> 开始记录时间
    /// - 每帧累加时间 -> 时间 >= 0.3s -> 显示轮盘
    /// - 按键释放(canceled) -> 如果轮盘显示则执行选择，否则执行短按
    /// </summary>
    public class InputInterceptor : MonoBehaviour
    {
        private static InputInterceptor _instance;

        // 长按检测配置
        private const float LONG_PRESS_THRESHOLD = 0.2f;  // 长按时间阈值（秒）

        // 按键状态追踪
        private int _currentPressedIndex = -1;
        private float _pressDuration = 0f;
        private bool _wheelShown = false;

        // 鼠标位置记录
        private Vector2 _pressDownMousePos = Vector2.zero;      // 按下时的鼠标位置
        private Vector2 _wheelShowMousePos = Vector2.zero;      // 轮盘显示时的鼠标位置

        private ItemWheelSelector _wheelSelector;

        public static InputInterceptor Instance => _instance;

        /// <summary>
        /// 检查轮盘是否正在显示
        /// </summary>
        public static bool IsWheelVisible => _instance != null && _instance._wheelShown;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(this);
        }

        private void OnEnable()
        {
            // 订阅快捷键按下和释放事件
            Patches.CharacterInputShortcutPatch.OnShortcutKeyDown += OnShortcutKeyDown;
            Patches.CharacterInputShortcutPatch.OnShortcutKeyUp += OnShortcutKeyUp;
            Debug.Log("[InputInterceptor] 已订阅快捷键按下/释放事件");
        }

        private void OnDisable()
        {
            // 取消订阅
            Patches.CharacterInputShortcutPatch.OnShortcutKeyDown -= OnShortcutKeyDown;
            Patches.CharacterInputShortcutPatch.OnShortcutKeyUp -= OnShortcutKeyUp;
            Debug.Log("[InputInterceptor] 已取消订阅快捷键事件");
        }

        private void Update()
        {
            // 如果有按键正在按下，累加时间
            if (_currentPressedIndex >= 0)
            {
                _pressDuration += Time.deltaTime;

                // 如果达到长按阈值且轮盘未显示，显示轮盘
                if (_pressDuration >= LONG_PRESS_THRESHOLD && !_wheelShown)
                {
                    Debug.Log($"[InputInterceptor] 长按时间达到 {_pressDuration:F2}s，显示轮盘");
                    ShowWheelSelector();
                    _wheelShown = true;
                }
            }
        }

        /// <summary>
        /// 快捷键按下事件处理
        /// </summary>
        private void OnShortcutKeyDown(int index)
        {
            // 只处理我们的快捷键范围
            if (!BackpackShortcutManager.IsBackpackShortcutIndex(index))
            {
                Debug.Log($"[InputInterceptor] 快捷键 {index} 不在范围内，忽略");
                return;
            }

            Debug.Log($"[InputInterceptor] ✓ 快捷键 {index} 按下（在范围内）");

            // 如果已有其他按键按下，先处理释放
            if (_currentPressedIndex >= 0 && _currentPressedIndex != index)
            {
                Debug.Log($"[InputInterceptor] 前一个按键未释放，先处理释放: {_currentPressedIndex}");
                OnShortcutKeyUp(_currentPressedIndex);
            }

            // 记录当前按下的快捷键
            _currentPressedIndex = index;
            _pressDuration = 0f;
            _wheelShown = false;

            // 记录按下时的鼠标位置（用于第一矢量）
            _pressDownMousePos = Input.mousePosition;

            Debug.Log($"[InputInterceptor] 开始计时，索引: {index}，鼠标位置: {_pressDownMousePos}");
        }

        /// <summary>
        /// 快捷键释放事件处理
        /// </summary>
        private void OnShortcutKeyUp(int index)
        {
            // 只处理我们的快捷键范围
            if (!BackpackShortcutManager.IsBackpackShortcutIndex(index))
            {
                Debug.Log($"[InputInterceptor] 快捷键 {index} 不在范围内，忽略");
                return;
            }

            // 只处理当前正在按下的快捷键
            if (_currentPressedIndex != index)
            {
                Debug.Log($"[InputInterceptor] 释放的快捷键 {index} 与当前按下的 {_currentPressedIndex} 不匹配，忽略");
                return;
            }

            Debug.Log($"[InputInterceptor] ✓ 快捷键 {index} 释放，按压时长: {_pressDuration:F2}s");

            if (_wheelShown)
            {
                // 长按后释放：轮盘已显示
                // 检查轮盘是否发生了拖拽
                if (_wheelSelector.HasBeenDragged())
                {
                    Debug.Log($"[InputInterceptor] 轮盘已发生拖拽，跳过物品使用，仅关闭轮盘");
                }
                else
                {
                    // 轮盘未拖拽，执行选中物品
                    Debug.Log($"[InputInterceptor] 轮盘未拖拽，执行选中物品");
                    HandleWheelItemSelection(index);
                }

                // 【轮盘布局持久化】轮盘关闭前保存当前布局
                BackpackShortcutManager.Instance?.PersistWheelLayouts();

                // 隐藏轮盘
                _wheelSelector.HideWheel();
            }
            else if (_pressDuration < LONG_PRESS_THRESHOLD)
            {
                // 短按：直接使用当前物品
                Debug.Log($"[InputInterceptor] 短按（{_pressDuration:F2}s < {LONG_PRESS_THRESHOLD}s），执行短按处理");
                HandleShortPress(index);
            }
            else
            {
                Debug.Log($"[InputInterceptor] 轮盘未显示，且已经超过长按阈值，不进行任何操作");
            }

            // 重置状态
            _currentPressedIndex = -1;
            _pressDuration = 0f;
            _wheelShown = false;
        }

        /// <summary>
        /// 显示轮盘选择器
        /// </summary>
        private void ShowWheelSelector()
        {
            if (_wheelSelector == null)
            {
                Debug.LogError("[InputInterceptor] ItemWheelSelector 未初始化");
                return;
            }

            if (_currentPressedIndex < 0)
            {
                Debug.LogError("[InputInterceptor] 当前没有快捷键被按下");
                return;
            }

            var category = BackpackShortcutManager.IndexToCategory(_currentPressedIndex);

            Debug.Log($"[InputInterceptor] ═══════════════════════════════════════");
            Debug.Log($"[InputInterceptor] 准备显示轮盘");
            Debug.Log($"[InputInterceptor] 快捷键索引: {_currentPressedIndex}，对应类别: {category}");
            Debug.Log($"[InputInterceptor] 正在从 BackpackShortcutManager 获取物品列表...");

            var items = BackpackShortcutManager.Instance?.GetAllItemsForCategory(category);

            if (items == null || items.Count == 0)
            {
                Debug.LogWarning($"[InputInterceptor] ✗ 类别 {category} 没有物品，不显示轮盘");
                Debug.Log($"[InputInterceptor] ═══════════════════════════════════════");
                _wheelShown = false;
                return;
            }

            // 记录轮盘显示时的鼠标位置（用于第二矢量）
            _wheelShowMousePos = Input.mousePosition;

            // 计算第一矢量（按下到显示时的鼠标移动）
            Vector2 firstVector = _wheelShowMousePos - _pressDownMousePos;

            Debug.Log($"[InputInterceptor] ✓ 获取到物品列表，数量: {items.Count}");
            Debug.Log($"[InputInterceptor] 第一矢量: {firstVector}，长度: {firstVector.magnitude}");
            Debug.Log($"[InputInterceptor] 轮盘显示鼠标位置: {_wheelShowMousePos}");
            Debug.Log($"[InputInterceptor] ═══════════════════════════════════════");

            _wheelSelector.ShowWheel(items, _currentPressedIndex, _pressDownMousePos, _wheelShowMousePos, category);
        }

        /// <summary>
        /// 处理轮盘中选中的物品
        /// </summary>
        private void HandleWheelItemSelection(int index)
        {
            if (_wheelSelector == null) return;

            var category = BackpackShortcutManager.IndexToCategory(index);
            var wheelLayoutManager = BackpackShortcutManager.Instance?.WheelLayoutManager;

            if (wheelLayoutManager == null)
            {
                Debug.LogWarning("[InputInterceptor] WheelLayoutManager为null，无法获取选中物品");
                return;
            }

            var selectedItem = wheelLayoutManager.GetSelectedItem(category);
            if (selectedItem == null)
            {
                Debug.LogWarning("[InputInterceptor] 轮盘中没有选中的物品");
                return;
            }

            Debug.Log($"[InputInterceptor] 从轮盘选中物品: {selectedItem.DisplayName}");

            // 🆕 架构修复：直接使用物品，无需通知BackpackShortcutManager
            // 物品进入系统时已经订阅了所有必要事件
            // SetCurrentSelection已删除，因为造成重复订阅

            // 使用该物品
            ItemUsageHandler.UseItem(selectedItem, category);
        }

        /// <summary>
        /// 处理短按（直接使用当前物品）
        /// </summary>
        private void HandleShortPress(int index)
        {
            Debug.Log($"[InputInterceptor] 检测到短按，索引: {index}");

            var category = BackpackShortcutManager.IndexToCategory(index);
            BackpackShortcutManager.Instance?.HandleShortcutInput(category);
        }

        /// <summary>
        /// 设置轮盘选择器引用
        /// </summary>
        public static void SetWheelSelector(ItemWheelSelector wheelSelector)
        {
            if (_instance != null)
            {
                _instance._wheelSelector = wheelSelector;
                Debug.Log("[InputInterceptor] 轮盘选择器已设置");
            }
        }
    }
}
