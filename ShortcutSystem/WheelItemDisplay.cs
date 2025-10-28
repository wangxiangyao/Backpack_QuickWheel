using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ItemStatsSystem;
using Great_backpack.AttachmentSystem;

namespace Great_backpack.ShortcutSystem
{
    /// <summary>
    /// 轮盘格子显示组件
    /// 负责单个格子的UI显示和hover动画效果
    /// 使用嵌入式格子图片作为背景，支持有物品和无物品两种状态
    /// </summary>
    public class WheelItemDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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

        /// <summary>
        /// 初始化格子（可以有物品，也可以没有物品）
        /// </summary>
        public void Initialize(Item item = null)
        {
            _item = item;
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
    }
}
