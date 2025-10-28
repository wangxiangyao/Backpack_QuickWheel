using HarmonyLib;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using UnityEngine;

namespace Great_backpack.ShortcutSystem.Patches
{
    [HarmonyPatch(typeof(CharacterEquipmentController))]
    [HarmonyPatch("ChangeBackpackModel")]
    public static class EquipmentControllerPatch
    {
        [HarmonyPostfix]
        static void OnBackpackChanged(CharacterEquipmentController __instance, Slot slot)
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log($"[EquipmentControllerPatch] ChangeBackpackModel 被调用");
            Debug.Log($"[EquipmentControllerPatch] 时间: {Time.time}");

            // 检查是否是无效的调用（比如背包内容为null或者正在销毁）
            if (slot?.Content != null && slot.Content.IsBeingDestroyed)
            {
                Debug.Log($"[EquipmentControllerPatch] 警告：背包 {slot.Content.DisplayName} 正在被销毁，跳过处理");
                Debug.Log("═══════════════════════════════════════");
                return;
            }

            // 检查是否正在初始化过程中
            if (BackpackShortcutManager.IsInitializing())
            {
                Debug.Log($"[EquipmentControllerPatch] 正在初始化过程中，跳过背包切换处理: {slot?.Content?.DisplayName ?? "null"}");
                Debug.Log("═══════════════════════════════════════");
                return;
            }

            // 检查背包是否属于主角（解决方案A）
            // 主角的 Character 在根路径，其他 Character（在 MultiSceneCore 下的 NPC）的背包事件应被忽略
            if (!IsMainCharacterBackpack(slot))
            {
                Debug.Log($"[EquipmentControllerPatch] 检测到非主角背包 {slot?.Content?.DisplayName ?? "null"}，忽略处理");
                Debug.Log("═══════════════════════════════════════");
                return;
            }

            // 如果BackpackShortcutManager.Instance为null，立即初始化它
            if (BackpackShortcutManager.Instance == null)
            {
                Debug.Log("[EquipmentControllerPatch] BackpackShortcutManager.Instance 为 null，正在初始化...");
                BackpackShortcutManager.Initialize(__instance);
                Debug.Log($"[EquipmentControllerPatch] 初始化完成");
            }

            // 现在调用OnBackpackChanged
            BackpackShortcutManager.Instance.OnBackpackChanged(slot);

            Debug.Log($"[EquipmentControllerPatch] OnBackpackChanged 调用完成");
            Debug.Log("═══════════════════════════════════════");
        }

        /// <summary>
        /// 检查背包是否属于主角
        /// </summary>
        private static bool IsMainCharacterBackpack(Slot backpackSlot)
        {
            // 如果背包槽或其所有者为空，不处理
            if (backpackSlot == null)
            {
                return false;
            }

            // 获取背包所属的角色
            Item backpackOwner = backpackSlot.Master;
            if (backpackOwner == null)
            {
                return false;
            }

            // 获取主角
            CharacterMainControl mainCharacter = CharacterMainControl.Main;
            if (mainCharacter == null)
            {
                // 主角尚未初始化，允许处理（可能是初始化阶段）
                return true;
            }

            Item mainCharacterItem = mainCharacter.CharacterItem;
            if (mainCharacterItem == null)
            {
                return false;
            }

            // 比较：背包的所有者是否是主角
            bool isMainCharacter = backpackOwner == mainCharacterItem;

            if (!isMainCharacter)
            {
                Debug.Log($"[EquipmentControllerPatch] 背包所有者: {backpackOwner.DisplayName}, 主角: {mainCharacterItem.DisplayName}");
            }

            return isMainCharacter;
        }
    }
}