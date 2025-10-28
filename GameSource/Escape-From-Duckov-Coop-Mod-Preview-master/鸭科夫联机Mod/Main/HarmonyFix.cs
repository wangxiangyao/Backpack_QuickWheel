// Escape-From-Duckov-Coop-Mod-Preview
// Copyright (C) 2025  Mr.sans and InitLoader's team
//
// This program is not a free software.
// It's distributed under a license based on AGPL-3.0,
// with strict additional restrictions:
//  YOU MUST NOT use this software for commercial purposes.
//  YOU MUST NOT use this software to run a headless game server.
//  YOU MUST include a conspicuous notice of attribution to
//  Mr-sans-and-InitLoader-s-team/Escape-From-Duckov-Coop-Mod-Preview as the original author.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.

using Cysharp.Threading.Tasks;
using Duckov;
using Duckov.Buffs;
using Duckov.Quests;
using Duckov.Quests.Tasks;
using Duckov.Scenes;
using Duckov.UI;
using Duckov.UI.Animations;
using Duckov.Utilities;
using HarmonyLib;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using LiteNetLib;
using LiteNetLib.Utils;
using NodeCanvas.StateMachines;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace 鸭科夫联机Mod
{
    public sealed class LocalMeleeOncePerFrame : UnityEngine.MonoBehaviour
    {
        public int lastFrame;
    }

    // 客户端：拦截本地主角的开火，改为发 FIRE_REQUEST，不在本地生成弹丸
    [HarmonyPatch(typeof(ItemAgent_Gun), "ShootOneBullet")]
    public static class Patch_ShootOneBullet_Client
    {
        static bool Prefix(ItemAgent_Gun __instance, Vector3 _muzzlePoint, Vector3 _shootDirection, Vector3 firstFrameCheckStartPoint)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted) return true;

            bool isClient = !mod.IsServer;
            if (!isClient) return true;

            var holder = __instance.Holder;
            bool isLocalMain = (holder == CharacterMainControl.Main);
            bool isAI = holder && holder.GetComponent<NetAiTag>() != null;

            if (isLocalMain)
            {
                mod.Net_OnClientShoot(__instance, _muzzlePoint, _shootDirection, firstFrameCheckStartPoint);
                return false; // 客户端不生成，交主机
            }

            if (isAI) return false;     // 客户端看到的AI，等主机的 FIRE_EVENT
            if (!isLocalMain) return false;
            return true;
        }
    }

    // 服务端：在 Projectile.Init 后，把“服务端算好的弹丸参数”一并广播给所有客户端
    [HarmonyPatch(typeof(Projectile), nameof(Projectile.Init), new[] { typeof(ProjectileContext) })]
    static class Patch_ProjectileInit_Broadcast
    {
        static void Postfix(Projectile __instance, ref ProjectileContext _context)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.IsServer || __instance == null) return;

            if (mod._serverSpawnedFromClient != null && mod._serverSpawnedFromClient.Contains(__instance)) return;

            var fromC = _context.fromCharacter;
            if (!fromC) return;

            string shooterId = null;
            if (fromC.IsMainCharacter) shooterId = mod.localPlayerStatus?.EndPoint;
            else
            {
                var tag = fromC.GetComponent<NetAiTag>();
                if (tag == null || tag.aiId == 0) return;
                shooterId = $"AI:{tag.aiId}";
            }

            int weaponType = 0;
            try { var gun = fromC.GetGun(); if (gun != null && gun.Item != null) weaponType = gun.Item.TypeID; } catch { }

            var w = new LiteNetLib.Utils.NetDataWriter();
            w.Put((byte)Op.FIRE_EVENT);
            w.Put(shooterId);
            w.Put(weaponType);
            w.PutV3cm(__instance.transform.position); // 近似 muzzle
            w.PutDir(_context.direction);
            w.Put(_context.speed);
            w.Put(_context.distance);

            // 把服务端算好的弹丸参数一并带上（含 explosionRange / explosionDamage 等）
            w.PutProjectilePayload(_context);

            mod.netManager.SendToAll(w, DeliveryMethod.ReliableOrdered);
        }
    }



    [HarmonyPatch]
    public static class Patch_Grenade_Sync
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Skill_Grenade), nameof(Skill_Grenade.OnRelease))]
        static bool Skill_Grenade_OnRelease_Prefix(Skill_Grenade __instance)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted) return true;

            // —— 无论主机/客户端，都先缓存一次 ——
            try
            {
                var prefab = __instance.grenadePfb; // public 字段
                int typeId = 0; try { typeId = (__instance.fromItem != null) ? __instance.fromItem.TypeID : __instance.damageInfo.fromWeaponItemID; } catch { }
                if (prefab) mod.CacheGrenadePrefab(typeId, prefab);
            }
            catch { }


            if (mod.IsServer)
            {
                // 服务器本地丢：确保 damageInfo.fromWeaponItemID 被写入，供 Grenade.Launch Postfix 读取
                try
                {
                    int tid = 0;
                    try { if (__instance.fromItem != null) tid = __instance.fromItem.TypeID; } catch { }
                    if (tid == 0)
                    {
                        try { tid = __instance.damageInfo.fromWeaponItemID; } catch { }
                    }
                    if (tid != 0)
                    {
                        try { __instance.damageInfo.fromWeaponItemID = tid; } catch { }
                    }
                }
                catch { }

                // 放行原流程，由 Launch 的 Postfix 广播
                return true;
            }


            // 未连接客户端：不拦截
            if (mod.connectedPeer == null || mod.connectedPeer.ConnectionState != ConnectionState.Connected) return true;

            // 只拦“本地主角”
            CharacterMainControl fromChar = null;
            try
            {
                var f_from = AccessTools.Field(typeof(SkillBase), "fromCharacter");
                fromChar = f_from?.GetValue(__instance) as CharacterMainControl;
            }
            catch { }
            if (fromChar != CharacterMainControl.Main) return true;

            try
            {
                Vector3 position = fromChar ? fromChar.CurrentUsingAimSocket.position : Vector3.zero;

                Vector3 releasePoint = Vector3.zero;
                var relCtx = AccessTools.Field(typeof(SkillBase), "skillReleaseContext")?.GetValue(__instance);
                if (relCtx != null)
                {
                    var f_rp = AccessTools.Field(relCtx.GetType(), "releasePoint");
                    if (f_rp != null) releasePoint = (Vector3)f_rp.GetValue(relCtx);
                }
                float y = releasePoint.y;
                Vector3 point = releasePoint - (fromChar ? fromChar.transform.position : Vector3.zero);
                point.y = 0f; float dist = point.magnitude;
                var ctxObj = AccessTools.Field(typeof(SkillBase), "skillContext")?.GetValue(__instance);
                if (!__instance.canControlCastDistance && ctxObj != null)
                {
                    var f_castRange = AccessTools.Field(ctxObj.GetType(), "castRange");
                    if (f_castRange != null) dist = (float)f_castRange.GetValue(ctxObj);
                }
                point.Normalize(); Vector3 target = position + point * dist; target.y = y;

                float vert = 8f, effectRange = 3f;
                if (ctxObj != null)
                {
                    var f_vert = AccessTools.Field(ctxObj.GetType(), "grenageVerticleSpeed");
                    var f_eff = AccessTools.Field(ctxObj.GetType(), "effectRange");
                    if (f_vert != null) vert = (float)f_vert.GetValue(ctxObj);
                    if (f_eff != null) effectRange = (float)f_eff.GetValue(ctxObj);
                }
                Vector3 velocity = __instance.CalculateVelocity(position, target, vert);

                string prefabType = __instance.grenadePfb ? __instance.grenadePfb.GetType().FullName : string.Empty;
                string prefabName = __instance.grenadePfb ? __instance.grenadePfb.name : string.Empty;
                int typeId2 = 0; try { typeId2 = (__instance.fromItem != null) ? __instance.fromItem.TypeID : __instance.damageInfo.fromWeaponItemID; } catch { }

                bool createExplosion = __instance.createExplosion;
                float shake = __instance.explosionShakeStrength;
                float damageRange = effectRange;
                bool delayFromCollide = __instance.delayFromCollide;
                float delayTime = __instance.delay;
                bool isLandmine = __instance.isLandmine;
                float landmineRange = __instance.landmineTriggerRange;

                // 只发请求，不本地生成
                mod.Net_OnClientThrow(__instance, typeId2, prefabType, prefabName, position, velocity,
                    createExplosion, shake, damageRange, delayFromCollide, delayTime, isLandmine, landmineRange);
                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[GRENADE Prefix] exception -> pass through: " + e);
                return true;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Grenade), nameof(Grenade.Launch))]
        static void Grenade_Launch_Postfix(Grenade __instance, Vector3 startPoint, Vector3 velocity, CharacterMainControl fromCharacter)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted || !mod.IsServer) return;

            int typeId = 0;
            try { typeId = __instance.damageInfo.fromWeaponItemID; } catch { }

            if (typeId == 0)
            {
                try
                {
                    typeId = Traverse.Create(__instance).Field<ItemAgent>("bindedAgent").Value.Item.TypeID;
                }
                catch {}
            }

            mod.Server_OnGrenadeLaunched(__instance, startPoint, velocity, typeId);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Grenade), "Explode")]
        static void Grenade_Explode_Prefix(Grenade __instance, ref bool __state)
        {
            __state = __instance.createExplosion;
            var mod = ModBehaviour.Instance;
            if (mod != null && mod.networkStarted && !mod.IsServer)
            {
                var isNetworkGrenade = __instance && __instance.GetComponent<鸭科夫联机Mod.NetGrenadeTag>() != null;
                if (!isNetworkGrenade)
                    __instance.createExplosion = false;
            }
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Grenade), "Explode")]
        static void Grenade_Explode_Postfix(Grenade __instance, bool __state)
        {
            var mod = ModBehaviour.Instance;
            if (mod != null && mod.networkStarted && !mod.IsServer) __instance.createExplosion = __state;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Grenade), "Explode")]
        static void Grenade_Explode_ServerBroadcast(Grenade __instance)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted || !mod.IsServer) return;
            mod.Server_OnGrenadeExploded(__instance);
        }
    }

    [HarmonyPatch(typeof(ItemExtensions), nameof(ItemExtensions.Drop), new[] { typeof(Item), typeof(Vector3), typeof(bool), typeof(Vector3), typeof(float) })]
    public static class Patch_Item_Drop
    {
        static bool Prefix(Item item, Vector3 pos, bool createRigidbody, Vector3 dropDirection, float randomAngle)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted) return true;


            if (NetSilenceGuards.InPickupItem || NetSilenceGuards.InCapacityShrinkCleanup)
            {
                UnityEngine.Debug.Log("[ITEM] 静音丢弃：拾取失败/容量清理导致的自动 Drop，不上报主机");
                return true;
            }

            if (mod.IsServer)
            {
                // 服务器：正常执行，由 Postfix 负责广播（除非来自客户端请求）
                return true;
            }

            // 客户端：若是"来自主机同步"的生成，放行且不再发请求
            if (mod._clientSpawnByServerItems.Remove(item))
                return true;


            // 客户端本地丢：先发送请求，再允许本地正常丢（这样本地背包立即变化）
            uint token = ++mod.nextLocalDropToken;
            mod.pendingLocalDropTokens.Add(token);
            mod.pendingTokenItems[token] = item; 
            mod.SendItemDropRequest(token, item, pos, createRigidbody, dropDirection, randomAngle);
            return true;

        }

        static void Postfix(Item item, DuckovItemAgent __result, Vector3 pos, bool createRigidbody, Vector3 dropDirection, float randomAngle)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted || !mod.IsServer) return;

            // 若这是“处理客户端请求”时生成的，别再次广播
            if (mod._serverSpawnedFromClientItems.Remove(item))
                return;

            // 仅在服务器分支
            if (NetSilenceGuards.InPickupItem)
            {
                UnityEngine.Debug.Log("[SVR] 自动Drop（拾取失败回滚）——不广播SPAWN，避免复制");
                return;
            }

            try
            {
                // 主机自身（或 AI）本地丢弃：广播给客户端
                var w = mod.writer;
                w.Reset();
                w.Put((byte)Op.ITEM_SPAWN);
                w.Put((uint)0);                     // token=0，表示主机自发
                uint id = mod.AllocateDropId();
                mod.serverDroppedItems[id] = item;
                w.Put(id);
                NetPack.PutV3cm(w, pos);
                NetPack.PutDir(w, dropDirection);
                w.Put(randomAngle);
                w.Put(createRigidbody);
                mod.WriteItemSnapshot(w, item);
                mod.BroadcastReliable(w);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ITEM] 主机广播失败: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(Item), "NotifyAddedToInventory")]
    public static class Patch_Item_Pickup_NotifyAdded
    {
        static void Postfix(Item __instance, Inventory __0 /* inv */)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted) return;

            // 只关注“主角相关”的拾取；例如 inv 属于玩家身上的背包/角色栏等
            // 如果游戏里还有其它 NPC/容器也会触发，可再加更精确的判断（比如 inv.AttachedToItem 是否是玩家背包）
            // —— 客户端逻辑：拾到自己客户端映射的掉落，就立刻销毁地面体并发拾取请求
            if (!mod.IsServer)
            {
                if (TryFindId(mod.clientDroppedItems, __instance, out uint cid))
                {
                    // 本地立刻把地面拾取体干掉（如果还在）
                    try
                    {
                        var ag = __instance.ActiveAgent;
                        if (ag && ag.gameObject) UnityEngine.Object.Destroy(ag.gameObject);
                    }
                    catch { }

                    // 发送拾取请求给主机（等主机广播 DESPAWN，让所有客户端一致删除）
                    var w = mod.writer; w.Reset();
                    w.Put((byte)Op.ITEM_PICKUP_REQUEST);
                    w.Put(cid);
                    mod.connectedPeer?.Send(w, DeliveryMethod.ReliableOrdered);
                }
                return;
            }

            // —— 主机逻辑：主机自己捡起主机表里的掉落，则直接移除并广播 DESPAWN
            if (mod.IsServer && TryFindId(mod.serverDroppedItems, __instance, out uint sid))
            {
                mod.serverDroppedItems.Remove(sid);

                try
                {
                    var ag = __instance.ActiveAgent;
                    if (ag && ag.gameObject) UnityEngine.Object.Destroy(ag.gameObject);
                }
                catch { }

                var w = mod.writer; w.Reset();
                w.Put((byte)Op.ITEM_DESPAWN);
                w.Put(sid);
                mod.netManager.SendToAll(w, DeliveryMethod.ReliableOrdered);
            }
        }

        // 小工具：ReferenceEquals 扫描映射
        static bool TryFindId(System.Collections.Generic.Dictionary<uint, Item> dict, Item item, out uint id)
        {
            foreach (var kv in dict)
                if (object.ReferenceEquals(kv.Value, item))
                { id = kv.Key; return true; }
            id = 0; return false;
        }
    }


    [HarmonyPatch(typeof(Inventory), "NotifyContentChanged")]
    public static class Patch_Inventory_NotifyContentChanged
    {
        const float PICK_RADIUS = 2.5f; // 你可按手感调 2~3
        const QueryTriggerInteraction QTI = QueryTriggerInteraction.Collide;
        const int LAYER_MASK = ~0; // 如有专用 Layer，可替换成它

        static void Postfix(Inventory __instance, Item item)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted || item == null) return;

            if (mod._applyingLootState) return;

            if (LootboxDetectUtil.IsLootboxInventory(__instance) && !LootboxDetectUtil.IsPrivateInventory(__instance))
                return;

            // --- 客户端 ---
            if (!mod.IsServer)
            {
                // A) 引用命中（少见：非合并）
                if (TryFindId(mod.clientDroppedItems, item, out uint cid))
                {
                    LocalDestroyAgent(item);
                    SendPickupReq(mod, cid);
                    return;
                }

                // B) 合并堆叠：引用不同，用近场 NetDropTag 反查
                if (TryFindNearestTaggedId(out uint nearId))
                {
                    LocalDestroyAgentById(mod.clientDroppedItems, nearId);
                    SendPickupReq(mod, nearId);
                }
                return;
            }

            // --- 主机 ---
            if (TryFindId(mod.serverDroppedItems, item, out uint sid))
            {
                ServerDespawn(mod, sid);
                return;
            }

            if (TryFindNearestTaggedId(out uint nearSid))
            {
                ServerDespawn(mod, nearSid);
            }
        }

        static void SendPickupReq(ModBehaviour mod, uint id)
        {
            var w = mod.writer; w.Reset();
            w.Put((byte)Op.ITEM_PICKUP_REQUEST);
            w.Put(id);
            mod.connectedPeer?.Send(w, DeliveryMethod.ReliableOrdered);
        }

        static void ServerDespawn(ModBehaviour mod, uint id)
        {
            if (mod.serverDroppedItems.TryGetValue(id, out var it) && it != null)
                LocalDestroyAgent(it);
            mod.serverDroppedItems.Remove(id);

            var w = mod.writer; w.Reset();
            w.Put((byte)Op.ITEM_DESPAWN);
            w.Put(id);
            mod.netManager.SendToAll(w, DeliveryMethod.ReliableOrdered);
        }

        static void LocalDestroyAgent(Item it)
        {
            try
            {
                var ag = it.ActiveAgent;
                if (ag && ag.gameObject) Object.Destroy(ag.gameObject);
            }
            catch { }
        }

        static void LocalDestroyAgentById(Dictionary<uint, Item> dict, uint id)
        {
            if (dict.TryGetValue(id, out var it) && it != null) LocalDestroyAgent(it);
        }

        static bool TryFindId(Dictionary<uint, Item> dict, Item it, out uint id)
        {
            foreach (var kv in dict)
                if (ReferenceEquals(kv.Value, it)) { id = kv.Key; return true; }
            id = 0; return false;
        }

        // 在主角附近找最近的带 NetDropTag 的拾取体
        static readonly Collider[] _nearbyBuf = new Collider[64];
        const int LAYER_MASK_ANY = ~0;

        static bool TryFindNearestTaggedId(out uint id)
        {
            id = 0;
            var main = CharacterMainControl.Main;
            if (main == null) return false;

            var pos = main.transform.position;
            int n = Physics.OverlapSphereNonAlloc(pos, PICK_RADIUS, _nearbyBuf, LAYER_MASK_ANY, QTI);

            float best = float.MaxValue;
            NetDropTag bestTag = null;

            for (int i = 0; i < n; i++)
            {
                var c = _nearbyBuf[i]; if (!c) continue;
                var t = c.GetComponentInParent<NetDropTag>() ?? c.GetComponent<NetDropTag>();
                if (t == null || t.id == 0) continue;

                float d2 = (t.transform.position - pos).sqrMagnitude;
                if (d2 < best) { best = d2; bestTag = t; }
            }

            if (bestTag != null) { id = bestTag.id; return true; }
            return false;
        }
    }

    [HarmonyPatch]
    public static class Patch_ItemExtensions_Drop_AddNetDropTag
    {
        // 明确锁定到扩展方法 Drop(Item, Vector3, bool, Vector3, float)
        [HarmonyPatch(typeof(global::ItemExtensions), "Drop",
            new System.Type[] {
            typeof(global::ItemStatsSystem.Item),
            typeof(global::UnityEngine.Vector3),
            typeof(bool),
            typeof(global::UnityEngine.Vector3),
            typeof(float)
            })]
        [HarmonyPostfix]
        private static void Postfix(
            // 扩展方法的第一个参数（this Item）
            global::ItemStatsSystem.Item item,
            global::UnityEngine.Vector3 pos,
            bool createRigidbody,
            global::UnityEngine.Vector3 dropDirection,
            float randomAngle,
            // 返回值必须用 ref 才能拿到
            ref global::DuckovItemAgent __result)
        {
            try
            {
                var agent = __result;
                if (agent == null) return;

                var go = agent.gameObject;
                if (go == null) return;

                // 已有就不重复加
                var tag = go.GetComponent<NetDropTag>();
                if (tag == null)
                    tag = go.AddComponent<NetDropTag>();

                // 如果你需要在这里写入标识信息，可在此处补充
                // 例如：tag.itemTypeId = item?.TypeID ?? 0;
                // 或者 tag.ownerNetId = ModBehaviour.Instance?.LocalPlayerId ?? 0;
            }
            catch (System.Exception e)
            {
                global::UnityEngine.Debug.LogError($"[Harmony][Drop.Postfix] Add NetDropTag failed: {e}");
            }
        }
    }

    public static class NetSilenceGuards
    {
        // 线程级：避免并发串线
        [ThreadStatic] public static bool InPickupItem;           // 正在执行 CharacterItemControl.PickupItem
        [ThreadStatic] public static bool InCapacityShrinkCleanup; // 正在执行容量下调清理（可选，见下）
    }

    //给 CharacterItemControl.PickupItem 打前后钩，围出一个方法域
    [HarmonyPatch(typeof(CharacterItemControl), nameof(CharacterItemControl.PickupItem))]
    static class Patch_CharacterItemControl_PickupItem
    {
        static void Prefix() { NetSilenceGuards.InPickupItem = true; }
        static void Finalizer() { NetSilenceGuards.InPickupItem = false; }
    }

    public static class MeleeLocalGuard
    {
        [ThreadStatic] public static bool LocalMeleeTryingToHurt;
    }

    [HarmonyPatch(typeof(ItemAgent_MeleeWeapon), "CheckCollidersInRange")]
    static class Patch_Melee_FlagLocalDeal
    {
        static void Prefix(ItemAgent_MeleeWeapon __instance, bool dealDamage)
        {
            var mod = 鸭科夫联机Mod.ModBehaviour.Instance;
            bool isClient = (mod != null && mod.networkStarted && !mod.IsServer);
            bool fromLocalMain = (__instance && __instance.Holder == CharacterMainControl.Main);
            鸭科夫联机Mod.MeleeLocalGuard.LocalMeleeTryingToHurt = (isClient && fromLocalMain && dealDamage);
        }
        static void Postfix()
        {
            鸭科夫联机Mod.MeleeLocalGuard.LocalMeleeTryingToHurt = false;
        }
    }

    [HarmonyPatch(typeof(DamageReceiver), "Hurt")]
    static class Patch_ClientReportMeleeHit
    {
        static bool Prefix(DamageReceiver __instance, ref global::DamageInfo __0)
        {
            var mod = 鸭科夫联机Mod.ModBehaviour.Instance;

            // 不在联网、在主机、或没到“本地近战结算阶段”，都不拦
            if (mod == null || !mod.networkStarted || mod.IsServer || !MeleeLocalGuard.LocalMeleeTryingToHurt)
                return true;

            if (mod.connectedPeer == null)
            {
                Debug.LogWarning("[CLIENT] MELEE_HIT_REPORT aborted: connectedPeer==null, fallback to local Hurt");
                return true; // 让原始 Hurt 生效，避免“无伤”
            }

            try
            {
                var w = new LiteNetLib.Utils.NetDataWriter();
                w.Put((byte)鸭科夫联机Mod.Op.MELEE_HIT_REPORT);
                w.Put(mod.localPlayerStatus != null ? mod.localPlayerStatus.EndPoint : "");

                // DamageInfo 关键字段
                w.Put(__0.damageValue);
                w.Put(__0.armorPiercing);
                w.Put(__0.critDamageFactor);
                w.Put(__0.critRate);
                w.Put(__0.crit);

                w.PutV3cm(__0.damagePoint);
                w.PutDir(__0.damageNormal);

                w.Put(__0.fromWeaponItemID);
                w.Put(__0.bleedChance);
                w.Put(__0.isExplosion);

                // 近战范围（主机用于邻域搜）
                float range = 1.2f;
                try
                {
                    var main = CharacterMainControl.Main;
                    var melee = main ? (main.CurrentHoldItemAgent as ItemAgent_MeleeWeapon) : null;
                    if (melee != null) range = Mathf.Max(0.6f, melee.AttackRange);
                }
                catch { }
                w.Put(range);

               
                mod.connectedPeer.Send(w, LiteNetLib.DeliveryMethod.ReliableOrdered);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[CLIENT] Melee hit report failed: " + e);
                return true; // 发送失败时回退到本地 Hurt，避免“无伤”
            }

            try
            {
                if (global::FX.PopText.instance)
                {
                    // 取默认物理伤害的弹字样式（跟 Health.Hurt 一致的来源）
                    var look = global::Duckov.Utilities.GameplayDataSettings.UIStyle
                        .GetElementDamagePopTextLook(global::ElementTypes.physics);

                    // 位置：优先用伤害点；没有就用受击者位置；整体上抬一点更清晰
                    Vector3 pos = (__0.damagePoint.sqrMagnitude > 1e-6f ? __0.damagePoint : __instance.transform.position)
                                  + global::UnityEngine.Vector3.up * 2f;

                    // 暴击大小/图标
                    float size = (__0.crit > 0) ? look.critSize : look.normalSize;
                    var sprite = (__0.crit > 0) ? global::Duckov.Utilities.GameplayDataSettings.UIStyle.CritPopSprite : null;

                    // 文本：有数值就显示数值，没有就显示“HIT”
                    string text = (__0.damageValue > 0f) ? __0.damageValue.ToString("F1") : "HIT";

                    global::FX.PopText.Pop(text, pos, look.color, size, sprite);
                }
            }
            catch { }



            // 成功上报，由主机权威结算
            return false;
        }
    }


    [HarmonyPatch(typeof(CharacterAnimationControl_MagicBlend), "OnAttack")]
    static class Patch_Melee_OnAttack_SendNetAndFx
    {
        static void Postfix(CharacterAnimationControl_MagicBlend __instance)
        {
            var mod = 鸭科夫联机Mod.ModBehaviour.Instance;
            var ctrl = __instance?.characterMainControl;
            if (mod == null || !mod.networkStarted || ctrl == null) return;
            if (ctrl != CharacterMainControl.Main) return; // 只处理本地玩家

            // 一帧一次闸门（解决重复注入/重复回调）
            var model = ctrl.characterModel;
            if (model)
            {
                var gate = model.GetComponent<LocalMeleeOncePerFrame>() ?? model.gameObject.AddComponent<LocalMeleeOncePerFrame>();
                if (gate.lastFrame == UnityEngine.Time.frameCount) return;
                gate.lastFrame = UnityEngine.Time.frameCount;
            }

            var melee = ctrl.CurrentHoldItemAgent as ItemAgent_MeleeWeapon;
            if (!melee) return;

            float dealDelay = 0.1f;
            try { dealDelay = Mathf.Max(0f, melee.DealDamageTime); } catch { }

            Vector3 snapPos = ctrl.modelRoot ? ctrl.modelRoot.position : ctrl.transform.position;
            Vector3 snapDir = ctrl.CurrentAimDirection.sqrMagnitude > 1e-6f ? ctrl.CurrentAimDirection : ctrl.transform.forward;

            if (mod.IsServer)
            {
                mod.BroadcastMeleeSwing(mod.localPlayerStatus.EndPoint, dealDelay);
            }
            else
            {
                // 客户端：本地FX + 告诉主机
                鸭科夫联机Mod.MeleeFx.SpawnSlashFx(ctrl.characterModel);
                mod.Net_OnClientMeleeAttack(dealDelay, snapPos, snapDir);
            }
        }
    }



    [HarmonyPatch(typeof(CharacterAnimationControl_MagicBlend), "Update")]
    static class Patch_MagicBlend_Update_ForRemote
    {
        static bool Prefix(CharacterAnimationControl_MagicBlend __instance)
        {
            // 远端实体：禁用本地“写Animator参数”的逻辑，避免覆盖网络同步
            if (__instance && __instance.GetComponentInParent<RemoteReplicaTag>() != null)
                return false;
            return true;
        }
    }

    public sealed class RemoteReplicaTag : MonoBehaviour { }



    [HarmonyPatch(typeof(HealthSimpleBase), "Awake")]
    public static class Patch_HSB_Awake_TagRegister
    {
        static void Postfix(HealthSimpleBase __instance)
        {
            if (!__instance) return;

            var tag = __instance.GetComponent<NetDestructibleTag>();
            if (!tag) return; // 你已标注在墙/油桶上了，这里不再 AddComponent

            // —— BreakableWall：用墙根节点来计算稳定ID，避免主客机层级差导致错位 —— //
            Transform wallRoot = FindBreakableWallRoot(__instance.transform);
            if (wallRoot != null)
            {
                try
                {
                    uint computed = NetDestructibleTag.ComputeStableId(wallRoot.gameObject);
                    if (tag.id != computed) tag.id = computed;
                }
                catch {}
            }

            // —— 幂等注册 —— //
            var mod = 鸭科夫联机Mod.ModBehaviour.Instance;
            if (mod != null)
            {
                mod.RegisterDestructible(tag.id, __instance);
            }
        }

        // 向上找名字含“BreakableWall”的祖先（不区分大小写）
        static Transform FindBreakableWallRoot(Transform t)
        {
            var p = t;
            while (p != null)
            {
                string nm = p.name;
                if (!string.IsNullOrEmpty(nm) &&
                    nm.IndexOf("BreakableWall", StringComparison.OrdinalIgnoreCase) >= 0)
                    return p;
                p = p.parent;
            }
            return null;
        }
    }


    // 客户端：阻断本地扣血，改为请求主机结算；
    // 主机：照常结算（原方法运行），并在 Postfix 广播受击
    [HarmonyPatch(typeof(HealthSimpleBase), "OnHurt")]
    public static class Patch_HSB_OnHurt_RedirectNet
    {
        static bool Prefix(HealthSimpleBase __instance, DamageInfo dmgInfo)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted) return true;

            if (!mod.IsServer)
            {

                // 本地 UI：子弹/爆炸统一点亮 Hit；若你能在此处判断“必死”，可传 true 亮 Kill
                LocalHitKillFx.ClientPlayForDestructible(__instance, dmgInfo, predictedDead: false);

                var tag = __instance.GetComponent<NetDestructibleTag>();
                if (!tag) tag = __instance.gameObject.AddComponent<NetDestructibleTag>();
                mod.Client_RequestDestructibleHurt(tag.id, dmgInfo);
                return false;
            }
            return true;
        }

        static void Postfix(HealthSimpleBase __instance, DamageInfo dmgInfo)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted || !mod.IsServer) return;

            var tag = __instance.GetComponent<NetDestructibleTag>();
            if (!tag) return;
            mod.Server_BroadcastDestructibleHurt(tag.id, __instance.HealthValue, dmgInfo);
        }
    }

    // 主机在死亡后广播；客户端收到“死亡广播”时只做视觉切换
    [HarmonyPatch(typeof(HealthSimpleBase), "Dead")]
    public static class Patch_HSB_Dead_Broadcast
    {
        static void Postfix(HealthSimpleBase __instance, DamageInfo dmgInfo)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted || !mod.IsServer) return;

            var tag = __instance.GetComponent<NetDestructibleTag>();
            if (!tag) return;
            mod.Server_BroadcastDestructibleDead(tag.id, dmgInfo);
        }
    }


    [HarmonyPatch(typeof(Buff), "Setup")]
    static class Patch_Buff_Setup_Safe
    {
        // 反射缓存
        static readonly FieldInfo FI_master = AccessTools.Field(typeof(Buff), "master");
        static readonly FieldInfo FI_timeWhenStarted = AccessTools.Field(typeof(Buff), "timeWhenStarted");
        static readonly FieldInfo FI_buffFxPfb = AccessTools.Field(typeof(Buff), "buffFxPfb");
        static readonly FieldInfo FI_buffFxInstance = AccessTools.Field(typeof(Buff), "buffFxInstance");
        static readonly FieldInfo FI_OnSetupEvent = AccessTools.Field(typeof(Buff), "OnSetupEvent");
        static readonly FieldInfo FI_effects = AccessTools.Field(typeof(Buff), "effects");
        static readonly MethodInfo MI_OnSetup = AccessTools.Method(typeof(Buff), "OnSetup");

        static bool Prefix(Buff __instance, CharacterBuffManager manager)
        {
            // 有 CharacterItem：让原方法照常执行
            var masterCMC = manager ? manager.Master : null;
            var item = (masterCMC != null) ? masterCMC.CharacterItem : null;
            if (item != null && item.transform != null) return true;

            // —— 无 CharacterItem 的“兜底初始化” —— //
            // 写 master / timeWhenStarted
            FI_master?.SetValue(__instance, manager);
            FI_timeWhenStarted?.SetValue(__instance, Time.time);

            // 先把 Buff 掛到角色 Transform 上（不要去访问 CharacterItem.transform）
            var parent = masterCMC ? masterCMC.transform : __instance.transform.parent;
            if (parent) __instance.transform.SetParent(parent, false);

            // 刷新 FX：销毁旧的，按角色的 ArmorSocket/根节点生成新的
            var oldFx = FI_buffFxInstance?.GetValue(__instance) as GameObject;
            if (oldFx) Object.Destroy(oldFx);

            var pfb = FI_buffFxPfb?.GetValue(__instance) as GameObject;
            if (pfb && masterCMC && masterCMC.characterModel)
            {
                var fx = Object.Instantiate(pfb);
                var t = masterCMC.characterModel.ArmorSocket ? masterCMC.characterModel.ArmorSocket : masterCMC.transform;
                fx.transform.SetParent(t);
                fx.transform.position = t.position;
                fx.transform.localRotation = Quaternion.identity;
                FI_buffFxInstance?.SetValue(__instance, fx);
            }

            // 跳过 effects.SetItem（当前没 Item 可设），但先把 OnSetup / OnSetupEvent 触发掉
            MI_OnSetup?.Invoke(__instance, null);
            var onSetupEvent = FI_OnSetupEvent?.GetValue(__instance) as UnityEvent;
            onSetupEvent?.Invoke();

            // 挂一个一次性补丁组件，等 CharacterItem 可用后把 SetItem/SetParent 补上
            if (!__instance.gameObject.GetComponent<_BuffLateBinder>())
            {
                var binder = __instance.gameObject.AddComponent<_BuffLateBinder>();
                binder.Init(__instance, FI_effects);
            }

            //sans的主义
            return false;
        }


        [HarmonyPatch(typeof(CharacterBuffManager), nameof(CharacterBuffManager.AddBuff))]
        static class Patch_BroadcastBuffToOwner
        {
            static void Postfix(CharacterBuffManager __instance, Buff buffPrefab, CharacterMainControl fromWho, int overrideWeaponID)
            {
                var mod = ModBehaviour.Instance;
                if (mod == null || !mod.networkStarted || !mod.IsServer) return;
                if (buffPrefab == null) return;

                var target = __instance.Master;                // 被加 Buff 的角色
                if (target == null) return;

                // 只给“这名远端玩家本人”发：在服务器的 remoteCharacters: NetPeer -> GameObject 中查找
                NetPeer peer = null;
                foreach (var kv in mod.remoteCharacters)
                {
                    if (kv.Value == null) continue;
                    if (kv.Value == target.gameObject) { peer = kv.Key; break; }
                }
                if (peer == null) return; // 非玩家，或者就是主机本地角色

                // 发一条“自加 Buff”消息（只给这名玩家）
                var w = new NetDataWriter();
                w.Put((byte)Op.PLAYER_BUFF_SELF_APPLY); // 新 opcode（见 Mod.cs）
                w.Put(overrideWeaponID);   // weaponTypeId：客户端可用它解析出正确的 buff prefab
                w.Put(buffPrefab.ID);      // 兜底：buffId（若武器没法解析，就用 id 回退）
                peer.Send(w, DeliveryMethod.ReliableOrdered);
            }
        }


        [HarmonyPatch(typeof(CharacterBuffManager), nameof(CharacterBuffManager.AddBuff))]
        static class Patch_BroadcastBuffApply
        {
            static void Postfix(CharacterBuffManager __instance, Buff buffPrefab, CharacterMainControl fromWho, int overrideWeaponID)
            {
                var mod = ModBehaviour.Instance;
                if (mod == null || !mod.networkStarted || !mod.IsServer) return;
                if (buffPrefab == null) return;

                var target = __instance.Master; // 被加 Buff 的角色
                if (target == null) return;

                // ① 原有：只通知“被命中的那位本人客户端”做自加（保证本地玩法效果）
                NetPeer ownerPeer = null;
                foreach (var kv in mod.remoteCharacters)
                {
                    if (kv.Value == null) continue;
                    if (kv.Value == target.gameObject) { ownerPeer = kv.Key; break; }
                }
                if (ownerPeer != null)
                {
                    var w = new NetDataWriter();
                    w.Put((byte)Op.PLAYER_BUFF_SELF_APPLY);
                    w.Put(overrideWeaponID);
                    w.Put(buffPrefab.ID);
                    ownerPeer.Send(w, DeliveryMethod.ReliableOrdered);
                }

                // ② 如果“被命中者是主机本体”，就广播给所有客户端，让他们在“主机的代理对象”上也加 Buff（用于可见 FX）
                if (target.IsMainCharacter)
                {
                    var w2 = new NetDataWriter();
                    w2.Put((byte)Op.HOST_BUFF_PROXY_APPLY);
                    // 用你们现有的玩家标识：Host 的 endPoint 已在 InitializeLocalPlayer 里设为 "Host:端口"
                    w2.Put(mod.localPlayerStatus?.EndPoint ?? $"Host:{mod.port}");
                    w2.Put(overrideWeaponID);
                    w2.Put(buffPrefab.ID);
                    mod.netManager.SendToAll(w2, DeliveryMethod.ReliableOrdered);
                }
            }
        }
    }


    [HarmonyPatch(typeof(SceneLoaderProxy), "LoadScene")]
    public static class Patch_SceneLoaderProxy_Authority
    {
        static bool Prefix(SceneLoaderProxy __instance)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted) return true;
            if (mod.allowLocalSceneLoad) return true;


            string proxySceneId = Traverse.Create(__instance).Field<string>("sceneID").Value;
            bool useLoc = Traverse.Create(__instance).Field<bool>("useLocation").Value;
            var loc = Traverse.Create(__instance).Field<Duckov.Scenes.MultiSceneLocation>("location").Value;
            var curtain = Traverse.Create(__instance).Field<Eflatun.SceneReference.SceneReference>("overrideCurtainScene").Value;
            bool notifyEvac = Traverse.Create(__instance).Field<bool>("notifyEvacuation").Value;
            bool save = Traverse.Create(__instance).Field<bool>("saveToFile").Value;

            string targetId = proxySceneId;
            string locationName = useLoc ? loc.LocationName : null;
            string curtainGuid = (curtain != null) ? curtain.Guid : null;

            if (mod.IsServer)
            {
                mod.Host_BeginSceneVote_Simple(targetId, curtainGuid, notifyEvac, save, useLoc, locationName);
                return false;
            }
            else
            {

                mod.Client_RequestBeginSceneVote(targetId, curtainGuid, notifyEvac, save, useLoc, locationName);
                //string mySceneId = null;
                //try { mySceneId = mod.localPlayerStatus != null ? mod.localPlayerStatus.SceneId : null; } catch { } 

                //ModBehaviour.PlayerStatus host = null;
                //if (mod.clientPlayerStatuses != null)
                //{
                //    foreach (var kv in mod.clientPlayerStatuses)
                //    {
                //        var st = kv.Value;
                //        if (st == null) continue;
                //        bool isHostName = false;
                //        try { isHostName = (st.PlayerName == "Host"); } catch { }
                //        bool isHostId = false;
                //        try { isHostId = (!string.IsNullOrEmpty(st.EndPoint) && st.EndPoint.StartsWith("Host:")); } catch { }

                //        if (isHostName || isHostId) { host = st; break; }
                //    }
                //}

                //bool hostMissing = (host == null);

                //bool hostNotInGame = false;
                //try { hostNotInGame = (host != null && !host.IsInGame); } catch { } 

                //bool hostSceneDiff = false;
                //try
                //{
                //    string hostSid = (host != null) ? host.SceneId : null;
                //    hostSceneDiff = (!string.IsNullOrEmpty(hostSid) && !string.IsNullOrEmpty(mySceneId) && !string.Equals(hostSid, mySceneId, StringComparison.Ordinal));
                //}
                //catch { }

                //bool hostDead = false;
                //try
                //{
                //    // Host 的 EndPoint 在初始化时就是 "Host:{port}"（见d1 Mod.cs.InitializeLocalPlayer）
                //    string hostKey = $"Host:{mod.port}";

                //    if (mod.clientRemoteCharacters != null &&
                //        mod.clientRemoteCharacters.TryGetValue(hostKey, out var hostProxy) &&
                //        hostProxy)
                //    {
                //        var h = hostProxy.GetComponentInChildren<Health>(true);
                //        hostDead = (h == null) || h.CurrentHealth <= 0.001f;
                //    }
                //    else
                //    {
                //        // 如果“主机状态存在且与我同图”，但连主机代理都不存在，多半也是死亡后进入观战
                //        if (!hostMissing && !hostSceneDiff) hostDead = true;
                //    }
                //}
                //catch { }

                //// 原来的 allow 条件基础上，把 hostDead 并进去
                //bool allowClientVote = hostMissing || hostNotInGame || hostSceneDiff || hostDead;

                //if (allowClientVote)
                //{
                //    Debug.Log($"[SCENE] 客户端放行切图（允许投票）：target={targetId}, hostMissing={hostMissing}, hostNotInGame={hostNotInGame}, hostSceneDiff={hostSceneDiff}");
                //    mod.Client_RequestBeginSceneVote(targetId, curtainGuid, notifyEvac, save, useLoc, locationName);
                //    return false;
                //}
                Debug.Log($"[SCENE] 客户端放行切图（允许投票）：target={targetId}");
                return false;
            }
        }
    }


    [HarmonyPatch(typeof(InteractableLootbox), "StartLoot")]
    static class Patch_Lootbox_StartLoot_RequestState
    {
        static void Postfix(InteractableLootbox __instance, ref bool __result)
        {
            if (!__result) return;

            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted || m.IsServer) return;

            var inv = __instance ? __instance.Inventory : null;
            if (inv == null) return;

            inv.Loading = true;                 // 先挂起 UI
            m.Client_RequestLootState(inv);     // 请求快照
            m.KickLootTimeout(inv, 1.5f);       // 每次开箱都拉起 1.5s 兜底，避免二次打开卡死
        }
    }


    [HarmonyPatch(typeof(InteractableLootbox), "OnInteractStop")]
    static class Patch_Lootbox_OnInteractStop_DisableFogWhenAllInspected
    {
        static void Postfix(InteractableLootbox __instance)
        {
            var inv = __instance?.Inventory;
            if (inv == null) return;

            // 判断是否全部已检视
            bool allInspected = true;
            int last = inv.GetLastItemPosition();
            for (int i = 0; i <= last; i++)
            {
                var it = inv.GetItemAt(i);
                if (it != null && !it.Inspected) { allInspected = false; break; }
            }

            if (allInspected)
            {
                inv.NeedInspection = false; 
            }
   
        }
    }

  

    [HarmonyPatch(typeof(ItemStatsSystem.Inventory), "AddAt")]
    static class Patch_Inventory_AddAt_LootPut
    {
        static bool Prefix(ItemStatsSystem.Inventory __instance, ItemStatsSystem.Item item, int atPosition, ref bool __result)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted) return true;

            // 只在客户端、且不是“应用服务器快照”阶段时干预
            if (!m.IsServer && !m._applyingLootState)
            {
                bool targetIsLoot = LootboxDetectUtil.IsLootboxInventory(__instance) && !LootboxDetectUtil.IsPrivateInventory(__instance);
                var srcInv = item ? item.InInventory : null;
                bool srcIsLoot = LootboxDetectUtil.IsLootboxInventory(srcInv) && !LootboxDetectUtil.IsPrivateInventory(srcInv);

                // === A) 容器内换位 ===
                if (targetIsLoot && ReferenceEquals(srcInv, __instance))
                {
                    int srcPos = __instance.GetIndex(item);
                    if (srcPos == atPosition) { __result = true; return false; }

                    uint tk = m.Client_SendLootTakeRequest(__instance, srcPos, null, -1, null);
                    m.NoteLootReorderPending(tk, __instance, atPosition);
                    __result = true;
                    return false;
                }

                // === B) 其它库存 -> 容器 ===
                if (targetIsLoot && srcInv && !ReferenceEquals(srcInv, __instance))
                {
                    int srcPos = srcInv.GetIndex(item);
                    if (srcPos >= 0)
                    {
                        m.Client_SendLootTakeRequest(srcInv, srcPos, __instance, atPosition, null);
                        __result = true;
                        return false;
                    }
                }

                // === C) 容器 -> 其它库存（直接 PUT）===
                if (!targetIsLoot && srcIsLoot)
                {
                    int srcPos = srcInv.GetIndex(item);
                    if (srcPos >= 0)
                    {
                        m.Client_SendLootTakeRequest(srcInv, srcPos, __instance, atPosition, null);
                        __result = true;
                        return false;
                    }
                }

                // === D) 直接往容器放（UI 上新建/拖入） ===
                bool isLootInv = LootboxDetectUtil.IsLootboxInventory(__instance) && !LootboxDetectUtil.IsPrivateInventory(__instance);
                if (isLootInv)
                {
                    m.Client_SendLootPutRequest(__instance, item, atPosition);
                    __result = false;
                    return false;
                }
            }

            return true;
        }
    }


    [HarmonyPatch(typeof(ItemStatsSystem.Inventory), "AddItem")]
    static class Patch_Inventory_AddItem_LootPut
    {
        static bool Prefix(ItemStatsSystem.Inventory __instance, ItemStatsSystem.Item item, ref bool __result)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted) return true;

            // ★ 只在“真正的战利品容器初始化”时吞掉本地 Add
            if (!m.IsServer && m.ClientLootSetupActive)
            {
                bool isLootInv = LootboxDetectUtil.IsLootboxInventory(__instance)
                                 && !LootboxDetectUtil.IsPrivateInventory(__instance);
                if (isLootInv)
                {
                    try { if (item) { item.Detach(); UnityEngine.Object.Destroy(item.gameObject); } } catch { }
                    __result = true;
                    return false;
                }
            }

            if (!m.IsServer && !m._applyingLootState)
            {
                bool isLootInv = LootboxDetectUtil.IsLootboxInventory(__instance)
                                 && !LootboxDetectUtil.IsPrivateInventory(__instance);
                if (isLootInv)
                {
                    m.Client_SendLootPutRequest(__instance, item, 0);
                    __result = false;
                    return false;
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(ItemUtilities), "AddAndMerge")]
    static class Patch_ItemUtilities_AddAndMerge_LootPut
    {
        static bool Prefix(ItemStatsSystem.Inventory inventory, ItemStatsSystem.Item item, int preferedFirstPosition, ref bool __result)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted) return true;

            //  同样仅限“战利品容器初始化”时屏蔽
            if (!m.IsServer && m.ClientLootSetupActive)
            {
                bool isLootInv = LootboxDetectUtil.IsLootboxInventory(inventory)
                                 && !LootboxDetectUtil.IsPrivateInventory(inventory);
                if (isLootInv)
                {
                    try { if (item) { item.Detach(); UnityEngine.Object.Destroy(item.gameObject); } } catch { }
                    __result = true;
                    return false;
                }
            }

            if (!m.IsServer && !m._applyingLootState)
            {
                bool isLootInv = LootboxDetectUtil.IsLootboxInventory(inventory)
                                 && !LootboxDetectUtil.IsPrivateInventory(inventory);
                if (isLootInv)
                {
                    m.Client_SendLootPutRequest(inventory, item, preferedFirstPosition);
                    __result = false;
                    return false;
                }
            }

            return true;
        }

        static void Postfix(ItemStatsSystem.Inventory inventory, ItemStatsSystem.Item item, int preferedFirstPosition, bool __result)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted || !m.IsServer) return;
            if (!__result || m._serverApplyingLoot) return;

            bool isLootInv = LootboxDetectUtil.IsLootboxInventory(inventory) && !LootboxDetectUtil.IsPrivateInventory(inventory);
            if (isLootInv)
                m.Server_SendLootboxState(null, inventory);
        }
    }



    [HarmonyPatch(typeof(LootBoxLoader), "Setup")]
    static class Patch_LootBoxLoader_Setup_GuardClientInit
    {
        static void Prefix()
        {
            var m = ModBehaviour.Instance;
            if (m != null && m.networkStarted && !m.IsServer)
                m._clientLootSetupDepth++;
        }

        // 用 Finalizer 确保异常时也能退出“初始化阶段”
        static void Finalizer(Exception __exception)
        {
            var m = ModBehaviour.Instance;
            if (m != null && m.networkStarted && !m.IsServer && m._clientLootSetupDepth > 0)
                m._clientLootSetupDepth--;
        }
    }


    [HarmonyPatch(typeof(LootBoxLoader), "Setup")]
    static class Patch_LootBoxLoader_Setup_BroadcastOnServer
    {
        static async void Postfix(LootBoxLoader __instance)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted || !m.IsServer) return;
            await Cysharp.Threading.Tasks.UniTask.Yield(); // 等一帧，确保物品都进箱子
            var box = __instance ? __instance.GetComponent<InteractableLootbox>() : null;
            var inv = box ? box.Inventory : null;
            if (inv != null) m.Server_SendLootboxState(null, inv);
        }
    }

    // === 主机：Inventory.AddAt 成功后广播 ===
    [HarmonyPatch(typeof(ItemStatsSystem.Inventory), "AddAt")]
    static class Patch_Inventory_AddAt_BroadcastOnServer
    {
        static void Postfix(ItemStatsSystem.Inventory __instance, ItemStatsSystem.Item item, int atPosition, bool __result)
        {

            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted || !m.IsServer) return;
            if (!__result || m._serverApplyingLoot) return;
            if (!LootboxDetectUtil.IsLootboxInventory(__instance)) return;

            if (!LootboxDetectUtil.IsLootboxInventory(__instance) || LootboxDetectUtil.IsPrivateInventory(__instance)) return;

            m.Server_SendLootboxState(null, __instance);
        }
    }

    // === 主机：Inventory.AddItem 成功后广播 ===
    [HarmonyPatch(typeof(ItemStatsSystem.Inventory), "AddItem")]
    static class Patch_Inventory_AddItem_BroadcastLootState
    {
        static void Postfix(ItemStatsSystem.Inventory __instance, ItemStatsSystem.Item item, bool __result)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted || !m.IsServer) return;
            if (!__result || m._serverApplyingLoot) return;

            if (!LootboxDetectUtil.IsLootboxInventory(__instance) || LootboxDetectUtil.IsPrivateInventory(__instance)) return;


            var dict = InteractableLootbox.Inventories;
            bool isLootInv = dict != null && dict.ContainsValue(__instance);
            if (!isLootInv) return;

            m.Server_SendLootboxState(null, __instance);
        }
    }


    [HarmonyPatch(typeof(LootBoxLoader), "RandomActive")]
    static class Patch_LootBoxLoader_RandomActive_NetAuthority
    {
        static bool Prefix(LootBoxLoader __instance)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted || m.IsServer) return true;

            try
            {
                var core = MultiSceneCore.Instance;
                if (core == null) return true; // 没 core 就让它走原逻辑，避免极端时序问题

                // 计算与游戏一致的 key（复制 GetKey 的算法）
                int key = ModBehaviour_ComputeLootKeyCompat(__instance.transform);


                if (core.inLevelData != null && core.inLevelData.TryGetValue(key, out object obj) && obj is bool on)
                {
                    __instance.gameObject.SetActive(on);
                }
                else
                {
                    __instance.gameObject.SetActive(false); // 未拿到就先关
                }

                return false; // 阻止原始随机
            }
            catch
            {
                return true; // 防守式：异常时走原逻辑
            }
        }

        static int ModBehaviour_ComputeLootKeyCompat(Transform t)
        {
            if (t == null) return 0;
            var v = t.position * 10f;
            int x = Mathf.RoundToInt(v.x);
            int y = Mathf.RoundToInt(v.y);
            int z = Mathf.RoundToInt(v.z);
            var v3i = new Vector3Int(x, y, z);
            return v3i.GetHashCode();
        }

    }

    [HarmonyPatch(typeof(InteractableLootbox), "get_Inventory")]
    static class Patch_Lootbox_GetInventory_Safe
    {
        // 已存在：异常/空返回时强制创建
        static System.Exception Finalizer(InteractableLootbox __instance, ref Inventory __result, System.Exception __exception)
        {
            try
            {
                if (__instance != null && (__exception != null || __result == null))
                {
                    var mCreate = AccessTools.Method(typeof(InteractableLootbox), "GetOrCreateInventory", new System.Type[] { typeof(InteractableLootbox) });
                    if (mCreate != null)
                    {
                        var inv = (Inventory)mCreate.Invoke(null, new object[] { __instance });
                        if (inv != null)
                        {
                            __result = inv;
                            return null; // 吞掉异常
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        static void Postfix(InteractableLootbox __instance, ref Inventory __result)
        {
            if (__result != null) return;

            // 用 LevelManager.LootBoxInventories 做二次兜底
            try
            {
                int key = ModBehaviour.Instance != null
                          ? ModBehaviour.Instance.ComputeLootKey(__instance.transform)
                          : __instance.GetHashCode();

                // 看 InteractableLootbox.Inventories
                var dict1 = InteractableLootbox.Inventories;
                if (dict1 != null && dict1.TryGetValue(key, out var inv1) && inv1)
                {
                    __result = inv1;
                    return;
                }

                // 再看 LevelManager.LootBoxInventories
                var lm = LevelManager.Instance;
                var dict2 = lm != null ? LevelManager.LootBoxInventories : null;
                if (dict2 != null && dict2.TryGetValue(key, out var inv2) && inv2)
                {
                    __result = inv2;

                    // 顺便把 InteractableLootbox.Inventories 也对齐一次
                    try { if (dict1 != null) dict1[key] = inv2; } catch { }
                }
            }
            catch { }
        }
    }


    [HarmonyPatch(typeof(InteractableLootbox), "get_Inventory")]
    static class Patch_Lootbox_GetInventory_Register
    {
        static void Postfix(InteractableLootbox __instance, ref ItemStatsSystem.Inventory __result)
        {
            try
            {
                if (!__result) return;

                int key = (ModBehaviour.Instance != null)
                          ? ModBehaviour.Instance.ComputeLootKey(__instance.transform)
                          : __instance.GetHashCode();

                var dictA = InteractableLootbox.Inventories;
                if (dictA != null) dictA[key] = __result;

                var lm = LevelManager.Instance;
                var dictB = lm != null ? LevelManager.LootBoxInventories : null;
                if (dictB != null) dictB[key] = __result;
            }
            catch { }
        }
    }

    public sealed class NetAiTag : MonoBehaviour
    {
        public int aiId;
        public int? iconTypeOverride;   // 来自主机的 CharacterIconTypes（int）
        public bool? showNameOverride;  // 主机裁决是否显示名字
        public string nameOverride;     // 主机下发的显示名（纯文本

        void Awake() { Guard(); }
        void OnEnable() { Guard(); }

        void Guard()
        {
            try
            {
                var cmc = GetComponent<CharacterMainControl>();
                var mod = ModBehaviour.Instance;
                if (!cmc || mod == null) return;

                if (!mod.IsRealAI(cmc))
                {
                    Destroy(this);
                }
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(CharacterSpawnerRoot), "StartSpawn")]
    static class Patch_Root_StartSpawn
    {
        static readonly System.Collections.Generic.HashSet<int> _waiting = new HashSet<int>();
        static readonly System.Collections.Generic.Stack<UnityEngine.Random.State> _rngStack = new Stack<UnityEngine.Random.State>();
        static readonly System.Reflection.MethodInfo _miStartSpawn =
            AccessTools.Method(typeof(CharacterSpawnerRoot), "StartSpawn");

        static bool Prefix(CharacterSpawnerRoot __instance)
        {
            try
            {
                var mod = ModBehaviour.Instance;
                int rootId = mod.StableRootId(__instance);

                // 核心科技:) 种子未到 → 阻止原版生成，并排队等待；到种子后再反射调用 StartSpawn()
                if (!mod.IsServer && !mod.aiRootSeeds.ContainsKey(rootId))
                {
                    if (_waiting.Add(rootId))
                        __instance.StartCoroutine(WaitSeedAndSpawn(__instance, rootId));
                    return false;
                }

                // 进入“随机数种子作用域”
                int useSeed = mod.IsServer ? mod.DeriveSeed(mod.sceneSeed, rootId) : mod.aiRootSeeds[rootId];
                _rngStack.Push(UnityEngine.Random.state);
                UnityEngine.Random.InitState(useSeed);
                return true;
            }
            catch { return true; }
        }

        static void ForceActivateHierarchy(Transform t)
        {
            while (t)
            {
                if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
                t = t.parent;
            }
        }

        static System.Collections.IEnumerator WaitSeedAndSpawn(CharacterSpawnerRoot inst, int rootId)
        {
            var mod = ModBehaviour.Instance;
            while (mod && !mod.aiRootSeeds.ContainsKey(rootId)) yield return null;

            _waiting.Remove(rootId);

            if (inst)
            {
                // 先把刷怪根及父链强制激活，防止在非激活层级里生成失败
                ForceActivateHierarchy(inst.transform);

                if (_miStartSpawn != null)
                    _miStartSpawn.Invoke(inst, null);  // 反射调用 private StartSpawn()
            }
        }

        static void Postfix(CharacterSpawnerRoot __instance)
        {
            try
            {
                if (_rngStack.Count > 0) UnityEngine.Random.state = _rngStack.Pop();

                // 你原有的“给 AI 打标签 / 注册 / 主机广播负载”逻辑保留
                var list = Traverse.Create(__instance)
                    .Field<System.Collections.Generic.List<CharacterMainControl>>("createdCharacters")
                    .Value;

                if (list != null && ModBehaviour.Instance.freezeAI)
                    foreach (var c in list) ModBehaviour.Instance.TryFreezeAI(c);

                if (list != null)
                {
                    var mod = ModBehaviour.Instance;
                    int rootId = mod.StableRootId(__instance);

                    // 按“名称 + 量化坐标 + InstanceID”稳定排序，避免回调时序导致乱序
                    var ordered = new List<CharacterMainControl>(list);
                    ordered.RemoveAll(c => !c);
                    ordered.Sort((a, b) =>
                    {
                        int n = string.Compare(a.name, b.name, StringComparison.Ordinal);
                        if (n != 0) return n;
                        var pa = a.transform.position; var pb = b.transform.position;
                        int ax = Mathf.RoundToInt(pa.x * 100f), az = Mathf.RoundToInt(pa.z * 100f), ay = Mathf.RoundToInt(pa.y * 100f);
                        int bx = Mathf.RoundToInt(pb.x * 100f), bz = Mathf.RoundToInt(pb.z * 100f), by = Mathf.RoundToInt(pb.y * 100f);
                        if (ax != bx) return ax.CompareTo(bx);
                        if (az != bz) return az.CompareTo(bz);
                        if (ay != by) return ay.CompareTo(by);
                        return a.GetInstanceID().CompareTo(b.GetInstanceID());
                    });

                    for (int i = 0; i < ordered.Count; i++)
                    {
                        var cmc = ordered[i];
                        if (!cmc || !mod.IsRealAI(cmc)) continue;

                        int aiId = mod.DeriveSeed(rootId, i + 1);
                        var tag = cmc.GetComponent<NetAiTag>() ?? cmc.gameObject.AddComponent<NetAiTag>();

                        // 主机赋 id + 登记 + 广播；客户端保持 tag.aiId=0 等待绑定（见修复 A）
                        if (mod.IsServer)
                        {
                            tag.aiId = aiId;
                            mod.RegisterAi(aiId, cmc);
                            mod.Server_BroadcastAiLoadout(aiId, cmc);
                        }
                    }


                    // 主机在本 root 刷完后即刻发一帧位置快照，收敛初始误差
                    if (mod.IsServer)
                    {
                        mod.Server_BroadcastAiTransforms();
                    }
                }
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(CharacterSpawnerRoot), "Init")]
    static class Patch_Root_Init_FixContain
    {
        static bool Prefix(CharacterSpawnerRoot __instance)
        {
            try
            {
                var msc = Duckov.Scenes.MultiSceneCore.Instance;

                // 仅在 SpawnerGuid != 0 时才做“重复过滤”
                if (msc != null && __instance.SpawnerGuid != 0 &&
                    msc.usedCreatorIds.Contains(__instance.SpawnerGuid))
                {
                    return true; // 放行原版 → 它会销毁重复体
                }

                var tr = Traverse.Create(__instance);
                tr.Field("inited").SetValue(true);

                var spComp = tr.Field<CharacterSpawnerComponentBase>("spawnerComponent").Value;
                if (spComp != null) spComp.Init(__instance);

                int buildIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
                tr.Field("relatedScene").SetValue(buildIndex);

                __instance.transform.SetParent(null);
                if (msc != null)
                {
                    Duckov.Scenes.MultiSceneCore.MoveToMainScene(__instance.gameObject);
                    // 仅在 Guid 非 0 时登记，避免把“0”当成全场唯一
                    if (__instance.SpawnerGuid != 0)
                        msc.usedCreatorIds.Add(__instance.SpawnerGuid);
                }

                var mod = ModBehaviour.Instance;
                if (mod != null && mod.IsServer) mod.Server_SendRootSeedDelta(__instance);


                return false; // 跳过原始 Init（避免误删）
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AI-SEED] Patch_Root_Init_FixContain failed: " + e);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(CharacterSpawnerRoot), "Update")]
    static class Patch_Root_Update_ClientAutoSpawn
    {
        static readonly MethodInfo _miStartSpawn =
            AccessTools.Method(typeof(CharacterSpawnerRoot), "StartSpawn");
        static readonly MethodInfo _miCheckTiming =
            AccessTools.Method(typeof(CharacterSpawnerRoot), "CheckTiming"); 

        static void Postfix(CharacterSpawnerRoot __instance)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted || mod.IsServer) return;

            var tr = Traverse.Create(__instance);
            bool inited = tr.Field<bool>("inited").Value;
            bool created = tr.Field<bool>("created").Value;
            if (!inited || created) return;

            int rootId = mod.StableRootId(__instance);

            // 没种子 → 兼容一次 AltId 映射（你已有）
            if (!mod.aiRootSeeds.ContainsKey(rootId))
            {
                int altId = mod.StableRootId_Alt(__instance);
                if (mod.aiRootSeeds.TryGetValue(altId, out var seed))
                    mod.aiRootSeeds[rootId] = seed;
                else
                    return; // 种子确实没到，别刷
            }

            // 关键：尊重原版判断（时间/天气/触发器）
            bool ok = false;
            try { ok = (bool)_miCheckTiming.Invoke(__instance, null); } catch { }
            if (!ok) return;

            // 与原逻辑一致：确保层级激活再刷
            ForceActivateHierarchy(__instance.transform);
            try { _miStartSpawn?.Invoke(__instance, null); } catch { }
        }

        static void ForceActivateHierarchy(Transform t)
        {
            while (t)
            {
                if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
                t = t.parent;
            }
        }
    }



    [HarmonyPatch(typeof(CharacterSpawnerGroup), "Awake")]
    static class Patch_Group_Awake
    {
        static void Postfix(CharacterSpawnerGroup __instance)
        {
            try
            {
                var mod = ModBehaviour.Instance;

                // 用“场景种子 + 该 Group 的 Transform 路径哈希”派生随机
                int gid = mod.StableHash(mod.TransformPath(__instance.transform));
                int seed = mod.DeriveSeed(mod.sceneSeed, gid);

                var rng = new System.Random(seed);
                if (__instance.hasLeader)
                {
                    // 与原版相同的比较方式：保留队长的概率 = hasLeaderChance
                    bool keep = rng.NextDouble() <= __instance.hasLeaderChance;
                    __instance.hasLeader = keep;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AI-SEED] Group.Awake Postfix 出错: {e.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(AICharacterController), "Init")]
    static class Patch_AI_Init
    {
        static void Postfix(AICharacterController __instance, CharacterMainControl _characterMainControl)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted) return;
            if (!mod.IsRealAI(_characterMainControl)) return;

            var cmc = _characterMainControl;
            if (mod.freezeAI) mod.TryFreezeAI(cmc);

            // 1) 取/补标签
            var tag = cmc.GetComponent<NetAiTag>() ?? cmc.gameObject.AddComponent<NetAiTag>();

            // ★ 客户端不在这里分配/登记 aiId，避免和主机分配顺序打架
            if (!mod.IsServer)
            {
                ModBehaviour.Instance.MarkAiSceneReady(); // 让客户端开始吞变换队列
                return;
            }

            // 2) 主机端：若还没 aiId，就按 rootId + 序号 分配
            if (tag.aiId == 0)
            {
                int rootId = 0;
                var root = cmc.GetComponentInParent<CharacterSpawnerRoot>();
                rootId = (root && root.SpawnerGuid != 0)
                    ? root.SpawnerGuid
                    : mod.StableHash(mod.TransformPath(root ? root.transform : cmc.transform));
                int serial = mod.NextAiSerial(rootId);
                tag.aiId = mod.DeriveSeed(rootId, serial);
            }

            // 3) 主机登记（客户端会在收到主机快照后再登记）
            mod.RegisterAi(tag.aiId, cmc);
            ModBehaviour.Instance.MarkAiSceneReady();
        }
    }



    [HarmonyPatch(typeof(Duckov.Utilities.SetActiveByPlayerDistance), "FixedUpdate")]
    static class Patch_SABPD_FixedUpdate_AllPlayersUnion
    {
        static bool Prefix(Duckov.Utilities.SetActiveByPlayerDistance __instance)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted) return true; // 单机：走原版

            var tr = Traverse.Create(__instance);

            // 被管理对象列表
            var list = tr.Field<List<GameObject>>("cachedListRef").Value;
            if (list == null) return false;

            // 距离阈值
            float dist;
            var prop = AccessTools.Property(__instance.GetType(), "Distance");
            if (prop != null) dist = (float)prop.GetValue(__instance, null);
            else dist = tr.Field<float>("distance").Value;
            float d2 = dist * dist;

            // === 收集所有在线玩家的位置（本地 + 远端） ===
            var sources = new List<Vector3>(8);
            var main = CharacterMainControl.Main;
            if (main) sources.Add(main.transform.position);

            foreach (var kv in mod.playerStatuses)
            {
                var st = kv.Value;
                if (st != null && st.IsInGame) sources.Add(st.Position);
            }

            // 没拿到位置：放行原版
            if (sources.Count == 0) return true;

            // 逐个对象：任一玩家在范围内就激活
            for (int i = 0; i < list.Count; i++)
            {
                var go = list[i];
                if (!go) continue;

                bool within = false;
                var p = go.transform.position;
                for (int s = 0; s < sources.Count; s++)
                {
                    if ((p - sources[s]).sqrMagnitude <= d2) { within = true; break; }
                }
                if (go.activeSelf != within) go.SetActive(within);
            }

            return false; // 跳过原方法

        }

    }

    [HarmonyPatch(typeof(CharacterAnimationControl_MagicBlend), "OnAttack")]
    static class Patch_AI_OnAttack_Broadcast
    {
        static void Postfix(CharacterAnimationControl_MagicBlend __instance)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.IsServer) return;

            var cmc = __instance.characterMainControl;
            if (!cmc) return;

            // 排除玩家本体，只给 AI 发
            // 判断条件：有 AI 组件/或 NetAiTag（你现成的 AI id 标签）
            var aiCtrl = cmc.GetComponent<AICharacterController>();
            var aiTag = cmc.GetComponent<NetAiTag>();
            if (!aiCtrl && aiTag == null) return;

            int aiId = aiTag != null ? aiTag.aiId : 0;
            if (aiId == 0) return;

            mod.writer.Reset();
            mod.writer.Put((byte)Op.AI_ATTACK_SWING);
            mod.writer.Put(aiId);
            mod.netManager.SendToAll(mod.writer, DeliveryMethod.ReliableUnordered);
        }
    }

    [HarmonyPatch(typeof(CharacterMainControl), "OnChangeItemAgentChangedFunc")]
    static class Patch_CMC_OnChangeHold_AIRebroadcast
    {
        static void Postfix(CharacterMainControl __instance)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted || !mod.IsServer) return; // 只在主机上发
            if (!__instance || __instance == CharacterMainControl.Main) return; // 排除本地玩家

            var tag = __instance.GetComponent<NetAiTag>();
            if (tag == null || tag.aiId == 0) return;

            // AI 切换/拿起/放下手持后，立即广播一份“装备+武器”快照
            mod.Server_BroadcastAiLoadout(tag.aiId, __instance);
        }
    }

    [HarmonyPatch(typeof(CharacterAnimationControl_MagicBlend), "OnAttack")]
    static class Patch_AI_OnAttack_BroadcastAll
    {
        static void Postfix(CharacterAnimationControl_MagicBlend __instance)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted || !mod.IsServer) return;

            var cmc = __instance ? __instance.characterMainControl : null;
            if (!cmc) return;

            if (cmc.IsMainCharacter) return;

            // 通过 NetAiTag 拿 aiId
            var tag = cmc.GetComponent<NetAiTag>();
            if (tag == null || tag.aiId == 0) return;

            // 持枪的 AI：逐弹丸广播由 Projectile.Init/Postfix 完成；这里不要再额外发送 FIRE_EVENT
            var gun = cmc.GetGun();
            if (gun != null)
            {
                return;
            }

            // 近战：复用玩家的 MELEE_ATTACK_SWING
            var w = new NetDataWriter();
            w.Put((byte)Op.MELEE_ATTACK_SWING);
            w.Put($"AI:{tag.aiId}");
            w.Put(__instance.attackTime); // 有就写；没有也无妨
            mod.netManager.SendToAll(w, DeliveryMethod.ReliableOrdered);
        }

    }

    [HarmonyPatch(typeof(CharacterAnimationControl_MagicBlend), "OnAttack")]
    static class Patch_AI_OnAttack_MeleeOnly
    {
        static void Postfix(CharacterAnimationControl_MagicBlend __instance)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted || !mod.IsServer) return;

            var cmc = __instance ? __instance.characterMainControl : null;
            if (!cmc || cmc.IsMainCharacter) return;

            var tag = cmc.GetComponent<NetAiTag>();
            if (tag == null || tag.aiId == 0) return;

            // 仅近战：复用玩家的 MELEE_ATTACK_SWING 协议
            var gun = cmc.GetGun();
            if (gun != null) return; // 手里是枪：真正的弹丸广播由 Projectile.Init 完成

            var w = new NetDataWriter();
            w.Put((byte)Op.MELEE_ATTACK_SWING);
            w.Put($"AI:{tag.aiId}");
            mod.netManager.SendToAll(w, DeliveryMethod.ReliableUnordered);
        }
    }



    [HarmonyPatch(typeof(CharacterMainControl), nameof(CharacterMainControl.SetCharacterModel))]
    static class Patch_CMC_SetCharacterModel_FaceReapply
    {
        static void Postfix(CharacterMainControl __instance)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || mod.IsServer) return;
            mod.ReapplyFaceIfKnown(__instance);
        }
    }

    [HarmonyPatch(typeof(CharacterMainControl), nameof(CharacterMainControl.SetCharacterModel))]
    static class Patch_CMC_SetCharacterModel_FaceReapply_Client
    {
        static void Postfix(CharacterMainControl __instance)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || mod.IsServer) return; // 只在客户端
            mod.ReapplyFaceIfKnown(__instance);
        }
    }

    // 主机：一旦换了模型，立刻二次广播（模型名/图标/脸 最新状态）
    [HarmonyPatch(typeof(CharacterMainControl), nameof(CharacterMainControl.SetCharacterModel))]
    static class Patch_CMC_SetCharacterModel_Rebroadcast_Server
    {
        static void Postfix(CharacterMainControl __instance)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.IsServer) return;

            int aiId = -1;
            foreach (var kv in mod.aiById) { if (kv.Value == __instance) { aiId = kv.Key; break; } }
            if (aiId < 0) return;

            if (ModBehaviour.LogAiLoadoutDebug)
                Debug.Log($"[AI-REBROADCAST] aiId={aiId} after SetCharacterModel");
            mod.Server_BroadcastAiLoadout(aiId, __instance);
        }
    }

    [HarmonyPatch(typeof(Health), "Hurt", new[] { typeof(global::DamageInfo) })]
    static class Patch_AIHealth_Hurt_HostAuthority
    {
        [HarmonyPriority(Priority.High)]
        static bool Prefix(Health __instance, ref global::DamageInfo damageInfo)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted) return true;
            if (mod.IsServer) return true;            // 主机照常
            bool isMain = false; try { isMain = __instance.IsMainCharacterHealth; } catch { }
            if (isMain) return true;

            if (__instance.gameObject.GetComponent<AutoRequestHealthBar>() != null)
            {
                return false;
            }

            // 是否 AI
            CharacterMainControl victim = null;
            try { victim = __instance.TryGetCharacter(); } catch { }
            if (!victim) { try { victim = __instance.GetComponentInParent<CharacterMainControl>(); } catch { } }

            bool victimIsAI = victim &&
                              (victim.GetComponent<AICharacterController>() != null ||
                               victim.GetComponent<NetAiTag>() != null);
            if (!victimIsAI) return true;

            // —— 不处理 AI→AI —— 
            var attacker = damageInfo.fromCharacter;
            bool attackerIsAI = attacker &&
                                (attacker.GetComponent<AICharacterController>() != null ||
                                 attacker.GetComponent<NetAiTag>() != null);
            if (attackerIsAI)
                return false; // 直接阻断，AI↔AI 不做任何本地效果


          //  LocalHitKillFx.ClientPlayForAI(victim, damageInfo, predictedDead: false);

            return false; 
        }

        // 主机在结算后广播 AI 当前血量（你已有的广播逻辑，保留）
        static void Postfix(Health __instance, global::DamageInfo damageInfo)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted || !mod.IsServer) return;

            var cmc = __instance.TryGetCharacter();
            if (!cmc) { try { cmc = __instance.GetComponentInParent<CharacterMainControl>(); } catch { } }
            if (!cmc) return;

            var tag = cmc.GetComponent<NetAiTag>();
            if (!tag) return;

            if (ModBehaviour.LogAiHpDebug)
                Debug.Log($"[AI-HP][SERVER] Hurt => broadcast aiId={tag.aiId} cur={__instance.CurrentHealth}");
            mod.Server_BroadcastAiHealth(tag.aiId, __instance.MaxHealth, __instance.CurrentHealth);
        }
    }

    // 回血/设血：主机也要广播（治疗、药效、脚本设血等）
    [HarmonyPatch(typeof(Health), "SetHealth")]
    static class Patch_AIHealth_SetHealth_Broadcast
    {
        static void Postfix(Health __instance, float healthValue)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted || !mod.IsServer) return;

            var cmc = __instance.TryGetCharacter();
            if (!cmc) { try { cmc = __instance.GetComponentInParent<CharacterMainControl>(); } catch { } }
            if (!cmc || !cmc.GetComponent<NetAiTag>()) return;

            var tag = cmc.GetComponent<NetAiTag>();
            if (!tag) return;

            if (ModBehaviour.LogAiHpDebug) Debug.Log($"[AI-HP][SERVER] SetHealth => broadcast aiId={tag.aiId} cur={__instance.CurrentHealth}");
            mod.Server_BroadcastAiHealth(tag.aiId, __instance.MaxHealth, __instance.CurrentHealth);
        }
    }

    [HarmonyPatch(typeof(Health), "AddHealth")]
    static class Patch_AIHealth_AddHealth_Broadcast
    {
        static void Postfix(Health __instance, float healthValue)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted || !mod.IsServer) return;

            var cmc = __instance.TryGetCharacter();
            if (!cmc) { try { cmc = __instance.GetComponentInParent<CharacterMainControl>(); } catch { } }
            if (!cmc || !cmc.GetComponent<NetAiTag>()) return;

            var tag = cmc.GetComponent<NetAiTag>();
            if (!tag) return;

            if (ModBehaviour.LogAiHpDebug) Debug.Log($"[AI-HP][SERVER] AddHealth => broadcast aiId={tag.aiId} cur={__instance.CurrentHealth}");
            mod.Server_BroadcastAiHealth(tag.aiId, __instance.MaxHealth, __instance.CurrentHealth);
        }
    }

    internal static class DeadLootSpawnContext
    {
        [ThreadStatic] public static CharacterMainControl InOnDead;
    }

    // 进入/离开 OnDead 时打/清标记（只关心 AI，不含玩家）
    [HarmonyPatch(typeof(CharacterMainControl), "OnDead")]
    static class Patch_CMC_OnDead_Mark
    {
        static void Prefix(CharacterMainControl __instance)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted) return;
            if (!__instance) return;

            // 只给 AI 打标记（排除本机玩家）
            if (__instance == CharacterMainControl.Main) return;
            bool isAI = __instance.GetComponent<AICharacterController>() != null
                        || __instance.GetComponent<NetAiTag>() != null;
            if (!isAI) return;

            DeadLootSpawnContext.InOnDead = __instance;
        }
        static void Finalizer()
        {
            DeadLootSpawnContext.InOnDead = null;
        }
    }


    // 阻断：客户端在“死亡路径”里不要本地创建（避免双生）
    [HarmonyPatch(typeof(InteractableLootbox), "CreateFromItem")]
    static class Patch_Lootbox_CreateFromItem_BlockClient
    {
        static bool Prefix()
        {
            var mod = ModBehaviour.Instance;
            if (mod != null && mod.networkStarted && !mod.IsServer && DeadLootSpawnContext.InOnDead != null)
                return false; // 客户端处于OnDead路径→禁止本地创建
            return true;
        }
    }

    // 广播：服务端在 CreateFromItem 返回实例的这一刻立即广播 spawn + state
    [HarmonyPatch(typeof(InteractableLootbox), "CreateFromItem")]
    static class Patch_Lootbox_CreateFromItem_DeferredSpawn
    {
        static void Postfix(InteractableLootbox __result)
        {
            var mod = ModBehaviour.Instance;
            var dead = DeadLootSpawnContext.InOnDead;
            if (mod == null || !mod.networkStarted || !mod.IsServer) return;
            if (dead == null || !__result) return;

            mod.StartCoroutine(DeferredSpawn(__result, dead));
        }

        static System.Collections.IEnumerator DeferredSpawn(InteractableLootbox box, CharacterMainControl who)
        {
            yield return null; 
            var mod = ModBehaviour.Instance;
            if (mod && mod.IsServer && box) mod.Server_OnDeadLootboxSpawned(box, who);
        }
    }

    [HarmonyPatch(typeof(InteractableLootbox), "CreateFromItem")]
    static class Patch_Lootbox_CreateFromItem_Register
    {
        static void Postfix(InteractableLootbox __result)
        {
            try
            {
                return;
                //之前排查击杀卡顿ret了的，优化好了一点之后就忘记放行了好像也不会影响正常的东西，能用就先别管：）

                if (!__result) return;
                var inv = __result.Inventory;
                if (!inv) return;

                int key = ModBehaviour.Instance != null
                          ? ModBehaviour.Instance.ComputeLootKey(__result.transform)
                          : __result.GetHashCode(); // 兜底

                var dict = InteractableLootbox.Inventories;
                if (dict != null) dict[key] = inv;
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(Duckov.UI.HealthBar), "RefreshCharacterIcon")]
    static class Patch_HealthBar_RefreshCharacterIcon_Override
    {
        static void Postfix(Duckov.UI.HealthBar __instance)
        {
            try
            {
                var h = __instance.target;
                if (!h) return;

                var cmc = h.TryGetCharacter();
                if (!cmc) return;

                var tag = cmc.GetComponent<NetAiTag>();
                if (!tag) return;

                // 若没有任何覆写数据，就不动原版结果
                bool hasIcon = tag.iconTypeOverride.HasValue;
                bool hasShow = tag.showNameOverride.HasValue;
                bool hasName = !string.IsNullOrEmpty(tag.nameOverride);
                if (!hasIcon && !hasShow && !hasName) return;

                // 取到 UI 私有字段
                var tr = Traverse.Create(__instance);
                var levelIcon = tr.Field<UnityEngine.UI.Image>("levelIcon").Value;
                var nameText = tr.Field<TMPro.TextMeshProUGUI>("nameText").Value;

                // 1) 图标覆写（有就用，没有就保留原版）
                if (levelIcon && hasIcon)
                {
                    var sp = ResolveIconSpriteCompat(tag.iconTypeOverride.Value);
                    if (sp)
                    {
                        levelIcon.sprite = sp;
                        levelIcon.gameObject.SetActive(true);
                    }
                    else
                    {
                        levelIcon.gameObject.SetActive(false);
                    }
                }

                // 2) 名字显隐与文本（主机裁决优先；boss/elete 兜底强制显示）
                bool show = hasShow ? tag.showNameOverride.Value : (cmc.characterPreset ? cmc.characterPreset.showName : false);
                if (tag.iconTypeOverride.HasValue)
                {
                    var t = (CharacterIconTypes)tag.iconTypeOverride.Value;
                    if (!show && (t == CharacterIconTypes.boss || t == CharacterIconTypes.elete))
                        show = true;
                }

                if (nameText)
                {
                    if (show)
                    {
                        if (hasName) nameText.text = tag.nameOverride;
                        nameText.gameObject.SetActive(true);
                    }
                    else
                    {
                        nameText.gameObject.SetActive(false);
                    }
                }
            }
            catch { /* 防守式：别让UI崩 */ }
        }

        // 拷一份兼容的解析函数（避免跨文件访问）
        static Sprite ResolveIconSpriteCompat(int iconType)
        {
            switch ((CharacterIconTypes)iconType)
            {
                case CharacterIconTypes.elete: return Duckov.Utilities.GameplayDataSettings.UIStyle.EleteCharacterIcon;
                case CharacterIconTypes.pmc: return Duckov.Utilities.GameplayDataSettings.UIStyle.PmcCharacterIcon;
                case CharacterIconTypes.boss: return Duckov.Utilities.GameplayDataSettings.UIStyle.BossCharacterIcon;
                case CharacterIconTypes.merchant: return Duckov.Utilities.GameplayDataSettings.UIStyle.MerchantCharacterIcon;
                case CharacterIconTypes.pet: return Duckov.Utilities.GameplayDataSettings.UIStyle.PetCharacterIcon;
                default: return null;
            }
        }
    }

    // 允许 LootView.RegisterEvents 总是执行；只做异常兜底，避免首次打开因 open==false 而错过注册
    [HarmonyPatch(typeof(Duckov.UI.LootView), "RegisterEvents")]
    static class Patch_LootView_RegisterEvents_Safe
    {
        // 不要 Prefix 拦截，让原方法总是运行
        static System.Exception Finalizer(Duckov.UI.LootView __instance, System.Exception __exception)
        {
            if (__exception != null)
            {
                UnityEngine.Debug.LogWarning("[LOOT][UI] RegisterEvents threw and was swallowed: " + __exception);
                return null; // 吞掉异常，保持 UI 可用
            }
            return null;
        }
    }


    // 翻页：未打开时直接吞掉
    [HarmonyPatch(typeof(Duckov.UI.LootView), "OnPreviousPage")]
    static class Patch_LootView_OnPreviousPage_OnlyWhenOpen
    {
        static bool Prefix(Duckov.UI.LootView __instance)
        {
            bool isOpen = false;
            try
            {
                var tr = Traverse.Create(__instance);
                try { isOpen = tr.Property<bool>("open").Value; }
                catch { isOpen = tr.Field<bool>("open").Value; }
            }
            catch { }
            return isOpen; // 未打开==false -> 不进原方法
        }
    }

    [HarmonyPatch(typeof(Duckov.UI.LootView), "OnNextPage")]
    static class Patch_LootView_OnNextPage_OnlyWhenOpen
    {
        static bool Prefix(Duckov.UI.LootView __instance)
        {
            bool isOpen = false;
            try
            {
                var tr = Traverse.Create(__instance);
                try { isOpen = tr.Property<bool>("open").Value; }
                catch { isOpen = tr.Field<bool>("open").Value; }
            }
            catch { }
            return isOpen;
        }
    }

    [HarmonyPatch(typeof(Duckov.UI.LootView), "get_TargetInventory")]
    static class Patch_LootView_GetTargetInventory_Safe
    {
        static System.Exception Finalizer(Duckov.UI.LootView __instance,
                                          ref ItemStatsSystem.Inventory __result,
                                          System.Exception __exception)
        {
            if (__exception != null)
            {
                __result = null;     // 直接当“未就绪/无容器”处理
                return null;         // 吞掉异常
            }
            return null;
        }
    }

    [HarmonyPatch(typeof(Slot), nameof(Slot.Plug))]
    public static class Patch_Slot_Plug_PickupCleanup
    {
        const float PICK_RADIUS = 2.5f;                 // 与库存补丁保持一致的半径
        const QueryTriggerInteraction QTI = QueryTriggerInteraction.Collide;
        const int LAYER_MASK = ~0;

        // 原签名：bool Plug(Item otherItem, out Item unpluggedItem, bool dontForce = false, Slot[] acceptableSlot = null, int acceptableSlotMask = 0)
        static void Postfix(Slot __instance, Item otherItem, Item unpluggedItem, bool __result)
        {
            if (!__result || otherItem == null) return;

            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted) return;

            // --- 客户端：发拾取请求 + 本地销毁地上Agent ---
            if (!mod.IsServer)
            {
                // A) 直接命中：字典里就是这个 item 引用（非合堆最常见）
                if (TryFindId(mod.clientDroppedItems, otherItem, out uint cid))
                {
                    LocalDestroyAgent(otherItem);
                    SendPickupReq(mod, cid);
                    return;
                }

                // B) 合堆/引用变化：用近场 NetDropTag 反查 ID
                if (TryFindNearestTaggedId(otherItem, out uint nearId))
                {
                    LocalDestroyAgentById(mod.clientDroppedItems, nearId);
                    SendPickupReq(mod, nearId);
                }
                return;
            }

            // --- 主机：本地销毁并广播 DESPAWN ---
            if (TryFindId(mod.serverDroppedItems, otherItem, out uint sid))
            {
                ServerDespawn(mod, sid);
                return;
            }
            if (TryFindNearestTaggedId(otherItem, out uint nearSid))
            {
                ServerDespawn(mod, nearSid);
            }
        }

        // ========= 工具函数（与库存补丁同等逻辑，自包含） =========
        static void SendPickupReq(ModBehaviour mod, uint id)
        {
            var w = mod.writer; w.Reset();
            w.Put((byte)Op.ITEM_PICKUP_REQUEST);
            w.Put(id);
            mod.connectedPeer?.Send(w, DeliveryMethod.ReliableOrdered);
        }

        static void ServerDespawn(ModBehaviour mod, uint id)
        {
            if (mod.serverDroppedItems.TryGetValue(id, out var it) && it != null)
                LocalDestroyAgent(it);
            mod.serverDroppedItems.Remove(id);

            var w = mod.writer; w.Reset();
            w.Put((byte)Op.ITEM_DESPAWN);
            w.Put(id);
            mod.netManager.SendToAll(w, DeliveryMethod.ReliableOrdered);
        }

        static void LocalDestroyAgent(Item it)
        {
            try
            {
                var ag = it.ActiveAgent;
                if (ag && ag.gameObject) UnityEngine.Object.Destroy(ag.gameObject);
            }
            catch { }
        }

        static void LocalDestroyAgentById(Dictionary<uint, Item> dict, uint id)
        {
            if (dict.TryGetValue(id, out var it) && it != null) LocalDestroyAgent(it);
        }

        static bool TryFindId(Dictionary<uint, Item> dict, Item it, out uint id)
        {
            foreach (var kv in dict)
                if (ReferenceEquals(kv.Value, it)) { id = kv.Key; return true; }
            id = 0; return false;
        }

        // 近场反查：以“被装备的物品”的位置（或其 ActiveAgent 位置）为圆心搜 NetDropTag
        static bool TryFindNearestTaggedId(Item item, out uint id)
        {
            id = 0;
            if (item == null) return false;

            Vector3 center;
            try
            {
                var ag = item.ActiveAgent;
                center = ag ? ag.transform.position : item.transform.position;
            }
            catch { center = item.transform.position; }

            var cols = Physics.OverlapSphere(center, PICK_RADIUS, LAYER_MASK, QTI);
            float best = float.MaxValue;
            uint bestId = 0;

            foreach (var c in cols)
            {
                var tag = c.GetComponentInParent<NetDropTag>();
                if (tag == null) continue;
                float d2 = (c.transform.position - center).sqrMagnitude;
                if (d2 < best)
                {
                    best = d2;
                    bestId = tag.id;
                }
            }

            if (bestId != 0) { id = bestId; return true; }
            return false;
        }
    }

    [HarmonyPatch(typeof(Slot), "Plug")]
    static class Patch_Slot_Plug_BlockEquipFromLoot
    {
        static bool Prefix(Slot __instance, Item otherItem, ref Item unpluggedItem)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted || m.IsServer) return true;
            if (m._applyingLootState) return true;

            var inv = otherItem ? otherItem.InInventory : null;
            // ★ 排除私有库存
            if (LootboxDetectUtil.IsLootboxInventory(inv) && !LootboxDetectUtil.IsPrivateInventory(inv))
            {
                int srcPos = inv?.GetIndex(otherItem) ?? -1;
                m.Client_SendLootTakeRequest(inv, srcPos, null, -1, __instance);
                unpluggedItem = null;
                return false;
            }
            return true;
        }
    }





    [HarmonyPatch(typeof(Inventory), "AddAt")]
    static class Patch_Inventory_AddAt_FromLoot
    {
        static bool Prefix(Inventory __instance, Item item, int atPosition, ref bool __result)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted || m.IsServer) return true;
            if (m._applyingLootState) return true;

            var srcInv = item ? item.InInventory : null;
            if (srcInv == null || srcInv == __instance) return true;

            // ★ 只拦公共容器来源
            if (LootboxDetectUtil.IsLootboxInventory(srcInv) && !LootboxDetectUtil.IsPrivateInventory(srcInv))
            {
                int srcPos = srcInv.GetIndex(item);

                // 进入保护区：标记“我现在是在 Loot.AddAt 流程里”
                LootUiGuards.InLootAddAtDepth++;
                try
                {
                    m.Client_SendLootTakeRequest(srcInv, srcPos, __instance, atPosition, null);
                }
                finally
                {
                    LootUiGuards.InLootAddAtDepth--;
                }

                __result = true;
                return false;
            }
            return true;
        }
    }


    [HarmonyPatch(typeof(ItemStatsSystem.Inventory), nameof(ItemStatsSystem.Inventory.AddAt), typeof(ItemStatsSystem.Item), typeof(int))]
    [HarmonyPriority(Priority.First)]
    static class Patch_Inventory_AddAt_SlotToPrivate_Reroute
    {
        static bool Prefix(Inventory __instance, Item item, int atPosition, ref bool __result)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted || m.IsServer) return true;

            // 只拦“落到私有库存”的情况（玩家背包/身上/宠物包）
            if (!LootboxDetectUtil.IsPrivateInventory(__instance)) return true;

            // 只拦“仍插在武器槽位里的附件”
            var slot = item ? item.PluggedIntoSlot : null;
            if (slot == null) return true;

            // 找到最外层主件（武器）
            var master = slot.Master;
            while (master && master.PluggedIntoSlot != null)
                master = master.PluggedIntoSlot.Master;

            // 源容器：优先 master.InInventory；拿不到时兜底用当前 LootView 的容器
            var srcLoot = master ? master.InInventory : null;
            if (!srcLoot)
            {
                try { var lv = LootView.Instance; if (lv) srcLoot = lv.TargetInventory; } catch { }
            }

            // 源容器必须是“公共容器”
            if (!srcLoot || !LootboxDetectUtil.IsLootboxInventory(srcLoot) || LootboxDetectUtil.IsPrivateInventory(srcLoot))
            {
                // 为了不触发原生的“父物体”报错，这里直接拦下，不执行原方法
                Debug.LogWarning($"[Coop] AddAt(private, slot->backpack) srcLoot not found; block local AddAt for '{item?.name}'");
                __result = false;
                return false;
            }

            Debug.Log($"[Coop] AddAt(private, slot->backpack) -> send UNPLUG(takeToBackpack), destPos={atPosition}");
            // 让主机先卸下，再由 TAKE_OK 驱动本地落到 atPosition
            m.Client_RequestSlotUnplugToBackpack(srcLoot, master, slot.Key, __instance, atPosition);

            __result = true;   // 本地视为已受理
            return false;      // 阻止原生 AddAt（否则就会出现“仍有父物体”的报错）
        }
    }


    // HarmonyFix.cs
    [HarmonyPatch]
    static class Patch_ItemUtilities_SendToPlayerCharacterInventory_FromLoot
    {
        static MethodBase TargetMethod()
        {
            var t = typeof(ItemUtilities);
            var m2 = AccessTools.Method(t, "SendToPlayerCharacterInventory",
                new[] { typeof(ItemStatsSystem.Item), typeof(bool) });
            if (m2 != null) return m2;

            // 兼容可能存在的 5 参重载
            return AccessTools.Method(t, "SendToPlayerCharacterInventory",
                new[] { typeof(ItemStatsSystem.Item), typeof(bool), typeof(bool),
                    typeof(ItemStatsSystem.Inventory), typeof(int) });
        }

        // 只写 (Item item, bool dontMerge, ref bool __result)，别再写不存在的参数

        static bool Prefix(ItemStatsSystem.Item item, bool dontMerge, ref bool __result)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted || m.IsServer) return true;

            // 在 Loot.AddAt 的保护期内不兜底，避免复制
            if (LootUiGuards.InLootAddAt)
            {
                __result = false;
                return false;
            }

            // A) 物品本身就在公共容器的格子里：走 TAKE
            var inv = item ? item.InInventory : null;
            if (inv && LootboxDetectUtil.IsLootboxInventory(inv) && !LootboxDetectUtil.IsPrivateInventory(inv))
            {
                int srcPos = inv.GetIndex(item);
                if (srcPos >= 0)
                {
                    m.Client_SendLootTakeRequest(inv, srcPos); // 目的地不指定，TAKE_OK 再落背包
                    __result = true;
                    return false;
                }
            }

            // B) 物品还插在“公共容器里的武器槽位”里：走 UNPLUG + takeToBackpack
            var slot = item ? item.PluggedIntoSlot : null;
            if (slot != null)
            {
                var master = slot.Master;
                while (master && master.PluggedIntoSlot != null) master = master.PluggedIntoSlot.Master;

                var srcLoot = master ? master.InInventory : null;
                if (!srcLoot)
                {
                    try { var lv = Duckov.UI.LootView.Instance; if (lv) srcLoot = lv.TargetInventory; } catch { }
                }

                if (srcLoot && LootboxDetectUtil.IsLootboxInventory(srcLoot) && !LootboxDetectUtil.IsPrivateInventory(srcLoot))
                {
                    Debug.Log("[Coop] SendToPlayerCharInv (slot->backpack) -> send UNPLUG(takeToBackpack=true)");
                    // 不指定落位；TAKE_OK 时走默认背包吸收（你在 Client_OnLootTakeOk 里已有逻辑）
                    m.Client_RequestLootSlotUnplug(srcLoot, master, slot.Key, true, 0);
                    __result = true;
                    return false;
                }
            }

            // 其它情况走原生
            return true;
        }


    }






    static class LootUiGuards
    {
        [ThreadStatic] public static int InLootAddAtDepth;
        public static bool InLootAddAt => InLootAddAtDepth > 0;
        [ThreadStatic] public static int BlockNextSendToInventory;
    }

    // HarmonyFix.cs
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddAt), typeof(Item), typeof(int))]
    static class Patch_Inventory_AddAt_BlockLocalInLoot
    {
        static bool Prefix(Inventory __instance, Item item, int atPosition, ref bool __result)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.IsClient) return true;

            if (LootboxDetectUtil.IsPrivateInventory(__instance)) return true; // 私有库存放行

            if (!ModBehaviour.IsCurrentLootInv(__instance)) return true;
            if (mod.ApplyingLootState) return true;

            LootUiGuards.InLootAddAtDepth++;
            try
            {
                var srcInv = item ? item.InInventory : null;

                // === A) 同容器内换位 / 挪动：改为 TAKE -> PUT 两段式 ===
                if (ReferenceEquals(srcInv, __instance))
                {
                    // 同格就直接视为成功
                    int srcPos = __instance.GetIndex(item);
                    if (srcPos == atPosition) { __result = true; return false; }

                    if (srcPos < 0) { __result = false; return false; }

                    // 1) 发 TAKE（不带目的地）
                    uint tk = mod.Client_SendLootTakeRequest(__instance, srcPos, null, -1, null);

                    // 2) 记录“待重排”，让 TAKE_OK 到达后立刻对同一容器发 PUT(atPosition)
                    mod.NoteLootReorderPending(tk, __instance, atPosition);

                    __result = true;   // 告诉上层：已受理，别回退
                    return false;      // 不执行原 AddAt（避免本地改内容）
                }

                // B) 容器 -> 其它库存：保持你现有逻辑（仍旧发 TAKE 携带目的地）
                if (ModBehaviour.IsCurrentLootInv(srcInv))
                {
                    int srcPos = srcInv.GetIndex(item);
                    if (srcPos < 0) { __result = false; return false; }

                    mod.Client_SendLootTakeRequest(srcInv, srcPos, __instance, atPosition, null);
                    __result = true;
                    return false;
                }

                // C) slot -> loot（同容器）：拦截“从容器内武器插槽卸下到容器格子”
                if (__instance && LootboxDetectUtil.IsLootboxInventory(__instance) && !LootboxDetectUtil.IsPrivateInventory(__instance))
                {
                    var slot = item ? item.PluggedIntoSlot : null;
                    if (slot != null)
                    {
                        // 找到这个槽位所属武器的“根主件”，以及它所在的容器
                        Item master = slot.Master;
                        while (master && master.PluggedIntoSlot != null) master = master.PluggedIntoSlot.Master;
                        var masterLoot = master ? master.InInventory : null;

                        if (masterLoot == __instance) // 同一个容器：这是“拆附件放回容器”的场景
                        {
                            Debug.Log("[Coop] AddAt@Loot (slot->loot) -> send UNPLUG(takeToBackpack=false)");
                            try { LootUiGuards.InLootAddAtDepth++; } catch { }
                            try
                            {
                                // 走“旧负载+追加字段”的新重载：takeToBackpack=false
                                ModBehaviour.Instance.Client_RequestLootSlotUnplug(__instance, slot.Master, slot.Key, false, 0);
                            }
                            finally { LootUiGuards.InLootAddAtDepth--; }

                            __result = true;     // 认为成功，等待主机的 LOOT_STATE 对齐UI
                            return false;        // 阻断本地 AddAt，避免出现本地就先放进去
                        }
                    }
                }


                // 其它来源 -> 容器：交给 PUT 拦截
                return true;
            }
            finally { LootUiGuards.InLootAddAtDepth--; }
        }
    }


    [HarmonyPatch(typeof(Inventory))]
    static class Patch_Inventory_RemoveAt_BlockLocalInLoot
    {
        // 用反射精确锁定 RemoveAt(int, out Item) 这个重载
        static MethodBase TargetMethod()
        {
            var tInv = typeof(Inventory);
            var tItemByRef = typeof(Item).MakeByRefType();
            return AccessTools.Method(tInv, "RemoveAt", new Type[] { typeof(int), tItemByRef });
        }

        // 注意第二个参数是 out Item —— 用 ref 接住即可
        static bool Prefix(Inventory __instance, int position, ref Item __1, ref bool __result)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.IsClient) return true;

            if (LootboxDetectUtil.IsPrivateInventory(__instance)) return true;

            bool isLootInv = false;
            try
            {
                var lv = LootView.Instance;
                isLootInv = lv && __instance && ReferenceEquals(__instance, lv.TargetInventory);

            }
            catch { }

            if (!isLootInv) return true;
            if (LootboxDetectUtil.IsPrivateInventory(__instance)) return true; // ★ 放行私有库存
            if (mod.ApplyingLootState) return true;
            __1 = null; __result = false; return false;

            // 应用服务器快照期间允许 RemoveAt（UI 刷新），其余时间一律拦截
            if (mod.ApplyingLootState) return true;

            __1 = null;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(LootView), "OnLootTargetItemDoubleClicked")]
    [HarmonyPriority(Priority.First)]
    static class Patch_LootView_OnLootTargetItemDoubleClicked_EquipDirectly
    {
        // ⚠ 第二个参数类型必须是 Duckov.UI.InventoryEntry（不是 InventoryDisplayEntry）
        static bool Prefix(LootView __instance, InventoryDisplay display, InventoryEntry entry, PointerEventData data)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted || mod.IsServer) return true;   // 主机/单机走原逻辑

            var item = entry?.Item;
            if (item == null) return false;

            var lootInv = __instance?.TargetInventory;
            if (lootInv == null) return true;

            // 只拦“公共战利品容器”，仓库/宠物包不拦
            if (!ReferenceEquals(item.InInventory, lootInv)) return true;
            if (!LootboxDetectUtil.IsLootboxInventory(lootInv)) return true;

            // 容器中的索引
            int pos;
            try { pos = lootInv.GetIndex(item); } catch { return true; }
            if (pos < 0) return true;

            // 选择一个可穿戴且为空的槽（武器位优先）
            var destSlot = PickEquipSlot(item);

            // 发 TAKE（带目标槽）；回包后由 Mod.cs 的 _cliPendingTake[token].slot → slot.Plug(item)
            if (destSlot != null)
                mod.Client_SendLootTakeRequest(lootInv, pos, null, -1, destSlot);
            else
                mod.Client_SendLootTakeRequest(lootInv, pos);

            data?.Use();     // 吃掉这次双击
            return false;    // 阻断原方法，避免回落到“塞背包”
        }

        static Slot PickEquipSlot(Item item)
        {
            var cmc = CharacterMainControl.Main;
            var charItem = cmc ? cmc.CharacterItem : null;
            var slots = charItem ? charItem.Slots : null;
            if (slots == null) return null;

            // 武器位优先
            try { var s = cmc.PrimWeaponSlot(); if (s != null && s.Content == null && s.CanPlug(item)) return s; } catch { }
            try { var s = cmc.SecWeaponSlot(); if (s != null && s.Content == null && s.CanPlug(item)) return s; } catch { }
            try { var s = cmc.MeleeWeaponSlot(); if (s != null && s.Content == null && s.CanPlug(item)) return s; } catch { }

            // 其余槽
            foreach (var s in slots)
            {
                if (s == null || s.Content != null) continue;
                try { if (s.CanPlug(item)) return s; } catch { }
            }
            return null;
        }
    }

    // 主机：Inventory.RemoveAt 成功后广播 —— 修复“主机本地拿走，客户端不刷”的幽灵物品
    [HarmonyPatch(typeof(ItemStatsSystem.Inventory))]
    static class Patch_Inventory_RemoveAt_BroadcastOnServer
    {
        // 精确锁定 RemoveAt(int, out Item) 这个重载
        static MethodBase TargetMethod()
        {
            var tInv = typeof(ItemStatsSystem.Inventory);
            var tItemByRef = typeof(ItemStatsSystem.Item).MakeByRefType();
            return AccessTools.Method(tInv, "RemoveAt", new Type[] { typeof(int), tItemByRef });
        }

        // Postfix：当主机本地从“公共战利品容器”取出成功后，广播一次全量状态
        static void Postfix(ItemStatsSystem.Inventory __instance, int position, ItemStatsSystem.Item __1, bool __result)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted || !m.IsServer) return;               // 仅主机
            if (!__result || m._serverApplyingLoot) return;                           // 跳过失败/网络路径内部调用
            if (!LootboxDetectUtil.IsLootboxInventory(__instance)) return;            // 只处理战利品容器
            if (LootboxDetectUtil.IsPrivateInventory(__instance)) return;             // 跳过玩家仓库/宠物包等私有库存

            m.Server_SendLootboxState(null, __instance);                              // 广播给所有客户端
        }
    }

    [HarmonyPatch(typeof(ItemAgent_Gun), "ShootOneBullet")]
    static class Patch_BlockClientAiShoot
    {
        [HarmonyPriority(Priority.First)]
        static bool Prefix(ItemAgent_Gun __instance, Vector3 _muzzlePoint, Vector3 _shootDirection, Vector3 firstFrameCheckStartPoint)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted) return true;

            // 主机照常；客户端才需要拦
            if (mod.IsServer) return true;

            var holder = __instance ? __instance.Holder : null;

            // 非本地主角 &&（AI 有 AICharacterController 或 NetAiTag 任一）=> 拦截
            if (holder && holder != CharacterMainControl.Main)
            {
                bool isAI = holder.GetComponent<AICharacterController>() != null
                         || holder.GetComponent<NetAiTag>() != null;

                if (isAI)
                {
                    if (ModBehaviour.LogAiHpDebug)
                        Debug.Log($"[CLIENT] Block local AI ShootOneBullet holder='{holder.name}'");
                    return false; // 不让客户端本地造弹
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(DamageReceiver), "Hurt")]
    static class Patch_BlockClientAiVsAi_AtReceiver
    {
        [HarmonyPriority(HarmonyLib.Priority.First)]
        static bool Prefix(DamageReceiver __instance, ref global::DamageInfo __0)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted || mod.IsServer) return true;

            var target = __instance ? __instance.GetComponentInParent<CharacterMainControl>() : null;
            bool victimIsAI = target && (target.GetComponent<AICharacterController>() != null || target.GetComponent<NetAiTag>() != null);
            if (!victimIsAI) return true;

            var attacker = __0.fromCharacter;
            bool attackerIsAI = attacker && (attacker.GetComponent<NetAiTag>() != null || attacker.GetComponent<NetAiTag>() != null);
            if (attackerIsAI) return false; // 不让伤害继续走向 Health

            return true;
        }
    }

    [HarmonyPatch(typeof(Health), "get_MaxHealth")]
    static class Patch_Health_get_MaxHealth_ClientOverride
    {
        static void Postfix(Health __instance, ref float __result)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || mod.IsServer) return;

            // 只给 AI 覆盖（避免动到玩家自身的本地 UI）
            var cmc = __instance.TryGetCharacter();
            bool isAI = cmc && (cmc.GetComponent<AICharacterController>() != null || cmc.GetComponent<NetAiTag>() != null);
            if (!isAI) return;

            if (mod.TryGetClientMaxOverride(__instance, out var v) && v > 0f)
            {
                if (__result <= 0f || v > __result) __result = v;
            }
        }
    }


    [HarmonyPatch(typeof(CharacterMainControl), "OnDead")]
    static class Patch_Client_OnDead_ReportCorpseTree
    {
        static void Postfix(CharacterMainControl __instance)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted) return;

            // 仅“客户端 + 本机玩家”分支上报
            if (mod.IsServer) return;
            if (__instance != CharacterMainControl.Main) return;

            // ⭐ 已经上报过（= 主机已经/可以生成过尸体战利品），直接跳过，不再创建/同步
            if (mod._cliCorpseTreeReported) return;

            try
            {
                // 给客户端的 CreateFromItem 拦截补丁一个“正在死亡路径”的标记，避免本地也生成（双生）
               DeadLootSpawnContext.InOnDead = __instance;

                // 首次上报整棵“尸体装备树”给主机（你已有的方法）
                mod.Net_ReportPlayerDeadTree(__instance);

                // ✅ 标记“本轮生命已经上报过尸体树”
                mod._cliCorpseTreeReported = true;
            }
            finally
            {
               DeadLootSpawnContext.InOnDead = null;
            }
        }
    }


    [HarmonyPatch(typeof(Item), nameof(Item.Split), new[] { typeof(int) })]
    static class Patch_Item_Split_RecordForLoot
    {
        static void Postfix(Item __instance, int count, ref UniTask<Item> __result)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted || m.IsServer) return;

            var srcInv = __instance ? __instance.InInventory : null;
            if (srcInv == null) return;
            if (!LootboxDetectUtil.IsLootboxInventory(srcInv) || LootboxDetectUtil.IsPrivateInventory(srcInv)) return;

            int srcPos = srcInv.GetIndex(__instance);
            if (srcPos < 0) return;

            __result = __result.ContinueWith(newItem =>
            {
                if (newItem != null)
                {
                    ModBehaviour.map[newItem.GetInstanceID()] = new ModBehaviour.Pending
                    {
                        inv = srcInv,
                        srcPos = srcPos,
                        count = count
                    };
                }
                return newItem;
            });
        }
    }

    [HarmonyPatch(typeof(Item), nameof(Item.Split), new[] { typeof(int) })]
    static class Patch_Item_Split_InterceptLoot_Prefix
    {
        static bool Prefix(Item __instance, int count, ref UniTask<Item> __result)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted || m.IsServer) return true;

            var inv = __instance ? __instance.InInventory : null;
            if (inv == null) return true;
            if (!LootboxDetectUtil.IsLootboxInventory(inv) || LootboxDetectUtil.IsPrivateInventory(inv)) return true;

            // 源格基于当前客户端视图索引；若后续容器变化导致索引不匹配，主机会据此拒绝
            int srcPos = inv.GetIndex(__instance);
            if (srcPos < 0) return true;

            // 选择一个优先落位（尽量不合并）：先找 srcPos 后面的空格，再全表，最后交给主机 -1
            int prefer = inv.GetFirstEmptyPosition(srcPos + 1);
            if (prefer < 0) prefer = inv.GetFirstEmptyPosition(0);
            if (prefer < 0) prefer = -1;

            // 只发请求，不做本地拆分
            m.Client_SendLootSplitRequest(inv, srcPos, count, prefer);

            // 立刻返回“没有本地新堆”，避免任何后续本地 Add/Merge 流程
            __result = UniTask.FromResult<Item>(null);
            return false;
        }
    }

    // 2) 优先拦截 AddAndMerge：若是“容器内拆分的新堆”，改发 SPLIT
    [HarmonyPatch(typeof(ItemUtilities), "AddAndMerge")]
    [HarmonyPriority(Priority.First)] // 一定要先于你现有的 AddAndMerge 拦截执行
    static class Patch_AddAndMerge_SplitFirst
    {
        static bool Prefix(Inventory inventory, Item item, int preferedFirstPosition, ref bool __result)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted || m.IsServer) return true; // 主机 / 未联网：放行

            if (inventory == null || item == null) return true;
            if (!LootboxDetectUtil.IsLootboxInventory(inventory) || LootboxDetectUtil.IsPrivateInventory(inventory))
                return true;

            // 是不是刚刚“容器内拆分”出来的那个新堆？
            if (!ModBehaviour.map.TryGetValue(item.GetInstanceID(), out var p)) return true;
            if (!ReferenceEquals(p.inv, inventory)) return true; // 必须是同一个容器内拆分

            // 发“拆分”请求：由主机把 srcPos 减 count，并尽量放在 preferedFirstPosition（或就近空格）
            m.Client_SendLootSplitRequest(inventory, p.srcPos, p.count, preferedFirstPosition);

            // 清理本地临时 newItem，避免和主机广播的正式实体重复
            try { if (item) { item.Detach(); UnityEngine.Object.Destroy(item.gameObject); } } catch { }
            ModBehaviour.map.Remove(item.GetInstanceID());

            __result = true;   // 告诉上层“处理完成”
            return false;      // 不要执行原方法（否则又会 PUT 一遍）
        }
    }

    // 3) 有些路径会直接 Inventory.AddAt(...)（不走 AddAndMerge），同样要拦
    [HarmonyPatch(typeof(Inventory), "AddAt")]
    [HarmonyPriority(Priority.First)]
    static class Patch_AddAt_SplitFirst
    {
        static bool Prefix(Inventory __instance, Item item, int atPosition, ref bool __result)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted || m.IsServer) return true;

            if (__instance == null || item == null) return true;
            if (!LootboxDetectUtil.IsLootboxInventory(__instance) || LootboxDetectUtil.IsPrivateInventory(__instance))
                return true;

            if (!ModBehaviour.map.TryGetValue(item.GetInstanceID(), out var p)) return true;
            if (!ReferenceEquals(p.inv, __instance)) return true;

            m.Client_SendLootSplitRequest(__instance, p.srcPos, p.count, atPosition);

            try { if (item) { item.Detach(); UnityEngine.Object.Destroy(item.gameObject); } } catch { }
            ModBehaviour.map.Remove(item.GetInstanceID());

            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(SplitDialogue), "DoSplit")]
    static class Patch_SplitDialogue_DoSplit_NetOnly
    {
        static bool Prefix(SplitDialogue __instance, int value, ref UniTask __result)
        {
            var m = ModBehaviour.Instance;
            // 未联网 / 主机执行 / 没有 Mod 行为时，走原版
            if (m == null || !m.networkStarted || m.IsServer)
                return true;

            // 读取 SplitDialogue 的私有字段
            var tr = Traverse.Create(__instance);
            var target = tr.Field<Item>("target").Value;
            var destInv = tr.Field<Inventory>("destination").Value;
            var destIndex = tr.Field<int>("destinationIndex").Value;

            var inv = target ? target.InInventory : null;
            // 非容器（或私域容器）拆分，保留原版逻辑
            if (inv == null || !LootboxDetectUtil.IsLootboxInventory(inv) || LootboxDetectUtil.IsPrivateInventory(inv))
                return true;

            // 源格（按当前客户端视图计算）
            int srcPos = inv.GetIndex(target);
            if (srcPos < 0)
            {
                __result = UniTask.CompletedTask;
                return false;
            }

            // 计算“优先落位”：如果用户是从容器拖到容器且目标格子为空，就强制落在那个格子
            int prefer = -1;
            if (destInv == inv && destIndex >= 0 && destIndex < inv.Capacity && inv.GetItemAt(destIndex) == null)
            {
                prefer = destIndex;
            }
            else
            {
                // 否则找就近空位；找不到就交给主机决定（-1）
                prefer = inv.GetFirstEmptyPosition(srcPos + 1);
                if (prefer < 0) prefer = inv.GetFirstEmptyPosition(0);
                if (prefer < 0) prefer = -1;
            }

            // 发请求给主机：仅网络，不在本地造新堆
            m.Client_SendLootSplitRequest(inv, srcPos, value, prefer);


            // 友好点：切成 Busy→Complete→收起对话框（避免 UI 挂在“忙碌中”）
            try { tr.Method("Hide").GetValue(); } catch { }

            __result = UniTask.CompletedTask;
            return false; // 阻止原方法，避免触发 <DoSplit>g__Send|24_0
        }
    }

    [HarmonyPatch(typeof(ItemStatsSystem.Inventory), nameof(ItemStatsSystem.Inventory.Sort), new Type[] { })]
    static class Patch_Inventory_Sort_BlockLocalInLoot
    {
        static bool Prefix(ItemStatsSystem.Inventory __instance)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.IsClient) return true;              // 主机/单机场景放行
            if (mod.ApplyingLootState) return true;                      // 应用服务器快照时放行（用于UI重建）
            if (!ModBehaviour.IsCurrentLootInv(__instance)) return true; // 只拦当前战利品容器
            if (LootboxDetectUtil.IsPrivateInventory(__instance)) return true; // 私有库存放行

            // 这里可选：发一个“请求合并/整理”的网络指令给主机，让主机执行再广播；
            // 若不想加协议，先纯拦也行，至少不会再制造幽灵。
            return false; // 阻止原始 Sort()
        }
    }

    [HarmonyPatch(typeof(CharacterMainControl), "OnDead")]
    static class Patch_Server_OnDead_Host_UsePlayerTree
    {
        static void Postfix(CharacterMainControl __instance)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted || !mod.IsServer) return;

            var lm = LevelManager.Instance;
            if (lm == null || __instance != lm.MainCharacter) return;  // 只处理主机自己的本机主角

            mod.Server_HandleHostDeathViaTree(__instance);             // ← 走“客户端同款”的树路径
        }
    }


    [HarmonyPatch(typeof(CharacterMainControl), "OnDead")]
    static class Patch_Client_OnDead_MarkAll_ForBlock
    {
        static void Prefix(CharacterMainControl __instance)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted) return;
            if (mod.IsServer) return;                  // 只在客户端打标记
            DeadLootSpawnContext.InOnDead = __instance;
        }

        static void Finalizer()
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted) return;
            if (mod.IsServer) return;
            DeadLootSpawnContext.InOnDead = null;
        }
    }


    /// ////防AI挥砍客户端的NRE报错////////////////////////// ////防AI挥砍客户端的NRE报错////////////////////////// ////防AI挥砍客户端的NRE报错///////////////////////


    [HarmonyPatch(typeof(CharacterMainControl), "GetHelmatItem")]
    static class Patch_CMC_GetHelmatItem_NullSafe
    {
        // 任何异常都吞掉并当作“没戴头盔”，避免打断 Health.Hurt
        static System.Exception Finalizer(System.Exception __exception, CharacterMainControl __instance, ref Item __result)
        {
            if (__exception != null)
            {
                Debug.LogWarning($"[NET] Suppressed exception in GetHelmatItem() on {__instance?.name}: {__exception}");
                __result = null;     // 相当于“无头盔”，正常继续后续伤害结算和 Buff 触发
                return null;         // 吞掉异常
            }
            return null;
        }
    }

    [HarmonyPatch(typeof(CharacterMainControl), "GetArmorItem")]
    static class Patch_CMC_GetArmorItem_NullSafe
    {
        // 任何异常都吞掉并当作“没穿护甲”，让伤害继续
        static System.Exception Finalizer(System.Exception __exception, CharacterMainControl __instance, ref Item __result)
        {
            if (__exception != null)
            {
                Debug.LogWarning($"[NET] Suppressed exception in GetArmorItem() on {__instance?.name}: {__exception}");
                __result = null;   // 视为无甲，继续照常计算伤害&流血
                return null;       // 吞掉异常
            }
            return null;
        }
    }

    /// ////防AI挥砍客户端的NRE报错////////////////////////// ////防AI挥砍客户端的NRE报错////////////////////////// ////防AI挥砍客户端的NRE报错///////////////////////

    // ========= 客户端：拦截 Door.Open -> 发送请求给主机 =========
    [HarmonyPatch(typeof(Door), nameof(Door.Open))]
    static class Patch_Door_Open_ClientToServer
    {
        static bool Prefix(Door __instance)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted) return true;
            if (m.IsServer) return true;                 // 主机放行
            if (ModBehaviour._applyingDoor) return true;  // 正在应用网络下发，放行

            m.Client_RequestDoorSetState(__instance, closed: false);
            return false; // 客户端不直接开门
        }
    }

    // ========= 客户端：拦截 Door.Close -> 发送请求给主机 =========
    [HarmonyPatch(typeof(Door), nameof(Door.Close))]
    static class Patch_Door_Close_ClientToServer
    {
        static bool Prefix(Door __instance)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted) return true;
            if (m.IsServer) return true;
            if (ModBehaviour._applyingDoor) return true;

            m.Client_RequestDoorSetState(__instance, closed: true);
            return false;
        }
    }

    // ========= 客户端：拦截 Door.Switch -> 发送请求给主机 =========
    [HarmonyPatch(typeof(Door), nameof(Door.Switch))]
    static class Patch_Door_Switch_ClientToServer
    {
        static bool Prefix(Door __instance)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted) return true;
            if (m.IsServer) return true;
            if (ModBehaviour._applyingDoor) return true;

            bool isOpen = false;
            try { isOpen = __instance.IsOpen; } catch { }
            m.Client_RequestDoorSetState(__instance, closed: isOpen /* open->关，close->开 */);
            return false;
        }
    }

    // ========= 主机：任何地方调用 SetClosed 都广播给所有客户端 =========
    [HarmonyPatch(typeof(Door), "SetClosed")]
    static class Patch_Door_SetClosed_BroadcastOnServer
    {
        static void Postfix(Door __instance, bool _closed)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted || !m.IsServer) return;

            int key = 0;
            try { key = (int)AccessTools.Field(typeof(Door), "doorClosedDataKeyCached").GetValue(__instance); } catch { }
            if (key == 0) key = m.ComputeDoorKey(__instance.transform);
            if (key == 0) return;

            m.Server_BroadcastDoorState(key, _closed);
        }
    }

    [HarmonyPatch(typeof(Breakable), "Awake")]
    static class Patch_Breakable_Awake_ForceVisibleInCoop
    {
        static void Postfix(Breakable __instance)
        {
            var mod = 鸭科夫联机Mod.ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted) return;

            // 确保可破坏体带 NetDestructibleTag 并注册
            try
            {
                var hs = __instance.simpleHealth;
                if (hs)
                {
                    var tag = hs.GetComponent<NetDestructibleTag>() ?? hs.gameObject.AddComponent<NetDestructibleTag>();
                    tag.id = NetDestructibleTag.ComputeStableId(hs.gameObject);
                    mod.RegisterDestructible(tag.id, hs);
                }
            }
            catch { }

            // 仅客户端：把 Awake 里因本地 Save 关掉的外观/碰撞体全部拉回“未破坏”
            if (!mod.IsServer)
            {
                try
                {
                    if (__instance.normalVisual) __instance.normalVisual.SetActive(true);
                    if (__instance.dangerVisual) __instance.dangerVisual.SetActive(false);
                    if (__instance.breakedVisual) __instance.breakedVisual.SetActive(false);
                    if (__instance.mainCollider) __instance.mainCollider.SetActive(true);

                    var hs = __instance.simpleHealth;
                    if (hs && hs.dmgReceiver) hs.dmgReceiver.gameObject.SetActive(true);
                }
                catch { }
            }
        }
    }

    [HarmonyPatch(typeof(CharacterMainControl), nameof(CharacterMainControl.SetCharacterModel))]
    static class Patch_CMC_SetCharacterModel_RebindNetAiFollower
    {
        static void Postfix(CharacterMainControl __instance)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted) return;

            try
            {
                if (mod != null && mod.networkStarted && !mod.IsServer)
                {
                    int id = -1;
                    // 从 aiById 反查当前 CMC 对应的 aiId
                    foreach (var kv in mod.aiById)
                    {
                        if (kv.Value == __instance) { id = kv.Key; break; }
                    }
                    if (id >= 0)
                    {
                        var tag = __instance.GetComponent<NetAiTag>() ?? __instance.gameObject.AddComponent<NetAiTag>();
                        if (tag.aiId != id) tag.aiId = id;
                    }
                }
            }
            catch { }

            // 只处理“真 AI”的远端复制体（本地玩家/主机侧不需要）
            // 你已有 IsRealAI(.) 判定；保持一致
            try
            {
                if (!mod.IsServer && mod.IsRealAI(__instance))
                {
                    // 确保有 RemoteReplicaTag（你已用它在 MagicBlend.Update 里早退）
                    if (!__instance.GetComponent<RemoteReplicaTag>())
                        __instance.gameObject.AddComponent<RemoteReplicaTag>();
                }
            }
            catch { }

            // 强制通知 NetAiFollower 重新抓取当前模型的 Animator
            try
            {
                var follower = __instance.GetComponent<鸭科夫联机Mod.NetAiFollower>();
                if (follower) follower.ForceRebindAfterModelSwap();
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(CharacterAnimationControl_MagicBlend), "Update")]
    static class Patch_MagicBlend_Update_SkipOnRemoteAI
    {
        [HarmonyPriority(Priority.First)]
        static bool Prefix(CharacterAnimationControl_MagicBlend __instance)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted) return true;
            if (mod.IsServer) return true;

            CharacterMainControl cmc = null;
            try
            {
                var cm = __instance.characterModel;
                cmc = cm ? cm.characterMainControl : __instance.GetComponentInParent<CharacterMainControl>();
            }
            catch { }

            if (!cmc) return true;

            // 只拦“客户端上的 AI 复制体”
            bool isAI =
                cmc.GetComponent<AICharacterController>() != null ||
                cmc.GetComponent<NetAiTag>() != null;

            bool isRemoteReplica =
                cmc.GetComponent<鸭科夫联机Mod.NetAiFollower>() != null ||
                cmc.GetComponent<RemoteReplicaTag>() != null;

            if (isAI && isRemoteReplica)
                return false; // 跳过 Update，不要覆盖网络来的参数

            return true;
        }
    }

    [HarmonyPatch(typeof(CharacterMainControl), nameof(CharacterMainControl.SetCharacterModel))]
    static class Patch_CMC_SetCharacterModel_TagAndRebindOnClient
    {
        static void Postfix(CharacterMainControl __instance)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted || mod.IsServer) return; // 只在客户端处理

            // 给客户端的 AI 复制体打上标记，并强制重绑 Animator
            bool isAI =
                __instance.GetComponent<AICharacterController>() != null ||
                __instance.GetComponent<NetAiTag>() != null;

            if (isAI)
            {
                if (!__instance.GetComponent<RemoteReplicaTag>())
                    __instance.gameObject.AddComponent<RemoteReplicaTag>();

                var follower = __instance.GetComponent<鸭科夫联机Mod.NetAiFollower>();
                if (follower) follower.ForceRebindAfterModelSwap();
            }
        }
    }

    [HarmonyPatch(typeof(SetActiveByPlayerDistance), "FixedUpdate")]
    static class Patch_SABD_KeepRemoteAIActive_Client
    {
        static void Postfix(SetActiveByPlayerDistance __instance)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted || m.IsServer) return;

            bool forceAll = m.Client_ForceShowAllRemoteAI;
            if (forceAll)
            {
                Traverse.Create(__instance).Field<float>("distance").Value = 9999f;
            }
        }
    }


    [HarmonyPatch(typeof(CharacterAnimationControl), "Update")]
    static class Patch_CharAnimCtrl_Update_SkipOnRemoteAI
    {
        [HarmonyPriority(Priority.First)]
        static bool Prefix(CharacterAnimationControl __instance)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted) return true;
            if (mod.IsServer) return true; // 主机侧仍由本地AI驱动

            CharacterMainControl cmc = null;
            try
            {
                var cm = __instance.characterModel;
                cmc = cm ? cm.characterMainControl : __instance.GetComponentInParent<CharacterMainControl>();
            }
            catch { }

            if (!cmc) return true;

            bool isAI =
                cmc.GetComponent<AICharacterController>() != null ||
                cmc.GetComponent<NetAiTag>() != null;

            bool isRemoteReplica =
                cmc.GetComponent<鸭科夫联机Mod.NetAiFollower>() != null ||
                cmc.GetComponent<RemoteReplicaTag>() != null;

            // 客户端的AI复制体：拦掉本地Update
            if (isAI && isRemoteReplica)
                return false;

            return true;
        }
    }


    //[HarmonyPatch(typeof(SteamManager), "OnRichPresenceChanged")]
    //static class Patch_OnRichPresenceChanged
    //{
    //    static bool Prefix(SteamManager __instance, RichPresenceManager manager)
    //    {
    //        if (!global::SteamManager.Initialized || manager == null)
    //            return false;


    //        string token = CallGetSteamDisplay(manager);  // 例: "#Status_Playing" / "#Status_MainMenu"
    //        Debug.Log(token);
    //        global::Steamworks.SteamFriends.SetRichPresence("steam_display", "#Status_UnityEditor");


    //        //var mapName = manager.levelDisplayNameRaw ?? "";
    //        //int playerCount = 2;
    //        //Debug.Log(mapName);
    //        //string levelText = $"{mapName} 联机模式人数:{playerCount}人";
    //        //global::Steamworks.SteamFriends.SetRichPresence("level", levelText);


    //        return false; 
    //    }

    //    public static string CallGetSteamDisplay(object target)
    //    {
    //        if (target == null) throw new ArgumentNullException(nameof(target));

    //        Type t = target.GetType();

    //        MethodInfo m = t.GetMethod(
    //            "GetSteamDisplay",
    //            BindingFlags.Instance | BindingFlags.NonPublic
    //        );

    //        if (m == null)
    //            throw new MissingMethodException(t.FullName, "GetSteamDisplay");

    //        return (string)m.Invoke(target, null);
    //    }
    //}

    // ========== 客户端：拦截 Health.Hurt（AI 被打） -> 仅本机玩家命中时播放本地特效/数字，然后发给主机 ==========
    [HarmonyPatch(typeof(Health), "Hurt")]
    static class Patch_Health
    {
        static bool Prefix(Health __instance, ref global::DamageInfo __0)
        {
            var mod = 鸭科夫联机Mod.ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted) return true;

            if (__instance.gameObject.GetComponent<AutoRequestHealthBar>() != null)
            {
                return false;
            }

            // 受击者是不是 AI/NPC
            global::CharacterMainControl victimCmc = null;
            try { victimCmc = __instance ? __instance.TryGetCharacter() : null; } catch { }
            bool isAiVictim = (victimCmc && victimCmc != global::CharacterMainControl.Main);

            // 攻击者是不是本机玩家
            var from = __0.fromCharacter;
            bool fromLocalMain = (from == global::CharacterMainControl.Main);

            // 仅客户端 + 仅本机玩家打到 AI 时，走“拦截→本地播特效→网络上报”
            if (!mod.IsServer && isAiVictim && fromLocalMain)
            {
                // 预测是否致死（用于提前播死亡特效/击杀标记，手感更好）
                bool predictedDead = false;
                try
                {
                    float cur = __instance.CurrentHealth;
                    predictedDead = (cur > 0f && __0.damageValue >= cur - 0.001f);
                }
                catch { }
               // LocalHitKillFx.RememberLastBaseDamage(__0.damageValue);
               // 鸭科夫联机Mod.LocalHitKillFx.ClientPlayForAI(victimCmc, __0, predictedDead);

                return false;
            }

            // 其它情况放行（包括 AI→AI、AI→障碍物、远端玩家→AI 等）
            return true;
        }
    }

    [HarmonyPatch(typeof(HealthSimpleBase), "OnHurt")]
    static class Patch_HealthSimpleBase_OnHurt_RedirectNet
    {
        static bool Prefix(HealthSimpleBase __instance, ref global::DamageInfo __0)
        {
            var mod = 鸭科夫联机Mod.ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted) return true;

            // 必须是本机玩家的命中才拦截；防止 AI 打障碍物也触发 UI
            var from = __0.fromCharacter;
            bool fromLocalMain = (from == global::CharacterMainControl.Main);

            if (!mod.IsServer && fromLocalMain)
            {
                // 预测是否致死（简单用 HealthValue 判断，足够做“演出预判”）
                bool predictedDead = false;
                try
                {
                    float cur = __instance.HealthValue;
                    predictedDead = (cur > 0f && __0.damageValue >= cur - 0.001f);
                }
                catch { }

                鸭科夫联机Mod.LocalHitKillFx.ClientPlayForDestructible(__instance, __0, predictedDead);

                // 继续你的原有逻辑：把命中发给主机权威结算
                return false;
            }

            return true;
        }
    }

   


    // 修正：拦截 AddAndMerge 时的参数名必须与原方法一致
    [HarmonyPatch(typeof(ItemUtilities), nameof(ItemUtilities.AddAndMerge))]
    static class Patch_ItemUtilities_AddAndMerge_InterceptSlotToBackpack
    {
        // 原方法签名：static bool AddAndMerge(Inventory inventory, Item item, int preferedFirstPosition)
        static bool Prefix(ItemStatsSystem.Inventory inventory, ItemStatsSystem.Item item, int preferedFirstPosition, ref bool __result)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted || m.IsServer) return true;
            if (m._applyingLootState) return true;

            // 目标必须是私有库存（背包/身上/宠物包）
            if (!inventory || !LootboxDetectUtil.IsPrivateInventory(inventory))
                return true;

            var slot = item ? item.PluggedIntoSlot : null;
            if (slot == null) return true;

            // 提升到最外层主件 + 源容器兜底（LootView）
            var master = slot.Master;
            while (master && master.PluggedIntoSlot != null) master = master.PluggedIntoSlot.Master;

            var srcLoot = master ? master.InInventory : null;
            if (!srcLoot)
            {
                try { var lv = Duckov.UI.LootView.Instance; if (lv) srcLoot = lv.TargetInventory; } catch { }
            }

            if (srcLoot && LootboxDetectUtil.IsLootboxInventory(srcLoot) && !LootboxDetectUtil.IsPrivateInventory(srcLoot))
            {
                Debug.Log($"[Coop] AddAndMerge(slot->backpack) -> UNPLUG(takeToBackpack), prefer={preferedFirstPosition}");
                // 直接携带目标格（preferedFirstPosition）做精确落位
                m.Client_RequestSlotUnplugToBackpack(srcLoot, master, slot.Key, inventory, preferedFirstPosition);
                __result = true;
                return false; // 阻止原生 AddAndMerge
            }

            // 源容器不明：阻止原方法以免触发“仍有父物体”的报错
            __result = false;
            return false;
        }

    }


    


    [HarmonyPatch(typeof(Slot), nameof(Slot.Plug))]
    static class Patch_Slot_Plug_ClientRedirect
    {
        static bool Prefix(Slot __instance, Item otherItem, ref bool __result)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted || m.IsServer || m.ClientLootSetupActive || m._applyingLootState)
                return true; // 主机/初始化/套快照时放行原逻辑

            var master = __instance?.Master;
            var inv = master ? master.InInventory : null;
            if (!inv) return true;
            if (!LootboxDetectUtil.IsLootboxInventory(inv) || LootboxDetectUtil.IsPrivateInventory(inv))
                return true; // 只拦“公共战利品容器”里的槽位

            if (!otherItem) return true;

            // 走网络：客户端 -> 主机
            m.Client_RequestLootSlotPlug(inv, master, __instance.Key, otherItem);

            __result = true;    // 让 UI 认为已处理，实际等主机广播来驱动可视变化
            return false;       // 阻止本地真正 Plug
        }
    }



    // HarmonyFix.cs
    [HarmonyPatch(typeof(Slot), nameof(Slot.Unplug))]
    [HarmonyPriority(Priority.First)]
    static class Patch_Slot_Unplug_ClientRedirect
    {
        static bool Prefix(Slot __instance, ref Item __result)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted || m.IsServer) return true;
            if (m.ApplyingLootState) return true;

            // 关键：用 Master.InInventory 判断该槽位属于哪个容器
            var inv = __instance?.Master ? __instance.Master.InInventory : null;
            if (inv == null) return true;
            // 仅在“公共战利品容器且非私有”时拦截
            if (!LootboxDetectUtil.IsLootboxInventory(inv) || LootboxDetectUtil.IsPrivateInventory(inv))
                return true;

            // 统一做法：本地完全不执行 Unplug，等待我们在 AddAt/​AddAndMerge/​SendToInventory 的前缀里走网络
            UnityEngine.Debug.Log("[Coop] Slot.Unplug@Loot -> ignore (network-handled)");
            __result = null;      // 别生成本地分离物
            return false;         // 阻断原始 Unplug
        }
    }



    [HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveAt))]
    static class Patch_ServerBroadcast_OnRemoveAt
    {
        static void Postfix(Inventory __instance, int position, Item removedItem, bool __result)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted || !m.IsServer) return;
            if (!__result || m._serverApplyingLoot) return;
            if (!LootboxDetectUtil.IsLootboxInventory(__instance) || LootboxDetectUtil.IsPrivateInventory(__instance)) return;

            if (m.Server_IsLootMuted(__instance)) return; // ★ 新增
            m.Server_SendLootboxState(null, __instance);
        }
    }


    // Inventory.AddAt 主机本地往容器放入（含：从武器卸下再放回容器）
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddAt))]
    static class Patch_ServerBroadcast_OnAddAt
    {
        // 死亡填充场景：在 AddAt 前给该容器加“静音窗口”，屏蔽本次及紧随其后的群发
        static void Prefix(Inventory __instance)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted || !m.IsServer) return;

            // 仅在 AI 死亡 OnDead 流程里触发（你项目里已有这个上下文标记）
            if (DeadLootSpawnContext.InOnDead == null) return;

            if (!LootboxDetectUtil.IsLootboxInventory(__instance) || LootboxDetectUtil.IsPrivateInventory(__instance)) return;
            m.Server_MuteLoot(__instance, 1.0f); // 1秒静音足够覆盖整次填充
        }

        static void Postfix(Inventory __instance, Item item, int atPosition, bool __result)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted || !m.IsServer) return;
            if (!__result || m._serverApplyingLoot) return;
            if (!LootboxDetectUtil.IsLootboxInventory(__instance) || LootboxDetectUtil.IsPrivateInventory(__instance)) return;

            // ★ 新增：静音期内跳过群发（真正有人打开时仍会单播，应答不受影响）
            if (m.Server_IsLootMuted(__instance)) return;

            m.Server_SendLootboxState(null, __instance);
        }
    }

    // Slot.Plug 主机在“容器里的武器”上装配件（目标 master 所在 Inventory 是容器）
    [HarmonyPatch(typeof(Slot), nameof(Slot.Plug))]
    static class Patch_ServerBroadcast_OnSlotPlug
    {
        static void Postfix(Slot __instance, Item otherItem, Item unpluggedItem, bool __result)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted || !m.IsServer) return;
            if (!__result || m._serverApplyingLoot) return;

            var master = __instance?.Master;
            var inv = master ? master.InInventory : null;
            if (!inv) return;
            if (!LootboxDetectUtil.IsLootboxInventory(inv) || LootboxDetectUtil.IsPrivateInventory(inv)) return;

            if (m.Server_IsLootMuted(inv)) return; // ★ 新增
            m.Server_SendLootboxState(null, inv);
        }
    }

    // Slot.Unplug 主机在“容器里的武器”上拆配件
    [HarmonyPatch(typeof(Slot), nameof(Slot.Unplug))]
    static class Patch_ServerBroadcast_OnSlotUnplug
    {
        static void Postfix(Slot __instance, Item __result)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted || !m.IsServer) return;
            if (m._serverApplyingLoot) return;

            var master = __instance?.Master;
            var inv = master ? master.InInventory : null;
            if (!inv) return;
            if (!LootboxDetectUtil.IsLootboxInventory(inv) || LootboxDetectUtil.IsPrivateInventory(inv)) return;

            m.Server_SendLootboxState(null, inv);
        }
    }

    //[HarmonyPatch(typeof(ItemStatsSystem.Items.Slot), nameof(ItemStatsSystem.Items.Slot.Plug))]
    //[HarmonyPriority(Priority.First)]
    //static class Patch_Slot_Plug_BlockEquipFromLoot_Client
    //{
    //    // 原签名：bool Plug(Item otherItem, out Item unpluggedItem)
    //    static bool Prefix(ItemStatsSystem.Items.Slot __instance,
    //                       ItemStatsSystem.Item otherItem,
    //                       ref ItemStatsSystem.Item unpluggedItem,
    //                       ref bool __result)
    //    {
    //        var m = ModBehaviour.Instance;
    //        if (m == null || !m.networkStarted || m.IsServer) return true; // 主机/单机放行
    //        if (m.ApplyingLootState) return true;                           // 套快照时放行

    //        var master = __instance?.Master;
    //        var inv = master ? master.InInventory : null;                   // ★ 必须用 InInventory
    //        if (inv == null) return true;
    //        if (!LootboxDetectUtil.IsLootboxInventory(inv) || LootboxDetectUtil.IsPrivateInventory(inv)) return true;

    //        // 重定向到主机，客户端本地不做实际 Plug
    //        unpluggedItem = null;
    //        __result = true; // 告诉调用方“处理完成”，具体可视更新等主机快照
    //        m.Client_RequestLootSlotPlug(inv, master, __instance.Key, otherItem);
    //        return false;    // 阻止进入原方法
    //    }
    //}

    // 统一给所有可破坏体（HealthSimpleBase）打上 NetDestructibleTag 并注册进索引
    [HarmonyPatch(typeof(HealthSimpleBase), "Awake")]
    static class Patch_HSB_Awake_AddTagAndRegister
    {
        static void Postfix(HealthSimpleBase __instance)
        {
            try
            {
                var mod = ModBehaviour.Instance;
                if (mod == null) return;

                // 没有就补一个
                var tag = __instance.GetComponent<NetDestructibleTag>();
                if (!tag) tag = __instance.gameObject.AddComponent<NetDestructibleTag>();

                // 尽量用“墙体根”等稳定根节点算稳定ID；失败则退回到自身
                uint id = 0;
                try
                {
                    // 你已有的稳定ID算法在 Mod.cs 里；这里直接复用 NetDestructibleTag 的稳定计算兜底
                    id = NetDestructibleTag.ComputeStableId(__instance.gameObject);
                }
                catch { /* 忽略差异 */ }

                tag.id = id;
                mod.RegisterDestructible(id, __instance);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Coop][HSB.Awake] Tag/Register failed: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(DamageReceiver), "Hurt")]
    static class Patch_ClientMelee_HurtRedirect_Destructible
    {
        [HarmonyPriority(HarmonyLib.Priority.First)]
        static bool Prefix(DamageReceiver __instance, ref global::DamageInfo __0)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted || m.IsServer) return true;

            // 只拦“本地玩家的近战结算帧”
            if (!MeleeLocalGuard.LocalMeleeTryingToHurt) return true;

            // 仅处理环境可破坏体
            var hs = __instance ? __instance.GetComponentInParent<HealthSimpleBase>() : null;
            if (!hs) return true;

            // 计算/获取稳定 id
            uint id = 0;
            var tag = hs.GetComponent<NetDestructibleTag>();
            if (tag) id = tag.id;
            if (id == 0)
            {
                try { id = NetDestructibleTag.ComputeStableId(hs.gameObject); } catch { }
            }
            if (id == 0) return true; // 算不出 id，就放行给原逻辑，避免“打不掉”

            // 正确的调用：传 id，而不是传 HealthSimpleBase
            m.Client_RequestDestructibleHurt(id, __0);
            return false; // 阻止本地结算，等主机广播
        }
    }

    [HarmonyPatch]
    static class Patch_ClosureView_ShowAndReturnTask_SpectatorGate
    {
        static System.Reflection.MethodBase TargetMethod()
        {
            var t = AccessTools.TypeByName("Duckov.UI.ClosureView");
            if (t == null) return null;
            return AccessTools.Method(t, "ShowAndReturnTask", new Type[] { typeof(global::DamageInfo), typeof(float) });
        }

        static bool Prefix(ref UniTask __result, global::DamageInfo dmgInfo, float duration)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted) return true;

            if (mod._skipSpectatorForNextClosure)
            {
                mod._skipSpectatorForNextClosure = false;
                __result = UniTask.CompletedTask;
                return true; 
            }

            // 如果还有队友活着，走观战并阻止结算 UI
            if (mod.TryEnterSpectatorOnDeath(dmgInfo))
            {
               //  __result = UniTask.CompletedTask;
               // ClosureView.Instance.gameObject.SetActive(false);
                return true; // 拦截原方法
            }

            return true;
        }

      
    }

    [HarmonyPatch(typeof(GameManager), "get_Paused")]
    internal static class Patch_Paused_AlwaysFalse
    {
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(ref bool __result)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted) return true;

            __result = false;

            return false; 
        }
    }

    [HarmonyPatch(typeof(PauseMenu), "Show")]
    internal static class Patch_PauseMenuShow_AlwaysFalse
    {
        [HarmonyPriority(Priority.First)]
        [HarmonyPostfix]
        private static void Postfix()
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted) return;

            mod.Pausebool = true;

        }
    }

    [HarmonyPatch(typeof(PauseMenu), "Hide")]
    internal static class Patch_PauseMenuHide_AlwaysFalse
    {
        [HarmonyPriority(Priority.First)]
        [HarmonyPostfix]
        private static void Postfix()
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted) return;

            mod.Pausebool = false;

        }
    }

    [HarmonyPatch(typeof(Health), "DestroyOnDelay")]
    static class Patch_Health_DestroyOnDelay_SkipForAI_Server
    {
        static bool Prefix(Health __instance)
        {
            var mod = 鸭科夫联机Mod.ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted || !mod.IsServer) return true;

            CharacterMainControl cmc = null;
            try { cmc = __instance.TryGetCharacter(); } catch { }
            if (!cmc) { try { cmc = __instance.GetComponentInParent<CharacterMainControl>(); } catch { } }

            bool isAI = cmc &&
                        (cmc.GetComponent<AICharacterController>() != null ||
                         cmc.GetComponent<NetAiTag>() != null);
            if (!isAI) return true;

            // 对 AI：主机不再走原 DestroyOnDelay，避免已销毁对象的后续访问导致 NRE
            return false;
        }
    }

    // 兜底：即使有第三方路径仍触发 DestroyOnDelay，吞掉异常防止打断主循环（可选）
    [HarmonyPatch(typeof(Health), "DestroyOnDelay")]
    static class Patch_Health_DestroyOnDelay_Finalizer
    {
        static Exception Finalizer(Exception __exception)
        {
            // 返回 null 表示吞掉异常
            if (__exception != null)
                Debug.LogWarning("[COOP] Swallow DestroyOnDelay exception: " + __exception.Message);
            return null;
        }
    }

    [HarmonyPatch(typeof(InteractableLootbox), "CreateFromItem")]
    [HarmonyPriority(Priority.High)]
    static class Patch_Lootbox_CreateFromItem_DeferOnServerFromOnDead
    {
        // 防止我们在协程里再次调用 CreateFromItem 时又被自己拦截
        [ThreadStatic] static bool _bypassDefer;

        static bool Prefix(
            ItemStatsSystem.Item item,
            UnityEngine.Vector3 position,
            UnityEngine.Quaternion rotation,
            bool moveToMainScene,
            InteractableLootbox prefab,
            bool filterDontDropOnDead,
            ref InteractableLootbox __result)
        {
            var mod = ModBehaviour.Instance;
            var dead = DeadLootSpawnContext.InOnDead;

            // 仅在：联机 + 服务端 + 正处于 OnDead 路径 时延帧，其余情况不动
            if (_bypassDefer || mod == null || !mod.networkStarted || !mod.IsServer || dead == null)
                return true;

            mod.StartCoroutine(DeferOneFrame(
                item, position, rotation, moveToMainScene, prefab, filterDontDropOnDead, dead
            ));

            // 原调用方（OnDead）不依赖立即返回值，先置空并跳过本帧
            __result = null;
            return false;
        }

        static IEnumerator DeferOneFrame(
            ItemStatsSystem.Item item,
            UnityEngine.Vector3 position,
            UnityEngine.Quaternion rotation,
            bool moveToMainScene,
            InteractableLootbox prefab,
            bool filterDontDropOnDead,
            CharacterMainControl deadOwner)
        {
            yield return null;

            var old = DeadLootSpawnContext.InOnDead;
            DeadLootSpawnContext.InOnDead = deadOwner;

            _bypassDefer = true;
            try
            {
                InteractableLootbox.CreateFromItem(
                    item, position, rotation, moveToMainScene, prefab, filterDontDropOnDead
                );
            }
            finally
            {
                _bypassDefer = false;
                DeadLootSpawnContext.InOnDead = old;
            }
        }
    }



    [HarmonyPatch(typeof(AICharacterController), "Update")]
    static class Patch_AICC_ZeroForceTraceMain
    {
        static void Prefix(AICharacterController __instance)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted || !mod.IsServer) return;

            // 直接把强追距离清零，避免原逻辑把目标强行锁到 CharacterMainControl.Main
            __instance.forceTracePlayerDistance = 0f;
        }
    }

    static class NcMainRedirector
    {
        [System.ThreadStatic] static CharacterMainControl _overrideMain;
        public static CharacterMainControl Current => _overrideMain;

        public static void Set(CharacterMainControl cmc) { _overrideMain = cmc; }
        public static void Clear() { _overrideMain = null; }
    }

    [HarmonyPatch(typeof(CharacterMainControl), "get_Main")]
    static class Patch_CMC_Main_OverrideDuringFSM
    {
        static bool Prefix(ref CharacterMainControl __result)
        {
            var ov = NcMainRedirector.Current;
            if (ov != null)
            {
                __result = ov;
                return false;
            }
            return true; 
        }
    }

    [HarmonyPatch(typeof(FSM), "OnGraphUpdate")]
    static class Patch_FSM_OnGraphUpdate_MainRedirect
    {
        static void Prefix(FSM __instance)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted || !mod.IsServer) return;


            Component agent = null;
            try
            {
                agent = (Component)AccessTools.Property(typeof(NodeCanvas.Framework.Graph), "agent").GetValue(__instance, null);
            }
            catch { }

            if (!agent) return;

            var aiCmc = agent.GetComponentInParent<CharacterMainControl>();
            if (!aiCmc) return;
            if (!mod.IsRealAI(aiCmc)) return; // 只对真正的AI生效，避免影响玩家自己的图

            // 计算这只 AI 同场景下最近、且活着、且敌对 的玩家（主机 + 各远端）
            var scene = agent.gameObject.scene;
            var best = FindNearestEnemyPlayer(mod, aiCmc, scene, aiCmc.transform.position);
            if (best != null)
                NcMainRedirector.Set(best);
        }

        static void Postfix()
        {
            // 清理现场，避免影响其它对象
            NcMainRedirector.Clear();
        }

        static CharacterMainControl FindNearestEnemyPlayer(ModBehaviour mod, CharacterMainControl ai, Scene scene, Vector3 aiPos)
        {
            CharacterMainControl best = null;
            float bestD2 = float.MaxValue;

            void Try(CharacterMainControl cmc)
            {
                if (!cmc) return;
                if (!cmc.gameObject.activeInHierarchy) return;
                if (cmc.gameObject.scene != scene) return;
                if (cmc.Team == ai.Team) return;          
                if (!mod.IsAlive(cmc)) return;

                float d2 = (cmc.transform.position - aiPos).sqrMagnitude;
                if (d2 < bestD2) { bestD2 = d2; best = cmc; }
            }

            // 主机本地玩家
            Try(CharacterMainControl.Main);

            // 服务器维护的各远端玩家克隆
            foreach (var kv in mod.remoteCharacters)
            {
                var go = kv.Value; if (!go) continue;
                var cmc = go.GetComponent<CharacterMainControl>() ?? go.GetComponentInChildren<CharacterMainControl>(true);
                Try(cmc);
            }

            return best;
        }
    }

    [HarmonyPatch(typeof(AICharacterController), "Update")]
    static class Patch_AIC_Update_PickNearestPlayer
    {
        static void Postfix(AICharacterController __instance)
        {
            var mod = ModBehaviour.Instance;
           
            if (mod == null || !mod.networkStarted || !mod.IsServer || __instance == null) return;

            var aiCmc = __instance.CharacterMainControl;
            if (!aiCmc) return;

            //商人判断（这就是让商人不主动攻击的真真真真的地方）:)))
            if (__instance.name == "AIController_Merchant_Myst(Clone)") { return; }

            CharacterMainControl best = null;
            float bestD2 = float.MaxValue;

            void Consider(CharacterMainControl cmc)
            {
                if (!cmc) return;
           
                if (cmc.Team == aiCmc.Team) return;

                // 存活判定
                var h = cmc.Health;
                if (!h) return;
                float hp = 1f;
                try { hp = h.CurrentHealth; } catch { }
                if (hp <= 0f) return;

                // 视距/视角
                Vector3 delta = cmc.transform.position - __instance.transform.position;
                float dist2 = delta.sqrMagnitude;
                float maxDist = (__instance.sightDistance > 0f ? __instance.sightDistance : 50f);
                if (dist2 > maxDist * maxDist) return;

                if (__instance.sightAngle > 1f)
                {
                    Vector3 fwd = __instance.transform.forward; fwd.y = 0f;
                    Vector3 dir = delta; dir.y = 0f;
                    if (dir.sqrMagnitude < 1e-6f) return;
                    float cos = Vector3.Dot(dir.normalized, fwd.normalized);
                    float cosThresh = Mathf.Cos(__instance.sightAngle * 0.5f * Mathf.Deg2Rad);
                    if (cos < cosThresh) return;
                }

                if (dist2 < bestD2)
                {
                    bestD2 = dist2;
                    best = cmc;
                }
            }

            // 1) 主机本体
            Consider(CharacterMainControl.Main); 

            // 2) 所有客户端玩家的镜像（主机表）
            if (mod.remoteCharacters != null)
            {
                foreach (var kv in mod.remoteCharacters) 
                {
                    var go = kv.Value;
                    if (!go) continue;
                    var cmc = go.GetComponent<CharacterMainControl>();
                    Consider(cmc);
                }
            }

            if (best == null) return;

            //  与现有目标比较若当前目标未死亡/同队且更近，则保留；否则切换 
            var cur = __instance.searchedEnemy;
            if (cur)
            {
                bool bad = false;
                try { if (cur.Team == aiCmc.Team) bad = true; } catch { }
                try { if (cur.health != null && cur.health.CurrentHealth <= 0f) bad = true; } catch { }
                if (cur.gameObject.scene != __instance.gameObject.scene) bad = true;

                if (!bad)
                {
                    float curD2 = (cur.transform.position - __instance.transform.position).sqrMagnitude;
       
                    if (curD2 <= bestD2 * 0.81f) return;
                }
            }

            // 切换到最近玩家
            var dr = best.mainDamageReceiver;
            if (dr)
            {
                __instance.searchedEnemy = dr;                      // 行为树/FSM普遍用 searchedEnemy
                __instance.SetTarget(dr.transform);
                __instance.SetNoticedToTarget(dr);                  // 同步“已注意到”的来源
            }
        }
    }


    [HarmonyPatch(typeof(LevelManager), "StartInit")]
    static class Patch_Level_StartInit_Gate
    {
        static bool Prefix(LevelManager __instance, SceneLoadingContext context)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null) return true;         
            if (mod.IsServer) return true;         

            bool needGate = mod.sceneVoteActive || (mod.networkStarted && !mod.IsServer);
            if (!needGate) return true;

            RunAsync(__instance, context).Forget();
            return false; 
        }

        static async UniTaskVoid RunAsync(LevelManager self, SceneLoadingContext ctx)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null) return;

            await mod.Client_SceneGateAsync();

            try
            {
                var m = AccessTools.Method(typeof(LevelManager), "InitLevel", new Type[] { typeof(SceneLoadingContext) });
                if (m != null) m.Invoke(self, new object[] { ctx });
            }
            catch (Exception e)
            {
                Debug.LogError("[SCENE] StartInit gate -> InitLevel failed: " + e);
            }
        }
    }

    [HarmonyPatch(typeof(MapSelectionEntry), "OnPointerClick")]
    static class Patch_Mapen_OnPointerClick
    {
        static bool Prefix(MapSelectionEntry __instance, PointerEventData eventData)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted) return true;  
            if (!mod.IsServer) return false;                       
            mod.IsMapSelectionEntry = true;
            mod.Host_BeginSceneVote_Simple(__instance.SceneID, "", false, false, false, "OnPointerClick");
            return false;
        }
    }

    //[HarmonyPatch(typeof(ZoneDamage), "Damage")]
    //static class Patch_Mapen_ZoneDamage
    //{
    //    static bool Prefix(ZoneDamage __instance)
    //    {
    //        var mod = ModBehaviour.Instance;
    //        if (mod == null || !mod.networkStarted) return true; 


    //        foreach (Health health in __instance.zone.Healths)
    //        {
    //            if(health.gameObject == null)
    //            {
    //                return false;
    //            }
    //            if(health.gameObject.GetComponent<AutoRequestHealthBar>() != null)
    //            {
    //                return false;
    //            }
    //        }


    //        return true;
    //    }
    //}


    [HarmonyPatch(typeof(Health), "Hurt", new[] { typeof(global::DamageInfo) })]
    static class Patch_CoopPlayer_Health_Hurt
    {
        [HarmonyPriority(Priority.First)]
        static bool Prefix(Health __instance, ref global::DamageInfo damageInfo)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted) return true; 

            if (!mod.IsServer)
            {
                bool isMain = false; try { isMain = __instance.IsMainCharacterHealth; } catch { }
                if (isMain) return true;
            }

            bool isProxy = __instance.gameObject.GetComponent<AutoRequestHealthBar>() != null;

            if (mod.IsServer && isProxy)
            {
                var owner = mod.Server_FindOwnerPeerByHealth(__instance);
                if (owner != null)
                {
                    try { mod.Server_ForwardHurtToOwner(owner, damageInfo); }
                    catch (System.Exception e) { UnityEngine.Debug.LogWarning("[HP] forward to owner failed: " + e); }
                }
                return false; 
            }

            if (!mod.IsServer && isProxy) return false;
            return true;
        }
    }

    static class WorldLootPrime
    {
        public static void PrimeIfClient(InteractableLootbox lb)
        {
            var mod = 鸭科夫联机Mod.ModBehaviour.Instance;
            if (mod == null || mod.IsServer) return;
            if (!lb) return;

            var inv = lb.Inventory;
            if (!inv) return;

            // 把它标记成“世界容器”（只缓存 true，避免误判成 false）
            LootSearchWorldGate.EnsureWorldFlag(inv);

            // 已经是需搜索就别重复改（幂等）
            bool need = false;
            try { need = inv.NeedInspection; } catch { }
            if (need) return;

            try { lb.needInspect = true; } catch { }
            try { inv.NeedInspection = true; } catch { }

            // 只把顶层物品置为未鉴定即可（Inventory 可 foreach）
            try
            {
                foreach (var it in inv)
                {
                    if (!it) continue;
                    try { it.Inspected = false; } catch { }
                }
            }
            catch { }
        }
    }

    static class LootSearchWorldGate
    {

        static readonly Dictionary<ItemStatsSystem.Inventory, bool> _world = new Dictionary<Inventory, bool>();

        public static void EnsureWorldFlag(ItemStatsSystem.Inventory inv)
        {
            if (inv) _world[inv] = true; // 只缓存 true避免一次误判把容器永久当“非世界”
        }

        public static bool IsWorldLootByInventory(ItemStatsSystem.Inventory inv)
        {
            if (!inv) return false;
            if (_world.TryGetValue(inv, out var yes) && yes) return true;

            // 动态匹配（不缓存 false）
            try
            {
                var boxes = UnityEngine.Object.FindObjectsOfType<InteractableLootbox>(true);
                foreach (var b in boxes)
                {
                    if (!b) continue;
                    if (b.Inventory == inv)
                    {
                        bool isWorld = b.GetComponent<Duckov.Utilities.LootBoxLoader>() != null;
                        if (isWorld) _world[inv] = true;
                        return isWorld;
                    }
                }
            }
            catch { }
            return false;
        }

        static MemberInfo _miNeedInspection;

        internal static bool GetNeedInspection(Inventory inv)
        {
            if (inv == null) return false;
            try
            {
                var m = FindNeedInspectionMember(inv.GetType());
                if (m is FieldInfo fi) return (bool)(fi.GetValue(inv) ?? false);
                if (m is PropertyInfo pi) return (bool)(pi.GetValue(inv) ?? false);
            }
            catch { }
            return false;
        }

        static MemberInfo FindNeedInspectionMember(Type t)
        {
            if (_miNeedInspection != null) return _miNeedInspection;
            _miNeedInspection = (MemberInfo)t.GetField("NeedInspection", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                               ?? t.GetProperty("NeedInspection", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return _miNeedInspection;
        }

        internal static void TrySetNeedInspection(ItemStatsSystem.Inventory inv, bool v)
        {
            if (!inv) return;
            inv.NeedInspection = v;
        }


        internal static void ForceTopLevelUninspected(Inventory inv)
        {
            if (inv == null) return;
            try
            {
                foreach (var it in inv)
                {
                    if (!it) continue;
                    try { it.Inspected = false; } catch {}
                }
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(Duckov.Utilities.LootSpawner), "Start")]
    static class Patch_LootSpawner_Start_PrimeNeedInspect
    {
        static void Postfix(Duckov.Utilities.LootSpawner __instance)
        {
            var lb = __instance.GetComponent<InteractableLootbox>();
            WorldLootPrime.PrimeIfClient(lb);
        }
    }

    [HarmonyPatch(typeof(Duckov.Utilities.LootSpawner), "Setup")]
    static class Patch_LootSpawner_Setup_PrimeNeedInspect
    {
        static void Postfix(Duckov.Utilities.LootSpawner __instance)
        {
            var lb = __instance.GetComponent<InteractableLootbox>();
            WorldLootPrime.PrimeIfClient(lb);
        }
    }

    [HarmonyPatch(typeof(Duckov.Utilities.LootBoxLoader), "Awake")]
    static class Patch_LootBoxLoader_Awake_PrimeNeedInspect
    {
        static void Postfix(Duckov.Utilities.LootBoxLoader __instance)
        {
            var lb = __instance.GetComponent<InteractableLootbox>();
            WorldLootPrime.PrimeIfClient(lb);
        }
    }


    // 让“是否需要搜索”对所有公共容器生效（世界箱 + 尸体箱等）可能的修复:)
    [HarmonyPatch(typeof(Duckov.UI.LootView), nameof(Duckov.UI.LootView.HasInventoryEverBeenLooted))]
    static class Patch_LootView_HasInventoryEverBeenLooted_NeedAware_AllLoot
    {
        [HarmonyPriority(Priority.First)]
        static bool Prefix(ref bool __result, ItemStatsSystem.Inventory inventory)
        {
            if (!inventory) return true;

            if (LootboxDetectUtil.IsPrivateInventory(inventory)) return true;

            if (!LootboxDetectUtil.IsLootboxInventory(inventory)) return true;

            bool needInspect = false;
            try { needInspect = inventory.NeedInspection; } catch { }

            if (needInspect)
            {
                __result = false;   // 视为“未搜过” → UI 走搜索条/迷雾
                return false;     
            }
            return true;          
        }
    }


    [HarmonyPatch(typeof(InteractableLootbox), "StartLoot")]
    static class Patch_Lootbox_StartLoot_RequestState_AndPrime
    {
        static void Postfix(InteractableLootbox __instance)
        {
            var m = ModBehaviour.Instance;
            if (m == null || !m.networkStarted || m.IsServer) return;

            var inv = __instance ? __instance.Inventory : null;
            if (!inv) return;

            try { inv.Loading = true; } catch { }
            m.Client_RequestLootState(inv);
            m.KickLootTimeout(inv, 1.5f);

            if (!LootboxDetectUtil.IsPrivateInventory(inv) && LootboxDetectUtil.IsLootboxInventory(inv))
            {
                bool needInspect = false; try { needInspect = inv.NeedInspection; } catch { }
                if (!needInspect)
                {
                    bool hasUninspected = false;
                    try
                    {
                        foreach (var it in inv) { if (it != null && !it.Inspected) { hasUninspected = true; break; } }
                    }
                    catch { }
                    if (hasUninspected) inv.NeedInspection = true;
                }
            }
        }
    }


    [HarmonyPatch(typeof(global::Duckov.UI.LootView), "OnStartLoot")]
    static class Patch_LootView_OnStartLoot_PrimeSearchGate_Robust
    {
        static void Postfix(global::Duckov.UI.LootView __instance, global::InteractableLootbox lootbox)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted || mod.IsServer) return;

            var inv = __instance.TargetInventory;
            if (!inv || lootbox == null) return;

            if (LootboxDetectUtil.IsPrivateInventory(inv)) return;

            if (inv.hasBeenInspectedInLootBox) return;

            {
                int last = inv.GetLastItemPosition();
                bool allInspectedNow = true;
                for (int i = 0; i <= last; i++)
                {
                    var it = inv.GetItemAt(i);
                    if (it != null && !it.Inspected) { allInspectedNow = false; break; }
                }
                if (allInspectedNow) return;
            }

            TrySetNeedInspection(inv, true);
            TrySetLootboxNeedInspect(lootbox, true);

            mod.StartCoroutine(KickSearchGateOnceStable(inv, lootbox));
        }

        static System.Collections.IEnumerator KickSearchGateOnceStable(
            global::ItemStatsSystem.Inventory inv,
            global::InteractableLootbox lootbox)
        {
            yield return null;
            yield return null;

            if (!inv) yield break;

            int last = inv.GetLastItemPosition();
            bool allInspected = true;
            for (int i = 0; i <= last; i++)
            {
                var it = inv.GetItemAt(i);
                if (it != null && !it.Inspected) { allInspected = false; break; }
            }

            TrySetNeedInspection(inv, !allInspected);
            TrySetLootboxNeedInspect(lootbox, !allInspected);
        }

        static void TrySetNeedInspection(global::ItemStatsSystem.Inventory inv, bool v)
        {
            try { inv.NeedInspection = v; } catch { }
        }

        static void TrySetLootboxNeedInspect(global::InteractableLootbox box, bool v)
        {
            if (box == null) return;
            try
            {
                var t = box.GetType();
                var f = HarmonyLib.AccessTools.Field(t, "needInspect");
                if (f != null) { f.SetValue(box, v); return; }
                var p = HarmonyLib.AccessTools.Property(t, "needInspect");
                if (p != null && p.CanWrite) { p.SetValue(box, v, null); return; }
            }
            catch { }
        }
    }


    [HarmonyPatch(typeof(global::ItemStatsSystem.Inventory), nameof(global::ItemStatsSystem.Inventory.AddAt),
    new System.Type[] { typeof(global::ItemStatsSystem.Item), typeof(int) })]
    static class Patch_Inventory_AddAt_FlagUninspected_WhenApplyingLoot
    {
        static void Postfix(global::ItemStatsSystem.Inventory __instance, global::ItemStatsSystem.Item item)
        {
            ApplyUninspectedFlag(__instance, item);
        }

        static void ApplyUninspectedFlag(global::ItemStatsSystem.Inventory inv, global::ItemStatsSystem.Item item)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted || mod.IsServer) return;


            if (!mod.ApplyingLootState) return;

            if (LootboxDetectUtil.IsPrivateInventory(inv)) return;
            if (!(LootboxDetectUtil.IsLootboxInventory(inv) || ModBehaviour.IsCurrentLootInv(inv))) return;

            try
            {
                int last = inv.GetLastItemPosition();
                bool hasUninspected = false;
                for (int i = 0; i <= last; i++)
                {
                    var it = inv.GetItemAt(i);
                    if (it != null && !it.Inspected) { hasUninspected = true; break; }
                }
                inv.NeedInspection = hasUninspected;
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(global::ItemStatsSystem.Inventory), "AddItem",
        new System.Type[] { typeof(global::ItemStatsSystem.Item) })]
    static class Patch_Inventory_AddItem_FlagUninspected_WhenApplyingLoot
    {
        static void Postfix(global::ItemStatsSystem.Inventory __instance, global::ItemStatsSystem.Item item)
        {
            ApplyUninspectedFlag(__instance, item);
        }

        static void ApplyUninspectedFlag(global::ItemStatsSystem.Inventory inv, global::ItemStatsSystem.Item item)
        {
            var mod = ModBehaviour.Instance;
            if (mod == null || !mod.networkStarted || mod.IsServer) return;

            if (!mod.ApplyingLootState) return;

            if (LootboxDetectUtil.IsPrivateInventory(inv)) return;
            if (!(LootboxDetectUtil.IsLootboxInventory(inv) || ModBehaviour.IsCurrentLootInv(inv))) return;

            try
            {
                int last = inv.GetLastItemPosition();
                bool hasUninspected = false;
                for (int i = 0; i <= last; i++)
                {
                    var it = inv.GetItemAt(i);
                    if (it != null && !it.Inspected) { hasUninspected = true; break; }
                }
                inv.NeedInspection = hasUninspected;
            }
            catch { }
        }

    }












}
