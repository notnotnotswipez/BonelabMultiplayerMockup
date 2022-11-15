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
using SLZ.Marrow.Data;
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
        public static bool shouldIgnoreNPCDeath = false;
    }

    public class PatchCoroutines
    {
        public static IEnumerator WaitForAvatarSwitch()
        {
            yield return new WaitForSecondsRealtime(1f);

            try
            {
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
                    BonelabMultiplayerMockup.debugRep.SetAvatar(Player.GetRigManager().GetComponentInChildren<RigManager>()
                        ._avatarCrate._barcode._id);
                }
            }
            catch (Exception e)
            {
                // Ignore.
            }
        }

        public static IEnumerator WaitForSpawnSync(GameObject npc)
        {
            yield return new WaitForSecondsRealtime(2);
            SyncedObject syncedObject = SyncedObject.GetSyncedComponent(npc);
            if (syncedObject == null)
            {
                SyncedObject.Sync(npc, false);
            }
            else
            {
                syncedObject.ManualSetOwner(DiscordIntegration.currentUser.Id, false);
            }
        }

        public static IEnumerator WaitForSpawnRequest(AssetPoolee poolee)
        {
            var spawnRequestData = new SpawnRequestData()
            {
                userId = DiscordIntegration.currentUser.Id,
                position = new BytePosition(poolee.gameObject.transform.position),
                barcode = poolee.spawnableCrate._barcode
            };

            PacketByteBuf packetByteBuf =
                PacketHandler.CompressMessage(NetworkMessageType.SpawnRequestPacket, spawnRequestData);
            
            Node.activeNode.SendMessage(DiscordIntegration.lobby.OwnerId, (byte)NetworkChannel.Transaction, packetByteBuf.getBytes());
            poolee.Despawn();
            yield break;
        }

        public static IEnumerator WaitForAttachSync(GameObject toSync, Handedness hand, bool setCacheGrabbed)
        {
            yield return new WaitForFixedUpdate();
            var syncedObject = SyncedObject.GetSyncedComponent(toSync);
            if (syncedObject == null)
            {
                SyncedObject.Sync(toSync);

                // Get the component again
                syncedObject = SyncedObject.GetSyncedComponent(toSync);

                // Could have been blacklisted.
                if (syncedObject != null)
                {
                    if (setCacheGrabbed)
                    {
                        syncedObject.BroadcastGrabState(true);
                        // We dont want duplicate cached synced objects cause obviously, its a bit pointless to store synced objects that belong to the same group on both hands.
                        if (hand == Handedness.RIGHT)
                        {
                            if (HandVariables.lSyncedObject != null)
                            {
                                if (HandVariables.lSyncedObject.groupId != syncedObject.groupId)
                                {
                                    HandVariables.rSyncedObject = syncedObject;
                                }
                            }
                            else
                            {
                                HandVariables.rSyncedObject = syncedObject;
                            }
                        }

                        if (hand == Handedness.LEFT)
                        {
                            if (HandVariables.rSyncedObject != null)
                            {
                                if (HandVariables.rSyncedObject.groupId != syncedObject.groupId)
                                {
                                    HandVariables.lSyncedObject = syncedObject;
                                }
                            }
                            else
                            {
                                HandVariables.lSyncedObject = syncedObject;
                            }
                        }
                    }
                }
            }
            else
            {
                syncedObject.BroadcastOwnerChange();

                if (setCacheGrabbed)
                {
                    syncedObject.BroadcastGrabState(true);
                    if (hand == Handedness.RIGHT)
                    {
                        if (HandVariables.lSyncedObject != null)
                        {
                            if (HandVariables.lSyncedObject.groupId != syncedObject.groupId)
                            {
                                HandVariables.rSyncedObject = syncedObject;
                            }
                        }
                        else
                        {
                            HandVariables.rSyncedObject = syncedObject;
                        }
                    }

                    if (hand == Handedness.LEFT)
                    {
                        if (HandVariables.rSyncedObject != null)
                        {
                            if (HandVariables.rSyncedObject.groupId != syncedObject.groupId)
                            {
                                HandVariables.lSyncedObject = syncedObject;
                            }
                        }
                        else
                        {
                            HandVariables.lSyncedObject = syncedObject;
                        }
                    }
                }
            }
        }

        public static IEnumerator WaitForObjectDetach(Handedness handedness)
        {
            yield return new WaitForSecondsRealtime(0.2f);
            SyncedObject handedSynced = null;
            if (handedness == Handedness.RIGHT)
            {
                handedSynced = HandVariables.rSyncedObject;
            }

            if (handedness == Handedness.LEFT)
            {
                handedSynced = HandVariables.lSyncedObject;
            }

            if (handedSynced != null)
            {
                if (!Utils.Utils.IsGrabbedStill(handedSynced))
                {
                    if (handedSynced.IsClientSimulated())
                    {
                        handedSynced.BroadcastGrabState(false);
                    }

                    if (handedness == Handedness.RIGHT)
                    {
                        HandVariables.rSyncedObject = null;
                    }

                    if (handedness == Handedness.LEFT)
                    {
                        HandVariables.lSyncedObject = null;
                    }
                }
            }
        }
    }

    public class HandVariables
    {
        public static FixedJoint lHandJoint;
        public static FixedJoint rHandJoint;

        public static FixedJoint lRepPelvisJoint;
        public static FixedJoint rRepPelvisJoint;

        public static PlayerRepresentation rGrabbedPlayerRep;
        public static PlayerRepresentation lGrabbedPlayerRep;

        public static SyncedObject lSyncedObject;
        public static SyncedObject rSyncedObject;
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
                    if (PatchVariables.shouldIgnoreNPCDeath)
                    {
                        PatchVariables.shouldIgnoreNPCDeath = false;
                        return;
                    }

                    SyncedObject syncedObject = SyncedObject.GetSyncedComponent(__instance.gameObject);

                    if (syncedObject)
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

        [HarmonyPatch(typeof(AIBrain), "OnResurrection")]
        private class AiResurrectionPatch
        {
            public static void Prefix(AIBrain __instance)
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

        [HarmonyPatch(typeof(AssetPoolee), "StageForDespawn")]
        private class PoolDespawnPatch
        {
            public static void Prefix(AssetPoolee __instance)
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

                    // Nimbus and spawn gun
                    if (__instance.spawnableCrate._barcode == "c1534c5a-6b38-438a-a324-d7e147616467" || __instance.spawnableCrate._barcode == "c1534c5a-5747-42a2-bd08-ab3b47616467")
                    {
                        foundSpawnGun = true;
                    }

                    bool isMag = __instance.gameObject.GetComponentInChildren<Magazine>();

                    if (isMag)
                    {
                        // Ignore magazines. Until I find a way to spawn them on every client with bullets in them.
                        return;
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
                                    if (isHost)
                                    {
                                        MelonCoroutines.Start(PatchCoroutines.WaitForSpawnSync(__instance.gameObject));
                                    }
                                    else
                                    {
                                        MelonCoroutines.Start(PatchCoroutines.WaitForSpawnRequest(__instance));
                                    }
                                }
                            }
                            else
                            {
                                if (foundSpawnGun)
                                {
                                    if (isHost)
                                    {
                                        MelonCoroutines.Start(PatchCoroutines.WaitForSpawnSync(__instance.gameObject));
                                    }
                                    else
                                    {
                                        MelonCoroutines.Start(PatchCoroutines.WaitForSpawnRequest(__instance));
                                    }
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
                if (!__instance._rb)
                {
                    return;
                }
                
                GameObject collided = c.gameObject;
                SyncedObject syncedObject = SyncedObject.GetSyncedComponent(collided);
                if (syncedObject)
                {
                    if (!syncedObject.IsClientSimulated() && !syncedObject.isGrabbed)
                    {
                        syncedObject.BroadcastOwnerChange();
                    }
                }

                if (!Utils.Utils.IsPlayerPart(c.gameObject))
                {
                    return;
                }

                string playerName = collided.transform.root.name.Replace("(PlayerRep) ", "");

                PlayerRepresentation punchedRep = null;
                foreach (PlayerRepresentation playerRepresentation in PlayerRepresentation.representations.Values)
                {
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

                if (!__instance._rb)
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

                SyncedObject collidedSynced = SyncedObject.GetSyncedComponent(collided);
                if (collidedSynced)
                {
                    if (!collidedSynced.IsClientSimulated() && !collidedSynced.isGrabbed)
                    {
                        collidedSynced.BroadcastOwnerChange();
                    }
                }

                if (!Utils.Utils.IsPlayerPart(c.gameObject))
                {
                    return;
                }

                string playerName = collided.transform.root.name.Replace("(PlayerRep) ", "");

                PlayerRepresentation punchedRep = null;
                foreach (PlayerRepresentation playerRepresentation in PlayerRepresentation.representations.Values)
                {
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

        [HarmonyPatch(typeof(StabSlash.StabPoint), "SpawnStab", typeof(Transform), typeof(Collision), typeof(float),
            typeof(ImpactProperties))]
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

                SyncedObject collidedSynced = SyncedObject.GetSyncedComponent(collided);
                if (collidedSynced)
                {
                    if (!collidedSynced.IsClientSimulated() && !collidedSynced.isGrabbed)
                    {
                        collidedSynced.BroadcastOwnerChange();
                    }
                }

                if (!Utils.Utils.IsPlayerPart(c.gameObject))
                {
                    return;
                }

                string playerName = collided.transform.root.name.Replace("(PlayerRep) ", "");

                PlayerRepresentation punchedRep = null;
                foreach (PlayerRepresentation playerRepresentation in PlayerRepresentation.representations.Values)
                {
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
        
        /*[HarmonyPatch(typeof(Gun), "OnMagazineInserted")]
        private class MagEnterPatch
        {
            public static void Postfix(Gun __instance)
            {
                if (DiscordIntegration.hasLobby)
                {
                    AmmoPlug socket = PoolManager.GetComponentOnObject<AmmoPlug>(__instance.gameObject);
                    SyncedObject gunSynced = SyncedObject.GetSyncedComponent(__instance.gameObject);
                    SyncedObject magSynced = SyncedObject.GetSyncedComponent(socket.magazine.gameObject);

                    if (PoolManager.GetComponentOnObject<Gun>(gunSynced.gameObject) && PoolManager.GetComponentOnObject<Magazine>(magSynced.gameObject))
                    {
                        if (gunSynced && magSynced)
                        {
                            // Allows people to put mags in other peoples guns. (IMMERSION SECTOR REFERENCE????)
                            if (magSynced.IsClientSimulated())
                            {
                                // A check, if the gun is not simulated by us that means we're trying to put this mag into somebody elses gun.
                                // Set the mags simulation owner to whoever owns the gun.
                                // Do this before so the client is already simulating the mag when it gets requested to be inserted.
                                if (!gunSynced.IsClientSimulated())
                                {
                                    magSynced.ManualSetOwner(gunSynced.simulatorId, false);
                                }
                                gunSynced.storedMag = magSynced;
                                magSynced.TransferGroup(gunSynced.groupId);
                                DebugLogger.Msg("Mag inserted into gun.");
                                var magInsertMessageData = new MagInsertMessageData()
                                {
                                    gunId = gunSynced.currentId,
                                    magId = magSynced.currentId
                                };
                                var packetByteBuf = PacketHandler.CompressMessage(NetworkMessageType.MagInsertPacket,
                                    magInsertMessageData);
                                Node.activeNode.BroadcastMessage((byte)NetworkChannel.Object, packetByteBuf.getBytes());
                            
                                
                            }
                        }
                    }
                }
            }
        }
        
        [HarmonyPatch(typeof(Gun), "OnMagazineRemoved")]
        private class MagExitPatch
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
                            DebugLogger.Msg("Mag ejected from gun.");
                            var gunStatePacket = new GunStateData()
                            {
                                objectid = gunSynced.currentId,
                                state = 2
                            };
                            var packetByteBuf = PacketHandler.CompressMessage(NetworkMessageType.GunStatePacket,
                                gunStatePacket);
                            Node.activeNode.BroadcastMessage((byte)NetworkChannel.Object, packetByteBuf.getBytes());
                        }
                    }
                }
            }
        }*/

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
                        GameObject.Destroy(HandVariables.rHandJoint);
                        if (HandVariables.rGrabbedPlayerRep != null)
                        {
                            HandVariables.rGrabbedPlayerRep.LetGoOfThisGuy(Handedness.RIGHT);
                        }
                    }

                    if (__instance.handedness == Handedness.LEFT)
                    {
                        GameObject.Destroy(HandVariables.lHandJoint);
                        if (HandVariables.lGrabbedPlayerRep != null)
                        {
                            HandVariables.lGrabbedPlayerRep.LetGoOfThisGuy(Handedness.LEFT);
                        }
                    }

                    MelonCoroutines.Start(PatchCoroutines.WaitForObjectDetach(__instance.handedness));
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
                        PlayerRepresentation playerRepresentation = Utils.Utils.GetRepresentation(objectToAttach);
                        if (__instance.handedness == Handedness.RIGHT)
                        {
                            if (Utils.Utils.CanPickup(playerRepresentation))
                            {
                                playerRepresentation.GrabThisGuy(__instance.handedness, objectToAttach);
                            }
                            else
                            {
                                if (HandVariables.rHandJoint != null)
                                {
                                    GameObject.Destroy(HandVariables.rHandJoint);
                                }

                                HandVariables.rHandJoint = __instance.gameObject.AddComponent<FixedJoint>();
                                HandVariables.rHandJoint.connectedBody = objectToAttach.GetComponent<Rigidbody>();
                                HandVariables.rHandJoint.breakForce = Single.PositiveInfinity;
                                HandVariables.rHandJoint.breakTorque = Single.PositiveInfinity;
                            }
                        }

                        if (__instance.handedness == Handedness.LEFT)
                        {
                            if (Utils.Utils.CanPickup(playerRepresentation))
                            {
                                playerRepresentation.GrabThisGuy(__instance.handedness, objectToAttach);
                            }
                            else
                            {
                                if (HandVariables.lHandJoint != null)
                                {
                                    GameObject.Destroy(HandVariables.lHandJoint);
                                }

                                HandVariables.lHandJoint = __instance.gameObject.AddComponent<FixedJoint>();
                                HandVariables.lHandJoint.connectedBody = objectToAttach.GetComponent<Rigidbody>();
                                HandVariables.lHandJoint.breakForce = Single.PositiveInfinity;
                                HandVariables.lHandJoint.breakTorque = Single.PositiveInfinity;
                            }
                        }
                    }

                    DebugLogger.Msg("Grabbed: " + objectToAttach.name);
                    MelonCoroutines.Start(
                        PatchCoroutines.WaitForAttachSync(objectToAttach, __instance.handedness, true));
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
                MelonCoroutines.Start(PatchCoroutines.WaitForAttachSync(__instance.gameObject, hand.handedness, false));
            }
        }
    }
}