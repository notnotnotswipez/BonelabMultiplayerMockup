using System.Collections;
using System.Collections.Generic;
using BonelabMultiplayerMockup.NetworkData;
using BonelabMultiplayerMockup.Nodes;
using BonelabMultiplayerMockup.Object;
using BonelabMultiplayerMockup.Packets;
using BonelabMultiplayerMockup.Packets.Gun;
using BonelabMultiplayerMockup.Packets.Player;
using BonelabMultiplayerMockup.Utils;
using BoneLib;
using HarmonyLib;
using MelonLoader;
using SLZ.AI;
using SLZ.Bonelab;
using SLZ.Interaction;
using SLZ.Marrow.Pool;
using SLZ.Marrow.Warehouse;
using SLZ.Props.Weapons;
using SLZ.Rig;
using SLZ.Zones;
using UnityEngine;
using Avatar = SLZ.VRMK.Avatar;

namespace BonelabMultiplayerMockup.Patches
{
    public class PatchVariables
    {
        public static bool shouldIgnoreGrabbing = false;
        public static bool shouldIgnoreGunEvents = false;
        public static bool shouldIgnoreAvatarSwitch = false;
    }

    public class PatchCoroutines
    {
        public static IEnumerator WaitForAvatarSwitch()
        {
            yield return new WaitForSecondsRealtime(1f);

            var swapAvatarMessageData = new AvatarChangeData()
            {
                userId = DiscordIntegration.currentUser.Id,
                barcode = Player.GetRigManager().GetComponentInChildren<RigManager>()._avatarCrate._barcode._id
            };
            var packetByteBuf = PacketHandler.CompressMessage(NetworkMessageType.AvatarChangePacket,
                swapAvatarMessageData);
            Node.activeNode.BroadcastMessage((byte)NetworkChannel.Reliable, packetByteBuf.getBytes());
            BonelabMultiplayerMockup.PopulateCurrentAvatarData();
            if (BonelabMultiplayerMockup.debugRep != null)
            {
                BonelabMultiplayerMockup.debugRep.SetAvatar(Player.GetRigManager().GetComponentInChildren<RigManager>()._avatarCrate._barcode._id);
            }
        }

        public static IEnumerator WaitForNPCSync(GameObject npc)
        {
            yield return new WaitForSecondsRealtime(2);
            SyncedObject syncedObject = SyncedObject.GetSyncedComponent(npc);
            if (syncedObject == null)
            {
                SyncedObject.Sync(npc);
            }
            else
            {
                syncedObject.BroadcastOwnerChange();
            }

        }

        public static IEnumerator WaitForAttachSync(GameObject toSync)
        {
            if (PatchVariables.shouldIgnoreGrabbing)
            {
                yield break;
            }

            PatchVariables.shouldIgnoreGrabbing = true;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            var syncedObject = SyncedObject.GetSyncedComponent(toSync);
            if (syncedObject == null)
                SyncedObject.Sync(toSync);
            else
                syncedObject.BroadcastOwnerChange();
            PatchVariables.shouldIgnoreGrabbing = false;
        }
        
        
    }

    public class Patches
    {
        [HarmonyPatch(typeof(AIBrain), "OnResurrection")]
        private class AiResurrectionPatch
        {
            public static void Postfix(AIBrain __instance)
            {
                if (DiscordIntegration.hasLobby)
                {

                    if (DiscordIntegration.isHost)
                    {
                        bool isSyncedAlready = SyncedObject.isSyncedObject(__instance.gameObject);

                        if (isSyncedAlready)
                        {
                            foreach (SyncedObject syncedObject in SyncedObject.GetAllSyncables(__instance.gameObject)) {
                                syncedObject.DestroySyncable(false);
                            }
                            MelonLogger.Msg("Erased sync data on: "+__instance.gameObject.name);
                        }
                        SyncedObject.Sync(__instance.gameObject);
                    }
                }
            }
        }
        
        [HarmonyPatch(typeof(AssetPoolee), "OnDespawn")]
        private class PoolDespawnPatch
        {
            public static void Postfix(AssetPoolee __instance)
            {
                if (DiscordIntegration.hasLobby)
                {

                    bool isSyncedAlready = SyncedObject.isSyncedObject(__instance.gameObject);

                    if (isSyncedAlready)
                    {
                        foreach (SyncedObject syncedObject in SyncedObject.GetAllSyncables(__instance.gameObject)) {
                            syncedObject.DestroySyncable(false);
                        }
                        MelonLogger.Msg("Erased sync data on: "+__instance.gameObject.name);
                    }
                }
            }
        }
        
