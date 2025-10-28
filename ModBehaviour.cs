using Duckov.Modding;
using Great_backpack.AttachmentSystem;
using Great_backpack.BackpackSystem;
using Great_backpack.ShortcutSystem;
using HarmonyLib;
using UnityEngine;

namespace Great_backpack
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private TagManager tagManager;
        private BackpackModifier backpackModifier;
        private AttachmentManager attachmentManager;
        private Harmony harmony;

        // Mod初始化状态标志
        private bool _isModInitialized = false;
        private bool _isShortcutSystemInitialized = false;

        void Awake()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("[ModBehaviour] Awake 被调用");
            Debug.Log($"[ModBehaviour] 当前时间: {Time.time}");
            Debug.Log($"[ModBehaviour] 游戏是否正在运行: {Application.isPlaying}");
            Debug.Log($"[ModBehaviour] 当前场景: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");

            // 确保ModBehaviour在场景切换时不被销毁
            DontDestroyOnLoad(this.gameObject);
            Debug.Log("[ModBehaviour] 已设置 DontDestroyOnLoad");

            // 初始化Harmony
            harmony = new Harmony("com.yourname.great_backpack");
            harmony.PatchAll(); // 自动补丁所有带有[HarmonyPatch]的类
            Debug.Log("Harmony补丁已应用");

            // 检测并导出支持的语言（开发时使用，完成后注释掉）
            // LanguageDetector.ExportSupportedLanguages();

            // 初始化本地化系统
            SystemLanguage currentLanguage = Application.systemLanguage; // 或者从游戏设置获取
            Great_backpack.Localization.LocalizationManager.Initialize(currentLanguage);

            // 初始化管理器
            tagManager = new TagManager();
            backpackModifier = new BackpackModifier(tagManager.CreatedTags);
            attachmentManager = new AttachmentManager(tagManager.CreatedTags);

            // 立即初始化背包系统
            InitializeBackpackSystem();

            // 订阅关卡初始化事件
            LevelManager.OnLevelInitialized += OnLevelInitialized;
            Debug.Log("[ModBehaviour] 已订阅 LevelManager.OnLevelInitialized 事件");

            Debug.Log("═══════════════════════════════════════");
        }

        void InitializeBackpackSystem()
        {
            Debug.Log("[ModBehaviour] 开始初始化背包系统");

            //  创建所有需要的Tag
            tagManager.CreateRequiredTags();

            // 检查我们需要的限制Tag是否存在
            TagExporter.CheckSpecificTags();

            // 导出所有Tag到文件（开发时使用）
            ExportAllTagsForDevelopment();

            // 创建配件物品
            attachmentManager.CreateAllAttachmentItems();

            // 修改现有背包
            backpackModifier.ModifyAllBackpacks();

            _isModInitialized = true;
            Debug.Log("行军包配件系统初始化完成");
        }





        void OnLevelInitialized()
        {
            Debug.Log("[ModBehaviour] 关卡初始化完成");

            // 检查当前场景是否有玩家，以及快捷键系统是否已初始化
            var player = FindObjectOfType<CharacterMainControl>();
            if (player != null && !_isShortcutSystemInitialized)
            {
                var equipmentController = player.GetComponent<CharacterEquipmentController>();
                if (equipmentController != null)
                {
                    Debug.Log("[ModBehaviour] 找到玩家和装备控制器，开始初始化快捷键系统");
                    BackpackShortcutManager.Initialize(equipmentController);
                    // 设置初始化完成状态
                    BackpackShortcutManager.SetInitializing(false);
                    _isShortcutSystemInitialized = true;
                    Debug.Log("[ModBehaviour] 快捷键系统初始化完成");

                    // 初始化输入拦截器和轮盘选择器
                    InitializeWheelSelectorSystem();
                }
                else
                {
                    Debug.LogWarning("[ModBehaviour] 无法找到 CharacterEquipmentController");
                }
            }
            else
            {
                if (player == null)
                {
                    Debug.Log("[ModBehaviour] 当前场景没有玩家，跳过快捷键系统初始化");
                }
                else if (_isShortcutSystemInitialized)
                {
                    Debug.Log("[ModBehaviour] 快捷键系统已经初始化，跳过重复初始化");
                }
            }
        }

        void InitializeWheelSelectorSystem()
        {
            Debug.Log("[ModBehaviour] 开始初始化轮盘选择器系统");

            // 创建InputInterceptor
            var interceptorObj = new GameObject("InputInterceptor");
            DontDestroyOnLoad(interceptorObj);
            var interceptor = interceptorObj.AddComponent<InputInterceptor>();
            Debug.Log("[ModBehaviour] InputInterceptor已创建");

            // 创建ItemWheelSelector
            var wheelObj = new GameObject("ItemWheelSelector");
            DontDestroyOnLoad(wheelObj);
            var wheelSelector = wheelObj.AddComponent<ItemWheelSelector>();
            Debug.Log("[ModBehaviour] ItemWheelSelector已创建");

            // 将轮盘选择器关联到输入拦截器
            InputInterceptor.SetWheelSelector(wheelSelector);
            Debug.Log("[ModBehaviour] 轮盘选择器已关联到输入拦截器");
        }


        void OnDestroy()
        {
            // 取消订阅事件
            LevelManager.OnLevelInitialized -= OnLevelInitialized;
            Debug.Log("[ModBehaviour] 已取消订阅 LevelManager.OnLevelInitialized 事件");
        }

        void ExportAllTagsForDevelopment()
        {
            // 只有在开发模式下才导出Tag
#if DEBUG
            TagExporter.ExportAllTagsToFile();
#else
            // 发布版本中可以选择性导出，或者不导出
            // TagExporter.ExportAllTagsToFile();
#endif
        }
    }
}