using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ItemStatsSystem;
using Great_backpack.AttachmentSystem;
using System.Collections.Generic;

namespace Great_backpack.ShortcutSystem
{
    /// <summary>
    /// 轮盘格子显示组件
    /// 负责单个格子的UI显示和hover动画效果
    /// 使用嵌入式格子图片作为背景，支持有物品和无物品两种状态
    /// 支持拖动调整物品在轮盘上的位置
    /// </summary>
    public class WheelItemDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private Item _item;
        private Image _bgImage;
        private Image _iconImage;
        private RectTransform _rectTransform;
        private Button _button;

        // 动画参数
        private const float NORMAL_SCALE = 1f;
        private const float HOVER_SCALE = 1.15f;
        private const float ANIMATION_DURATION = 0.1f;

        private float _animationProgress = 0f;
        private bool _isAnimating = false;
        private bool _isHovering = false;
        private bool _isSelected = false;  // 是否被轮盘选中（通过矢量选择）

        // 颜色
        private readonly Color NORMAL_COLOR = Color.white;
        private readonly Color HOVER_COLOR = new Color(1f, 1f, 1f, 1f);  // 保持白色，但可能有其他视觉效果

        // 拖动相关
        private bool _isDragging = false;
        private int _cellIndex = -1;  // 当前格子的索引
        private ItemWheelSelector _wheelSelector;  // 轮盘选择器引用，用于交换物品
        private const float DRAG_ALPHA = 0.7f;  // 拖动时的透明度

        /// <summary>
        /// 初始化格子（可以有物品，也可以没有物品）
        /// </summary>
        public void Initialize(Item item = null, int cellIndex = -1, ItemWheelSelector wheelSelector = null)
        {
            _item = item;
            _cellIndex = cellIndex;
            _wheelSelector = wheelSelector;
            CreateDisplay();
        }

