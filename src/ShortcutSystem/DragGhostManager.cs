using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ItemStatsSystem;
using Duckov;
using System.Collections.Generic;

namespace Backpack_QuickWheel.ShortcutSystem
{
    /// <summary>
    /// 拖拽虚影管理器
    /// 负责创建和管理拖拽时的虚影效果，以及播放拖拽声音
    /// 参考官方拖拽系统的实现方式
    /// </summary>
    public class DragGhostManager : MonoBehaviour
    {
        #region 单例模式
        private static DragGhostManager _instance;
        public static DragGhostManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<DragGhostManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("DragGhostManager");
                        _instance = go.AddComponent<DragGhostManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region 私有字段
        private GameObject _currentDragGhost;
        private Canvas _dragCanvas;
        private Image _ghostImage;
        private Item _currentDragItem;

        // 声音相关
        private const string PICKUP_SOUND_PREFIX = "SFX/Item/pickup_";
        private const string PUT_SOUND_PREFIX = "SFX/Item/put_";

        // 虚影参数
        private const float GHOST_ALPHA = 0.6f;
        private const float GHOST_SCALE = 1.2f;
        private const int DRAG_SORTING_ORDER = 999;
        #endregion

        #region Unity生命周期
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeDragCanvas();
        }

        private void InitializeDragCanvas()
        {
            // 创建专用的拖拽Canvas
            var canvasObj = new GameObject("DragCanvas");
            canvasObj.transform.SetParent(transform, false);

            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = DRAG_SORTING_ORDER;
            canvas.pixelPerfect = false;

            var graphicRaycaster = canvasObj.AddComponent<GraphicRaycaster>();

            var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvasScaler.scaleFactor = 1f;

            _dragCanvas = canvas;
            canvasObj.SetActive(false);

            Debug.Log("[DragGhostManager] 拖拽Canvas初始化完成");
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 开始拖拽，创建虚影并播放声音
        /// </summary>
        public void StartDrag(Item item, PointerEventData eventData)
        {
            if (item == null)
            {
                Debug.LogWarning("[DragGhostManager] 开始拖拽但物品为null");
                return;
            }

            _currentDragItem = item;
            CreateDragGhost(item);
            UpdateGhostPosition(eventData);
            PlayPickupSound(item);

            Debug.Log($"[DragGhostManager] 开始拖拽物品: {item.DisplayName}");
        }

        /// <summary>
        /// 更新虚影位置
        /// </summary>
        public void UpdateDrag(PointerEventData eventData)
        {
            if (_currentDragGhost == null) return;

            UpdateGhostPosition(eventData);
        }

        /// <summary>
        /// 结束拖拽，清理虚影并播放声音
        /// </summary>
        public void EndDrag(bool success = true)
        {
            if (_currentDragItem != null)
            {
                PlayPutSound(_currentDragItem, success); // success=true表示成功放置
                Debug.Log($"[DragGhostManager] 结束拖拽物品: {_currentDragItem.DisplayName}, 成功: {success}");
            }

            CleanupDragGhost();
        }

        /// <summary>
        /// 强制清理当前拖拽状态
        /// </summary>
        public void ForceCleanup()
        {
            CleanupDragGhost();
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 创建拖拽虚影
        /// </summary>
        private void CreateDragGhost(Item item)
        {
            CleanupDragGhost(); // 先清理旧的

            _currentDragGhost = new GameObject("DragGhost");
            _currentDragGhost.transform.SetParent(_dragCanvas.transform, false);

            // 创建Image组件显示物品图标
            _ghostImage = _currentDragGhost.AddComponent<Image>();
            _ghostImage.sprite = item.Icon;
            _ghostImage.color = new Color(1f, 1f, 1f, GHOST_ALPHA);
            _ghostImage.raycastTarget = false; // 不阻挡光线

            // 设置RectTransform
            var rectTransform = _currentDragGhost.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(74, 74); // 标准64px + 10px
            rectTransform.localScale = Vector3.one * GHOST_SCALE;

            // 添加动画效果
            var animator = _currentDragGhost.AddComponent<DragGhostAnimator>();

            _dragCanvas.gameObject.SetActive(true);

            Debug.Log($"[DragGhostManager] 创建虚影完成: {item.DisplayName}");
        }

        /// <summary>
        /// 更新虚影位置
        /// </summary>
        private void UpdateGhostPosition(PointerEventData eventData)
        {
            if (_currentDragGhost == null || _ghostImage == null) return;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _dragCanvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint
            );

            var rectTransform = _currentDragGhost.GetComponent<RectTransform>();
            rectTransform.localPosition = localPoint;
        }

        /// <summary>
        /// 清理拖拽虚影
        /// </summary>
        private void CleanupDragGhost()
        {
            // 强制清理所有残留的DragGhost对象
            CleanupAllDragGhosts();

            if (_currentDragGhost != null)
            {
                // 立即销毁当前虚影，不使用动画避免残留
                DestroyImmediate(_currentDragGhost);
                _currentDragGhost = null;
                _ghostImage = null;
            }

            _currentDragItem = null;

            if (_dragCanvas != null)
            {
                _dragCanvas.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 强制清理所有残留的拖拽虚影对象
        /// </summary>
        private void CleanupAllDragGhosts()
        {
            if (_dragCanvas != null)
            {
                // 查找所有名为DragGhost的对象并销毁
                var dragGhosts = new List<GameObject>();
                for (int i = 0; i < _dragCanvas.transform.childCount; i++)
                {
                    var child = _dragCanvas.transform.GetChild(i);
                    if (child.name == "DragGhost")
                    {
                        dragGhosts.Add(child.gameObject);
                    }
                }

                // 销毁所有找到的DragGhost对象
                foreach (var ghost in dragGhosts)
                {
                    if (ghost != null)
                    {
                        DestroyImmediate(ghost);
                        Debug.Log("[DragGhostManager] 清理残留的拖拽虚影");
                    }
                }

                if (dragGhosts.Count > 0)
                {
                    Debug.Log($"[DragGhostManager] 清理了 {dragGhosts.Count} 个残留的拖拽虚影");
                }
            }
        }

        /// <summary>
        /// 播放拾取声音
        /// </summary>
        private void PlayPickupSound(Item item)
        {
            try
            {
                // 使用官方音频系统播放声音
                string soundKey = item.SoundKey.ToLower();
                string soundPath = $"{PICKUP_SOUND_PREFIX}{soundKey}";

                // 调用AudioManager播放声音
                var audioManager = FindObjectOfType<AudioManager>();
                if (audioManager != null)
                {
                    // 使用反射调用AudioManager的私有方法
                    var playMethod = typeof(AudioManager).GetMethod("PlayPutItemSFX",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                    if (playMethod != null)
                    {
                        playMethod.Invoke(null, new object[] { item, true }); // pickup=true
                        Debug.Log($"[DragGhostManager] 播放拾取声音: {soundPath}");
                    }
                    else
                    {
                        // 如果找不到方法，尝试直接调用
                        AudioManager.PlayPutItemSFX(item, true);
                    }
                }
                else
                {
                    Debug.LogWarning("[DragGhostManager] 找不到AudioManager，无法播放声音");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DragGhostManager] 播放拾取声音失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 播放放置声音
        /// </summary>
        private void PlayPutSound(Item item, bool success)
        {
            try
            {
                // 直接调用静态方法，不需要反射
                // success=true → 播放放置声音 (pickup=false)
                // success=false → 播放拾取声音 (pickup=true)
                AudioManager.PlayPutItemSFX(item, !success);
                Debug.Log($"[DragGhostManager] 播放拖拽声音: 成功={success}, pickup参数={!success}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DragGhostManager] 播放拖拽声音失败: {ex.Message}");
            }
        }
        #endregion

        #region 调试信息
        private void OnGUI()
        {
            if (!Input.GetKey(KeyCode.LeftAlt)) return;

            GUILayout.BeginArea(new Rect(10, 200, 300, 150));
            GUILayout.Label("DragGhostManager 状态:");
            GUILayout.Label($"拖拽中: {_currentDragGhost != null}");
            GUILayout.Label($"当前物品: {_currentDragItem?.DisplayName ?? "无"}");
            GUILayout.Label("按住左Alt查看此信息");
            GUILayout.EndArea();
        }
        #endregion
    }

    /// <summary>
    /// 拖拽虚影动画组件
    /// 负责拖拽时的视觉效果动画
    /// </summary>
    public class DragGhostAnimator : MonoBehaviour
    {
        private Image _image;
        private Vector3 _originalScale;
        private Color _originalColor;
        private Coroutine _animationCoroutine;

        void Awake()
        {
            _image = GetComponent<Image>();
            _originalScale = transform.localScale;
            _originalColor = _image.color;
        }

        void Start()
        {
            PlayAppearAnimation();
        }

        /// <summary>
        /// 播放出现动画
        /// </summary>
        public void PlayAppearAnimation()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }

            _animationCoroutine = StartCoroutine(AppearAnimationCoroutine());
        }

        /// <summary>
        /// 播放消失动画
        /// </summary>
        public void PlayDisappearAnimation(System.Action onComplete)
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }

            _animationCoroutine = StartCoroutine(DisappearAnimationCoroutine(onComplete));
        }

        private System.Collections.IEnumerator AppearAnimationCoroutine()
        {
            float duration = 0.2f;
            float elapsed = 0f;

            Vector3 startScale = _originalScale * 0.5f;
            Color startColor = new Color(_originalColor.r, _originalColor.g, _originalColor.b, 0f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // 缓动函数
                t = Mathf.Sin(t * Mathf.PI * 0.5f);

                transform.localScale = Vector3.Lerp(startScale, _originalScale, t);
                _image.color = Color.Lerp(startColor, _originalColor, t);

                yield return null;
            }

            transform.localScale = _originalScale;
            _image.color = _originalColor;
        }

        private System.Collections.IEnumerator DisappearAnimationCoroutine(System.Action onComplete)
        {
            float duration = 0.15f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // 快速淡出
                float alpha = Mathf.Lerp(_originalColor.a, 0f, t);
                Vector3 scale = Vector3.Lerp(_originalScale, _originalScale * 0.8f, t);

                _image.color = new Color(_image.color.r, _image.color.g, _image.color.b, alpha);
                transform.localScale = scale;

                yield return null;
            }

            onComplete?.Invoke();
        }
    }
}