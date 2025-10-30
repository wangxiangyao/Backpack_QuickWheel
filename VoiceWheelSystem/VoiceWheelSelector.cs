using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using VoiceWheelSystem;

namespace VoiceWheelSystem
{
    /// <summary>
    /// 语音轮盘选择器UI组件
    /// 复用物品轮盘的九宫格布局和矢量选择逻辑
    /// </summary>
    public class VoiceWheelSelector : MonoBehaviour
    {
        #region UI组件
        private Canvas _wheelCanvas;
        private RectTransform _wheelContainer;
        private List<GameObject> _voiceDisplayObjects = new List<GameObject>();
        private List<VoiceItemDisplay> _voiceDisplayComponents = new List<VoiceItemDisplay>();
        private int _selectedVoiceIndex = -1;
        private List<VoiceItem> _currentVoices = new List<VoiceItem>();

        // 全屏拦截面板（防止鼠标输入传给游戏）
        private GameObject _inputBlockerPanel;
        private Image _inputBlockerImage;
        #endregion

        #region 轮盘状态
        private Vector2 _wheelCenterScreenPos;
        private bool _wheelActive = false;

        // 输入检测
        private Vector2 _pressDownMousePos = Vector2.zero;
        private Vector2 _wheelShowMousePos = Vector2.zero;
        private const float FIRST_VECTOR_THRESHOLD = 20f;

        // 九宫格配置（复用物品轮盘的配置）
        private const float CELL_SIZE = 40f;
        private const float GRID_OFFSET = CELL_SIZE + 5f;

        // 九宫格位置（相对于中心的偏移）
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
        #endregion

        #region Unity生命周期
        private void Awake()
        {
            InitializeWheel();
        }

        private void Update()
        {
            if (_wheelActive)
            {
                UpdateWheelSelection();
            }
        }

        private void OnDestroy()
        {
            CleanupWheel();
        }
        #endregion

        #region 初始化
        private void InitializeWheel()
        {
            Debug.Log("[VoiceWheelSelector] 初始化语音轮盘...");

            // 创建Canvas
            CreateWheelCanvas();

            // 创建轮盘容器
            CreateWheelContainer();

            // 创建输入拦截面板
            CreateInputBlocker();

            // 创建轮盘格子（8个）
            CreateWheelSlots();

            // 默认隐藏轮盘
            SetWheelActive(false);
        }

        private void CreateWheelCanvas()
        {
            var canvasGO = new GameObject("VoiceWheelCanvas");
            canvasGO.transform.SetParent(transform);

            _wheelCanvas = canvasGO.AddComponent<Canvas>();
            _wheelCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _wheelCanvas.sortingOrder = 100; // 确保在最上层

            var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();
        }

        private void CreateWheelContainer()
        {
            var containerGO = new GameObject("WheelContainer");
            containerGO.transform.SetParent(_wheelCanvas.transform, false);

            _wheelContainer = containerGO.AddComponent<RectTransform>();
            _wheelContainer.anchorMin = Vector2.zero;
            _wheelContainer.anchorMax = Vector2.one;
            _wheelContainer.pivot = new Vector2(0.5f, 0.5f);
        }

        private void CreateInputBlocker()
        {
            var blockerGO = new GameObject("InputBlocker");
            blockerGO.transform.SetParent(_wheelCanvas.transform, false);

            var blockerRect = blockerGO.AddComponent<RectTransform>();
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.sizeDelta = Vector2.zero;

            _inputBlockerImage = blockerGO.AddComponent<Image>();
            _inputBlockerImage.color = new Color(0, 0, 0, 0.1f); // 半透明黑色

            _inputBlockerPanel = blockerGO;
            _inputBlockerPanel.SetActive(false);
        }

        private void CreateWheelSlots()
        {
            // 创建8个轮盘位置
            for (int i = 0; i < 8; i++)
            {
                CreateWheelSlot(i);
            }
        }

