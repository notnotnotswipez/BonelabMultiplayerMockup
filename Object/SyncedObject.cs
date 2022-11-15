using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BonelabMultiplayerMockup.NetworkData;
using BonelabMultiplayerMockup.Nodes;
using BonelabMultiplayerMockup.Packets;
using BonelabMultiplayerMockup.Packets.Object;
using BonelabMultiplayerMockup.Utils;
using BoneLib;
using Il2CppSystem.Numerics;
using MelonLoader;
using SLZ.AI;
using SLZ.Marrow.Pool;
using SLZ.Props.Weapons;
using UnityEngine;

namespace BonelabMultiplayerMockup.Object
{
    [RegisterTypeInIl2Cpp]
    public class SyncedObject : MonoBehaviour
    {
        public static List<GameObject> tempActiveObjects = new List<GameObject>();

        public static Dictionary<ushort, SyncedObject> syncedObjectIds = new Dictionary<ushort, SyncedObject>();
        public static Dictionary<ushort, GameObject> cachedSpawnedObjects = new Dictionary<ushort, GameObject>();
        public static List<GameObject> syncedObjects = new List<GameObject>();

        public static Dictionary<ushort, List<SyncedObject>> relatedSyncedObjects =
            new Dictionary<ushort, List<SyncedObject>>();

        public static List<ushort> queuedObjectsToDelete = new List<ushort>();
        public static List<ushort> totalRemovedGroups = new List<ushort>();

        public static ushort lastId;
        public static ushort lastGroupId;
        public Rigidbody _rigidbody;
        public bool spawnedObject = false;

        public Vector3 lastPosition;
        public Quaternion lastRotation;
        public long simulatorId;
        public bool isGrabbed = false;
        public ushort currentId;
        public ushort groupId;
        public ushort originalGroupId;
        public long firstEverOwner = 0;
        public SyncedObject storedMag;

        public bool isNpc = false;
        public static Dictionary<ushort, Rigidbody> npcWithRoots = new Dictionary<ushort, Rigidbody>();
        public Dictionary<SimpleGripEvents, byte> gripEvents = new Dictionary<SimpleGripEvents, byte>();
        public static List<ushort> npcGroupIdsToSync = new List<ushort>();

        public InterpolatedObject InterpolatedObject;
        private bool shouldTeleport = true;

        private bool hasUpdatedBefore = false;

        // Sync nearest <maxNpcsToSync> NPCs and thats all. The specific NPCs which are synced are the ones closest. Good for performance.
        private static int maxNpcsToSync = 3;

        public SyncedObject(IntPtr intPtr) : base(intPtr)
        {
        }

        public void Start()
        {
            shouldTeleport = true;
            AIBrain aiBrain = PoolManager.GetComponentOnObject<AIBrain>(gameObject);
            isNpc = aiBrain != null;
            if (isNpc)
            {

                if (!npcWithRoots.ContainsKey(groupId))
                {
                    Rigidbody positional = aiBrain.gameObject.GetComponentInChildren<Rigidbody>();
                    npcWithRoots.Add(groupId, positional);
                    DebugLogger.Msg("Added NPC with root to list.");
                    if (!npcGroupIdsToSync.Contains(groupId))
                    {
                        npcGroupIdsToSync.Add(groupId);
                    }
                }
            }

            _rigidbody = gameObject.GetComponent<Rigidbody>();

            if (_rigidbody)
            {
                InterpolatedObject = new InterpolatedObject(gameObject);
            }

            if (IsClientSimulated())
            {
                var compressedTransform =
                    new CompressedTransform(gameObject.transform.position,
                        Quaternion.Euler(gameObject.transform.eulerAngles));

                var transformUpdateData = new TransformUpdateData
                {
                    objectId = currentId,
                    userId = DiscordIntegration.currentUser.Id,
                    compressedTransform = compressedTransform
                };

                var packetByteBuf =
                    PacketHandler.CompressMessage(NetworkMessageType.TransformUpdatePacket, transformUpdateData);
                Node.activeNode.BroadcastMessage((byte)NetworkChannel.Unreliable, packetByteBuf.getBytes());
            }
            
            PopulateGripEvents();
        }

