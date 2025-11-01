using HarmonyLib;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using UnityEngine;
using System.Collections;

namespace Backpack_QuickWheel.ShortcutSystem.Patches
{
    [HarmonyPatch(typeof(CharacterEquipmentController))]
    [HarmonyPatch("ChangeBackpackModel")]
    public static class EquipmentControllerPatch
    {
        [HarmonyPostfix]
        static void OnBackpackChanged(CharacterEquipmentController __instance, Slot slot)
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log($"[EquipmentControllerPatch] 🚨 ChangeBackpackModel 被调用！");
            Debug.Log($"[EquipmentControllerPatch] 时间: {Time.time}");
            Debug.Log($"[EquipmentControllerPatch] __instance: {__instance?.name}");
            Debug.Log($"[EquipmentControllerPatch] slot: {slot?.GetType().Name}");
            Debug.Log($"[EquipmentControllerPatch] slot.Content: {slot?.Content?.DisplayName ?? "null"}");

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

            // 直接调用OnBackpackChanged，现在初始化流程更可靠了
            Debug.Log($"[EquipmentControllerPatch] 直接调用OnBackpackChanged: {slot?.Content?.DisplayName ?? "null"}");
            BackpackShortcutManager.Instance.OnBackpackChanged(slot);

            Debug.Log($"[EquipmentControllerPatch] OnBackpackChanged 调用完成");
        }

        /// <summary>
        /// 检查背包是否属于有CharacterMainControl的角色
        /// 直接递归查找，简单可靠
        /// </summary>
        private static bool IsMainCharacterBackpack(Slot backpackSlot)
        {
            // 快速检查：空值验证
            if (backpackSlot?.Master?.gameObject == null)
            {
                return false;
            }

            // 直接递归查找CharacterMainControl组件
            bool hasCharacterMainControl = backpackSlot.Master.gameObject.GetComponentInParent<CharacterMainControl>() != null;

            Debug.Log($"[EquipmentControllerPatch] 背包 {backpackSlot.Content?.DisplayName ?? "null"} 属于 {(hasCharacterMainControl ? "✅ 玩家角色" : "❌ NPC/其他")}");

            return hasCharacterMainControl;
        }

      }
}