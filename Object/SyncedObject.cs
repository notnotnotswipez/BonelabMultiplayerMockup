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
using MelonLoader;
using SLZ.AI;
using SLZ.Marrow.Pool;
using UnityEngine;

namespace BonelabMultiplayerMockup.Object
{
    [RegisterTypeInIl2Cpp]
    public class SyncedObject : MonoBehaviour
    {
        public static List<GameObject> tempActiveObjects = new List<GameObject>();

        public static Dictionary<ushort, SyncedObject> syncedObjectIds = new Dictionary<ushort, SyncedObject>();
        public static List<GameObject> syncedObjects = new List<GameObject>();

        public static Dictionary<ushort, List<SyncedObject>> relatedSyncedObjects =
            new Dictionary<ushort, List<SyncedObject>>();

        public static List<ushort> queuedObjectsToDelete = new List<ushort>();
        public static List<ushort> totalRemovedGroups = new List<ushort>();

        public static ushort lastId;
        public static ushort lastGroupId;
        private Rigidbody _rigidbody;
        public GameObject mainReference;

        public Vector3 lastPosition;
        public Quaternion lastRotation;
        public long simulatorId;
        public ushort currentId;
        public ushort groupId;
        public long firstEverOwner = 0;

        public bool isNpc = false;
        public static Dictionary<ushort, Rigidbody> npcWithRoots = new Dictionary<ushort, Rigidbody>();
        public static List<ushort> npcGroupIdsToSync = new List<ushort>();
        
        // Sync nearest <maxNpcsToSync> NPCs and thats all. The specific NPCs which are synced are the ones closest. Good for performance.
        private static int maxNpcsToSync = 3;

        public SyncedObject(IntPtr intPtr) : base(intPtr)
        {
        }