        public void OnDisable()
        {
            syncedObjects.Remove(gameObject);
            syncedObjectIds.Remove(currentId);
            if (npcWithRoots.ContainsKey(groupId))
            {
                npcWithRoots.Remove(groupId);
            }
            
            if (IsClientSimulated())
            {
                if (!totalRemovedGroups.Contains(groupId))
                {
                    totalRemovedGroups.Add(groupId);
                    GroupDestroyData groupDestroyData = new GroupDestroyData()
                    {
                        groupId = groupId,
                        backupObjectId = lastId
                    };

                    PacketByteBuf message =
                        PacketHandler.CompressMessage(NetworkMessageType.GroupDestroyPacket, groupDestroyData);
                    Node.activeNode.BroadcastMessage((byte)NetworkChannel.Object, message.getBytes());
                }
            }
            Destroy(this);
            if (relatedSyncedObjects.ContainsKey(groupId))
            {
                var objects = relatedSyncedObjects[groupId];
                objects.Remove(this);
                relatedSyncedObjects[groupId] = objects;
            }
        }

        public SyncedObject Transfer(GameObject gameObject)
        {
            SyncedObject syncedObject = gameObject.AddComponent<SyncedObject>();
            syncedObject.currentId = currentId;
            syncedObject.groupId = groupId;
            syncedObject.originalGroupId = originalGroupId;
            syncedObjects.Add(gameObject);
            syncedObjects.Remove(this.gameObject);
            if (relatedSyncedObjects.ContainsKey(groupId))
            {
                var otherSynced = relatedSyncedObjects[groupId];
                if (!otherSynced.Contains(syncedObject)) otherSynced.Add(syncedObject);
                otherSynced.Remove(this);
                relatedSyncedObjects[groupId] = otherSynced;
            }
            else
            {
                var otherSynced = new List<SyncedObject>();
                otherSynced.Add(syncedObject);
                relatedSyncedObjects.Add(groupId, otherSynced);
            }

            syncedObjectIds[currentId] = syncedObject;
            Destroy(this);
            Destroy(this.gameObject);

            return syncedObject;
        }

        public void SetGrabbed(bool grabbed)
        {
            isGrabbed = grabbed;
        }

        public void BroadcastGrabState(bool grabbed)
        {
            if (grabbed)
            {
                // Means this client has grabbed it.
                isGrabbed = false;
            }

            var grabStateData = new GrabStateData()
            {
                objectId = currentId,
                state = (byte)(grabbed ? 1 : 0)
            };

            var packetByteBuf =
                PacketHandler.CompressMessage(NetworkMessageType.GrabStatePacket, grabStateData);
            Node.activeNode.BroadcastMessage((byte)NetworkChannel.Transaction, packetByteBuf.getBytes());
        }

        private void PopulateGripEvents()
        {
            SimpleGripEvents highestGripEvent = PoolManager.GetComponentOnObject<SimpleGripEvents>(gameObject);
            if (highestGripEvent != null)
            {
                byte index = 0;
                foreach (SimpleGripEvents gripEvent in gameObject.GetComponentsInParent<SimpleGripEvents>())
                {
                    highestGripEvent = gripEvent;
                }
                
                
                foreach (var totalGrip in highestGripEvent.GetComponentsInChildren<SimpleGripEvents>())
                {
                    MelonLogger.Msg("Indexed grip event at: "+index);
                    totalGrip.OnIndexDown.AddListener(new Action(() => OnIndexDown(totalGrip)));
                    totalGrip.OnMenuTapDown.AddListener(new Action(() => OnMenuTapDown(totalGrip)));
                    gripEvents.Add(totalGrip, index);
                    DebugLogger.Msg("Indexed grip event: "+index);
                    index++;
                }
            }
        }

        private void OnIndexDown(SimpleGripEvents gripEvent)
        {
            if (!IsClientSimulated())
            {
                return;
            }

            byte gripIndex = gripEvents[gripEvent];
            var gripEventData = new SimpleGripEventData()
            {
                objectId = currentId,
                gripIndex = gripIndex,
                eventIndex = 1
            };
            
            MelonLogger.Msg("Sent grip index: "+1+" for: "+gripIndex);

            var packetByteBuf =
                PacketHandler.CompressMessage(NetworkMessageType.SimpleGripEventPacket, gripEventData);
            Node.activeNode.BroadcastMessage((byte)NetworkChannel.Reliable, packetByteBuf.getBytes());
        }
        
