using HarmonyLib;
using ItemStatsSystem.Items;

namespace Great_backpack.ShortcutSystem.Patches
{
    [HarmonyPatch(typeof(CharacterEquipmentController))]
    [HarmonyPatch("ChangeBackpackModel")]
    public static class EquipmentControllerPatch
    {
        [HarmonyPostfix]
        static void OnBackpackChanged(CharacterEquipmentController __instance, Slot slot)
        {
            UnityEngine.Debug.Log($"[EquipmentControllerPatch] ChangeBackpackModel 被调用");
            UnityEngine.Debug.Log($"[EquipmentControllerPatch] Slot: {(slot != null ? "存在" : "null")}");
            UnityEngine.Debug.Log($"[EquipmentControllerPatch] Slot.Content: {(slot?.Content != null ? slot.Content.DisplayName : "null")}");
            UnityEngine.Debug.Log($"[EquipmentControllerPatch] BackpackShortcutManager.Instance: {(BackpackShortcutManager.Instance != null ? "存在" : "null")}");

            BackpackShortcutManager.Instance?.OnBackpackChanged(slot);

            UnityEngine.Debug.Log($"[EquipmentControllerPatch] OnBackpackChanged 调用完成");
        }
    }
}