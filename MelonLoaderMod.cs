using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using BonelabMultiplayerMockup.Extention;
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
using BoneLib.BoneMenu;
using BoneLib.BoneMenu.Elements;
using BoneLib.RandomShit;
using MelonLoader;
using SLZ;
using SLZ.Interaction;
using SLZ.Marrow.SceneStreaming;
using SLZ.Rig;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using Avatar = SLZ.VRMK.Avatar;
using Color = UnityEngine.Color;

namespace BonelabMultiplayerMockup
{
    public static class BuildInfo
    {
        public const string Name = "BonelabMultiplayerMockup"; // Name of the Mod.  (MUST BE SET)
        public const string Author = "notnotnotswipez"; // Author of the Mod.  (Set as null if none)
        public const string Company = null; // Company that made the Mod.  (Set as null if none)
        public const string Version = "7.0.0"; // Version of the Mod.  (MUST BE SET)
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
        private static float currentStall = 1;
        public static string sceneName = "";
        public static bool waitingForSceneLoad = false;
        public long lastMsUpdate = 0;

        public static bool shouldModifyControllerPos = false;

        public static GameObject pelvis;
        public static byte pelvisId;
        
        public static MelonPreferences_Category BLMPCategory;
        public static MelonPreferences_Entry<bool> playerMotionSmoothing;

        public override void OnInitializeMelon()
        {
            BLMPCategory = MelonPreferences.CreateCategory("BLMP");
            playerMotionSmoothing = BLMPCategory.CreateEntry<bool>("playerMotionSmoothing", false);
            CreateMenu();
        }
        
        public void CreateMenu()
        {
            MenuCategory mainCategory = MenuManager.CreateCategory("MP Mockup", Color.yellow);
            SubPanelElement serverManagement = mainCategory.CreateSubPanel("Server Management", Color.green);
            serverManagement.CreateFunctionElement("Start Server", Color.green, () => StartServer());
            serverManagement.CreateFunctionElement("Disconnect/Stop", Color.red, () => SteamIntegration.Disconnect(false));
        }