        [HarmonyPatch(typeof(AssetPoolee), "OnSpawn")]
        private class PoolPatch
        {
            public static void Postfix(AssetPoolee __instance)
            {
                if (DiscordIntegration.hasLobby)
                {

                    bool isNPC = PoolManager.GetComponentOnObject<AIBrain>(__instance.gameObject) != null;
                    bool foundSpawnGun = false;
                    bool isHost = DiscordIntegration.isHost;
                    if (Player.GetComponentInHand<SpawnGun>(Player.leftHand))
                    {
                        foundSpawnGun = true;
                    }

                    if (Player.GetComponentInHand<SpawnGun>(Player.rightHand))
                    {
                        foundSpawnGun = true;
                    }
                    DebugLogger.Msg("Spawned object via pool: "+__instance.name);

                    bool isSyncedAlready = false;

                    if (SyncedObject.isSyncedObject(__instance.gameObject))
                    {
                        isSyncedAlready = true;
                        DebugLogger.Error("THIS OBJECT IS ALREADY SYNCED, THIS SHOULD BE TRANSFERRED.");
                    }
                    

                    if (!foundSpawnGun && !DiscordIntegration.isHost && isNPC)
                    {
                        DebugLogger.Msg("Didnt spawn NPC via spawn gun and is not the host of the lobby, despawning.");
                        DebugLogger.Msg("PROBABLY CAMPAIGN NPC! WAIT FOR THE HOST TO SPAWN THIS NPC IN!");
                        __instance.Despawn();
                    }
                    else
                    {
                        if (PoolManager.GetComponentOnObject<Rigidbody>(__instance.gameObject))
                        {
                            if (isNPC)
                            {
                                if (foundSpawnGun || isHost)
                                {
                                    MelonCoroutines.Start(PatchCoroutines.WaitForNPCSync(__instance.gameObject));
                                }
                            }
                            else
                            {
                                if (foundSpawnGun)
                                {
                                    MelonCoroutines.Start(PatchCoroutines.WaitForNPCSync(__instance.gameObject));
                                    DebugLogger.Msg("Synced spawn gun spawned Object: " + __instance.gameObject.name);
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Gun), "OnFire")]
        private class GunFirePatch
        {
            public static void Postfix(Gun __instance)
            {
                if (DiscordIntegration.hasLobby)
                {

                    bool isMine = false;
                    /*if (Player.GetComponentInHand<Gun>(Player.leftHand))
                    {
                        Gun gun = Player.GetComponentInHand<Gun>(Player.leftHand);
                        if (gun == __instance)
                        {
                            isMine = true;
                        }
                    }
                    if (Player.GetComponentInHand<Gun>(Player.rightHand))
                    {
                        Gun gun = Player.GetComponentInHand<Gun>(Player.rightHand);
                        if (gun == __instance)
                        {
                            isMine = true;
                        }
                    }*/
                    
                    SyncedObject gunSynced = SyncedObject.GetSyncedComponent(__instance.gameObject);

                    if (gunSynced)
                    {
                        if (gunSynced.IsClientSimulated())
                        {
                            DebugLogger.Msg("Gun fired");
                            var gunStateMessageData = new GunStateData()
                            {
                                objectid = gunSynced.currentId,
                                state = 0
                            };
                            var packetByteBuf = PacketHandler.CompressMessage(NetworkMessageType.GunStatePacket,
                                gunStateMessageData);
                            Node.activeNode.BroadcastMessage((byte)NetworkChannel.Object, packetByteBuf.getBytes());
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(RigManager), "SwapAvatar", new[] { typeof(Avatar) })]
        private class AvatarSwapPatch
        {
            public static void Postfix(RigManager __instance)
            {
                if (DiscordIntegration.hasLobby)
                {
                    if (PatchVariables.shouldIgnoreAvatarSwitch)
                    {
                        return;
                    }

                    DebugLogger.Msg("Swapped Avatar");
                    MelonCoroutines.Start(PatchCoroutines.WaitForAvatarSwitch());
                }
            }
        }
        
        [HarmonyPatch(typeof(RigManager), "SwitchAvatar", new[] { typeof(Avatar) })]
        private class AvatarSwitchPatch
        {
            public static void Postfix(RigManager __instance)
            {
                if (DiscordIntegration.hasLobby)
                {
                    if (PatchVariables.shouldIgnoreAvatarSwitch)
                    {
                        return;
                    }
                    DebugLogger.Msg("Switched Avatar");
                    MelonCoroutines.Start(PatchCoroutines.WaitForAvatarSwitch());
                }
            }
        }

        [HarmonyPatch(typeof(Hand), "AttachObject", typeof(GameObject))]
        private class HandGrabPatch
        {
            public static void Postfix(Hand __instance, GameObject objectToAttach)
            {
                if (DiscordIntegration.hasLobby)
                {
                    DebugLogger.Msg("Grabbed: " + objectToAttach.name);
                    MelonCoroutines.Start(PatchCoroutines.WaitForAttachSync(objectToAttach));
                }
            }
        }

        [HarmonyPatch(typeof(ForcePullGrip), "OnFarHandHoverUpdate")]
        public class ForcePullPatch
        {
            public static void Prefix(ForcePullGrip __instance, ref bool __state, Hand hand)
            {
                __state = __instance.pullCoroutine != null;
            }

            public static void Postfix(ForcePullGrip __instance, ref bool __state, Hand hand)
            {
                if (!(__instance.pullCoroutine != null && !__state))
                    return;
                MelonCoroutines.Start(PatchCoroutines.WaitForAttachSync(__instance.gameObject));
            }
        }
    }
}