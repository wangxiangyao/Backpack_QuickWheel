using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Backpack_QuickWheel.ShortcutSystem.Patches
{
    /// <summary>
    /// 补丁 CharacterInputControl 来拦截F9按键，用于模式切换
    /// F9按键用于在配件系统模式和主背包模式之间切换
    /// </summary>
    [HarmonyPatch(typeof(CharacterInputControl))]
    public static class CharacterInputModeSwitchPatch
    {
        /// <summary>
        /// 补丁Update方法来检测F9按键
        /// </summary>
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePostfix(CharacterInputControl __instance)
        {
            // 检测F9按键按下
            if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
            {
                Debug.Log("[CharacterInputModeSwitchPatch] 检测到F9按键按下，触发模式切换");

                // 通知InputInterceptor处理模式切换
                InputInterceptor.Instance?.HandleModeSwitchKey();
            }
        }
    }
}