        private void OnMenuTapDown(SimpleGripEvents gripEvent)
        {
            if (!IsClientSimulated())
            {
                return;
            }

            byte gripIndex = gripEvents[gripEvent];
            var gripEventData = new SimpleGripEventData()
            {
                objectId = currentId,
                gripIndex = gripIndex,
                eventIndex = 2
            };
            DebugLogger.Msg("Sent grip index: "+2+" for: "+gripIndex);

            var packetByteBuf =
                PacketHandler.CompressMessage(NetworkMessageType.SimpleGripEventPacket, gripEventData);
            Node.activeNode.BroadcastMessage((byte)NetworkChannel.Reliable, packetByteBuf.getBytes());
        }

        public static void UpdateSyncedNPCs()
        {
            bool shouldRun = npcWithRoots.Count != 0;

            if (!shouldRun)
            {
                return;
            }

            Dictionary<Vector3, ushort> groupIdsFlipped = new Dictionary<Vector3, ushort>();
            foreach (var pair in npcWithRoots)
            {
                if (pair.Value != null)
                {
                    SyncedObject syncedObject = pair.Value.gameObject.GetComponent<SyncedObject>();
                    if (syncedObject != null)
                    {
                        if (syncedObject.IsClientSimulated() && !syncedObject._rigidbody.IsSleeping())
                        {
                            if (!groupIdsFlipped.ContainsKey(pair.Value.transform.position)){
                                groupIdsFlipped.Add(pair.Value.transform.position, pair.Key);
                            }
                        }
                    }
                }
            }

            List<ushort> finalGroupIds = new List<ushort>();
            Vector3 playerHead = Player.GetPlayerHead().transform.position;
            List<Vector3> vectorsToCheck = groupIdsFlipped.Keys.ToList();
            for (int i = 0; i < maxNpcsToSync; i++)
            {
                if (vectorsToCheck.Count == 0)
                {
                    break;
                }

                Vector3 result = GetClosestInList(vectorsToCheck, playerHead);
                vectorsToCheck.Remove(result);
                finalGroupIds.Add(groupIdsFlipped[result]);
            }

            npcGroupIdsToSync = finalGroupIds;
        }

        private static Vector3 GetClosestInList(List<Vector3> vectors, Vector3 comparison)
        {
            Vector3 closestPos = comparison;
            float closest = float.PositiveInfinity;
            foreach (var doubleCycle in vectors)
            {
                float squareDistance = (doubleCycle - comparison).sqrMagnitude;
                if (closest > squareDistance)
                {
                    closestPos = doubleCycle;
                    closest = squareDistance;
                }
            }

            return closestPos;
        }

        public void DestroySyncable(bool fullyRemoveList)
        {
            syncedObjectIds.Remove(currentId);
            syncedObjects.Remove(gameObject);

            if (!totalRemovedGroups.Contains(groupId))
            {
                totalRemovedGroups.Add(groupId);
                GroupDestroyData groupDestroyData = new GroupDestroyData()
                {
                    groupId = groupId,
                    backupObjectId = lastId
                };

                PacketByteBuf message =
                    PacketHandler.CompressMessage(NetworkMessageType.GroupDestroyPacket, groupDestroyData);
                Node.activeNode.BroadcastMessage((byte)NetworkChannel.Object, message.getBytes());
            }

            if (fullyRemoveList)
            {
                relatedSyncedObjects[groupId] = new List<SyncedObject>();
            }
            else
            {
                var objects = relatedSyncedObjects[groupId];
                objects.Remove(this);
                relatedSyncedObjects[groupId] = objects;
            }

            Destroy(this);
        }

        public static List<SyncedObject> GetAllSyncables(GameObject gameObject)
        {
            List<SyncedObject> syncedObjects = new List<SyncedObject>();
            foreach (SyncedObject syncedObject in gameObject.GetComponentsInChildren<SyncedObject>())
            {
                if (syncedObjects.Contains(syncedObject))
                {
                    syncedObjects.Add(syncedObject);
                }
            }

            foreach (SyncedObject syncedObject in gameObject.GetComponentsInParent<SyncedObject>())
            {
                if (syncedObjects.Contains(syncedObject))
                {
                    syncedObjects.Add(syncedObject);
                }
            }

            return syncedObjects;
        }