        public void Start()
        {
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
                SyncedObject syncedObject = pair.Value.gameObject.GetComponent<SyncedObject>();
                if (syncedObject.IsClientSimulated())
                {
                    groupIdsFlipped.Add(pair.Value.transform.position, pair.Key);
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
            foreach (SyncedObject syncedObject in gameObject.GetComponentsInChildren<SyncedObject>()) {
                if (syncedObjects.Contains(syncedObject))
                {
                    syncedObjects.Add(syncedObject);
                }
            }
            foreach (SyncedObject syncedObject in gameObject.GetComponentsInParent<SyncedObject>()) {
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

            var shouldSendUpdate = HasChangedPositions();
            if (IsClientSimulated() && shouldSendUpdate)
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
                    PacketHandler.CompressMessage(NetworkMessageType.TransformUpdateMessage, transformUpdateData);
                Node.activeNode.BroadcastMessage((byte)NetworkChannel.Unreliable, packetByteBuf.getBytes());
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
                var packetByteBuf = PacketHandler.CompressMessage(NetworkMessageType.OwnerChangeMessage,
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
            foreach (Rigidbody rigidbody in GetProperRigidBodies(gameObject.transform)) {
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
            var packetByteBuf = PacketHandler.CompressMessage(NetworkMessageType.OwnerChangeMessage,
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
                                if (syncedObject.mainReference != null)
                                {
                                    Destroy(syncedObject.mainReference);
                                    continue;
                                }
                            }
                        }
                    
                        Destroy(syncedObject);
                    }
                }

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

        public static void MakeSyncedObject(GameObject gameObject, ushort objectId, long ownerId, ushort groupId,
            bool properAdd = true)
        {
            if (syncedObjects.Contains(gameObject)) return;

            var syncedObject = gameObject.AddComponent<SyncedObject>();
            // If the group ID coming in is greater than or equal to the one stored clientside, then we should set it.
            if (properAdd)
                if (lastGroupId <= groupId)
                {
                    lastGroupId = groupId;
                    lastGroupId++;
                }

            if (relatedSyncedObjects.ContainsKey(groupId))
            {
                var otherSynced = relatedSyncedObjects[groupId];
                if (!otherSynced.Contains(syncedObject))
                {
                    otherSynced.Add(syncedObject);
                    DebugLogger.Msg("Added related sync in group ID: " + groupId);
                }

                relatedSyncedObjects[groupId] = otherSynced;
            }
            else
            {
                var otherSynced = new List<SyncedObject>();
                otherSynced.Add(syncedObject);
                relatedSyncedObjects.Add(groupId, otherSynced);
                DebugLogger.Msg("Added related sync in group ID: " + groupId);
            }

            syncedObjects.Add(gameObject);
            syncedObject.SetOwner(ownerId);
            syncedObject.currentId = objectId;
            syncedObject.groupId = groupId;
            
            
            
            DebugLogger.Msg("Made sync object for: " + gameObject.name + ", with an ID of: " + objectId +
                            ", and group ID of: " + groupId);
            DebugLogger.Msg("Owner: " + ownerId);

            syncedObjectIds.Add(objectId, syncedObject);
        }

        public static void Sync(GameObject desiredSync)
        {
            if (!DiscordIntegration.hasLobby) return;

            if (isSyncedObject(desiredSync) || syncedObjects.Contains(desiredSync))
            {
                return;
            }

            if (Blacklist.isBlacklisted(GetGameObjectPath(desiredSync.gameObject))) return;

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

                FutureProofSync(rigidbody.gameObject, groupId, DiscordIntegration.currentUser.Id);
            }

            ushort finalId = lastId;

            if (main != null)
            {
                string syncObject = GetGameObjectPath(main);
                var initializeSyncData = new InitializeSyncData
                {
                    userId = DiscordIntegration.currentUser.Id,
                    objectId = startingId,
                    finalId = finalId,
                    objectName = syncObject,
                    groupId = groupId,
                    barcode = PoolManager.GetSpawnableBarcode(desiredSync)
                };

                var packetByteBuf =
                    PacketHandler.CompressMessage(NetworkMessageType.InitializeSyncMessage, initializeSyncData);

                Node.activeNode.BroadcastMessage((byte)NetworkChannel.Object, packetByteBuf.getBytes());
                DebugLogger.Msg("Starting Id: " + startingId);
                DebugLogger.Msg("Final Id:" + finalId);
                DebugLogger.Msg("THIS SHOULD BE REFLECTED ON ALL OTHER CLIENTS.");
            }

            DebugLogger.Msg("Synced or atleast got to finish syncing " + desiredSync.name);
        }

        public void AddToSyncGroup(SyncedObject syncedObject)
        {
            syncedObject.TransferGroup(groupId);
            DebugLogger.Msg("Transferred group of " + syncedObject.gameObject.name + " to group: " + groupId);
        }

        public void TransferGroup(ushort newGroupId)
        {
            var otherSynced = relatedSyncedObjects[groupId];
            otherSynced.Remove(this);
            relatedSyncedObjects[groupId] = otherSynced;

            var newList = relatedSyncedObjects[newGroupId];
            if (!newList.Contains(this))
            {
                newList.Add(this);
            }

            relatedSyncedObjects[newGroupId] = newList;
            groupId = newGroupId;
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
            syncedObject.currentId = syncedId;
            syncedObjects.Add(gameObject);
            if (!syncedObjectIds.ContainsKey(syncedId)) syncedObjectIds.Add(syncedId, syncedObject);
        }
        
        public static void ManualClientSync(GameObject gameObject, ushort groupId, long ownerId, ushort objectId)
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

            var syncedId = objectId;

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
            syncedObject.currentId = syncedId;
            syncedObjects.Add(gameObject);
            if (!syncedObjectIds.ContainsKey(syncedId)) syncedObjectIds.Add(syncedId, syncedObject);
        }

        private static void BroadcastSyncData(GameObject gameObject, ushort groupId)
        {
            if (syncedObjects.Contains(gameObject)) return;

            var syncObject = GetGameObjectPath(gameObject);
            // We add this after, just incase someone else syncs and object that matches, this should never ever be looked for. Since its already synced.

            DebugLogger.Msg("Attempting to sync object, base path is: " + syncObject);

            var syncedId = GetSyncId();
            DebugLogger.Msg("Sync ID: " + syncedId);


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

            syncedObject.groupId = groupId;
            syncedObject.currentId = syncedId;
            syncedObject.SetOwner(DiscordIntegration.currentUser.Id);
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

            return (transform.position - lastPosition).sqrMagnitude > 0.0005f || Quaternion.Angle(transform.rotation, lastRotation) > 0.05f;
        }

        private static string GetGameObjectPath(GameObject obj)
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
                    _rigidbody.velocity = new Vector3(0, 0, 0);
                    _rigidbody.isKinematic = true;
                }

                gameObject.transform.position = compressedTransform.position;
                gameObject.transform.eulerAngles = compressedTransform.rotation.eulerAngles;
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