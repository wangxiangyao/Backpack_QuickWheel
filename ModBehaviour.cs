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

        void Awake()
        {
            Debug.Log("行军包配件系统已加载！");

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

            // 延迟执行，确保游戏物品系统已初始化
            Invoke("InitializeBackpackSystem", 2f);
        }

        void InitializeBackpackSystem()
        {
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

            // 初始化快捷键系统
            InitializeShortcutSystem();

            Debug.Log("行军包配件系统初始化完成");
        }

        void InitializeShortcutSystem()
        {
            // 查找玩家角色装备控制器
            var player = FindObjectOfType<CharacterMainControl>();
            if (player != null)
            {
                var equipmentController = player.GetComponent<CharacterEquipmentController>();
                BackpackShortcutManager.Initialize(equipmentController);
            }
            else
            {
                // 延迟初始化，等待玩家生成
                Invoke("InitializeShortcutSystem", 3f);
            }
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