        public void UpdatePos()
        {
            if (!DiscordIntegration.hasLobby) return;

            if (_rigidbody)
            {
                if (_rigidbody.IsSleeping())
                {
                    return;
                }
            }

            if (totalRemovedGroups.Contains(groupId))
            {
                return;
            }

            if (DiscordIntegration.currentUser.Id == firstEverOwner)
            {
                if (!gameObject.activeInHierarchy)
                {
                    if (!totalRemovedGroups.Contains(groupId))
                    {
                        queuedObjectsToDelete.Add(groupId);
                        totalRemovedGroups.Add(groupId);
                    }

                    return;
                }
            }
            if (IsClientSimulated())
            {
                var shouldSendUpdate = false;
                var changedPositions = HasChangedPositions();
                if (_rigidbody)
                {
                    if (!_rigidbody.IsSleeping())
                    {
                        if (changedPositions)
                        {
                            shouldSendUpdate = true;
                        }
                    }
                }
                else
                {
                    shouldSendUpdate = changedPositions;
                }



                if (shouldSendUpdate)
                {
                    var compressedTransform =
                        new CompressedTransform(gameObject.transform.position,
                            Quaternion.Euler(gameObject.transform.eulerAngles));

                    var transformUpdateData = new TransformUpdateData
                    {
                        objectId = currentId,
                        userId = DiscordIntegration.currentUser.Id,
                        compressedTransform = compressedTransform
                    };

                    var packetByteBuf =
                        PacketHandler.CompressMessage(NetworkMessageType.TransformUpdatePacket, transformUpdateData);
                    Node.activeNode.BroadcastMessage((byte)NetworkChannel.Unreliable, packetByteBuf.getBytes()); 
                }
            }
            UpdateStoredPositions();
        }

        public void BroadcastOwnerChange()
        {
            if (!IsClientSimulated())
            {
                DebugLogger.Msg("Transferred ownership of sync Id: " + currentId);

                var currentUserId = DiscordIntegration.currentUser.Id;

                SetOwner(currentUserId);
                var ownerQueueChangeData = new OwnerChangeData
                {
                    userId = currentUserId,
                    objectId = currentId
                };
                var packetByteBuf = PacketHandler.CompressMessage(NetworkMessageType.OwnerChangePacket,
                    ownerQueueChangeData);
                Node.activeNode.BroadcastMessage((byte)NetworkChannel.Reliable, packetByteBuf.getBytes());
                DebugLogger.Msg("Transferring ownership of whole group ID: " + groupId);

                List<SyncedObject> relatedSynced = relatedSyncedObjects[groupId];
                for (int i = 0; i < relatedSynced.Count; i++)
                {
                    SyncedObject relatedSync = relatedSynced[i];
                    DebugLogger.Msg("Transferred ownership of related sync part: " + relatedSync.gameObject.name);

                    relatedSync.SetOwner(currentUserId);
                }
            }
        }

        public static void FutureSync(GameObject gameObject, ushort groupId, long userId)
        {
            foreach (Rigidbody rigidbody in GetProperRigidBodies(gameObject.transform))
            {
                GameObject npcObj = rigidbody.gameObject;
                FutureProofSync(npcObj, groupId, userId);
            }
        }

        public void ManualSetOwner(long userId, bool checkForSelfOwner)
        {
            if (checkForSelfOwner)
                if (!IsClientSimulated())
                    return;

            DebugLogger.Msg("Manually setting owner to: " + userId);
            DebugLogger.Msg("Transferred ownership of sync Id: " + currentId);

            SetOwner(userId);
            var ownerQueueChangeData = new OwnerChangeData
            {
                userId = userId,
                objectId = currentId
            };
            
            var packetByteBuf = PacketHandler.CompressMessage(NetworkMessageType.OwnerChangePacket,
                ownerQueueChangeData);
            Node.activeNode.BroadcastMessage((byte)NetworkChannel.Reliable, packetByteBuf.getBytes());

            DebugLogger.Msg("Transferring ownership of whole group ID: " + groupId);

            foreach (var relatedSync in relatedSyncedObjects[groupId])
            {
                DebugLogger.Msg("Transferred ownership of related sync part: " + relatedSync.gameObject.name);

                relatedSync.SetOwner(userId);
            }
        }

