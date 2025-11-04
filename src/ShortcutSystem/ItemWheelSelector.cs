using UnityEngine;
using UnityEngine.UI;
using ItemStatsSystem;
using System.Collections.Generic;
using System.Linq;
using Duckov.UI;

namespace Backpack_QuickWheel.ShortcutSystem
{
    /// <summary>
    /// 轮盘关闭模式枚举
    /// </summary>
    public enum WheelCloseMode
    {
        UseHoveredItem,      // 使用hover的物品，不改变选中状态（松开快捷键时）
        ChangeSelection      // 改变选中物品，不使用物品（左键点击时）
    }

    /// <summary>
    /// 物品轮盘选择器UI组件 - 九宫格布局版本
    ///
    /// 克隆游戏官方的 ItemDisplay 格子，排列成九宫格（中心空）
    ///
    /// 布局（以鼠标为中心）：
    /// [-1,-1] [0,-1] [1,-1]
    /// [-1, 0] [中心] [1, 0]
    /// [-1, 1] [0, 1] [1, 1]
    ///
    /// 逻辑：
    /// - 长按时轮盘显示在鼠标位置
    /// - 根据鼠标相对于中心的方向确定选择的物品
    /// </summary>
    public class ItemWheelSelector : MonoBehaviour
    {
        private Canvas _wheelCanvas;
        private RectTransform _wheelContainer;
        private List<GameObject> _itemDisplayClones = new List<GameObject>();
        private List<ItemDisplay> _itemDisplayComponents = new List<ItemDisplay>();
        // 鼠标悬停状态索引（用于视觉放大效果）
        private int _hoverIndex = -1;
        private List<Item> _currentItems = new List<Item>();

        // 轮盘位置
        private Vector2 _wheelCenterScreenPos;

        // 🆕 持有管理器引用，方便获取和调整选中状态
        private BackpackShortcutManager _backpackManager;
        private WheelLayoutManager _wheelLayoutManager;
        private bool _wheelActive = false;

        // 全屏拦截面板（防止鼠标输入传给游戏）
        private GameObject _inputBlockerPanel;
        private Image _inputBlockerImage;

        // ItemDisplay 模板（从库存克隆）
        private ItemDisplay _itemDisplayTemplate;

        // 矢量选择逻辑
        private Vector2 _pressDownMousePos = Vector2.zero;       // 按下时的鼠标位置
        private Vector2 _wheelShowMousePos = Vector2.zero;       // 轮盘显示时的鼠标位置
        private const float FIRST_VECTOR_THRESHOLD = 40f;        // 第一矢量的激活阈值（死区），增大到40px防止误触

        // 当前显示的物品所属类别（用于保存布局时）
        private ItemCategory _currentCategory = ItemCategory.Medical;

        // 拖拽状态标志
        private bool _hasBeenDragged = false;  // 轮盘显示期间是否发生了拖拽

        // 九宫格配置
        private const float CELL_SIZE = 40f;                        // 格子大小（宽高）
        private const float GRID_OFFSET = CELL_SIZE + 5f;           // 格子间距（包括间距）

        // 🔧 新布局：优化的轮盘位置分布
        // 索引映射：1-8 对应 8 个位置（索引0预留，中心跳过）
        // 1-左中，2-右中，3-上中，4-下中，5-左下，6-右下，7-右上，8-左上
        // [8] [7] [3]
        // [1] [ ] [2]
        // [5] [6] [4]
        private static readonly Vector2Int[] GRID_POSITIONS = new Vector2Int[]
        {
            new Vector2Int( 0,  0),  // 0: 预留（不使用）
            new Vector2Int(-1,  0),  // 1: 左中
            new Vector2Int( 1,  0),  // 2: 右中
            new Vector2Int( 0, -1),  // 3: 上中
            new Vector2Int( 0,  1),  // 4: 下中
            new Vector2Int(-1,  1),  // 5: 左下
            new Vector2Int( 1,  1),  // 6: 右下
            new Vector2Int( 1, -1),  // 7: 右上
            new Vector2Int(-1, -1),  // 8: 左上
        };

        // 🔧 新角度映射：对应新的8个位置
        private static readonly float[] DIRECTION_ANGLES = new float[]
        {
            0f,    // 0: 预留（不使用）
            180f,  // 1: 左中
            0f,    // 2: 右中
            270f,  // 3: 上中
            90f,   // 4: 下中
            135f,  // 5: 左下
            45f,   // 6: 右下
            315f,  // 7: 右上
            225f,  // 8: 左上
        };

