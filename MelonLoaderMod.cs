using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using AkilliMum.SRP.Mirror;
using BonelabMultiplayerMockup.NetworkData;
using BonelabMultiplayerMockup.Nodes;
using BonelabMultiplayerMockup.Object;
using BonelabMultiplayerMockup.Packets;
using BonelabMultiplayerMockup.Packets.Gun;
using BonelabMultiplayerMockup.Packets.Object;
using BonelabMultiplayerMockup.Packets.Player;
using BonelabMultiplayerMockup.Packets.Reset;
using BonelabMultiplayerMockup.Patches;
using BonelabMultiplayerMockup.Representations;
using BonelabMultiplayerMockup.Utils;
using BoneLib;
using MelonLoader;
using SLZ.Interaction;
using SLZ.Marrow.SceneStreaming;
using SLZ.Rig;
using UnityEngine;
using Avatar = SLZ.VRMK.Avatar;

namespace BonelabMultiplayerMockup
{
    public static class BuildInfo
    {
        public const string Name = "BonelabMultiplayerMockup"; // Name of the Mod.  (MUST BE SET)
        public const string Author = "notnotnotswipez"; // Author of the Mod.  (Set as null if none)
        public const string Company = null; // Company that made the Mod.  (Set as null if none)
        public const string Version = "3.5.0"; // Version of the Mod.  (MUST BE SET)
        public const string DownloadLink = null; // Download Link for the Mod.  (Set as null if none)
    }

    public class BonelabMultiplayerMockup : MelonMod
    {
        public static PlayerRepresentation debugRep = null;
        public static Dictionary<byte, GameObject> boneDictionary = new Dictionary<byte, GameObject>();
        public static Dictionary<byte, GameObject> colliderDictionary = new Dictionary<byte, GameObject>();
        private static byte currentBoneId = 0;
        private static byte currentColliderId = 0;
        private int updateCount = 0;
        private int desiredFrames = 2;
        public static string sceneName = "";
        public static bool waitingForSceneLoad = false;


        public static void PopulateCurrentAvatarData()
        {
            boneDictionary.Clear();
            currentBoneId = 0;
            
            PopulateBoneDictionary(Player.GetRigManager().GetComponentInChildren<RigManager>().avatar.gameObject.transform);
            PopulateCurrentColliderData();
        }

        public static void PopulateCurrentColliderData()
        {
            colliderDictionary.Clear();
            currentColliderId = 0;
            foreach (var collider in Player.GetPhysicsRig().GetComponentsInChildren<MeshCollider>())
            {
                if (collider.isTrigger)
                {
                    continue;
                }

                if (collider.GetComponentInParent<SlotContainer>())
                {
                    continue;
                }

                colliderDictionary.Add(currentColliderId++, collider.gameObject);
            }

            foreach (var collider in Player.GetPhysicsRig().GetComponentsInChildren<BoxCollider>())
            {
                if (collider.isTrigger)
                {
                    continue;
                }
                
                if (collider.GetComponentInParent<SlotContainer>())
                {
                    continue;
                }

                colliderDictionary.Add(currentColliderId++, collider.gameObject);
            }

            foreach (var collider in Player.GetPhysicsRig().GetComponentsInChildren<CapsuleCollider>())
            {
                if (collider.isTrigger)
                {
                    continue;
                }
                
                if (collider.GetComponentInParent<SlotContainer>())
                {
                    continue;
                }
                
                colliderDictionary.Add(currentColliderId++, collider.gameObject);
            }
        }



        private static void PopulateBoneDictionary(Transform parent)
        {
            var childCount = parent.childCount;

            for (var i = 0; i < childCount; i++)
            {
                var child = parent.GetChild(i).gameObject;
                boneDictionary.Add(currentBoneId++, child);

                if (child.transform.childCount > 0) PopulateBoneDictionary(child.transform);
            }
        }
        
        public override void OnApplicationStart()
        {
            GameSDK.LoadGameSDK();
            PacketHandler.RegisterPackets();
            DiscordIntegration.Init();
            Client.StartClient();
            DataDirectory.Initialize();
            
        }

        private IEnumerator WaitForSceneLoad()
        {
            waitingForSceneLoad = true;
            while (SceneStreamer.Session.Status == StreamStatus.LOADING)
            {
                yield return null;
            }
            
            SceneChangeData sceneChangePacket = new SceneChangeData()
            {
                barcode = SceneStreamer.Session._level._barcode._id
            };
            var sceneBuff = PacketHandler.CompressMessage(NetworkMessageType.LevelResponsePacket, sceneChangePacket);
            Node.activeNode.BroadcastMessage((byte)NetworkChannel.Reliable, sceneBuff.getBytes());
            waitingForSceneLoad = false;
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (DiscordIntegration.hasLobby)
            {
                if (DiscordIntegration.isHost)
                {
                    if (SceneStreamer.Session.Status == StreamStatus.LOADING)
                    {
                        if (!waitingForSceneLoad)
                        {
                            MelonCoroutines.Start(WaitForSceneLoad());
                        }
                    }
                }

                if (Player.GetRigManager() != null)
                {
                    
                    GameObject rigManager = Player.GetRigManager();
                    if (rigManager.scene.name != BonelabMultiplayerMockup.sceneName)
                    {
                        DebugLogger.Msg("Cleaned data");
                        DebugLogger.Msg("Loaded scene with name: " + rigManager.scene.name);
                        SyncedObject.CleanData();
                        PopulateCurrentAvatarData();
                        BonelabMultiplayerMockup.sceneName = rigManager.scene.name;
                        MelonCoroutines.Start(PatchCoroutines.WaitForAvatarSwitch());
                
                        // Send packet to the lobby owner saying we loaded into the scene
                        var catchupBuff = PacketHandler.CompressMessage(NetworkMessageType.LevelResponsePacket, new LoadedLevelResponseData());
                        Node.activeNode.SendMessage(DiscordIntegration.lobby.OwnerId, (byte)NetworkChannel.Reliable, catchupBuff.getBytes());
                    }
                    else
                    {
                        DebugLogger.Msg("Loaded a new scene zone!");
                        MelonCoroutines.Start(PatchCoroutines.WaitForAvatarSwitch());
                        if (DiscordIntegration.isHost)
                        {
                            var syncResetData = new SyncResetData()
                            {
                                // empty data
                            };
                            PacketByteBuf packetByteBuf =
                                PacketHandler.CompressMessage(NetworkMessageType.SyncResetPacket, syncResetData);
                            Node.activeNode.BroadcastMessage((byte)NetworkChannel.Transaction ,packetByteBuf.getBytes());
                        
                            SyncedObject.CleanData(true);
                        }
                    }
                }
            }
        }