        public static void CleanData(bool deleteIfPossible = false)
        {
            lastId = 0;
            lastGroupId = 0;
            foreach (var gameObject in syncedObjects)
                if (gameObject != null)
                {
                    var syncedObject = gameObject.GetComponent<SyncedObject>();
                    if (syncedObject != null)
                    {
                        if (deleteIfPossible)
                        {
                            if (!syncedObject.IsClientSimulated())
                            {
                                // Means it was spawned in (was NOT in the map)
                                if (syncedObject.spawnedObject)
                                {
                                    Transform parent = syncedObject.transform;
                                    while (parent.parent != null)
                                    {
                                        parent = parent.parent;
                                    }

                                    Destroy(parent);
                                    continue;
                                }
                            }
                        }

                        Destroy(syncedObject);
                    }
                }

            cachedSpawnedObjects.Clear();
            syncedObjectIds.Clear();
            syncedObjects.Clear();
            relatedSyncedObjects.Clear();
            totalRemovedGroups.Clear();
            queuedObjectsToDelete.Clear();
            npcWithRoots.Clear();
            npcGroupIdsToSync.Clear();
        }

        public static SyncedObject GetSyncedComponent(GameObject gameObject)
        {
            if (gameObject.GetComponent<SyncedObject>())
            {
                return gameObject.GetComponent<SyncedObject>();
            }

            if (gameObject.GetComponentInParent<SyncedObject>())
            {
                return gameObject.GetComponentInParent<SyncedObject>();
            }

            if (gameObject.GetComponentInChildren<SyncedObject>())
            {
                return gameObject.GetComponentInChildren<SyncedObject>();
            }

            return null;
        }

        public static bool isSyncedObject(GameObject gameObject)
        {
            if (GetSyncedComponent(gameObject) != null) return true;

            return false;
        }

        public static SyncedObject Sync(GameObject desiredSync, bool shouldCheckScene = true, string manualBarcode = "", long userId = 0)
        {
            if (!DiscordIntegration.hasLobby) return null;

            if (isSyncedObject(desiredSync) || syncedObjects.Contains(desiredSync))
            {
                return null;
            }

            if (Blacklist.isBlacklisted(GetGameObjectPath(desiredSync.gameObject))) return null;

            long desiredId = DiscordIntegration.currentUser.Id;

            if (userId != 0)
            {
                desiredId = userId;
            }

            var groupId = GetGroupId();
            ushort startingId = lastId;
            bool hasBroadcasted = false;
            GameObject main = null;
            foreach (var rigidbody in GetProperRigidBodies(desiredSync.transform))
            {
                if (!hasBroadcasted)
                {
                    main = rigidbody.gameObject;
                    hasBroadcasted = true;
                }

                FutureProofSync(rigidbody.gameObject, groupId, desiredId);
            }

            ushort finalId = lastId;
            string sentBarcode = PoolManager.GetSpawnableBarcode(desiredSync);
            if (manualBarcode != "")
            {
                sentBarcode = manualBarcode;
            }

            if (main != null)
            {
                string syncObject = GetGameObjectPath(main);
                var initializeSyncData = new InitializeSyncData
                {
                    userId = desiredId,
                    objectId = startingId,
                    checkInScene = shouldCheckScene,
                    finalId = finalId,
                    objectName = syncObject,
                    groupId = groupId,
                    barcode = sentBarcode
                };

                var packetByteBuf =
                    PacketHandler.CompressMessage(NetworkMessageType.InitializeSyncPacket, initializeSyncData);

                Node.activeNode.BroadcastMessage((byte)NetworkChannel.Object, packetByteBuf.getBytes());
                DebugLogger.Msg("Starting Id: " + startingId);
                DebugLogger.Msg("Final Id:" + finalId);
                DebugLogger.Msg("THIS SHOULD BE REFLECTED ON ALL OTHER CLIENTS.");
            }

            DebugLogger.Msg("Synced or atleast got to finish syncing " + desiredSync.name);
            return GetSyncedComponent(desiredSync);
        }

        public void AddToSyncGroup(SyncedObject syncedObject)
        {
            syncedObject.TransferGroup(groupId);
            DebugLogger.Msg("Transferred group of " + syncedObject.gameObject.name + " to group: " + groupId);
        }

