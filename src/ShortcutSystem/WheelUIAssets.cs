using UnityEngine;
using UnityEngine.UI;

namespace Backpack_QuickWheel.ShortcutSystem
{
    /// <summary>
    /// 轮盘UI资源加载管理器
    /// 支持从Resources文件夹加载自定义素材
    ///
    /// 素材加载路径规范：
    /// Resources/GreatBackpack/WheelUI/
    /// ├── grid_bg.png              - 单个格子背景（正常状态）
    /// ├── grid_bg_hover.png        - 单个格子背景（悬停状态）
    /// ├── grid_border.png          - 单个格子边框（可选）
    /// ├── wheel_bg.png             - 整体轮盘背景
    /// └── glow_effect.png          - 发光效果
    ///
    /// 如果找不到素材，使用默认的渐变色+代码绘制
    /// </summary>
    public class WheelUIAssets
    {
        private static WheelUIAssets _instance;
        private const string RESOURCES_PATH = "GreatBackpack/WheelUI/";

        // 已加载的素材缓存
        private Sprite _gridBgSprite;
        private Sprite _gridBgHoverSprite;
        private Sprite _gridBorderSprite;
        private Sprite _wheelBgSprite;
        private Sprite _glowEffectSprite;

        // 默认颜色配置
        public readonly Color GRID_BG_NORMAL = new Color(0.15f, 0.15f, 0.2f, 0.85f);      // 深蓝灰
        public readonly Color GRID_BG_HOVER = new Color(0.2f, 0.4f, 0.7f, 0.95f);         // 蓝色
        public readonly Color GRID_BORDER_NORMAL = new Color(0.4f, 0.6f, 1f, 0.6f);       // 浅蓝
        public readonly Color GRID_BORDER_HOVER = new Color(0.6f, 0.8f, 1f, 1f);          // 亮蓝
        public readonly Color GLOW_COLOR = new Color(0.3f, 0.6f, 1f, 0.7f);               // 发光蓝
        public readonly Color WHEEL_BG_COLOR = new Color(0.05f, 0.05f, 0.08f, 0.3f);      // 极深蓝，半透明

        public static WheelUIAssets Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new WheelUIAssets();
                    _instance.LoadAssets();
                }
                return _instance;
            }
        }

        private void LoadAssets()
        {
            Debug.Log("[WheelUIAssets] 开始加载轮盘UI素材...");

            // 尝试加载自定义素材
            _gridBgSprite = Resources.Load<Sprite>(RESOURCES_PATH + "grid_bg");
            _gridBgHoverSprite = Resources.Load<Sprite>(RESOURCES_PATH + "grid_bg_hover");
            _gridBorderSprite = Resources.Load<Sprite>(RESOURCES_PATH + "grid_border");
            _wheelBgSprite = Resources.Load<Sprite>(RESOURCES_PATH + "wheel_bg");
            _glowEffectSprite = Resources.Load<Sprite>(RESOURCES_PATH + "glow_effect");

            // 报告加载结果
            if (_gridBgSprite != null) Debug.Log("[WheelUIAssets] ✓ 已加载 grid_bg.png");
            else Debug.Log("[WheelUIAssets] ✗ 未找到 grid_bg.png，将使用默认颜色");

            if (_gridBgHoverSprite != null) Debug.Log("[WheelUIAssets] ✓ 已加载 grid_bg_hover.png");
            else Debug.Log("[WheelUIAssets] ✗ 未找到 grid_bg_hover.png，将使用默认颜色");

            if (_wheelBgSprite != null) Debug.Log("[WheelUIAssets] ✓ 已加载 wheel_bg.png");
            else Debug.Log("[WheelUIAssets] ✗ 未找到 wheel_bg.png，将使用默认颜色");

            Debug.Log("[WheelUIAssets] 素材加载完成。如需自定义UI，请在以下路径放置素材：");
            Debug.Log($"[WheelUIAssets] {RESOURCES_PATH}");
        }

        /// <summary>
        /// 获取格子背景精灵（正常状态）
        /// </summary>
        public Sprite GetGridBgSprite() => _gridBgSprite;

        /// <summary>
        /// 获取格子背景精灵（悬停状态）
        /// </summary>
        public Sprite GetGridBgHoverSprite() => _gridBgHoverSprite;

        /// <summary>
        /// 获取格子边框精灵
        /// </summary>
        public Sprite GetGridBorderSprite() => _gridBorderSprite;

        /// <summary>
        /// 获取轮盘背景精灵
        /// </summary>
        public Sprite GetWheelBgSprite() => _wheelBgSprite;

        /// <summary>
        /// 获取发光效果精灵
        /// </summary>
        public Sprite GetGlowEffectSprite() => _glowEffectSprite;

        /// <summary>
        /// 设置Image的背景（自动处理Sprite或颜色）
        /// </summary>
        public void SetImageBackground(Image image, Sprite sprite, Color fallbackColor)
        {
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;  // 如果有图片就用白色作为基底
            }
            else
            {
                image.sprite = null;
                image.color = fallbackColor;  // 否则用指定的颜色
            }
        }

        /// <summary>
        /// 创建一个可爱的渐变背景（用于默认样式）
        /// </summary>
        public static Texture2D CreateGradientTexture(int width, int height, Color topColor, Color bottomColor)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            for (int y = 0; y < height; y++)
            {
                Color lerpColor = Color.Lerp(topColor, bottomColor, (float)y / height);
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, lerpColor);
                }
            }

            texture.Apply();
            return texture;
        }
    }
}
