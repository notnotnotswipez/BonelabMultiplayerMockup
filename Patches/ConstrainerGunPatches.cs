using BonelabMultiplayerMockup.NetworkData;
using BonelabMultiplayerMockup.Nodes;
using BonelabMultiplayerMockup.Object;
using BonelabMultiplayerMockup.Packets;
using BonelabMultiplayerMockup.Packets.Gun;
using BonelabMultiplayerMockup.Utils;
using HarmonyLib;
using SLZ.Props;
using SLZ.Props.Weapons;
using UnityEngine;

namespace BonelabMultiplayerMockup.Patches
{
    public class ConstrainerGunPatches
    {
        [HarmonyPatch(typeof(Constrainer), "JointTether", typeof(Rigidbody), typeof(Rigidbody), typeof(Vector3), typeof(Vector3))]
        private class JointTetherPatch
        {
            public static void Postfix(Constrainer __instance, Rigidbody main, Rigidbody cb, Vector3 anchor, Vector3 connectedA)
            {
                if (SteamIntegration.hasLobby)
                {
                    SyncedObject constrainerSynced = SyncedObject.GetSyncedComponent(__instance.gameObject);

                    if (constrainerSynced)
                    {
                        if (constrainerSynced.IsClientSimulated())
                        {
                            SyncedObject mainSynced = SyncedObject.GetSyncedComponent(main.gameObject);
                            SyncedObject cbSynced = SyncedObject.GetSyncedComponent(cb.gameObject);
                            if (mainSynced != null && cbSynced != null)
                            {
                                ConstrainerJointTypes type = ConstrainerJointTypes.Tether;
                                DebugLogger.Msg("Constrainer jointed things.");
                                var constrainerGunJointData = new ConstrainerGunJointData()
                                {
                                    type = type,
                                    main = mainSynced.currentId,
                                    cb = cbSynced.currentId,
                                    anchor = new BytePosition(anchor),
                                    connectedA = new BytePosition(connectedA),
                                    constrainerId = constrainerSynced.currentId
                                };
                                var packetByteBuf = PacketHandler.CompressMessage(NetworkMessageType.ConstrainerJointPacket,
                                    constrainerGunJointData);
                                SteamPacketNode.BroadcastMessage(NetworkChannel.Object, packetByteBuf.getBytes());
                            }
                        }
                    }
                }
            }
        }
        
        [HarmonyPatch(typeof(Constrainer), "JointWeld", typeof(Rigidbody), typeof(Rigidbody), typeof(Vector3), typeof(Vector3))]
        private class JointWeldPatch
        {
            public static void Postfix(Constrainer __instance, Rigidbody main, Rigidbody cb, Vector3 anchor, Vector3 connectedA)
            {
                if (SteamIntegration.hasLobby)
                {
                    SyncedObject constrainerSynced = SyncedObject.GetSyncedComponent(__instance.gameObject);

                    if (constrainerSynced)
                    {
                        if (constrainerSynced.IsClientSimulated())
                        {
                            SyncedObject mainSynced = SyncedObject.GetSyncedComponent(main.gameObject);
                            SyncedObject cbSynced = SyncedObject.GetSyncedComponent(cb.gameObject);
                            if (mainSynced != null && cbSynced != null)
                            {
                                ConstrainerJointTypes type = ConstrainerJointTypes.Weld;
                                DebugLogger.Msg("Constrainer jointed things.");
                                var constrainerGunJointData = new ConstrainerGunJointData()
                                {
                                    type = type,
                                    main = mainSynced.currentId,
                                    cb = cbSynced.currentId,
                                    anchor = new BytePosition(anchor),
                                    connectedA = new BytePosition(connectedA),
                                    constrainerId = constrainerSynced.currentId
                                };
                                var packetByteBuf = PacketHandler.CompressMessage(NetworkMessageType.ConstrainerJointPacket,
                                    constrainerGunJointData);
                                SteamPacketNode.BroadcastMessage(NetworkChannel.Object, packetByteBuf.getBytes());
                            }
                        }
                    }
                }
            }
        }
        
        [HarmonyPatch(typeof(Constrainer), "JointBallSocket", typeof(Rigidbody), typeof(Rigidbody), typeof(Vector3), typeof(Vector3), typeof(bool))]
        private class JointBallSocketPatch
        {
            public static void Postfix(Constrainer __instance, Rigidbody main, Rigidbody cb, Vector3 anchor, Vector3 connectedA, bool swapped)
            {
                if (SteamIntegration.hasLobby)
                {
                    SyncedObject constrainerSynced = SyncedObject.GetSyncedComponent(__instance.gameObject);

                    if (constrainerSynced)
                    {
                        if (constrainerSynced.IsClientSimulated())
                        {
                            SyncedObject mainSynced = SyncedObject.GetSyncedComponent(main.gameObject);
                            SyncedObject cbSynced = SyncedObject.GetSyncedComponent(cb.gameObject);
                            if (mainSynced != null && cbSynced != null)
                            {
                                ConstrainerJointTypes type = ConstrainerJointTypes.BallSocket;
                                DebugLogger.Msg("Constrainer jointed things.");
                                var constrainerGunJointData = new ConstrainerGunJointData()
                                {
                                    type = type,
                                    main = mainSynced.currentId,
                                    cb = cbSynced.currentId,
                                    anchor = new BytePosition(anchor),
                                    connectedA = new BytePosition(connectedA),
                                    constrainerId = constrainerSynced.currentId
                                };
                                var packetByteBuf = PacketHandler.CompressMessage(NetworkMessageType.ConstrainerJointPacket,
                                    constrainerGunJointData);
                                SteamPacketNode.BroadcastMessage(NetworkChannel.Object, packetByteBuf.getBytes());
                            }
                        }
                    }
                }
            }
        }
        