        private void CreateWheelSlot(int index)
        {
            var slotGO = new GameObject($"VoiceSlot_{index}");
            slotGO.transform.SetParent(_wheelContainer.transform, false);

            // 设置位置
            var rectTransform = slotGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(CELL_SIZE, CELL_SIZE);
            rectTransform.anchoredPosition = (Vector2)GRID_POSITIONS[index] * GRID_OFFSET;

            // 创建背景
            var background = new GameObject("Background");
            background.transform.SetParent(rectTransform.transform, false);
            var bgRect = background.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            bgImage.type = Image.Type.Sliced;

            // 创建文本显示
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(rectTransform.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = new Vector2(-10f, -10f);
            textRect.offsetMin = new Vector2(5f, 5f);
            textRect.offsetMax = new Vector2(-5f, -5f);

            var text = textGO.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 20;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = "?"; // 默认显示

            // 添加VoiceItemDisplay组件
            var voiceDisplay = slotGO.AddComponent<VoiceItemDisplay>();
            voiceDisplay.Initialize(text, bgImage, index);

            _voiceDisplayObjects.Add(slotGO);
            _voiceDisplayComponents.Add(voiceDisplay);

            // 默认禁用
            slotGO.SetActive(false);
        }
        #endregion

        #region 轮盘控制
        public void Show(List<VoiceItem> voices)
        {
            if (voices == null || voices.Count == 0)
            {
                Debug.LogWarning("[VoiceWheelSelector] 没有可显示的语音");
                return;
            }

            _currentVoices = voices;
            _pressDownMousePos = Input.mousePosition;

            // 更新轮盘位置到鼠标位置
            UpdateWheelPosition(Input.mousePosition);

            // 更新轮盘内容
            UpdateWheelContent();

            // 显示轮盘
            SetWheelActive(true);

            Debug.Log($"[VoiceWheelSelector] 显示语音轮盘，包含 {voices.Count} 个语音");
        }

        public void Hide()
        {
            SetWheelActive(false);
            _selectedVoiceIndex = -1;
            _wheelActive = false;

            Debug.Log("[VoiceWheelSelector] 隐藏语音轮盘");
        }

        public void RefreshWheel(List<VoiceItem> voices)
        {
            _currentVoices = voices;
            UpdateWheelContent();
        }

        /// <summary>
        /// 处理轮盘内的鼠标移动和选择
        /// 由VoiceWheelManager调用
        /// </summary>
        public void HandleSelection()
        {
            // 直接调用现有的更新选择逻辑
            UpdateWheelSelection();
        }

        private void SetWheelActive(bool active)
        {
            _wheelActive = active;

            if (_wheelContainer != null)
            {
                _wheelContainer.gameObject.SetActive(active);
            }

            if (_inputBlockerPanel != null)
            {
                _inputBlockerPanel.SetActive(active);
            }
        }

        private void UpdateWheelPosition(Vector2 screenPosition)
        {
            _wheelCenterScreenPos = screenPosition;
            _wheelShowMousePos = screenPosition;

            if (_wheelContainer != null)
            {
                var canvasRT = _wheelCanvas.GetComponent<RectTransform>();
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRT, screenPosition, _wheelCanvas.worldCamera, out var localPos);
                _wheelContainer.localPosition = localPos;
            }
        }

        private void UpdateWheelContent()
        {
            // 先禁用所有格子
            foreach (var slot in _voiceDisplayObjects)
            {
                slot.SetActive(false);
            }

            // 更新8个位置的语音
            for (int i = 0; i < 8 && i < _currentVoices.Count; i++)
            {
                var voice = _currentVoices[i];
                if (voice != null && voice.IsAvailable())
                {
                    _voiceDisplayObjects[i].SetActive(true);
                    _voiceDisplayComponents[i].SetVoiceItem(voice);
                }
            }
        }
        #endregion

        #region 选择逻辑
        private void UpdateWheelSelection()
        {
            if (!_wheelActive) return;

            Vector2 currentMousePos = Input.mousePosition;
            Vector2 direction = currentMousePos - _wheelCenterScreenPos;

            // 检查是否超过死区阈值
            if (direction.magnitude < FIRST_VECTOR_THRESHOLD)
            {
                // 在死区内，不选择任何语音
                SetSelectedVoice(-1);
                return;
            }

            // 计算角度并确定选择的格子
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            int selectedIndex = GetSelectedIndexFromAngle(angle);
            SetSelectedVoice(selectedIndex);
        }

