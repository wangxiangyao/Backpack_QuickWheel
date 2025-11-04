using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Backpack_QuickWheel.VoiceWheelSystem;
using Backpack_QuickWheel.ShortcutSystem;
using Backpack_QuickWheel.AttachmentSystem;
using ItemStatsSystem;
using Duckov.UI;

namespace Backpack_QuickWheel.VoiceWheelSystem
{
    /// <summary>
    /// 语音轮盘选择器UI组件
    /// 完全复制ItemWheelSelector的实现，只是将Item替换为VoiceItem
    /// </summary>
    public class VoiceWheelSelector : MonoBehaviour
    {
        #region UI组件 - 完全复制ItemWheelSelector
        private Canvas _wheelCanvas;
        private RectTransform _wheelContainer;
        private List<GameObject> _itemDisplayClones = new List<GameObject>();
        private List<VoiceWheelDisplay> _voiceDisplayComponents = new List<VoiceWheelDisplay>();
        private int _selectedItemIndex = -1;
        private List<VoiceItem> _currentVoices = new List<VoiceItem>();

        // 轮盘位置
        private Vector2 _wheelCenterScreenPos;
        private bool _wheelActive = false;

        // 全屏拦截面板（防止鼠标输入传给游戏）
        private GameObject _inputBlockerPanel;
        private Image _inputBlockerImage;

        // 矢量选择逻辑
        private Vector2 _pressDownMousePos = Vector2.zero;       // 按下时的鼠标位置
        private Vector2 _wheelShowMousePos = Vector2.zero;       // 轮盘显示时的鼠标位置
        private const float FIRST_VECTOR_THRESHOLD = 20f;        // 第一矢量的激活阈值（死区）

        // 九宫格配置（完全复制ItemWheelSelector的配置）
        private const float CELL_SIZE = 95f;                        // 调整格子尺寸
        private const float GRID_OFFSET = CELL_SIZE + 6f;          // 调整间距

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

        #region 初始化 - 完全复制ItemWheelSelector
        private void InitializeWheel()
        {
            // 创建Canvas
            var canvasObj = new GameObject("VoiceWheelCanvas");
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
            canvasObj.AddComponent<GraphicRaycaster>();

            var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);

            // 创建全屏拦截面板
            var blockerObj = new GameObject("InputBlocker");
            blockerObj.transform.SetParent(canvasObj.transform, false);
            var blockerRect = blockerObj.AddComponent<RectTransform>();
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;

            _inputBlockerImage = blockerObj.AddComponent<Image>();
            _inputBlockerImage.color = new Color(0, 0, 0, 0.1f);
            _inputBlockerPanel = blockerObj;
            _inputBlockerPanel.SetActive(false);

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

            // 创建轮盘格子（8个）
            CreateWheelSlots();

            // 默认隐藏轮盘
            SetWheelActive(false);
        }

        private void CreateWheelSlots()
        {
            // 完全复制ItemWheelSelector的实现
            for (int i = 0; i < 8; i++)
            {
                VoiceItem voiceToDisplay = (i < _currentVoices.Count) ? _currentVoices[i] : null;

                
                var displayClone = CreateWheelItemDisplay(i, GRID_POSITIONS[i], voiceToDisplay);
                if (displayClone != null)
                {
                    _itemDisplayClones.Add(displayClone);

                    // 如果有语音，记录显示组件
                    if (voiceToDisplay != null)
                    {
                        var voiceDisplay = displayClone.GetComponent<VoiceWheelDisplay>();
                        if (voiceDisplay != null)
                        {
                            _voiceDisplayComponents.Add(voiceDisplay);
                            Debug.Log($"[VoiceWheelSelector] 格子 {i} VoiceWheelDisplay组件已记录: {voiceToDisplay.displayName}");
                        }
                    }
                }
            }

            Debug.Log($"[VoiceWheelSelector] ✓ 创建完成：克隆了8个格子，其中 {_voiceDisplayComponents.Count} 个有语音");
        }

        /// <summary>
        /// 完全复制ItemWheelSelector的格子创建方法
        /// </summary>
        private GameObject CreateWheelItemDisplay(int cellIndex, Vector2Int gridPos, VoiceItem voice)
        {
            // 创建格子
            var cellObj = new GameObject($"VoiceWheelItem_{cellIndex}");
            cellObj.transform.SetParent(_wheelContainer, false);

            var rectTransform = cellObj.AddComponent<RectTransform>();
            Vector2 localPos = new Vector2(gridPos.x * GRID_OFFSET, gridPos.y * GRID_OFFSET);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = localPos;
            rectTransform.sizeDelta = new Vector2(CELL_SIZE, CELL_SIZE);

            Debug.Log($"[VoiceWheelSelector] 创建格子 {cellIndex} 在位置 {localPos} (网格位置: {gridPos})");

            // 添加VoiceWheelDisplay组件
            var voiceDisplay = cellObj.AddComponent<VoiceWheelDisplay>();
            voiceDisplay.Initialize(voice, cellIndex);

            return cellObj;
        }
        #endregion

