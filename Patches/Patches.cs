using System;
using System.Collections;
using System.Collections.Generic;
using BonelabMultiplayerMockup.NetworkData;
using BonelabMultiplayerMockup.Nodes;
using BonelabMultiplayerMockup.Object;
using BonelabMultiplayerMockup.Packets;
using BonelabMultiplayerMockup.Packets.Gun;
using BonelabMultiplayerMockup.Packets.Object;
using BonelabMultiplayerMockup.Packets.Player;
using BonelabMultiplayerMockup.Representations;
using BonelabMultiplayerMockup.Utils;
using BoneLib;
using HarmonyLib;
using MelonLoader;
using SLZ;
using SLZ.AI;
using SLZ.Bonelab;
using SLZ.Combat;
using SLZ.Interaction;
using SLZ.Marrow.Pool;
using SLZ.Marrow.Warehouse;
using SLZ.Props.Weapons;
using SLZ.Rig;
using SLZ.SFX;
using SLZ.Zones;
using UnityEngine;
using UnityEngine.Events;
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

    public class HandJoints
    {
        public static FixedJoint lHandJoint;
        public static FixedJoint rHandJoint;
    }

    public class Patches
    {

        [HarmonyPatch(typeof(AIBrain), "OnDeath")]
        private class AiDeathPatch
        {
            public static void Postfix(AIBrain __instance)
            {
                if (DiscordIntegration.hasLobby)
                {
                    SyncedObject syncedObject = SyncedObject.GetSyncedComponent(__instance.gameObject);

                    if (syncedObject)
                    {
                        if (syncedObject.IsClientSimulated())
                        {
                            var npcDeathData = new NpcDeathData()
                            {
                                npcId = syncedObject.currentId
                            };
                            var packetByteBuf = PacketHandler.CompressMessage(NetworkMessageType.NpcDeathPacket,
                                npcDeathData);
                            Node.activeNode.BroadcastMessage((byte)NetworkChannel.Object, packetByteBuf.getBytes());
                        }
                    }
                }
            }
        }

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
                            foreach (SyncedObject syncedObject in SyncedObject.GetAllSyncables(__instance.gameObject))
                            {
                                syncedObject.DestroySyncable(false);
                            }

                            DebugLogger.Msg("Erased sync data on: " + __instance.gameObject.name);
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
                        foreach (SyncedObject syncedObject in SyncedObject.GetAllSyncables(__instance.gameObject))
                        {
                            syncedObject.DestroySyncable(false);
                        }

                        DebugLogger.Msg("Erased sync data on: " + __instance.gameObject.name);
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

                    DebugLogger.Msg("Spawned object via pool: " + __instance.name);

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
        
        [HarmonyPatch(typeof(HandSFX), "PunchAttack", typeof(Collision), typeof(float), typeof(float))]
        public static class PunchDamagePatch
        {
            public static void Postfix(HandSFX __instance, Collision c, float impulse, float relVelSqr)
            {

                GameObject collided = c.gameObject;
                if (!Utils.Utils.IsPlayerPart(c.gameObject))
                {
                    return;
                }
                string playerName = collided.transform.root.name.Replace("(PlayerRep) ", "");
                
                PlayerRepresentation punchedRep = null;
                foreach (PlayerRepresentation playerRepresentation in PlayerRepresentation.representations.Values) {
                    if (playerName == playerRepresentation.username)
                    {
                        punchedRep = playerRepresentation;
                        break;
                    }
                }

                if (punchedRep == null)
                {
                    return;
                }

                var damageData = new PlayerDamageData()
                {
                    damage = impulse / 5
                };
                PacketByteBuf packetByteBuf =
                    PacketHandler.CompressMessage(NetworkMessageType.PlayerDamagePacket, damageData);
                Node.activeNode.SendMessage(punchedRep.user.Id, (byte)NetworkChannel.Attack, packetByteBuf.getBytes());
            }
        }

        [HarmonyPatch(typeof(ImpactSFX), "BluntAttack", typeof(float), typeof(Collision))]
        public static class ImpactDamagePatch
        {
            public static void Postfix(ImpactSFX __instance, float impulse, Collision c)
            {
                if (!DiscordIntegration.hasLobby)
                {
                    return;
                }
                
                SyncedObject syncedObject = SyncedObject.GetSyncedComponent(__instance._rb.gameObject);
                if (syncedObject)
                {
                    if (!syncedObject.IsClientSimulated())
                    {
                        return;
                    }
                }

                GameObject collided = c.gameObject;
                if (!Utils.Utils.IsPlayerPart(c.gameObject))
                {
                    return;
                }
                string playerName = collided.transform.root.name.Replace("(PlayerRep) ", "");
                
                PlayerRepresentation punchedRep = null;
                foreach (PlayerRepresentation playerRepresentation in PlayerRepresentation.representations.Values) {
                    if (playerName == playerRepresentation.username)
                    {
                        punchedRep = playerRepresentation;
                        break;
                    }
                }

                if (punchedRep == null)
                {
                    return;
                }

                var damageData = new PlayerDamageData()
                {
                    damage = (impulse / 5) * __instance.bluntDamageMult
                };
                PacketByteBuf packetByteBuf =
                    PacketHandler.CompressMessage(NetworkMessageType.PlayerDamagePacket, damageData);
                Node.activeNode.SendMessage(punchedRep.user.Id, (byte)NetworkChannel.Attack, packetByteBuf.getBytes());
            }
        }
        
        [HarmonyPatch(typeof(StabSlash.StabPoint), "SpawnStab", typeof(Transform), typeof(Collision), typeof(float), typeof(ImpactProperties))]
        public static class SpawnStabPatch
        {
            public static void Postfix(StabSlash.StabPoint __instance, 
                Transform tran,
                Collision c,
                float stabForce,
                ImpactProperties surfaceProperties)
            {
                if (!DiscordIntegration.hasLobby)
                {
                    return;
                }

                SyncedObject syncedObject = SyncedObject.GetSyncedComponent(__instance.rb.gameObject);
                if (syncedObject)
                {
                    if (!syncedObject.IsClientSimulated())
                    {
                        return;
                    }
                }

                GameObject collided = c.gameObject;
                if (!Utils.Utils.IsPlayerPart(c.gameObject))
                {
                    return;
                }
                string playerName = collided.transform.root.name.Replace("(PlayerRep) ", "");
                
                PlayerRepresentation punchedRep = null;
                foreach (PlayerRepresentation playerRepresentation in PlayerRepresentation.representations.Values) {
                    if (playerName == playerRepresentation.username)
                    {
                        punchedRep = playerRepresentation;
                        break;
                    }
                }

                if (punchedRep == null)
                {
                    return;
                }

                var damageData = new PlayerDamageData()
                {
                    damage = __instance.damage
                };
                PacketByteBuf packetByteBuf =
                    PacketHandler.CompressMessage(NetworkMessageType.PlayerDamagePacket, damageData);
                Node.activeNode.SendMessage(punchedRep.user.Id, (byte)NetworkChannel.Attack, packetByteBuf.getBytes());
            }
        }

        
        [HarmonyPatch(typeof(BalloonGun), "Fire")]
        private class BalloonGunFirePatch
        {
            public static void Postfix(BalloonGun __instance)
            {
                if (DiscordIntegration.hasLobby)
                {
                    SyncedObject gunSynced = SyncedObject.GetSyncedComponent(__instance.gameObject);

                    if (gunSynced)
                    {
                        if (gunSynced.IsClientSimulated())
                        {
                            DebugLogger.Msg("Balloon Gun fired");
                            var balloonFireData = new BalloonGunFireData()
                            {
                                objectId = gunSynced.currentId,
                            };
                            var packetByteBuf = PacketHandler.CompressMessage(NetworkMessageType.BalloonFirePacket,
                                balloonFireData);
                            Node.activeNode.BroadcastMessage((byte)NetworkChannel.Object, packetByteBuf.getBytes());
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

        [HarmonyPatch(typeof(Hand), "DetachObject")]
        private class HandDetachPatch
        {
            public static void Postfix(Hand __instance)
            {
                if (DiscordIntegration.hasLobby)
                {
                    if (__instance.handedness == Handedness.RIGHT)
                    {
                        GameObject.Destroy(HandJoints.rHandJoint);
                    }

                    if (__instance.handedness == Handedness.LEFT)
                    {
                        GameObject.Destroy(HandJoints.lHandJoint);
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
                        if (Utils.Utils.IsPlayerPart(objectToAttach))
                        {
                            if (__instance.handedness == Handedness.RIGHT)
                            {
                                if (HandJoints.rHandJoint != null)
                                {
                                    GameObject.Destroy(HandJoints.rHandJoint);
                                }

                                HandJoints.rHandJoint = __instance.gameObject.AddComponent<FixedJoint>();
                                HandJoints.rHandJoint.connectedBody = objectToAttach.GetComponent<Rigidbody>();
                                HandJoints.rHandJoint.breakForce = Single.PositiveInfinity;
                                HandJoints.rHandJoint.breakTorque = Single.PositiveInfinity;
                            }

                            if (__instance.handedness == Handedness.LEFT)
                            {
                                if (HandJoints.lHandJoint != null)
                                {
                                    GameObject.Destroy(HandJoints.lHandJoint);
                                }

                                HandJoints.lHandJoint = __instance.gameObject.AddComponent<FixedJoint>();
                                HandJoints.lHandJoint.connectedBody = objectToAttach.GetComponent<Rigidbody>();
                                HandJoints.lHandJoint.breakForce = Single.PositiveInfinity;
                                HandJoints.lHandJoint.breakTorque = Single.PositiveInfinity;
                            }
                        }

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
}