        private int GetSelectedIndexFromAngle(float angle)
        {
            // 将角度映射到8个方向
            for (int i = 0; i < DIRECTION_ANGLES.Length; i++)
            {
                float angleDiff = Mathf.Abs(angle - DIRECTION_ANGLES[i]);
                if (angleDiff > 180f) angleDiff = 360f - angleDiff;

                if (angleDiff <= 22.5f) // 每个方向22.5度范围
                {
                    return i;
                }
            }

            return -1; // 没有匹配的方向
        }

        private void SetSelectedVoice(int index)
        {
            if (_selectedVoiceIndex == index) return;

            _selectedVoiceIndex = index;

            // 更新所有格子的选中状态
            for (int i = 0; i < _voiceDisplayComponents.Count; i++)
            {
                bool isSelected = (i == index);
                _voiceDisplayComponents[i].SetSelected(isSelected);
            }

            // 通知管理器悬停变化
            if (index >= 0 && index < _currentVoices.Count)
            {
                VoiceWheelManager.Instance.OnWheelHover(_currentVoices[index]);
            }
        }

        public VoiceItem GetSelectedVoice()
        {
            if (_selectedVoiceIndex >= 0 && _selectedVoiceIndex < _currentVoices.Count)
            {
                return _currentVoices[_selectedVoiceIndex];
            }
            return null;
        }

        /// <summary>
        /// 获取当前选中的语音（VoiceWheelManager调用）
        /// </summary>
        public VoiceItem GetCurrentSelectedVoice()
        {
            return GetSelectedVoice();
        }
        #endregion

        #region 输入处理
        public void OnKeyPressed()
        {
            _pressDownMousePos = Input.mousePosition;
        }
        #endregion

        #region 清理
        private void CleanupWheel()
        {
            if (_wheelCanvas != null)
            {
                Destroy(_wheelCanvas.gameObject);
            }
        }
        #endregion
    }

    /// <summary>
    /// 语音轮盘格子显示组件
    /// </summary>
    public class VoiceItemDisplay : MonoBehaviour
    {
        #region 组件引用
        private Text _displayText;
        private Image _backgroundImage;
        private VoiceItem _currentVoice;
        private int _wheelIndex;
        #endregion

        #region 显示参数
        private const float NORMAL_SCALE = 1.0f;
        private const float HOVER_SCALE = 1.2f;
        private const float SELECTED_SCALE = 1.3f;
        private const float ANIMATION_SPEED = 8f;

        private bool _isSelected = false;
        private Vector3 _targetScale;
        #endregion

        public void Initialize(Text displayText, Image backgroundImage, int wheelIndex)
        {
            _displayText = displayText;
            _backgroundImage = backgroundImage;
            _wheelIndex = wheelIndex;

            _targetScale = Vector3.one * NORMAL_SCALE;
        }

        public void SetVoiceItem(VoiceItem voice)
        {
            _currentVoice = voice;

            if (_displayText != null)
            {
                _displayText.text = voice.GetDisplayText();
            }

            if (_backgroundImage != null)
            {
                // 可以根据语音类型设置不同的颜色
                _backgroundImage.color = GetVoiceColor(voice);
            }
        }

        public void SetSelected(bool selected)
        {
            if (_isSelected == selected) return;

            _isSelected = selected;
            _targetScale = Vector3.one * (selected ? SELECTED_SCALE : NORMAL_SCALE);
        }

        private void Update()
        {
            // 平滑缩放动画
            if (transform.localScale != _targetScale)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * ANIMATION_SPEED);
            }
        }

        private Color GetVoiceColor(VoiceItem voice)
        {
            // 根据语音影响范围返回不同颜色
            float normalizedRange = Mathf.InverseLerp(10f, 50f, voice.affectRange);

            if (normalizedRange < 0.33f)
                return new Color(0.2f, 0.6f, 0.2f, 0.8f); // 绿色 - 小范围
            else if (normalizedRange < 0.67f)
                return new Color(0.6f, 0.6f, 0.2f, 0.8f); // 黄色 - 中范围
            else
                return new Color(0.8f, 0.2f, 0.2f, 0.8f); // 红色 - 大范围
        }
    }
}