        public void TransferGroup(ushort newGroupId, bool fullSearch = true)
        {
            DebugLogger.Msg("Called transfer from group id: "+groupId+", to: "+newGroupId);
            if (groupId == newGroupId) return;
            if (!relatedSyncedObjects.ContainsKey(groupId)) return;

            ushort originalGroupId = groupId;

            if (fullSearch)
            {
                foreach (var allSynced in relatedSyncedObjects[originalGroupId])
                {
                    var newList = new List<SyncedObject>();
        
                    if (relatedSyncedObjects.ContainsKey(newGroupId))
                    {
                        newList = relatedSyncedObjects[newGroupId];
                    }
                    else
                    {
                        relatedSyncedObjects.Add(newGroupId, newList);
                    }

                    if (!newList.Contains(allSynced))
                    {
                        newList.Add(allSynced);
                    }
                
                    relatedSyncedObjects[newGroupId] = newList;
                    DebugLogger.Msg("Changed group Id of: "+allSynced.gameObject.name);
                    DebugLogger.Msg("Was: "+allSynced.originalGroupId);
                    allSynced.groupId = newGroupId;
                    DebugLogger.Msg("Is now: "+newGroupId);
                }
            }
            else
            {
                var newList = new List<SyncedObject>();

                if (relatedSyncedObjects.ContainsKey(newGroupId))
                {
                    newList = relatedSyncedObjects[newGroupId];
                }
                else
                {
                    relatedSyncedObjects.Add(newGroupId, newList);
                }

                if (!newList.Contains(this))
                {
                    newList.Add(this);
                }
                
                relatedSyncedObjects[newGroupId] = newList;
                DebugLogger.Msg("Changed group Id of: "+gameObject.name);
                DebugLogger.Msg("Was: "+originalGroupId);
                groupId = newGroupId;
                DebugLogger.Msg("Is now: "+groupId);
            }

            relatedSyncedObjects.Remove(originalGroupId);
        }

        public void Update()
        {
            if (!IsClientSimulated())
            {
                InterpolatedObject.Lerp();
            }
        }

        private static void FutureProofSync(GameObject gameObject, ushort groupId, long ownerId)
        {
            if (gameObject.GetComponent<SyncedObject>())
            {
                return;
            }

            if (lastGroupId <= groupId)
            {
                lastGroupId = groupId;
                lastGroupId++;
            }

            var syncedId = GetSyncId();

            var syncedObject = gameObject.AddComponent<SyncedObject>();
            if (relatedSyncedObjects.ContainsKey(groupId))
            {
                var otherSynced = relatedSyncedObjects[groupId];
                if (!otherSynced.Contains(syncedObject)) otherSynced.Add(syncedObject);
                relatedSyncedObjects[groupId] = otherSynced;
            }
            else
            {
                var otherSynced = new List<SyncedObject>();
                otherSynced.Add(syncedObject);
                relatedSyncedObjects.Add(groupId, otherSynced);
            }

            syncedObject.SetOwner(ownerId);
            syncedObject.groupId = groupId;
            syncedObject.originalGroupId = groupId;
            syncedObject.currentId = syncedId;
            syncedObjects.Add(gameObject);
            if (!syncedObjectIds.ContainsKey(syncedId)) syncedObjectIds.Add(syncedId, syncedObject);
        }

        private void OnOwnershipChange(bool owning)
        {
            if (owning)
            {
                var rigidbody = gameObject.GetComponent<Rigidbody>();
                if (rigidbody)
                    if (rigidbody.isKinematic)
                        rigidbody.isKinematic = false;
            }
            else
            {
                shouldTeleport = true;
                if (!Node.activeNode.connectedUsers.Contains(simulatorId)) SetOwner(DiscordIntegration.lobby.OwnerId);
            }
        }