        #region 轮盘控制 - 复制ItemWheelSelector方法
        public void Show(List<VoiceItem> voices)
        {
            if (voices == null || voices.Count == 0)
            {
                Debug.LogError("[VoiceWheelSelector] ERROR: 没有可显示的语音");
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

            Debug.LogError($"[VoiceWheelSelector] === SHOW CALLED === 显示语音轮盘，包含 {voices.Count} 个语音");
            Debug.LogError($"[VoiceWheelSelector] 轮盘容器尺寸: {_wheelContainer?.sizeDelta}");
            Debug.LogError($"[VoiceWheelSelector] Canvas活跃: {_wheelCanvas?.gameObject.activeInHierarchy}");
        }

        public void Hide()
        {
            SetWheelActive(false);
            _selectedItemIndex = -1;
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
            Debug.Log($"[VoiceWheelSelector] 更新轮盘内容，语音数量: {_currentVoices.Count}, 格子对象数: {_itemDisplayClones.Count}");

            // 始终显示所有8个格子，不管有没有语音
            for (int i = 0; i < 8; i++)
            {
                var slotObj = _itemDisplayClones[i];
                if (slotObj == null) continue;

                // 始终激活格子
                slotObj.SetActive(true);

                if (i < _currentVoices.Count)
                {
                    var voice = _currentVoices[i];
                    if (voice != null && voice.IsAvailable())
                    {
                        Debug.Log($"[VoiceWheelSelector] 格子 {i} 显示语音: {voice.displayName}");

                        // 找到对应的VoiceWheelDisplay组件
                        var voiceDisplay = slotObj.GetComponent<VoiceWheelDisplay>();
                        if (voiceDisplay != null)
                        {
                            voiceDisplay.SetVoice(voice);
                            Debug.Log($"[VoiceWheelSelector] 格子 {i} 已设置语音内容");
                        }
                        else
                        {
                            Debug.LogError($"[VoiceWheelSelector] 格子 {i} 找不到VoiceWheelDisplay组件！");
                        }
                    }
                    else
                    {
                        Debug.Log($"[VoiceWheelSelector] 格子 {i} 语音不可用，显示空格子");
                    }
                }
                else
                {
                    Debug.Log($"[VoiceWheelSelector] 格子 {i} 无语音，显示空格子");
                }
            }

            Debug.Log($"[VoiceWheelSelector] ✓ 轮盘内容更新完成，所有8个格子都已激活显示");
        }
        #endregion

        #region 选择逻辑 - 复制ItemWheelSelector方法
        private void UpdateWheelSelection()
        {
            if (!_wheelActive) return;

            // 完全复制ItemWheelSelector的选择逻辑
            Vector2 currentMousePos = Input.mousePosition;
            Vector2 currentVector = currentMousePos - _wheelCenterScreenPos;

            // 检查矢量长度是否超过阈值
            if (currentVector.magnitude < FIRST_VECTOR_THRESHOLD)
            {
                // 矢量长度不足，不做选择
                return;
            }

            // 矢量长度足够，根据当前矢量方向选中语音
            SelectByVector(currentVector, "当前矢量");
        }

        /// <summary>
        /// 完全复制ItemWheelSelector的矢量选择方法
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

            // 找到最接近的语音
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

            SetSelectedVoice(closestIndex);
        }

        private void SetSelectedVoice(int index)
        {
            if (_selectedItemIndex == index) return;

            _selectedItemIndex = index;

            // 更新所有VoiceWheelDisplay的选中状态
            for (int i = 0; i < _voiceDisplayComponents.Count; i++)
            {
                if (_voiceDisplayComponents[i] != null)
                {
                    bool isSelected = (i == index);
                    _voiceDisplayComponents[i].SetSelected(isSelected);
                }
            }

            // 通知管理器悬停变化
            if (index >= 0 && index < _currentVoices.Count)
            {
                VoiceWheelManager.Instance.OnWheelHover(_currentVoices[index]);
            }
        }

        public VoiceItem GetSelectedVoice()
        {
            if (_selectedItemIndex >= 0 && _selectedItemIndex < _currentVoices.Count)
            {
                return _currentVoices[_selectedItemIndex];
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
    /// 语音类别枚举
    /// </summary>
    public enum VoiceCategory
    {
        General,
        Combat,
        Social,
        Emergency
    }
}