        [HarmonyPatch(typeof(Constrainer), "JointElastic", typeof(Rigidbody), typeof(Rigidbody), typeof(Vector3), typeof(Vector3))]
        private class JointElasticPatch
        {
            public static void Postfix(Constrainer __instance, Rigidbody main, Rigidbody cb, Vector3 anchor, Vector3 connectedA)
            {
                if (SteamIntegration.hasLobby)
                {
                    SyncedObject constrainerSynced = SyncedObject.GetSyncedComponent(__instance.gameObject);

                    if (constrainerSynced)
                    {
                        if (constrainerSynced.IsClientSimulated())
                        {
                            SyncedObject mainSynced = SyncedObject.GetSyncedComponent(main.gameObject);
                            SyncedObject cbSynced = SyncedObject.GetSyncedComponent(cb.gameObject);
                            if (mainSynced != null && cbSynced != null)
                            {
                                ConstrainerJointTypes type = ConstrainerJointTypes.Elastic;
                                DebugLogger.Msg("Constrainer jointed things.");
                                var constrainerGunJointData = new ConstrainerGunJointData()
                                {
                                    type = type,
                                    main = mainSynced.currentId,
                                    cb = cbSynced.currentId,
                                    anchor = new BytePosition(anchor),
                                    connectedA = new BytePosition(connectedA),
                                    constrainerId = constrainerSynced.currentId
                                };
                                var packetByteBuf = PacketHandler.CompressMessage(NetworkMessageType.ConstrainerJointPacket,
                                    constrainerGunJointData);
                                SteamPacketNode.BroadcastMessage(NetworkChannel.Object, packetByteBuf.getBytes());
                            }
                        }
                    }
                }
            }
        }
        
        [HarmonyPatch(typeof(Constrainer), "JointEntangleRotation", typeof(Rigidbody), typeof(Rigidbody), typeof(Vector3), typeof(Vector3))]
        private class JointEntangleRotationPatch
        {
            public static void Postfix(Constrainer __instance, Rigidbody main, Rigidbody cb, Vector3 anchor, Vector3 connectedA)
            {
                if (SteamIntegration.hasLobby)
                {
                    SyncedObject constrainerSynced = SyncedObject.GetSyncedComponent(__instance.gameObject);

                    if (constrainerSynced)
                    {
                        if (constrainerSynced.IsClientSimulated())
                        {
                            SyncedObject mainSynced = SyncedObject.GetSyncedComponent(main.gameObject);
                            SyncedObject cbSynced = SyncedObject.GetSyncedComponent(cb.gameObject);
                            if (mainSynced != null && cbSynced != null)
                            {
                                ConstrainerJointTypes type = ConstrainerJointTypes.EntangleRotation;
                                DebugLogger.Msg("Constrainer jointed things.");
                                var constrainerGunJointData = new ConstrainerGunJointData()
                                {
                                    type = type,
                                    main = mainSynced.currentId,
                                    cb = cbSynced.currentId,
                                    anchor = new BytePosition(anchor),
                                    connectedA = new BytePosition(connectedA),
                                    constrainerId = constrainerSynced.currentId
                                };
                                var packetByteBuf = PacketHandler.CompressMessage(NetworkMessageType.ConstrainerJointPacket,
                                    constrainerGunJointData);
                                SteamPacketNode.BroadcastMessage(NetworkChannel.Object, packetByteBuf.getBytes());
                            }
                        }
                    }
                }
            }
        }
        
        [HarmonyPatch(typeof(Constrainer), "JointEntangleVelocity", typeof(Rigidbody), typeof(Rigidbody), typeof(Vector3), typeof(Vector3))]
        private class JointEntangleVelocityPatch
        {
            public static void Postfix(Constrainer __instance, Rigidbody main, Rigidbody cb, Vector3 anchor, Vector3 connectedA)
            {
                if (SteamIntegration.hasLobby)
                {
                    SyncedObject constrainerSynced = SyncedObject.GetSyncedComponent(__instance.gameObject);

                    if (constrainerSynced)
                    {
                        if (constrainerSynced.IsClientSimulated())
                        {
                            SyncedObject mainSynced = SyncedObject.GetSyncedComponent(main.gameObject);
                            SyncedObject cbSynced = SyncedObject.GetSyncedComponent(cb.gameObject);
                            if (mainSynced != null && cbSynced != null)
                            {
                                ConstrainerJointTypes type = ConstrainerJointTypes.EntangleVelocity;
                                DebugLogger.Msg("Constrainer jointed things.");
                                var constrainerGunJointData = new ConstrainerGunJointData()
                                {
                                    type = type,
                                    main = mainSynced.currentId,
                                    cb = cbSynced.currentId,
                                    anchor = new BytePosition(anchor),
                                    connectedA = new BytePosition(connectedA),
                                    constrainerId = constrainerSynced.currentId
                                };
                                var packetByteBuf = PacketHandler.CompressMessage(NetworkMessageType.ConstrainerJointPacket,
                                    constrainerGunJointData);
                                SteamPacketNode.BroadcastMessage(NetworkChannel.Object, packetByteBuf.getBytes());
                            }
                        }
                    }
                }
            }
        }
    }
}