        public static Rigidbody[] GetProperRigidBodies(Transform transform)
        {
            DebugLogger.Msg("Getting all rigidbodies for: " + transform.gameObject.name);

            var rigidbodies = new List<Rigidbody>();

            var ultimateParent = transform;
            var isNPC = false;

            var foundBody = transform.GetComponentInParent<AIBrain>();
            if (!foundBody)
            {
                foundBody = transform.GetComponent<AIBrain>();
                if (!foundBody) foundBody = transform.GetComponentInChildren<AIBrain>();
            }

            if (foundBody)
            {
                ultimateParent = foundBody.gameObject.transform;
                isNPC = true;
            }

            if (!isNPC)
            {
                DebugLogger.Msg("Is not an npc");
                var parentWithInteractive = transform;
                if (!transform.gameObject.GetComponent<Rigidbody>())
                {
                    if (transform.gameObject.GetComponentInParent<Rigidbody>())
                    {
                        parentWithInteractive =
                            transform.gameObject.GetComponentInParent<Rigidbody>().gameObject.transform;
                    }
                    else
                    {
                        if (transform.gameObject.GetComponentInChildren<Rigidbody>())
                            parentWithInteractive = transform.gameObject.GetComponentInChildren<Rigidbody>().gameObject
                                .transform;
                    }
                }

                Rigidbody mostRecent = null;

                foreach (Rigidbody rigidbody in parentWithInteractive.GetComponentsInParent<Rigidbody>())
                {
                    mostRecent = rigidbody;
                }

                if (mostRecent != null)
                {
                    parentWithInteractive = mostRecent.gameObject.transform;
                }

                while (isPartOfBiggerObject(parentWithInteractive))
                {
                    DebugLogger.Msg(parentWithInteractive.gameObject.name + " Is part of a bigger object.");
                    parentWithInteractive = parentWithInteractive.parent;
                }

                DebugLogger.Msg("Going with: " + parentWithInteractive.gameObject.name);
                ultimateParent = parentWithInteractive;
            }

            var baseRigidBody = ultimateParent.transform.gameObject.GetComponent<Rigidbody>();

            if (baseRigidBody) rigidbodies.Add(baseRigidBody);

            foreach (var rigidbody in ultimateParent.transform.gameObject.GetComponentsInChildren<Rigidbody>())
                if (!rigidbodies.Contains(rigidbody) && !rigidbody.isKinematic)
                {
                    rigidbodies.Add(rigidbody);
                }


            DebugLogger.Msg("Found: " + rigidbodies.Count + " for object:" + ultimateParent.transform.name);
            return rigidbodies.ToArray();
        }

        private static bool isPartOfBiggerObject(Transform transform)
        {
            if (transform.parent == null) return false;

            if (transform.parent.gameObject.name.Contains("hand")) return false;

            if (transform.parent.gameObject.GetComponent<Rigidbody>() != null) return true;

            return false;
        }

        protected void UpdateStoredPositions()
        {
            lastPosition = transform.position;
            lastRotation = transform.rotation;
        }

        public bool HasChangedPositions()
        {
            if (isNpc)
            {
                if (!_rigidbody.IsSleeping() && npcGroupIdsToSync.Contains(groupId))
                {
                    return true;
                }

                return false;
            }

            return (transform.position - lastPosition).sqrMagnitude > 0.0005f ||
                   Quaternion.Angle(transform.rotation, lastRotation) > 0.05f;
        }

        public static string GetGameObjectPath(GameObject obj)
        {
            var path = "/" + obj.name;
            while (obj.transform.parent)
            {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }

            return path;
        }

        public void SetOwner(long userId)
        {
            if (simulatorId == userId)
            {
                return;
            }

            if (firstEverOwner == 0)
            {
                firstEverOwner = userId;
            }

            simulatorId = userId;
            if (IsClientSimulated())
            {
                OnOwnershipChange(true);
            }
            else
            {
                OnOwnershipChange(false);
            }
        }

        public bool IsClientSimulated()
        {
            return simulatorId == DiscordIntegration.currentUser.Id;
        }

        public static SyncedObject GetSyncedObject(ushort objectId)
        {
            if (syncedObjectIds.ContainsKey(objectId)) return syncedObjectIds[objectId];

            return null;
        }

        public void UpdateObject(CompressedTransform compressedTransform)
        {
            if (!IsClientSimulated())
            {
                if (!gameObject.activeInHierarchy)
                {
                    Transform parent = gameObject.transform;
                    while (parent.gameObject.activeSelf)
                    {
                        parent = parent.parent;
                    }

                    parent.gameObject.SetActive(true);
                }

                if (_rigidbody)
                {
                    _rigidbody.velocity = Vector3.zero;
                    _rigidbody.isKinematic = true;
                }
                
                InterpolatedObject.UpdateTarget(compressedTransform.position, compressedTransform.rotation, shouldTeleport);
                shouldTeleport = false;
            }
        }

        public static ushort GetSyncId()
        {
            return lastId++;
        }

        public static ushort GetGroupId()
        {
            return lastGroupId++;
        }
    }
}