        private void Awake()
        {
            InitializeWheel();
        }

        /// <summary>
        /// 初始化轮盘UI（九宫格，中心为空）
        /// 从库存查找ItemDisplay模板
        /// </summary>
        private void InitializeWheel()
        {
            // 🆕 获取管理器引用
            _backpackManager = BackpackShortcutManager.Instance;
            if (_backpackManager != null)
            {
                // 使用反射获取WheelLayoutManager
                var wheelLayoutManagerField = typeof(BackpackShortcutManager).GetField("_wheelLayoutManager",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (wheelLayoutManagerField != null)
                {
                    _wheelLayoutManager = wheelLayoutManagerField.GetValue(_backpackManager) as WheelLayoutManager;
                }
                else
                {
                    Debug.LogError("[ItemWheelSelector] ✗ 无法获取WheelLayoutManager字段");
                }
            }
            else
            {
                Debug.LogError("[ItemWheelSelector] ✗ 无法获取BackpackShortcutManager实例");
            }

            // 创建Canvas
            var canvasObj = new GameObject("ItemWheelCanvas");
            canvasObj.transform.SetParent(transform, false);
            canvasObj.transform.localPosition = Vector3.zero;

            // 配置Canvas的RectTransform
            var canvasRect = canvasObj.GetComponent<RectTransform>();
            if (canvasRect == null)
            {
                canvasRect = canvasObj.AddComponent<RectTransform>();
            }
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;
            canvasRect.localPosition = Vector3.zero;

            _wheelCanvas = canvasObj.AddComponent<Canvas>();
            _wheelCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _wheelCanvas.sortingOrder = 100;

            var graphicRaycaster = canvasObj.AddComponent<GraphicRaycaster>();

            // 🔧 优化：确保鼠标指针不被遮挡
            // 1. 降低Canvas排序，避免遮挡系统鼠标
            // 2. 使用合适的输入拦截策略
            // 注意：inputBlockerImage 稍后创建，这里先预留注释

            // 在轮盘显示时隐藏系统鼠标，在隐藏时恢复
            // 这将在ShowWheel/HideWheel中处理

            var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            // 创建全屏拦截面板（防止鼠标输入传给游戏）
            var blockerObj = new GameObject("InputBlocker");
            blockerObj.transform.SetParent(canvasObj.transform, false);
            blockerObj.transform.SetAsFirstSibling();  // 放在最后面，不遮挡轮盘

            var blockerRect = blockerObj.GetComponent<RectTransform>();
            if (blockerRect == null)
            {
                blockerRect = blockerObj.AddComponent<RectTransform>();
            }
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;

            _inputBlockerImage = blockerObj.AddComponent<Image>();
            _inputBlockerImage.color = new Color(0, 0, 0, 0);  // 完全透明
            _inputBlockerImage.raycastTarget = true;  // 拦截输入
            _inputBlockerPanel = blockerObj;

            // 创建轮盘容器
            var wheelObj = new GameObject("WheelContainer");
            wheelObj.transform.SetParent(canvasObj.transform, false);
            wheelObj.transform.localPosition = Vector3.zero;

            var wheelRectTransform = wheelObj.GetComponent<RectTransform>();
            if (wheelRectTransform == null)
            {
                wheelRectTransform = wheelObj.AddComponent<RectTransform>();
            }
            // 中心锚点
            wheelRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            wheelRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            wheelRectTransform.pivot = new Vector2(0.5f, 0.5f);
            wheelRectTransform.sizeDelta = new Vector2(GRID_OFFSET * 3, GRID_OFFSET * 3);
            _wheelContainer = wheelRectTransform;

            // 验证关键组件
            if (_wheelContainer == null)
            {
                Debug.LogError("[ItemWheelSelector] 轮盘容器创建失败");
                return;
            }

            // 查找ItemDisplay模板
            FindItemDisplayTemplate();

            // 默认隐藏
            canvasObj.SetActive(false);
            _wheelActive = false;

            Debug.Log("[ItemWheelSelector] 九宫格轮盘初始化完成");
        }

        /// <summary>
        /// 从库存查找ItemDisplay模板
        /// </summary>
        private void FindItemDisplayTemplate()
        {
            // 在场景中查找任何ItemDisplay实例作为模板
            var itemDisplaysInScene = FindObjectsOfType<ItemDisplay>();

            if (itemDisplaysInScene != null && itemDisplaysInScene.Length > 0)
            {
                _itemDisplayTemplate = itemDisplaysInScene[0];
                Debug.Log("[ItemWheelSelector] 已找到ItemDisplay模板");
            }
            else
            {
                Debug.LogWarning("[ItemWheelSelector] 找不到ItemDisplay模板，轮盘可能显示不正常");
            }
        }

        // 鼠标可见性状态
        private bool _originalCursorVisible;

        /// <summary>
        /// 显示轮盘选择器
        /// </summary>
        public void ShowWheel(List<Item> items, int shortcutIndex, Vector2 pressDownPos, Vector2 wheelShowPos, ItemCategory category = ItemCategory.Medical)
        {
            Debug.Log($"[ItemWheelSelector] ═══════════════════════════════════════");
            Debug.Log($"[ItemWheelSelector] ShowWheel 被调用");
            Debug.Log($"[ItemWheelSelector] 快捷键索引: {shortcutIndex}，类别: {category}");
            Debug.Log($"[ItemWheelSelector] 接收物品数量: {items?.Count ?? 0}");
            if (items != null && items.Count > 0)
            {
                var itemNames = items.ConvertAll(item => item?.DisplayName ?? "null");
                Debug.Log($"[ItemWheelSelector] 物品列表: {string.Join(", ", itemNames)}");
            }
            Debug.Log($"[ItemWheelSelector] 按下时鼠标位置: {pressDownPos}");
            Debug.Log($"[ItemWheelSelector] 轮盘显示时鼠标位置: {wheelShowPos}");

            _currentCategory = category;

            // 重置拖拽标志
            _hasBeenDragged = false;

            // 如果没有找到ItemDisplay模板，重新查找一次
            if (_itemDisplayTemplate == null)
            {
                Debug.Log($"[ItemWheelSelector] ItemDisplay模板为null，重新查找...");
                FindItemDisplayTemplate();
            }

            // 如果仍然找不到模板，记录错误并返回
            if (_itemDisplayTemplate == null)
            {
                Debug.LogError("[ItemWheelSelector] 无法找到ItemDisplay模板，无法显示轮盘");
                Debug.Log($"[ItemWheelSelector] ═══════════════════════════════════════");
                return;
            }

            Debug.Log($"[ItemWheelSelector] ItemDisplay模板已找到");

            // 🔧 修复：保持轮盘布局稳定性，使用null占位符代替已使用的物品
            _currentItems = CreateStableLayout(items);
            Debug.Log($"[ItemWheelSelector] 稳定布局创建完成，物品数: {_currentItems.Count}");

            // 保存鼠标位置用于矢量选择
            _pressDownMousePos = pressDownPos;           // 按下时的鼠标位置
            _wheelShowMousePos = wheelShowPos;           // 轮盘显示时的鼠标位置

            // 重置hover状态
            _hoverIndex = -1;

            // 轮盘中心使用按下时的鼠标位置（而不是显示时的位置）
            _wheelCenterScreenPos = pressDownPos;

            // 🔧 优化：鼠标指针管理
            // 保存原始鼠标可见性状态
            _originalCursorVisible = Cursor.visible;
            // 显示系统鼠标指针，确保用户能看见
            Cursor.visible = true;

            // 激活轮盘
            _wheelCanvas.gameObject.SetActive(true);
            _wheelActive = true;

            // 设置轮盘中心位置（必须在激活后才能正确转换屏幕坐标）
            if (_wheelContainer != null)
            {
                _wheelContainer.position = _wheelCenterScreenPos;
            }
            else
            {
                Debug.LogError("[ItemWheelSelector] _wheelContainer 为null，无法设置位置");
                return;
            }

            Debug.Log($"[ItemWheelSelector] 轮盘中心位置已设置: {_wheelCenterScreenPos}");
            Debug.Log($"[ItemWheelSelector] 正在创建物品显示...");

            CreateItemDisplays();

            Debug.Log($"[ItemWheelSelector] ✓ 轮盘显示完成 @ {_wheelCenterScreenPos}，物品数: {_currentItems.Count}");
            Debug.Log($"[ItemWheelSelector] ═══════════════════════════════════════");
        }

        /// <summary>
        /// 重载方法，保持向后兼容
        /// </summary>
        public void ShowWheel(List<Item> items, int shortcutIndex)
        {
            ShowWheel(items, shortcutIndex, Input.mousePosition, Input.mousePosition);
        }

        /// <summary>
        /// 隐藏轮盘选择器
        /// 注意：布局已在 SwapItems 时实时保存，这里只负责清理UI
        /// </summary>
        public void HideWheel()
        {
            HideWheel(WheelCloseMode.UseHoveredItem);
        }

        /// <summary>
        /// 隐藏轮盘选择器（带模式参数）- 用于不同的关闭模式
        /// </summary>
        /// <param name="mode">关闭模式</param>
        public void HideWheel(WheelCloseMode mode)
        {
            Debug.Log($"[ItemWheelSelector] HideWheel 开始: mode={mode}");
            Debug.Log($"[ItemWheelSelector] 当前hover索引: {_hoverIndex}");
            Debug.Log($"[ItemWheelSelector] 当前物品数量: {_currentItems.Count}");

            // 根据模式决定是否同步选中状态
            bool shouldSyncSelection = (mode == WheelCloseMode.ChangeSelection);

            if (shouldSyncSelection)
            {
                Debug.Log($"[ItemWheelSelector] ChangeSelection模式：准备同步hover状态");
                // 左键点击模式：同步hover状态到WheelLayoutManager作为真实选中状态
                SyncHoverToSelection();
                Debug.Log($"[ItemWheelSelector] ChangeSelection模式：同步选中状态完成");
            }
            else
            {
                // UseHoveredItem模式：不改变选中状态，只使用hover的物品
                Debug.Log($"[ItemWheelSelector] UseHoveredItem模式：不改变选中状态");

                // 检查是否有有效的hover状态，只有hover到物品时才使用
                if (_hoverIndex >= 0 && _hoverIndex < _currentItems.Count && _currentItems[_hoverIndex] != null)
                {
                    var hoveredItem = _currentItems[_hoverIndex];
                    Debug.Log($"[ItemWheelSelector] 有hover状态，使用物品: {hoveredItem.DisplayName}");

                    // 调用物品使用逻辑
                    ItemUsageHandler.UseItem(hoveredItem, _currentCategory);
                }
                else
                {
                    Debug.Log($"[ItemWheelSelector] 没有hover状态，不使用任何物品");
                }
            }

            // 执行隐藏轮盘的标准流程（不包含SyncHoverToSelection）
            Cursor.visible = _originalCursorVisible;

            // 🔧 优化：清理任何正在进行的拖拽
            var dragManager = DragGhostManager.Instance;
            if (dragManager != null)
            {
                dragManager.ForceCleanup();
            }

            _wheelCanvas.gameObject.SetActive(false);
            _wheelActive = false;
            ClearItemDisplays();

            Debug.Log($"[ItemWheelSelector] 隐藏轮盘完成 (模式: {mode})");
        }

        /// <summary>
        /// 获取当前hover索引 - 用于调试
        /// </summary>
        public int GetHoverIndex()
        {
            return _hoverIndex;
        }

        /// <summary>
        /// 检查轮盘是否当前活跃显示
        /// </summary>
        public bool IsWheelActive()
        {
            return _wheelActive;
        }

        /// <summary>
        /// 创建物品显示 - 克隆官方ItemDisplay
        /// </summary>
        private void CreateItemDisplays()
        {
            Debug.Log($"[ItemWheelSelector] CreateItemDisplays 开始创建物品显示");
            ClearItemDisplays();

            if (_itemDisplayTemplate == null)
            {
                Debug.LogError("[ItemWheelSelector] ItemDisplay模板未找到，无法创建轮盘");
                return;
            }

            Debug.Log($"[ItemWheelSelector] 当前物品数: {_currentItems.Count}");

            // 🔧 创建所有8个格子（使用新的1-8索引映射）
            for (int i = 0; i < 8; i++)
            {
                Item itemToDisplay = (i < _currentItems.Count) ? _currentItems[i] : null;
                int gridIndex = i + 1; // 新布局使用1-8索引

                if (itemToDisplay != null)
                {
                    Debug.Log($"[ItemWheelSelector] 格子 {i+1} (位置 {GRID_POSITIONS[gridIndex]}): 创建 '{itemToDisplay.DisplayName}'");
                }
                else
                {
                    Debug.Log($"[ItemWheelSelector] 格子 {i+1} (位置 {GRID_POSITIONS[gridIndex]}): 空格子");
                }

                var displayClone = CreateWheelItemDisplay(i, GRID_POSITIONS[gridIndex], itemToDisplay);
                if (displayClone != null)
                {
                    _itemDisplayClones.Add(displayClone);

                    // 如果有物品，记录显示组件
                    if (itemToDisplay != null)
                    {
                        var itemDisplay = displayClone.GetComponent<ItemDisplay>();
                        if (itemDisplay != null)
                        {
                            _itemDisplayComponents.Add(itemDisplay);
                            Debug.Log($"[ItemWheelSelector] 格子 {i} ItemDisplay组件已记录: {itemToDisplay.DisplayName}");
                        }
                    }
                }
            }

            Debug.Log($"[ItemWheelSelector] ✓ 创建完成：克隆了8个ItemDisplay，其中 {_itemDisplayComponents.Count} 个有物品");
        }

        /// <summary>
        /// 克隆一个ItemDisplay并放置到九宫格位置
        /// </summary>
        private GameObject CreateWheelItemDisplay(int cellIndex, Vector2Int gridPos, Item item)
        {
            // 创建格子
            var cellObj = new GameObject($"WheelItem_{cellIndex}");
            cellObj.transform.SetParent(_wheelContainer, false);

            var rectTransform = cellObj.AddComponent<RectTransform>();
            Vector2 localPos = new Vector2(gridPos.x * GRID_OFFSET, gridPos.y * GRID_OFFSET);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = localPos;
            rectTransform.sizeDelta = new Vector2(CELL_SIZE, CELL_SIZE);

            // 添加自定义UI组件
            var wheelDisplay = cellObj.AddComponent<WheelItemDisplay>();
            wheelDisplay.Initialize(item, cellIndex, this);  // 传入索引和轮盘选择器引用

            return cellObj;
        }

        /// <summary>
        /// 清空物品显示
        /// </summary>
        private void ClearItemDisplays()
        {
            foreach (var displayClone in _itemDisplayClones)
            {
                if (displayClone != null)
                {
                    Destroy(displayClone);
                }
            }
            _itemDisplayClones.Clear();
            _itemDisplayComponents.Clear();
        }

        private void Update()
        {
            if (!_wheelActive) return;

            // 根据当前鼠标位置和轮盘中心（按下时的位置）来更新选择
            UpdateSelectionByCurrentMouse();
        }

        /// <summary>
        /// 根据当前鼠标位置更新选择
        /// 轮盘中心 = 按下时的鼠标位置（固定不变）
        /// 矢量 = 当前鼠标位置 - 轮盘中心位置
        /// </summary>
        private void UpdateSelectionByCurrentMouse()
        {
            Vector2 currentMousePos = Input.mousePosition;
            Vector2 currentVector = currentMousePos - _wheelCenterScreenPos;
            float vectorMagnitude = currentVector.magnitude;

            // 检查矢量长度是否超过阈值
            if (vectorMagnitude < FIRST_VECTOR_THRESHOLD)
            {
                // 在死区内，清除悬停（如果之前有悬停的话）
                if (_hoverIndex >= 0)
                {
                    ClearHover();
                    Debug.Log($"[ItemWheelSelector] 进入死区（{vectorMagnitude:F1}px < {FIRST_VECTOR_THRESHOLD}px），清除悬停");
                }
                return;
            }

            // 矢量长度足够，根据当前矢量方向选中物品
            SelectByVector(currentVector, "当前矢量");
        }

        /// <summary>
        /// 根据矢量方向选择物品
        /// </summary>
        private void SelectByVector(Vector2 vector, string vectorName)
        {
            if (_itemDisplayClones.Count == 0) return;

            // 如果矢量长度为0，不做处理
            if (vector.magnitude < 0.1f)
            {
                return;
            }

            Vector2 direction = vector.normalized;
            float vectorAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            if (vectorAngle < 0) vectorAngle += 360f;

            // 找到最接近的物品
            int closestIndex = 0;
            float closestAngleDiff = 360f;

            // 🛡️ 修复：始终检查所有8个位置，即使某些位置是空的
            // 这样可以确保轮盘360度都有响应，不会有"死角"
            int maxSlots = Mathf.Min(8, _itemDisplayClones.Count, DIRECTION_ANGLES.Length - 1);
            for (int i = 0; i < maxSlots; i++)
            {
                float cellAngle = DIRECTION_ANGLES[i + 1]; // 新布局使用1-8索引
                float angleDiff = Mathf.Abs(vectorAngle - cellAngle);

                // 处理跨越0度的情况
                if (angleDiff > 180f)
                {
                    angleDiff = 360f - angleDiff;
                }

                if (angleDiff < closestAngleDiff)
                {
                    closestAngleDiff = angleDiff;
                    closestIndex = i;
                }
            }

            // 🔧 修复：如果选中的位置是null，则清除悬停
            if (closestIndex < _currentItems.Count && _currentItems[closestIndex] == null)
            {
                if (_hoverIndex >= 0)
                {
                    Debug.Log($"[ItemWheelSelector] {vectorName}指向空位置{closestIndex}，清除悬停");
                    ClearHover();
                }
                return;
            }

            // 更新悬停
            if (closestIndex != _hoverIndex)
            {
                _hoverIndex = closestIndex;
                UpdateHover();

                if (closestIndex < _currentItems.Count && _currentItems[closestIndex] != null)
                {
                    Debug.Log($"[ItemWheelSelector] {vectorName}悬停 - 索引{closestIndex}：{_currentItems[closestIndex].DisplayName}，矢量长度{vector.magnitude:F1}");
                }
            }
        }

        /// <summary>
        /// 创建稳定的轮盘布局，使用null占位符保持布局不变
        /// 🔧 修复：统一使用WheelLayoutManager的布局系统
        /// </summary>
        private List<Item> CreateStableLayout(List<Item> currentItems)
        {
            // 最多支持8个物品
            if (currentItems.Count > 8)
            {
                Debug.LogWarning($"[ItemWheelSelector] 物品数超过8个（{currentItems.Count}），只显示前8个");
                currentItems = currentItems.GetRange(0, 8);
            }

            // 🔧 修复：通过BackpackShortcutManager访问WheelLayoutManager
            // 移除ItemWheelSelector的独立布局逻辑，统一使用WheelLayoutManager
            var backpackManager = BackpackShortcutManager.Instance;
            if (backpackManager != null)
            {
                // 使用反射获取私有字段_wheelLayoutManager
                var wheelLayoutManagerField = typeof(BackpackShortcutManager).GetField("_wheelLayoutManager",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (wheelLayoutManagerField != null)
                {
                    var wheelLayoutManager = wheelLayoutManagerField.GetValue(backpackManager) as WheelLayoutManager;
                    if (wheelLayoutManager != null)
                    {
                        var layoutForUI = wheelLayoutManager.GetLayoutForUI(_currentCategory);
                        if (layoutForUI != null && layoutForUI.Count > 0)
                        {
                            Debug.Log($"[ItemWheelSelector] 使用WheelLayoutManager提供的UI布局，包含{layoutForUI.Count}个位置");

                            // 确保布局长度不超过8个
                            var result = new List<Item>(layoutForUI);
                            while (result.Count > 8)
                            {
                                result.RemoveAt(8);
                            }

                            return result;
                        }
                    }
                }
            }

            // 回退方案：直接使用当前物品
            Debug.Log($"[ItemWheelSelector] WheelLayoutManager未提供布局，使用当前物品列表");
            return new List<Item>(currentItems);
        }

        /// <summary>
        /// 更新悬停显示（放大效果）
        /// </summary>
        private void UpdateHover()
        {
            // 🛡️ 边界检查：确保_hoverIndex在有效范围内
            bool isValidHoverIndex = _hoverIndex >= 0 && _hoverIndex < _itemDisplayClones.Count;

            // 清除所有格子的悬停状态，然后设置当前悬停格子
            for (int i = 0; i < _itemDisplayClones.Count; i++)
            {
                var display = _itemDisplayClones[i].GetComponent<WheelItemDisplay>();
                if (display != null)
                {
                    // 只有被悬停的格子才显示放大效果，加上边界检查
                    display.SetSelected(isValidHoverIndex && i == _hoverIndex);
                }
            }
        }

        /// <summary>
        /// 清除悬停（回到死区时调用）
        /// </summary>
        private void ClearHover()
        {
            if (_hoverIndex >= 0)
            {
                Debug.Log($"[ItemWheelSelector] 清除悬停：之前悬停索引 {_hoverIndex}");
                _hoverIndex = -1;
                UpdateHover(); // 更新所有格子的视觉状态
            }
        }

        /// <summary>
        /// 同步hover状态到WheelLayoutManager作为真实选中状态
        /// 在轮盘关闭时调用
        /// </summary>
        private void SyncHoverToSelection()
        {
            Debug.Log($"[ItemWheelSelector] SyncHoverToSelection 开始");
            Debug.Log($"[ItemWheelSelector] _hoverIndex: {_hoverIndex}");
            Debug.Log($"[ItemWheelSelector] _currentItems.Count: {_currentItems.Count}");
            Debug.Log($"[ItemWheelSelector] _wheelLayoutManager是否为null: {_wheelLayoutManager == null}");

            if (_hoverIndex >= 0 && _hoverIndex < _currentItems.Count && _currentItems[_hoverIndex] != null)
            {
                // 有hover状态，同步到WheelLayoutManager
                if (_wheelLayoutManager != null)
                {
                    var itemToSelect = _currentItems[_hoverIndex];
                    Debug.Log($"[ItemWheelSelector] 准备同步: 类别{_currentCategory}, 索引{_hoverIndex}, 物品{itemToSelect.DisplayName}");

                    _wheelLayoutManager.SetSelectedSlot(_currentCategory, _hoverIndex);
                    Debug.Log($"[ItemWheelSelector] 同步hover到选中完成: 类别{_currentCategory}, 索引{_hoverIndex}, 物品{itemToSelect.DisplayName}");
                }
                else
                {
                    Debug.LogError("[ItemWheelSelector] WheelLayoutManager为null，无法同步选中状态");
                }
            }
            else
            {
                // 没有hover状态，不同步
                Debug.Log($"[ItemWheelSelector] 没有有效的hover状态，不同步到选中");
                if (_hoverIndex < 0)
                    Debug.Log($"[ItemWheelSelector] 原因: _hoverIndex < 0 ({_hoverIndex})");
                else if (_hoverIndex >= _currentItems.Count)
                    Debug.Log($"[ItemWheelSelector] 原因: _hoverIndex >= _currentItems.Count ({_hoverIndex} >= {_currentItems.Count})");
                else if (_currentItems[_hoverIndex] == null)
                    Debug.Log($"[ItemWheelSelector] 原因: _currentItems[_hoverIndex] 为null");
            }
        }

        /// <summary>
        /// 交换两个格子中的物品（用于拖动调整）
        /// 🆕 适配新架构：直接更新WheelLayoutManager的SimpleWheelSlot数组
        /// </summary>
        public void SwapItems(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= _itemDisplayClones.Count ||
                toIndex < 0 || toIndex >= _itemDisplayClones.Count)
            {
                Debug.LogWarning($"[ItemWheelSelector] 交换索引无效: from={fromIndex}, to={toIndex}");
                return;
            }

            // 标记轮盘已发生拖拽
            _hasBeenDragged = true;
            Debug.Log($"[ItemWheelSelector] 轮盘拖拽标志已设置");

            // 确保 _currentItems 列表足够大
            while (_currentItems.Count <= Mathf.Max(fromIndex, toIndex))
            {
                _currentItems.Add(null);
            }

            // 交换物品列表中的项
            Item tempItem = _currentItems[fromIndex];
            _currentItems[fromIndex] = _currentItems[toIndex];
            _currentItems[toIndex] = tempItem;

            // 更新对应的显示组件
            var fromDisplay = _itemDisplayClones[fromIndex].GetComponent<WheelItemDisplay>();
            var toDisplay = _itemDisplayClones[toIndex].GetComponent<WheelItemDisplay>();

            if (fromDisplay != null && toDisplay != null)
            {
                fromDisplay.SetItem(_currentItems[fromIndex]);
                toDisplay.SetItem(_currentItems[toIndex]);

                Debug.Log($"[ItemWheelSelector] 物品交换完成: 索引 {fromIndex} <-> {toIndex}");

                // 🆕 新架构：直接通知WheelLayoutManager进行槽位交换
                if (_wheelLayoutManager != null)
                {
                    _wheelLayoutManager.SwapSlots(_currentCategory, fromIndex, toIndex);
                    Debug.Log($"[ItemWheelSelector] 槽位交换已通知WheelLayoutManager: 类别{_currentCategory}, 索引{fromIndex}<->{toIndex}");
                }
                else
                {
                    Debug.LogWarning($"[ItemWheelSelector] WheelLayoutManager为null，无法交换槽位");
                }
            }
        }

      
        /// <summary>
        /// 检查轮盘显示期间是否发生过拖拽
        /// </summary>
        public bool HasBeenDragged()
        {
            return _hasBeenDragged;
        }
    }
}
