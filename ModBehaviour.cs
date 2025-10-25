using Duckov.Modding;
using Great_backpack.AttachmentSystem;
using Great_backpack.BackpackSystem;
using UnityEngine;

namespace Great_backpack
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private TagManager tagManager;
        private BackpackModifier backpackModifier;
        private AttachmentManager attachmentManager;

        void Awake()
        {
            Debug.Log("行军包配件系统已加载！");

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

            // 2. 创建配件物品
            attachmentManager.CreateAllAttachmentItems();

            // 3. 修改现有背包
            backpackModifier.ModifyAllBackpacks();

            Debug.Log("行军包配件系统初始化完成");
        }
    }
}