        private void CreateDisplay()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null)
            {
                _rectTransform = gameObject.AddComponent<RectTransform>();
            }

            // 创建背景Image（显示格子图片）
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(transform, false);

            var bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // 背景Image
            _bgImage = bgObj.AddComponent<Image>();
            _bgImage.color = NORMAL_COLOR;
            _bgImage.raycastTarget = true;

            // 从嵌入资源加载格子图片
            var gridSprite = ResourceLoader.LoadEmbeddedSprite("Textures.grid_bg.png");
            if (gridSprite != null)
            {
                _bgImage.sprite = gridSprite;
                Debug.Log("[WheelItemDisplay] 已加载嵌入式格子背景图片");
            }
            else
            {
                Debug.LogWarning("[WheelItemDisplay] 未能加载格子背景图片，使用默认颜色");
                _bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            }

            // 添加Button组件用于交互
            _button = bgObj.AddComponent<Button>();
            _button.interactable = true;
            _button.targetGraphic = _bgImage;
            _button.onClick.AddListener(() => { });

            // 添加EventTrigger用于hover效果
            var eventTrigger = bgObj.AddComponent<EventTrigger>();

            var enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => OnPointerEnter((PointerEventData)data));
            eventTrigger.triggers.Add(enterEntry);

            var exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) => OnPointerExit((PointerEventData)data));
            eventTrigger.triggers.Add(exitEntry);

            // 添加DragHandler来处理拖动（关键！）
            var dragHandler = bgObj.AddComponent<EventTrigger>();
            var beginDragEntry = new EventTrigger.Entry();
            beginDragEntry.eventID = EventTriggerType.BeginDrag;
            beginDragEntry.callback.AddListener((data) => OnBeginDrag((PointerEventData)data));
            dragHandler.triggers.Add(beginDragEntry);

            var dragEntry = new EventTrigger.Entry();
            dragEntry.eventID = EventTriggerType.Drag;
            dragEntry.callback.AddListener((data) => OnDrag((PointerEventData)data));
            dragHandler.triggers.Add(dragEntry);

            var endDragEntry = new EventTrigger.Entry();
            endDragEntry.eventID = EventTriggerType.EndDrag;
            endDragEntry.callback.AddListener((data) => OnEndDrag((PointerEventData)data));
            dragHandler.triggers.Add(endDragEntry);

            // 如果有物品，添加图标
            if (_item != null)
            {
                CreateItemIcon();
            }
        }

        private void CreateItemIcon()
        {
            // 创建图标对象
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(transform, false);

            _iconImage = iconObj.AddComponent<Image>();
            _iconImage.sprite = _item.Icon;
            _iconImage.color = Color.white;
            _iconImage.raycastTarget = false;

            var iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(2, 2);
            iconRect.offsetMax = new Vector2(-2, -2);
        }

        /// <summary>
        /// 设置格子是否被选中（通过轮盘的矢量选择）
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (_isSelected != selected)
            {
                _isSelected = selected;
                _isAnimating = true;
                _animationProgress = 0f;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Hover 事件不再触发聚焦效果，只有轮盘选择才会
            _isHovering = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovering = false;
        }

        /// <summary>
        /// 开始拖动
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            // 只有有物品的格子才能拖动
            if (_item == null)
            {
                return;
            }

            _isDragging = true;
            Debug.Log($"[WheelItemDisplay] 开始拖动物品 '{_item.DisplayName}' (索引 {_cellIndex})");

            // 拖动时降低透明度，显示被拖起的效果
            if (_bgImage != null)
            {
                Color dragColor = _bgImage.color;
                dragColor.a = DRAG_ALPHA;
                _bgImage.color = dragColor;
            }

            // 消费事件，防止传递给游戏
            eventData.Use();
        }

        /// <summary>
        /// 拖动中（保持拖动状态，不处理选择）
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;

            // 可以在这里添加视觉反馈，比如跟随鼠标的拖动物品图标
            // 暂时不做额外处理

            // 消费事件
            eventData.Use();
        }

        /// <summary>
        /// 结束拖动
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;

            _isDragging = false;
            Debug.Log($"[WheelItemDisplay] 结束拖动物品 '{_item.DisplayName}' (索引 {_cellIndex})");

            // 恢复透明度
            if (_bgImage != null)
            {
                _bgImage.color = NORMAL_COLOR;
            }

            // 找到释放时鼠标指向的格子
            if (_wheelSelector != null)
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current);
                pointerData.position = Input.mousePosition;

                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);

                Debug.Log($"[WheelItemDisplay] OnEndDrag raycast 结果数: {results.Count}");

                // 寻找轮盘格子（优先查找 WheelItemDisplay 或其子物体）
                foreach (RaycastResult result in results)
                {
                    Debug.Log($"[WheelItemDisplay] Raycast 击中: {result.gameObject.name}");

                    // 方法1: 直接找 WheelItemDisplay
                    var targetDisplay = result.gameObject.GetComponent<WheelItemDisplay>();
                    if (targetDisplay != null && targetDisplay._cellIndex >= 0 && targetDisplay._cellIndex != _cellIndex)
                    {
                        Debug.Log($"[WheelItemDisplay] ✓ 拖动完成: 从索引 {_cellIndex} 拖到索引 {targetDisplay._cellIndex}");
                        _wheelSelector.SwapItems(_cellIndex, targetDisplay._cellIndex);
                        eventData.Use();
                        return;
                    }

                    // 方法2: 检查父物体是否是 WheelItemDisplay
                    var parentDisplay = result.gameObject.GetComponentInParent<WheelItemDisplay>();
                    if (parentDisplay != null && parentDisplay._cellIndex >= 0 && parentDisplay._cellIndex != _cellIndex)
                    {
                        Debug.Log($"[WheelItemDisplay] ✓ 拖动完成（通过父级）: 从索引 {_cellIndex} 拖到索引 {parentDisplay._cellIndex}");
                        _wheelSelector.SwapItems(_cellIndex, parentDisplay._cellIndex);
                        eventData.Use();
                        return;
                    }
                }

                Debug.Log($"[WheelItemDisplay] ✗ 未找到有效的目标格子，拖动取消");
            }

            // 消费事件
            eventData.Use();
        }

        private void Update()
        {
            if (!_isAnimating) return;

            _animationProgress += Time.deltaTime / ANIMATION_DURATION;
            _animationProgress = Mathf.Clamp01(_animationProgress);

            // 聚焦效果根据 _isSelected 而不是 _isHovering
            float targetScale = _isSelected ? HOVER_SCALE : NORMAL_SCALE;
            float currentScale = Mathf.Lerp(
                _isSelected ? NORMAL_SCALE : HOVER_SCALE,
                targetScale,
                _animationProgress
            );

            _rectTransform.localScale = new Vector3(currentScale, currentScale, 1f);

            // 更新背景颜色亮度（被选中时略微变亮）
            float colorLerp = _animationProgress;
            if (!_isSelected)
            {
                colorLerp = 1f - colorLerp;
            }
            Color bgColor = Color.Lerp(NORMAL_COLOR, new Color(1.2f, 1.2f, 1.2f, 1f), colorLerp * 0.2f);
            _bgImage.color = bgColor;

            // 如果有icon，也略微提亮
            if (_iconImage != null)
            {
                Color iconColor = Color.Lerp(Color.white, new Color(1.1f, 1.1f, 1.1f, 1f), colorLerp * 0.15f);
                _iconImage.color = iconColor;
            }

            if (_animationProgress >= 1f)
            {
                _isAnimating = false;
            }
        }

        public Item GetItem() => _item;

        /// <summary>
        /// 设置格子中的物品（用于交换时更新显示）
        /// </summary>
        public void SetItem(Item item)
        {
            _item = item;

            // 清除旧的icon
            if (_iconImage != null)
            {
                Destroy(_iconImage.gameObject);
                _iconImage = null;
            }

            // 如果有新物品，创建新的icon
            if (_item != null)
            {
                CreateItemIcon();
                Debug.Log($"[WheelItemDisplay] 格子 {_cellIndex} 的物品已更新为 '{_item.DisplayName}'");
            }
            else
            {
                Debug.Log($"[WheelItemDisplay] 格子 {_cellIndex} 的物品已清空");
            }
        }

        /// <summary>
        /// 获取格子的索引
        /// </summary>
        public int GetCellIndex() => _cellIndex;
    }
}