        public override void OnUpdate()
        {
            if (Input.GetKey(KeyCode.S))
            {
                Server.StartServer();
            }
            
            if (Input.GetKey(KeyCode.M))
            {
                if (DiscordIntegration.hasLobby)
                {
                    if (DiscordIntegration.isHost)
                    {
                        Server.instance.Shutdown();
                    }
                    else
                    {
                        Client.instance.Shutdown();
                    }
                }
            }

            if (Input.GetKey(KeyCode.O))
            {
                if (DiscordIntegration.hasLobby)
                {
                    if (DiscordIntegration.isHost)
                    {
                        var syncResetData = new SyncResetData()
                        {
                            // empty data
                        };
                        PacketByteBuf packetByteBuf =
                            PacketHandler.CompressMessage(NetworkMessageType.SyncResetPacket, syncResetData);
                        Node.activeNode.BroadcastMessage((byte)NetworkChannel.Transaction ,packetByteBuf.getBytes());
                        
                        SyncedObject.CleanData(true);
                    }
                }
            }

            foreach (var player in PlayerRepresentation.representations.Values)
            {
                player.Update();
            }

            updateCount++;
            if (updateCount >= desiredFrames)
            {
                if (DiscordIntegration.hasLobby)
                {
                    SendBones();
                    SendColliders();
                    SyncedObject.UpdateSyncedNPCs();
                }

                updateCount = 0;
            }
        }

        public override void OnFixedUpdate()
        {
            
            if (DiscordIntegration.hasLobby)
            {
                if (SyncedObject.syncedObjectIds.Count > 0)
                {
                    foreach (SyncedObject syncedObject in SyncedObject.syncedObjectIds.Values) {
                        try
                        {
                            syncedObject.UpdatePos();
                        }
                        catch (Exception e)
                        {
                            // Ignore it, if something goes wrong we dont want everything in the list to break
                        }
                    }
                    
                    if (SyncedObject.queuedObjectsToDelete.Count > 0)
                    {
                        for (int i = 0; i < SyncedObject.queuedObjectsToDelete.Count; i++)
                        {
                            List<SyncedObject> syncedObjects = SyncedObject.GetAllSyncables(
                                SyncedObject.relatedSyncedObjects[SyncedObject.queuedObjectsToDelete[i]][0].gameObject);
                            ushort lastId = 0;
                            foreach (var synced in syncedObjects)
                            {
                                lastId = synced.groupId;
                                synced.DestroySyncable(true);
                            }

                            ushort groupId = SyncedObject.queuedObjectsToDelete[i];
                            GroupDestroyData groupDestroyData = new GroupDestroyData()
                            {
                                groupId = groupId,
                                backupObjectId = lastId
                            };

                            PacketByteBuf message = PacketHandler.CompressMessage(NetworkMessageType.GroupDestroyPacket, groupDestroyData);
                            Node.activeNode.BroadcastMessage((byte)NetworkChannel.Object, message.getBytes());
                        }

                        SyncedObject.queuedObjectsToDelete.Clear();
                    }
                }
            }
        }

        private static void SendBones()
        {
            for (byte i = 0; i < boneDictionary.Count; i++)
            {
                GameObject bone = boneDictionary[i];
                if (bone == null)
                {
                    // Assume its all wrong
                    break;
                }

                var simplifiedTransform = new CompressedTransform(bone.transform.position,
                    Quaternion.Euler(bone.transform.eulerAngles));

                PlayerBoneData playerBoneData = new PlayerBoneData()
                {
                    userId = DiscordIntegration.currentUser.Id,
                    boneId = i,
                    transform = simplifiedTransform
                };

                PacketByteBuf message = PacketHandler.CompressMessage(NetworkMessageType.PlayerUpdatePacket, playerBoneData);
                Node.activeNode.BroadcastMessage((byte)NetworkChannel.Unreliable, message.getBytes());
            }
        }
        
        private static void SendColliders()
        {
            for (byte i = 0; i < colliderDictionary.Count; i++)
            {
                GameObject collider = colliderDictionary[i];
                if (collider == null)
                {
                    // Assume its all wrong
                    break;
                }

                var simplifiedTransform = new CompressedTransform(collider.transform.position,
                    Quaternion.Euler(collider.transform.eulerAngles));

                PlayerColliderData playerColliderData = new PlayerColliderData()
                {
                    userId = DiscordIntegration.currentUser.Id,
                    colliderIndex = i,
                    CompressedTransform = simplifiedTransform
                };

                PacketByteBuf message = PacketHandler.CompressMessage(NetworkMessageType.PlayerColliderPacket, playerColliderData);
                Node.activeNode.BroadcastMessage((byte)NetworkChannel.Unreliable, message.getBytes());
            }
        }

        public override void OnLateUpdate()
        {
            DiscordIntegration.Tick();
        }
    }
}