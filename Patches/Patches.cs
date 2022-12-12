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
using Il2CppSystem;
using MelonLoader;
using SLZ;
using SLZ.AI;
using SLZ.Bonelab;
using SLZ.Combat;
using SLZ.Interaction;
using SLZ.Marrow.Data;
using SLZ.Marrow.Pool;
using SLZ.Marrow.Warehouse;
using SLZ.Props;
using SLZ.Props.Weapons;
using SLZ.Rig;
using SLZ.SFX;
using SLZ.UI;
using SLZ.Zones;
using UnityEngine;
using UnityEngine.Events;
using Avatar = SLZ.VRMK.Avatar;
using Exception = System.Exception;
using Single = System.Single;

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

            MelonLogger.Msg("Swapped avatar and sending to everyone.");
            try
            {
                var swapAvatarMessageData = new AvatarChangeData()
                {
                    userId = SteamIntegration.currentId,
                    barcode = Player.rigManager._avatarCrate._barcode._id
                };
                MelonLogger.Msg("Created ");
                var packetByteBuf = PacketHandler.CompressMessage(NetworkMessageType.AvatarChangePacket,
                    swapAvatarMessageData);
                SteamPacketNode.BroadcastMessage(NetworkChannel.Transaction, packetByteBuf.getBytes());
                BonelabMultiplayerMockup.PopulateCurrentAvatarData();
                if (BonelabMultiplayerMockup.debugRep != null)
                {
                    BonelabMultiplayerMockup.debugRep.SetAvatar(Player.rigManager
                        ._avatarCrate._barcode._id);
                }
            }
            catch (Exception e)
            {
                MelonLogger.Msg(e.Message);
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
                syncedObject.ManualSetOwner(SteamIntegration.currentId, false);
            }
        }

        public static IEnumerator WaitForSpawnRequest(AssetPoolee poolee)
        {
            var spawnRequestData = new SpawnRequestData()
            {
                userId = SteamIntegration.currentId,
                position = new BytePosition(poolee.gameObject.transform.position),
                barcode = poolee.spawnableCrate._barcode
            };

            PacketByteBuf packetByteBuf =
                PacketHandler.CompressMessage(NetworkMessageType.SpawnRequestPacket, spawnRequestData);
            
            SteamPacketNode.SendMessage(SteamIntegration.Instance.currentLobby.Owner.Id, NetworkChannel.Transaction, packetByteBuf.getBytes());
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

        public static GameObject rigManagerObject;
        public static GameObject lControllerObject;
        public static GameObject rControllerObject;
        public static GameObject headObject;
        
        [HarmonyPatch(typeof(RigManager), "Awake")]
        private class RigmanagerCachePatch
        {
            public static void Prefix(RigManager __instance)
            {
                /*if (!__instance.gameObject.name.ToLower().Contains("clone"))
                {
                    rigManagerObject = GameObject.Instantiate(__instance.gameObject);
                    foreach (var cam in rigManagerObject.GetComponentsInChildren<Camera>())
                    {
                        cam.enabled = false;
                    }
                    foreach (var controller in rigManagerObject.GetComponentsInChildren<OpenController>())
                    {
                        controller.handedness = Handedness.UNDEFINED;
                    }
                    FixRigManager(rigManagerObject.GetComponent<RigManager>());
                }*/
            }
        }

        private static void FixRigManager(RigManager rigManager)
        {
            rigManager.gameObject.transform.rotation = Quaternion.identity;
            HeadSFX headSfx = rigManager.GetComponentInChildren<HeadSFX>();
            GameObject.Destroy(rigManager.GetComponentInChildren<PageView>().gameObject);

            GameObject originalCamera = rigManager.gameObject.transform.Find("[OpenControllerRig]")
                .Find("TrackingSpace").Find("Head").gameObject;

            GameObject copiedHead = GameObject.Instantiate(originalCamera);
            
            GameObject overrideRig = new GameObject("[OVERRIDE RIG]");

            overrideRig.transform.parent = rigManager.gameObject.transform;
            
            overrideRig.transform.localPosition = Vector3.zero;
            
            GameObject fakeTrackingZone = new GameObject("TrackingSpace");
            fakeTrackingZone.transform.parent = overrideRig.transform;
            
            fakeTrackingZone.transform.localPosition = Vector3.zero;
            fakeTrackingZone.transform.rotation = Quaternion.identity;
            
            GameObject lController = new GameObject("LController");
            GameObject rController = new GameObject("RController");
            GameObject lFilter = new GameObject("LFilter");
            GameObject rFilter = new GameObject("RFilter");
            GameObject head = copiedHead;
            
            lController.transform.parent = fakeTrackingZone.transform;
            rController.transform.parent = fakeTrackingZone.transform;
            lFilter.transform.parent = fakeTrackingZone.transform;
            rFilter.transform.parent = fakeTrackingZone.transform;
            head.transform.parent = fakeTrackingZone.transform;
            
            lFilter.transform.localPosition = Vector3.zero;
            rFilter.transform.localPosition = Vector3.zero;
            rController.transform.localPosition = Vector3.zero;
            lController.transform.localPosition = Vector3.zero;
            head.transform.localPosition = Vector3.zero;
            
            lFilter.transform.rotation = Quaternion.identity;
            rFilter.transform.rotation = Quaternion.identity;
            rController.transform.rotation = Quaternion.identity;
            lController.transform.rotation = Quaternion.identity;
            head.transform.rotation = Quaternion.identity;
            
            BaseController lBaseController = lController.AddComponent<BaseController>();
            BaseController rBaseController = rController.AddComponent<BaseController>();
            ControllerRig controllerRig = overrideRig.AddComponent<ControllerRig>();
            
            rigManager.rigs.AddItem(controllerRig);

            controllerRig.hmdTransform = head.transform;
            controllerRig.directionMasterTransform = head.transform;
            
            controllerRig.vrRoot = fakeTrackingZone.transform;

            controllerRig.leftController = lBaseController;
            controllerRig.leftFilter = lFilter.transform;
            
            controllerRig.rightController = rBaseController;
            controllerRig.rightFilter = rFilter.transform;

            controllerRig._headSfx = headSfx;

            rBaseController.manager = controllerRig;
            lBaseController.manager = controllerRig;

            controllerRig.m_head = head.transform;
            controllerRig.m_handLf = lController.transform;
            controllerRig.m_handRt = rController.transform;
            controllerRig.manager = rigManager;

            rigManager.tempOverrideControl = controllerRig;

            lControllerObject = lController;
            rControllerObject = rController;
            headObject = head;
        }

        [HarmonyPatch(typeof(AIBrain), "OnDeath")]
        private class AiDeathPatch
        {
            public static void Postfix(AIBrain __instance)
            {
                if (SteamIntegration.hasLobby)
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
                        SteamPacketNode.BroadcastMessage(NetworkChannel.Object, packetByteBuf.getBytes());
                    }
                }
            }
        }

        [HarmonyPatch(typeof(AIBrain), "OnResurrection")]
        private class AiResurrectionPatch
        {
            public static void Prefix(AIBrain __instance)
            {
                if (SteamIntegration.hasLobby)
                {

                    if (SteamIntegration.isHost)
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
                if (SteamIntegration.hasLobby)
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
                if (SteamIntegration.hasLobby)
                {

                    bool isNPC = PoolManager.GetComponentOnObject<AIBrain>(__instance.gameObject) != null;
                    bool foundSpawnGun = false;
                    bool isHost = SteamIntegration.isHost;
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

                    if (!foundSpawnGun && !SteamIntegration.isHost && isNPC)
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
                SteamPacketNode.SendMessage(punchedRep.user.Id, NetworkChannel.Attack, packetByteBuf.getBytes());
            }
        }

        [HarmonyPatch(typeof(ImpactSFX), "BluntAttack", typeof(float), typeof(Collision))]
        public static class ImpactDamagePatch
        {
            public static void Postfix(ImpactSFX __instance, float impulse, Collision c)
            {
                if (!SteamIntegration.hasLobby)
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
                SteamPacketNode.SendMessage(punchedRep.user.Id, NetworkChannel.Attack, packetByteBuf.getBytes());
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
                if (!SteamIntegration.hasLobby)
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
                SteamPacketNode.SendMessage(punchedRep.user.Id, NetworkChannel.Attack, packetByteBuf.getBytes());
            }
        }


        [HarmonyPatch(typeof(BalloonGun), "Fire")]
        private class BalloonGunFirePatch
        {
            public static void Postfix(BalloonGun __instance)
            {
                if (SteamIntegration.hasLobby)
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
                            SteamPacketNode.BroadcastMessage(NetworkChannel.Object, packetByteBuf.getBytes());
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
                if (SteamIntegration.hasLobby)
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
                            SteamPacketNode.BroadcastMessage(NetworkChannel.Object, packetByteBuf.getBytes());
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
                if (SteamIntegration.hasLobby)
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
                if (SteamIntegration.hasLobby)
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
                if (SteamIntegration.hasLobby)
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
                if (SteamIntegration.hasLobby)
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