        public static void PopulateCurrentAvatarData()
        {
            boneDictionary.Clear();
            currentBoneId = 0;
            
            PopulateBoneDictionary(Player.rigManager.avatar.gameObject.transform);
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

                if (collider.gameObject.name.Equals("Pelvis"))
                {
                    pelvis = collider.gameObject;
                    pelvisId = currentColliderId;
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
                
                if (collider.gameObject.name.Equals("Pelvis"))
                {
                    pelvis = collider.gameObject;
                    pelvisId = currentColliderId;
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
                
                if (collider.gameObject.name.Equals("Pelvis"))
                {
                    pelvis = collider.gameObject;
                    pelvisId = currentColliderId;
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
                if (currentBoneId == 254)
                {
                    if (!boneDictionary.ContainsKey(254))
                    {
                        boneDictionary.Add(currentBoneId, child);
                    }
                    return;
                }
                boneDictionary.Add(currentBoneId++, child);

                if (child.transform.childCount > 0) PopulateBoneDictionary(child.transform);
            }
        }
        
        public override void OnApplicationStart()
        {
            GameSDK.LoadGameSDK();
            SteamIntegration.Init();
            DataDirectory.Initialize();
            PacketHandler.RegisterPackets();
            
            DiscordRichPresence.Init();

            Thread thread = new Thread(ProcessPackets);
            thread.Start();
            
        }

        void ProcessPackets()
        {
            while (true)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                try
                {
                    while (SteamPacketNode.queuedBufs.Count > 0)
                    {
                        // Failsafe, if the game stalls, the accumulated packets goes to the thousands. This is wrong, the cache must be cleared.
                        if (DateTime.Now.Millisecond - lastMsUpdate > 40)
                        {
                            SteamPacketNode.cachedUnreliable = new ConcurrentQueue<SteamPacketNode.QueuedReceived>();
                            SteamPacketNode.queuedBufs = new ConcurrentQueue<SteamPacketNode.QueuedPacket>();
                            break;
                        }
                        
                        SteamPacketNode.QueuedPacket packets;
                        while (!SteamPacketNode.queuedBufs.TryDequeue(out packets)) continue;

                        P2PSend sendType = SteamIntegration.networkChannels[packets.channel];
                        SteamNetworking.SendP2PPacket(packets._steamId, packets._packetByteBuf.getBytes(),
                            packets._packetByteBuf.byteIndex, (byte)packets.channel, sendType);
                    }
                }
                catch (Exception e)
                {
                    MelonLogger.Error(e.ToString());
                }

                stopwatch.Stop();
                SteamPacketNode.flushMsTime = stopwatch.ElapsedMilliseconds;

                foreach (NetworkChannel channel in SteamIntegration.reliableChannels.Keys)
                {
                    while (SteamNetworking.IsP2PPacketAvailable((int)channel))
                    {
                        P2Packet? packet = SteamNetworking.ReadP2PPacket((int)channel);
                        if (packet.HasValue)
                        {
                            byte[] data = packet.Value.Data;
                            if (data.Length <= 0) // Idk
                                throw new Exception("Data was invalid!");
                            byte messageType = data[0];
                            byte[] realData = new byte[data.Length - sizeof(byte)];
                            for (int b = sizeof(byte); b < data.Length; b++)
                                realData[b - sizeof(byte)] = data[b];
                            PacketByteBuf packetByteBuf = new PacketByteBuf(realData);
                            NetworkMessageType networkMessageType = (NetworkMessageType)messageType;
                            SteamPacketNode.QueuedReceived queuedReceived = new SteamPacketNode.QueuedReceived()
                            {
                                networkMessageType = networkMessageType,
                                packetByteBuf = packetByteBuf
                            };
                            SteamPacketNode.receivedPackets.Enqueue(queuedReceived);
                        }
                    }
                }

                while (SteamNetworking.IsP2PPacketAvailable((int)NetworkChannel.Unreliable))
                {
                    P2Packet? packet = SteamNetworking.ReadP2PPacket((int)NetworkChannel.Unreliable);
                    if (packet.HasValue)
                    {
                        byte[] data = packet.Value.Data;
                        if (data.Length <= 0) // Idk
                            throw new Exception("Data was invalid!");
                        byte messageType = data[0];
                        byte[] realData = new byte[data.Length - sizeof(byte)];
                        for (int b = sizeof(byte); b < data.Length; b++)
                            realData[b - sizeof(byte)] = data[b];
                        PacketByteBuf packetByteBuf = new PacketByteBuf(realData);
                        NetworkMessageType networkMessageType = (NetworkMessageType)messageType;
                        SteamPacketNode.QueuedReceived queuedReceived = new SteamPacketNode.QueuedReceived()
                        {
                            networkMessageType = networkMessageType,
                            packetByteBuf = packetByteBuf
                        };
                        SteamPacketNode.cachedUnreliable.Enqueue(queuedReceived);
                    }
                }
            }
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
            SteamPacketNode.BroadcastMessage(NetworkChannel.Reliable, sceneBuff.getBytes());
            waitingForSceneLoad = false;
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (SteamIntegration.hasLobby)
            {
                if (SteamIntegration.isHost)
                {
                    if (SceneStreamer.Session.Status == StreamStatus.LOADING)
                    {
                        if (!waitingForSceneLoad)
                        {
                            MelonCoroutines.Start(WaitForSceneLoad());
                        }
                    }
                }

                if (Player.rigManager != null)
                {
                    GameObject rigManager = Player.rigManager.gameObject;
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
                        SteamPacketNode.SendMessage(SteamIntegration.Instance.currentLobby.Owner.Id, (byte)NetworkChannel.Reliable, catchupBuff.getBytes());
                    }
                }
            }
        }

        public async void StartServer()
        {
            await SteamIntegration.Instance.CreateLobby();
        }

