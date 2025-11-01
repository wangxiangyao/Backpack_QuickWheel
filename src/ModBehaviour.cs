using Duckov.Modding;
using Backpack_QuickWheel.AttachmentSystem;
using Backpack_QuickWheel.AttachmentUI;
using Backpack_QuickWheel.BackpackSystem;
using Backpack_QuickWheel.ShortcutSystem;
using Backpack_QuickWheel.VoiceWheelSystem;
using Backpack_QuickWheel.LootDebugSystem;
using LootDebugSystem;
using HarmonyLib;
using UnityEngine;

namespace Backpack_QuickWheel
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
        private bool _isVoiceWheelSystemInitialized = false;

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

            // 初始化本地化系统 - 延迟到关卡初始化后，确保游戏本地化系统已就绪
            // Backpack_QuickWheel.Localization.LocalizationManager.Initialize(currentLanguage);

            // 初始化管理器
            tagManager = new TagManager();
            backpackModifier = new BackpackModifier(tagManager.CreatedTags);
            attachmentManager = new AttachmentManager(tagManager.CreatedTags);

            // 立即初始化背包系统
            InitializeBackpackSystem();

            // 初始化配件Hover信息显示
            AttachmentHoveringUIManager.Initialize();

            // 初始化圆孔拖拽高亮缓存管理器
            Backpack_QuickWheel.AttachmentUI.SlotIndicatorCacheManager.Initialize();

            // 初始化LootDebug系统（用于调试和数据分析）
            Debug.Log("[ModBehaviour] 初始化LootDebug系统...");
            var lootDebugObj = new GameObject("LootDebugManager");
            lootDebugObj.transform.SetParent(transform); // 设置为ModBehaviour的子对象
            DontDestroyOnLoad(lootDebugObj);
            var lootDebugManager = lootDebugObj.AddComponent<LootDebugManager>();
            Debug.Log("[ModBehaviour] LootDebugManager已创建，使用快捷键8触发");

            // 初始化ItemFilter拦截器
            Debug.Log("[ModBehaviour] 初始化ItemFilter拦截器...");
            ItemFilterInterceptorPatch.Initialize();
            Debug.Log("[ModBehaviour] ItemFilter拦截器已初始化");

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

            // 调试：强制重新注册本地化，确保物品创建后本地化生效
            StartCoroutine(DebugLocalizationAfterItemsCreated());
        }





        void OnLevelInitialized()
        {
            Debug.Log("[ModBehaviour] 关卡初始化完成");

            // 在关卡初始化后初始化本地化系统，确保游戏本地化系统已就绪
            SystemLanguage currentLanguage = Application.systemLanguage;
            Debug.Log($"[ModBehaviour] 检测到系统语言: {currentLanguage}");
            Debug.Log($"[ModBehaviour] 转换为语言代码: {Backpack_QuickWheel.Localization.LanguageDetector.ToLanguageCode(currentLanguage)}");

            // 强制使用中文，如果检测到的不是中文
            if (currentLanguage != SystemLanguage.Chinese && currentLanguage != SystemLanguage.ChineseSimplified)
            {
                Debug.Log($"[ModBehaviour] 强制使用中文，覆盖检测到的语言: {currentLanguage}");
                currentLanguage = SystemLanguage.ChineseSimplified;
            }

            Backpack_QuickWheel.Localization.LocalizationManager.Initialize(currentLanguage);

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

                    // 初始化语音轮盘系统
                    if (!_isVoiceWheelSystemInitialized)
                    {
                        Debug.Log("[ModBehaviour] 开始初始化语音轮盘系统");
                        InitializeVoiceWheelSystem();
                        _isVoiceWheelSystemInitialized = true;
                        Debug.Log("[ModBehaviour] 语音轮盘系统初始化完成");
                    }
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


        void InitializeVoiceWheelSystem()
        {
            Debug.Log("[ModBehaviour] 开始初始化语音轮盘系统");

            try
            {
                // 创建语音轮盘管理器容器
                var voiceManagerObj = new GameObject("VoiceWheelManager");
                DontDestroyOnLoad(voiceManagerObj);

                // 添加VoiceWheelManager主控制器
                var voiceManager = voiceManagerObj.AddComponent<VoiceWheelManager>();

                // 添加语音轮盘的所有必需组件
                voiceManagerObj.AddComponent<VoiceBubbleManager>();
                voiceManagerObj.AddComponent<VoiceNPCManager>();
                voiceManagerObj.AddComponent<VoiceWheelSelector>();
                voiceManagerObj.AddComponent<VoiceAudioManager>();

                // 创建语音输入拦截器（自动单例）
                var voiceInputInterceptor = VoiceInputInterceptor.Instance;

                Debug.Log("[ModBehaviour] 语音轮盘系统初始化完成");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ModBehaviour] 语音轮盘系统初始化失败: {e.Message}");
                Debug.LogError($"[ModBehaviour] Exception stack trace: {e.StackTrace}");
            }
        }

        void OnDestroy()
        {
            // 取消订阅事件
            LevelManager.OnLevelInitialized -= OnLevelInitialized;
            Debug.Log("[ModBehaviour] 已取消订阅 LevelManager.OnLevelInitialized 事件");

            // 反初始化管理器
            Backpack_QuickWheel.AttachmentUI.SlotIndicatorCacheManager.Uninitialize();
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

        // 调试方法：在物品创建后强制重新注册本地化
        System.Collections.IEnumerator DebugLocalizationAfterItemsCreated()
        {
            // 等待一帧，确保物品创建完成
            yield return new WaitForEndOfFrame();

            Debug.Log("[Debug] 物品创建完成，强制重新注册本地化...");
            Backpack_QuickWheel.Localization.LocalizationManager.ForceReRegister();
            Debug.Log("[Debug] 本地化强制重新注册完成");
        }
    }
}