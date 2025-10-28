using UnityEngine;
using UnityEngine.UI;
using ItemStatsSystem;
using System.Collections.Generic;
using Duckov.UI;

namespace Great_backpack.ShortcutSystem
{
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
        private int _selectedItemIndex = -1;
        private List<Item> _currentItems = new List<Item>();

        // 轮盘位置
        private Vector2 _wheelCenterScreenPos;
        private bool _wheelActive = false;

        // ItemDisplay 模板（从库存克隆）
        private ItemDisplay _itemDisplayTemplate;

        // 矢量选择逻辑
        private Vector2 _pressDownMousePos = Vector2.zero;       // 按下时的鼠标位置
        private Vector2 _wheelShowMousePos = Vector2.zero;       // 轮盘显示时的鼠标位置
        private const float FIRST_VECTOR_THRESHOLD = 20f;        // 第一矢量的激活阈值（死区）

        // 九宫格配置
        private const float CELL_SIZE = 40f;                        // 格子大小（宽高）
        private const float GRID_OFFSET = CELL_SIZE + 5f;           // 格子间距（包括间距）

        // 九宫格位置（相对于中心的偏移）
        // 索引映射：0-7 对应 8 个位置（中心跳过）
        // [0] [1] [2]
        // [3] [ ] [4]
        // [5] [6] [7]
        private static readonly Vector2Int[] GRID_POSITIONS = new Vector2Int[]
        {
            new Vector2Int(-1, -1),  // 0: 左上
            new Vector2Int( 0, -1),  // 1: 上
            new Vector2Int( 1, -1),  // 2: 右上
            new Vector2Int(-1,  0),  // 3: 左
            new Vector2Int( 1,  0),  // 4: 右
            new Vector2Int(-1,  1),  // 5: 左下
            new Vector2Int( 0,  1),  // 6: 下
            new Vector2Int( 1,  1),  // 7: 右下
        };

        // 角度映射（对应8个方向）
        private static readonly float[] DIRECTION_ANGLES = new float[]
        {
            225f,  // 0: 左上
            270f,  // 1: 上
            315f,  // 2: 右上
            180f,  // 3: 左
            0f,    // 4: 右
            135f,  // 5: 左下
            90f,   // 6: 下
            45f,   // 7: 右下
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
            Debug.Log("[ItemWheelSelector] 初始化九宫格轮盘（中心为空）...");

            // 创建Canvas
            var canvasObj = new GameObject("ItemWheelCanvas");
            canvasObj.transform.SetParent(transform, false);
            canvasObj.transform.localPosition = Vector3.zero;

            Debug.Log($"[ItemWheelSelector] Canvas 父节点: {canvasObj.transform.parent.name}, Canvas 活跃: {canvasObj.activeInHierarchy}");

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

            canvasObj.AddComponent<GraphicRaycaster>();

            var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

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

        /// <summary>
        /// 显示轮盘选择器
        /// </summary>
        public void ShowWheel(List<Item> items, int shortcutIndex, Vector2 pressDownPos, Vector2 wheelShowPos)
        {
            // 如果没有找到ItemDisplay模板，重新查找一次
            if (_itemDisplayTemplate == null)
            {
                FindItemDisplayTemplate();
            }

            // 如果仍然找不到模板，记录错误并返回
            if (_itemDisplayTemplate == null)
            {
                Debug.LogError("[ItemWheelSelector] 无法找到ItemDisplay模板，无法显示轮盘");
                return;
            }

            // 最多支持8个物品
            if (items.Count > 8)
            {
                Debug.LogWarning($"[ItemWheelSelector] 物品数超过8个（{items.Count}），只显示前8个");
                _currentItems = new List<Item>(items.GetRange(0, 8));
            }
            else
            {
                _currentItems = new List<Item>(items);
            }

            // 保存鼠标位置用于矢量选择
            _pressDownMousePos = pressDownPos;           // 按下时的鼠标位置
            _wheelShowMousePos = wheelShowPos;           // 轮盘显示时的鼠标位置

            _selectedItemIndex = -1;

            // 轮盘中心使用按下时的鼠标位置（而不是显示时的位置）
            _wheelCenterScreenPos = pressDownPos;

            // 激活轮盘
            _wheelCanvas.gameObject.SetActive(true);
            _wheelActive = true;

            // 设置轮盘中心位置（必须在激活后才能正确转换屏幕坐标）
            _wheelContainer.position = _wheelCenterScreenPos;

            CreateItemDisplays();

            Debug.Log($"[ItemWheelSelector] 显示轮盘 @ {_wheelCenterScreenPos}，物品数: {_currentItems.Count}");
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
        /// </summary>
        public void HideWheel()
        {
            _wheelCanvas.gameObject.SetActive(false);
            _wheelActive = false;
            ClearItemDisplays();

            Debug.Log("[ItemWheelSelector] 隐藏轮盘");
        }

        /// <summary>
        /// 创建物品显示 - 克隆官方ItemDisplay
        /// </summary>
        private void CreateItemDisplays()
        {
            ClearItemDisplays();

            if (_itemDisplayTemplate == null)
            {
                Debug.LogError("[ItemWheelSelector] ItemDisplay模板未找到，无法创建轮盘");
                return;
            }

            // 创建所有8个格子
            for (int i = 0; i < 8; i++)
            {
                Item itemToDisplay = (i < _currentItems.Count) ? _currentItems[i] : null;
                var displayClone = CreateWheelItemDisplay(i, GRID_POSITIONS[i], itemToDisplay);
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
                        }
                    }
                }
            }

            Debug.Log($"[ItemWheelSelector] 克隆了8个ItemDisplay，其中 {_itemDisplayComponents.Count} 个有物品");
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
            wheelDisplay.Initialize(item);  // item 可以为 null，显示空格子

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

            // 检查矢量长度是否超过阈值
            if (currentVector.magnitude < FIRST_VECTOR_THRESHOLD)
            {
                // 矢量长度不足，不做选择
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

            for (int i = 0; i < _itemDisplayClones.Count && i < DIRECTION_ANGLES.Length; i++)
            {
                float cellAngle = DIRECTION_ANGLES[i];
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

            // 更新选择
            if (closestIndex != _selectedItemIndex)
            {
                _selectedItemIndex = closestIndex;
                UpdateSelection();

                if (closestIndex < _currentItems.Count && _currentItems[closestIndex] != null)
                {
                    Debug.Log($"[ItemWheelSelector] {vectorName}选择 - 索引{closestIndex}：{_currentItems[closestIndex].DisplayName}，矢量长度{vector.magnitude:F1}");
                }
            }
        }

        /// <summary>
        /// 更新选中显示（聚焦效果）
        /// 根据矢量选择的结果，更新对应格子的选中状态
        /// </summary>
        private void UpdateSelection()
        {
            // 清除所有格子的选中状态，然后设置当前选中格子
            for (int i = 0; i < _itemDisplayClones.Count; i++)
            {
                var display = _itemDisplayClones[i].GetComponent<WheelItemDisplay>();
                if (display != null)
                {
                    // 只有被选中的格子才显示聚焦效果
                    display.SetSelected(i == _selectedItemIndex);
                }
            }
        }

        /// <summary>
        /// 获取当前选中的物品
        /// </summary>
        public Item GetSelectedItem()
        {
            if (_selectedItemIndex >= 0 && _selectedItemIndex < _currentItems.Count)
            {
                return _currentItems[_selectedItemIndex];
            }
            return null;
        }
    }
}
