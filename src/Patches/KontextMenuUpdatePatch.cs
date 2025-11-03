using HarmonyLib;
using ItemStatsSystem;
using Duckov.UI;
using UnityEngine;

namespace Backpack_QuickWheel.Patches
{
    /// <summary>
    /// 修补 KontextMenu.Update() 方法，轻微延迟UI关闭
    ///
    /// 注意：此功能存在已知问题，详见 KNOWN_ISSUES.md
    /// 保留为简单版本，仅提供轻微的延迟保护
    /// </summary>
    [HarmonyPatch(typeof(KontextMenu), "Update")]
    public class KontextMenuUpdatePatch
    {
        private static float _lastItemClickTime;
        private const float ITEM_CLICK_PROTECTION_DURATION = 0.1f; // 减少到100ms

        /// <summary>
        /// 记录物品点击时间
        /// </summary>
        public static void RecordItemClick()
        {
            _lastItemClickTime = Time.time;
        }

        static bool Prefix(KontextMenu __instance)
        {
            // 轻微保护，避免过于复杂的逻辑
            float timeSinceClick = Time.time - _lastItemClickTime;
            if (timeSinceClick < ITEM_CLICK_PROTECTION_DURATION)
            {
                return false; // 短暂跳过位置检测
            }

            return true; // 正常执行
        }
    }
}