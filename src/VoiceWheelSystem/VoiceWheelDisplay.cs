using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Backpack_QuickWheel.AttachmentSystem;

namespace Backpack_QuickWheel.VoiceWheelSystem
{
    /// <summary>
    /// 语音轮盘格子显示组件
    /// 负责单个语音格子的UI显示和hover动画效果
    /// 使用嵌入式格子图片作为背景，支持有语音和无语音两种状态
    /// </summary>
    public class VoiceWheelDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private VoiceItem _voice;
        private Image _bgImage;
        private Text _text;
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
        private readonly Color HOVER_COLOR = new Color(1f, 1f, 1f, 1f);

        /// <summary>
        /// 初始化语音格子（可以有语音，也可以没有语音）
        /// </summary>
        public void Initialize(VoiceItem voice = null, int cellIndex = -1)
        {
            _voice = voice;
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
                Debug.Log("[VoiceWheelDisplay] 已加载嵌入式格子背景图片");
            }
            else
            {
                Debug.LogWarning("[VoiceWheelDisplay] 未能加载格子背景图片，使用默认颜色");
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

            // 如果有语音，添加文本
            if (_voice != null)
            {
                CreateVoiceText();
            }
        }

        private void CreateVoiceText()
        {
            // 创建文本对象
            var textObj = new GameObject("VoiceText");
            textObj.transform.SetParent(transform, false);

            _text = textObj.AddComponent<Text>();
            _text.text = _voice.displayName;
            _text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _text.fontSize = 30;
            _text.fontStyle = FontStyle.Bold;
            _text.color = Color.white;
            _text.alignment = TextAnchor.MiddleCenter;
            _text.raycastTarget = false;

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8, 8);
            textRect.offsetMax = new Vector2(-8, -8);

            Debug.Log($"[VoiceWheelDisplay] 创建语音文本: {_voice.displayName}");
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
            if (_isHovering != true)
            {
                _isHovering = true;
                _isAnimating = true;
                _animationProgress = 0f;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isHovering != false)
            {
                _isHovering = false;
                _isAnimating = true;
                _animationProgress = 0f;
            }
        }

        private void Update()
        {
            if (!_isAnimating) return;

            _animationProgress += Time.deltaTime / ANIMATION_DURATION;
            _animationProgress = Mathf.Clamp01(_animationProgress);

            // 确定目标缩放：选中 > hover > 普通
            float targetScale;
            if (_isSelected)
            {
                targetScale = HOVER_SCALE * 1.1f; // 选中时稍微更大一点
            }
            else if (_isHovering)
            {
                targetScale = HOVER_SCALE;
            }
            else
            {
                targetScale = NORMAL_SCALE;
            }

            // 确定起始缩放
            float startScale;
            if (_isHovering || _isSelected)
            {
                startScale = NORMAL_SCALE;
            }
            else
            {
                startScale = HOVER_SCALE;
            }

            float currentScale = Mathf.Lerp(startScale, targetScale, _animationProgress);
            _rectTransform.localScale = new Vector3(currentScale, currentScale, 1f);

            // 更新背景颜色亮度
            float colorIntensity = 1f;
            if (_isSelected)
            {
                colorIntensity = 1.3f; // 选中时更亮
            }
            else if (_isHovering)
            {
                colorIntensity = 1.2f; // hover 时略微变亮
            }

            Color bgColor = Color.Lerp(NORMAL_COLOR, new Color(colorIntensity, colorIntensity, colorIntensity, 1f), _animationProgress * 0.3f);
            _bgImage.color = bgColor;

            // 如果有文本，也略微提亮
            if (_text != null)
            {
                Color textColor = Color.Lerp(Color.white, new Color(colorIntensity * 0.9f, colorIntensity * 0.9f, colorIntensity * 0.9f, 1f), _animationProgress * 0.2f);
                _text.color = textColor;
            }

            if (_animationProgress >= 1f)
            {
                _isAnimating = false;
            }
        }

        public VoiceItem GetVoice() => _voice;

        /// <summary>
        /// 设置格子中的语音（用于更新显示）
        /// </summary>
        public void SetVoice(VoiceItem voice)
        {
            _voice = voice;

            // 清除旧的文本
            if (_text != null)
            {
                Destroy(_text.gameObject);
                _text = null;
            }

            // 如果有新语音，创建新的文本
            if (_voice != null)
            {
                CreateVoiceText();
                Debug.Log($"[VoiceWheelDisplay] 格子的语音已更新为 '{_voice.displayName}'");
            }
            else
            {
                Debug.Log($"[VoiceWheelDisplay] 格子的语音已清空");
            }
        }
    }
}