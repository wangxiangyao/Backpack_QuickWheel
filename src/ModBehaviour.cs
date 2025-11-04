using Duckov.Modding;
using Backpack_QuickWheel.AttachmentSystem;
using Backpack_QuickWheel.AttachmentUI;
using Backpack_QuickWheel.BackpackSystem;
using Backpack_QuickWheel.ShortcutSystem;
using Backpack_QuickWheel.VoiceWheelSystem;
using ItemStatsSystem.Items;
// LootDebugSystem 已移除 - 使用官方掉落系统
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
            Debug.Log("[ModBehaviour] 模组初始化开始");

            // 确保ModBehaviour在场景切换时不被销毁
            DontDestroyOnLoad(this.gameObject);

            // 初始化Harmony
            harmony = new Harmony("com.yourname.great_backpack");
            harmony.PatchAll(); // 自动补丁所有带有[HarmonyPatch]的类

            // 🆕 提前创建BackpackShortcutManager实例，解决依赖注入顺序问题
            var backpackManagerObj = new GameObject("BackpackShortcutManager");
            DontDestroyOnLoad(backpackManagerObj);
            var backpackManager = backpackManagerObj.AddComponent<BackpackShortcutManager>();

            // 🚀 优先初始化快捷键系统 - 在程序入口点尽早初始化，确保不会错过任何背包装备事件
            Debug.Log("[ModBehaviour] 初始化快捷键系统");
            InitializeShortcutSystemAtEntryPoint();

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

    
            // LootDebug系统已移除 - 配件现在直接使用官方掉落系统

            // ItemFilter拦截器已移除 - 不再需要

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





        /// <summary>
        /// 🆕 在程序入口点初始化快捷键系统 - 确保不会错过任何背包装备事件
        /// 这个方法在Awake中尽早调用，在任何游戏对象初始化之前
        /// </summary>
        void InitializeShortcutSystemAtEntryPoint()
        {
            Debug.Log("[ModBehaviour] 🚀 在程序入口点初始化快捷键系统");

            try
            {
                // 系统会自己管理初始化状态，无需外部干预

                // 创建InputInterceptor
                var interceptorObj = new GameObject("InputInterceptor");
                DontDestroyOnLoad(interceptorObj);
                var interceptor = interceptorObj.AddComponent<InputInterceptor>();
                Debug.Log("[ModBehaviour] InputInterceptor已创建（程序入口点）");

                // 创建ItemWheelSelector
                var wheelObj = new GameObject("ItemWheelSelector");
                DontDestroyOnLoad(wheelObj);
                var wheelSelector = wheelObj.AddComponent<ItemWheelSelector>();
                Debug.Log("[ModBehaviour] ItemWheelSelector已创建（程序入口点）");

                // 将轮盘选择器关联到输入拦截器
                InputInterceptor.SetWheelSelector(wheelSelector);
                Debug.Log("[ModBehaviour] 轮盘选择器已关联到输入拦截器（程序入口点）");

                // 标记系统已初始化，但EquipmentController将在后续设置
                _isShortcutSystemInitialized = true;
                Debug.Log("[ModBehaviour] 快捷键系统框架初始化完成（等待EquipmentController）");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ModBehaviour] 程序入口点快捷键系统初始化失败: {e.Message}");
                Debug.LogError($"[ModBehaviour] Exception stack trace: {e.StackTrace}");
            }
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

            // 检查当前场景是否有玩家，为快捷键系统设置EquipmentController
            var player = FindObjectOfType<CharacterMainControl>();
            if (player != null)
            {
                var equipmentController = player.GetComponent<CharacterEquipmentController>();
                if (equipmentController != null)
                {
                    // 如果快捷键系统已在程序入口点初始化，只需要设置EquipmentController
                    if (_isShortcutSystemInitialized)
                    {
                        Debug.Log("[ModBehaviour] 快捷键系统已初始化，设置EquipmentController");
                        BackpackShortcutManager.Initialize(equipmentController);
                        Debug.Log("[ModBehaviour] EquipmentController设置完成");

                        // 🔧 混合方案：主动检查当前背包状态
                        Debug.Log("[ModBehaviour] 🚨 立即检查当前背包状态...");
                        CheckCurrentBackpackStatus();
                    }
                    else
                    {
                        // 备用初始化路径（如果程序入口点初始化失败）
                        Debug.LogWarning("[ModBehaviour] 程序入口点初始化失败，使用备用初始化路径");
                        BackpackShortcutManager.Initialize(equipmentController);
                        _isShortcutSystemInitialized = true;

                        // 初始化输入拦截器和轮盘选择器
                        InitializeWheelSelectorSystem();
                        Debug.Log("[ModBehaviour] 备用路径：快捷键系统初始化完成");

                        // 🔧 混合方案：主动检查当前背包状态
                        Debug.Log("[ModBehaviour] 🚨 立即检查当前背包状态...");
                        CheckCurrentBackpackStatus();
                    }

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
                Debug.Log("[ModBehaviour] 当前场景没有玩家，等待后续初始化");
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

        /// <summary>
        /// 🆕 立即检查当前背包状态
        /// 在OnLevelInitialized中调用，确保不会错过已装备的背包
        /// </summary>
        private void CheckCurrentBackpackStatus()
        {
            Debug.Log("[ModBehaviour] 开始检查当前背包状态...");

            try
            {
                // 查找玩家
                var player = CharacterMainControl.Main;
                if (player == null)
                {
                    Debug.Log("[ModBehaviour] 玩家尚未加载，稍后再试");
                    return;
                }

                // 检查角色的装备
                var characterItem = player.CharacterItem;
                if (characterItem == null)
                {
                    Debug.Log("[ModBehaviour] 角色物品尚未初始化，稍后再试");
                    return;
                }

                Debug.Log($"[ModBehaviour] 找到角色物品: {characterItem.DisplayName}");

                // 通过反射获取背包槽位
                var equipmentSlots = characterItem.GetType().GetField("equipmentSlots",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (equipmentSlots != null)
                {
                    var slots = equipmentSlots.GetValue(characterItem);
                    if (slots != null)
                    {
                        Debug.Log($"[ModBehaviour] 成功获取equipmentSlots，类型: {slots.GetType().Name}");

                        var slotsArray = slots as object[];
                        if (slotsArray != null)
                        {
                            Debug.Log($"[ModBehaviour] equipmentSlots数组长度: {slotsArray.Length}");

                            if (slotsArray.Length > 3)
                            {
                                var backpackSlot = slotsArray[3] as Slot;
                                Debug.Log($"[ModBehaviour] 背包槽位(索引3): {backpackSlot?.GetType().Name ?? "null"}");

                                if (backpackSlot != null)
                                {
                                    Debug.Log($"[ModBehaviour] 背包槽位内容: {backpackSlot.Content?.DisplayName ?? "null"}");

                                    if (backpackSlot.Content != null)
                                    {
                                        Debug.Log($"[ModBehaviour] 🎯 发现已装备背包: {backpackSlot.Content.DisplayName}");

                                        // 触发背包变化事件
                                        BackpackShortcutManager.Instance.OnBackpackChanged(backpackSlot);
                                        Debug.Log("[ModBehaviour] 已触发背包变化事件");
                                    }
                                    else
                                    {
                                        Debug.Log("[ModBehaviour] 背包槽位为空，未装备背包");
                                    }
                                }
                                else
                                {
                                    Debug.Log("[ModBehaviour] 背包槽位本身为null");
                                }
                            }
                            else
                            {
                                Debug.Log($"[ModBehaviour] equipmentSlots数组长度不足，当前长度: {slotsArray.Length}，需要至少4个");
                            }
                        }
                        else
                        {
                            Debug.Log($"[ModBehaviour] equipmentSlots不是数组类型，实际类型: {slots.GetType().Name}");
                        }
                    }
                    else
                    {
                        Debug.Log("[ModBehaviour] equipmentSlots字段值为null");
                    }
                }
                else
                {
                    Debug.Log("[ModBehaviour] 找不到equipmentSlots字段");

                    // 尝试其他可能的字段名
                    var alternativeFields = new string[] { "_equipmentSlots", "m_equipmentSlots", "EquipmentSlots" };
                    foreach (var fieldName in alternativeFields)
                    {
                        var field = characterItem.GetType().GetField(fieldName,
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                        if (field != null)
                        {
                            Debug.Log($"[ModBehaviour] 找到替代字段: {fieldName}");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ModBehaviour] 检查背包状态时出错: {ex.Message}");
                Debug.LogError($"[ModBehaviour] 错误堆栈: {ex.StackTrace}");
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