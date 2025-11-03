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
            
            // 检查是否正在初始化过程中
            if (BackpackShortcutManager.IsInitializing())
            {

                BackpackShortcutManager.Initialize(__instance);
                Debug.Log($"[EquipmentControllerPatch] 初始化完成，挂载背包: {slot?.Content?.DisplayName ?? "null"}");
                Debug.Log("═══════════════════════════════════════");
            }

            // 直接调用OnBackpackChanged，现在初始化流程更可靠了
            Debug.Log($"[EquipmentControllerPatch] 直接调用OnBackpackChanged: {slot?.Content?.DisplayName ?? "null"}");
            BackpackShortcutManager.Instance.OnBackpackChanged(slot);

            Debug.Log($"[EquipmentControllerPatch] OnBackpackChanged 调用完成");
        }

        /// <summary>
        /// 检查背包是否属于顶层玩家角色
        /// 主要通过层级位置判断，避免名称重复问题
        /// </summary>
        private static bool IsMainCharacterBackpack(Slot backpackSlot)
        {
            // 快速检查：空值验证
            if (backpackSlot?.Master?.gameObject == null)
            {
                Debug.Log($"[EquipmentControllerPatch] ❌ 背包slot或Master为null");
                return false;
            }

            var characterGO = backpackSlot.Master.gameObject;
            var backpackName = backpackSlot.Content?.DisplayName ?? "null";

            // 验证1：检查是否有CharacterMainControl组件（基本要求）
            bool hasCharacterMainControl = characterGO.GetComponentInParent<CharacterMainControl>() != null;
            if (!hasCharacterMainControl)
            {
                Debug.Log($"[EquipmentControllerPatch] ❌ 背包 {backpackName} 没有CharacterMainControl组件");
                return false;
            }

            // 验证2：检查是否在顶层层级（关键判断）
            bool isTopLevel = IsTopLevelCharacter(characterGO);
            if (!isTopLevel)
            {
                Debug.Log($"[EquipmentControllerPatch] ❌ 背包 {backpackName} 的角色不在顶层层级");
                return false;
            }

            Debug.Log($"[EquipmentControllerPatch] ✅ 背包 {backpackName} 通过验证，确认为玩家背包");
            Debug.Log($"[EquipmentControllerPatch]   - 有CharacterMainControl: {hasCharacterMainControl}");
            Debug.Log($"[EquipmentControllerPatch]   - 顶层层级: {isTopLevel}");

            return true;
        }

        /// <summary>
        /// 检查角色是否在顶层层级
        /// 玩家角色通常在场景根层级或直接在CharacterMainControl下
        /// NPC通常在特定的管理器容器下（如MultiSceneCore等）
        /// </summary>
        private static bool IsTopLevelCharacter(GameObject characterGO)
        {
            if (characterGO == null)
                return false;

            // 获取完整路径
            string path = GetGameObjectPath(characterGO);
            Debug.Log($"[EquipmentControllerPatch]   角色路径: {path}");

            // 检查是否在已知的管理器容器下（排除这些）
            bool isUnderManager = path.Contains("MultiSceneCore") ||
                                 path.Contains("NPCManager") ||
                                 path.Contains("CivilianManager") ||
                                 path.Contains("EnemyManager") ||
                                 path.Contains("AIManager") ||
                                 path.Contains("EntityManager");

            // 玩家角色通常不在这些管理器容器下
            bool isTopLevel = !isUnderManager;
            Debug.Log($"[EquipmentControllerPatch]   层级检查结果: {isTopLevel} ({(isUnderManager ? "在管理器下" : "在顶层")})");

            return isTopLevel;
        }

        /// <summary>
        /// 获取GameObject的完整路径
        /// </summary>
        private static string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

      }
}