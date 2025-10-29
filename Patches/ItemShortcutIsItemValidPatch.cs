using HarmonyLib;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using Duckov;
using UnityEngine;

namespace Great_backpack.Patches
{
    /// <summary>
    /// 修补 ItemShortcut.IsItemValid() 方法以支持背包配件系统
    ///
    /// 原始逻辑：只检查物品是否直接在玩家库存中
    /// 修改后：支持检查物品是否间接通过配件属于玩家库存
    ///
    /// 问题背景：
    /// - 官方系统只认可 MainInventory == item.InInventory 的物品
    /// - 配件中的物品：item.InInventory = attachment.Inventory（不等于MainInventory）
    /// - 这导致快捷键系统拒绝配件中的物品
    ///
    /// 解决方案：
    /// - 检查物品是否直接在MainInventory中 ✓
    /// - 或者检查物品的ParentItem链是否最终属于玩家背包 ✓
    /// </summary>
    [HarmonyPatch(typeof(ItemShortcut), "IsItemValid")]
    public class ItemShortcutIsItemValidPatch
    {
        /// <summary>
        /// Prefix方式Patch：替换原有逻辑
        /// </summary>
        static bool Prefix(Item item, ref bool __result)
        {
            // 获取玩家主库存
            CharacterMainControl master = CharacterMainControl.Main;
            if (master == null)
            {
                __result = false;
                return false;  // 不执行原方法
            }

            if (master.CharacterItem == null)
            {
                __result = false;
                return false;
            }

            Inventory mainInventory = master.CharacterItem.Inventory;

            // 基础检查
            if (item == null || mainInventory == null)
            {
                __result = false;
                return false;
            }

            // 检查是否标记为武器（快捷栏排除武器）
            if (item.Tags.Contains("Weapon"))
            {
                __result = false;
                return false;
            }

            // ✅ 检查1：物品是否直接在主库存中
            if (mainInventory == item.InInventory)
            {
                __result = true;
                return false;
            }

            // ✅ 检查2：物品是否通过配件间接属于主库存
            // 递归检查物品的ParentItem链
            Item current = item.ParentItem;
            while (current != null)
            {
                // 如果找到一个物品直接在主库存中，说明当前物品间接属于主库存
                if (mainInventory == current.InInventory)
                {
                    __result = true;
                    return false;
                }

                // 继续向上查找（配件→背包→角色物品）
                current = current.ParentItem;
            }

            // 物品既不在主库存中，也不在任何配件中
            __result = false;
            return false;
        }
    }
}