        public override void OnUpdate()
        {
            /*if (Input.GetKey(KeyCode.J))
            {
                foreach (var rig in Patches.Patches.rigManagerObject.GetComponentsInChildren<OpenController>())
                {
                    rig.handedness = Handedness.UNDEFINED;
                }
            }
            
            if (Input.GetKeyDown(KeyCode.K))
            {
                shouldModifyControllerPos = !shouldModifyControllerPos;
                RigManager rigManager = Player.GetRigManager().GetComponentInChildren<RigManager>();
                RigManager clonesRigManager = Patches.Patches.rigManagerObject.GetComponentInChildren<RigManager>();
                string original = rigManager._avatarCrate._barcode._id;
                AssetsManager.LoadAvatar(original, o =>
                {
                    GameObject spawned = GameObject.Instantiate(o);
                    Avatar avatar = spawned.GetComponent<Avatar>();
                    spawned.transform.parent = rigManager.gameObject.transform;
                    clonesRigManager.SwitchAvatar(avatar);
                });
            }

            if (shouldModifyControllerPos)
            {
                if (Player.GetRigManager() != null)
                {
                    if (Patches.Patches.rigManagerObject != null)
                    {

                        Vector3 desiredPelvisPos =
                            Player.GetRigManager().transform.Find("[PhysicsRig]").Find("Pelvis").position +
                            new Vector3(5, 0, 0);
                        
                        GameObject rigManagerPelvis = Patches.Patches.rigManagerObject.transform.Find("[PhysicsRig]").Find("Pelvis").gameObject;

                        Patches.Patches.rigManagerObject.transform.Find("[OVERRIDE RIG]").transform.position = Patches
                            .Patches.rigManagerObject.transform.Find("[OpenControllerRig]").transform.position;
                        
                        Patches.Patches.rigManagerObject.transform.Find("[OVERRIDE RIG]").Find("TrackingZone").transform.position = Patches
                            .Patches.rigManagerObject.transform.Find("[OpenControllerRig]").Find("TrackingSpace").transform.position;

                        rigManagerPelvis.GetComponent<Rigidbody>().velocity =
                            (desiredPelvisPos - rigManagerPelvis.transform.position).normalized * 2;
                        
                        Patches.Patches.rigManagerObject.transform.Find("[PhysicsRig]").Find("Pelvis").transform
                            .rotation = Player.GetRigManager().transform.Find("[PhysicsRig]").Find("Pelvis").rotation;
                        
                        Patches.Patches.headObject.transform.position = Player.GetPlayerHead().transform.position + new Vector3(5, 0, 0);

                        Patches.Patches.rControllerObject.transform.position =
                            Player.rightController.transform.position;
                        Patches.Patches.lControllerObject.transform.position = 
                            Player.leftController.transform.position;

                        Patches.Patches.rControllerObject.transform.rotation = Player.rightController.transform.rotation;
                        Patches.Patches.lControllerObject.transform.rotation = Player.leftController.transform.rotation;
                        Patches.Patches.headObject.transform.rotation = Player.GetPlayerHead().transform.rotation;
                    }
                }
            }*/
            
            DiscordRichPresence.Update();
            lastMsUpdate = DateTime.Now.Millisecond;
            SteamPacketNode.Callbacks();
            if (SteamIntegration.Instance != null)
            {
                SteamIntegration.Instance.Update();
            }

            if (SceneStreamer._session.Status == StreamStatus.LOADING)
            {
                SteamPacketNode.queuedBufs = new ConcurrentQueue<SteamPacketNode.QueuedPacket>();
                SteamPacketNode.cachedUnreliable = new ConcurrentQueue<SteamPacketNode.QueuedReceived>();
                SteamPacketNode.receivedPackets = new ConcurrentQueue<SteamPacketNode.QueuedReceived>();
            }

            if (Input.GetKey(KeyCode.S))
            {
                StartServer();
            }

            if (Input.GetKey(KeyCode.D))
            {
                MelonLogger.Msg("----------- SWIPEZ's AWESOME DEBUGGER MESSAGE -----------");
                MelonLogger.Msg("Packets to receive size: "+SteamPacketNode.receivedPackets.Count);
                MelonLogger.Msg("Packets to packets to send size buffer: "+SteamPacketNode.queuedBufs.Count);
                MelonLogger.Msg("Connected users size: "+SteamIntegration.connectedIds.Count);
                MelonLogger.Msg("User ids size: "+SteamIntegration.connectedIds.Count);
                MelonLogger.Msg("LAST PACKET SENDING TIME TOOK: "+SteamPacketNode.flushMsTime+"ms");
                MelonLogger.Msg("LAST PACKET RECEIVING TIME TOOK: "+SteamPacketNode.callbackMsTime+"ms");
                MelonLogger.Msg("----------------------------------------------------------");
            }

            if (Input.GetKey(KeyCode.M))
            {
                if (SteamIntegration.hasLobby)
                {
                    SteamIntegration.Disconnect(false);
                }
            }

            if (Input.GetKey(KeyCode.O))
            {
                if (SteamIntegration.hasLobby)
                {
                    if (SteamIntegration.isHost)
                    {
                        var syncResetData = new SyncResetData()
                        {
                            // empty data
                        };
                        PacketByteBuf packetByteBuf =
                            PacketHandler.CompressMessage(NetworkMessageType.SyncResetPacket, syncResetData);
                        SteamPacketNode.BroadcastMessage(NetworkChannel.Transaction, packetByteBuf.getBytes());
                        
                        SyncedObject.CleanData(true);
                    }
                }
            }

            foreach (PlayerRepresentation playerRepresentation in PlayerRepresentation.representations.Values) {
                playerRepresentation.Update();
            }

            updateCount++;
            if (updateCount >= desiredFrames)
            {
                if (SteamIntegration.hasLobby)
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
            
            if (SteamIntegration.hasLobby)
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
                                lastId = synced.currentId;
                                synced.DestroySyncable(true);
                            }

                            ushort groupId = SyncedObject.queuedObjectsToDelete[i];
                            GroupDestroyData groupDestroyData = new GroupDestroyData()
                            {
                                groupId = groupId,
                                backupObjectId = lastId
                            };

                            PacketByteBuf message = PacketHandler.CompressMessage(NetworkMessageType.GroupDestroyPacket, groupDestroyData);
                            SteamPacketNode.BroadcastMessage(NetworkChannel.Object, message.getBytes());
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

                Quaternion rotation = bone.transform.rotation.Diff(pelvis.transform.rotation);
                Vector3 position = bone.transform.position - pelvis.transform.position;

                var simplifiedTransform = new CompressedTransform(position,
                    rotation);

                PlayerBoneData playerBoneData = new PlayerBoneData()
                {
                    userId = SteamIntegration.currentId,
                    boneId = i,
                    transform = simplifiedTransform
                };

                PacketByteBuf message = PacketHandler.CompressMessage(NetworkMessageType.PlayerUpdatePacket, playerBoneData);
                SteamPacketNode.BroadcastMessage(NetworkChannel.Unreliable, message.getBytes());
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
                
                Quaternion rotation = collider.transform.rotation.Diff(pelvis.transform.rotation);
                Vector3 position = collider.transform.position - pelvis.transform.position;

                if (i == pelvisId)
                {
                    rotation = pelvis.transform.rotation;
                    position = pelvis.transform.position;
                }

                var simplifiedTransform = new CompressedTransform(position,
                    rotation);

                PlayerColliderData playerColliderData = new PlayerColliderData()
                {
                    userId = SteamIntegration.currentId,
                    colliderIndex = i,
                    CompressedTransform = simplifiedTransform
                };

                PacketByteBuf message = PacketHandler.CompressMessage(NetworkMessageType.PlayerColliderPacket, playerColliderData);
                SteamPacketNode.BroadcastMessage(NetworkChannel.Unreliable, message.getBytes());
            }
        }
    }
}