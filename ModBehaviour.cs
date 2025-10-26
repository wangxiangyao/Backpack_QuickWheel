using Duckov.Modding;
using Great_backpack.AttachmentSystem;
using Great_backpack.BackpackSystem;
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

            // 初始化管理器
            tagManager = new TagManager();
            backpackModifier = new BackpackModifier(tagManager.CreatedTags);
            attachmentManager = new AttachmentManager(tagManager.CreatedTags);

            // 延迟执行，确保游戏物品系统已初始化
            Invoke("InitializeBackpackSystem", 2f);
        }

        void InitializeBackpackSystem()
        {
            // 1. 创建所有需要的Tag
            tagManager.CreateRequiredTags();

            // 2. 检查我们需要的限制Tag是否存在
            TagExporter.CheckSpecificTags();

            // 2. 导出所有Tag到文件（开发时使用）
            ExportAllTagsForDevelopment();

            // 2. 创建配件物品
            attachmentManager.CreateAllAttachmentItems();

            // 3. 修改现有背包
            backpackModifier.ModifyAllBackpacks();

            Debug.Log("行军包配件系统初始化完成");
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