using System.Collections.Generic;
using BonelabMultiplayerMockup.NetworkData;
using BonelabMultiplayerMockup.Object;
using MelonLoader;
using UnityEngine;

namespace BonelabMultiplayerMockup.Packets.Object
{
    public class GroupDestroyPacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            GroupDestroyData groupDestroyData = (GroupDestroyData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteUShort(groupDestroyData.groupId);
            packetByteBuf.WriteUShort(groupDestroyData.backupObjectId);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            ushort groupId = packetByteBuf.ReadUShort();
            ushort backupObjectId = packetByteBuf.ReadUShort();
            SyncedObject backup = SyncedObject.GetSyncedObject(backupObjectId);
            if (backup)
            {
                groupId = backup.groupId;
            }

            if (!SyncedObject.relatedSyncedObjects.ContainsKey(groupId))
            {
                return;
            }

            List<SyncedObject> syncedObjectsToRemove = new List<SyncedObject>();
            for (int i = 0; i < SyncedObject.relatedSyncedObjects[groupId].Count; i++) {
                syncedObjectsToRemove.Add(SyncedObject.relatedSyncedObjects[groupId][i]);
            }

            for (int i = 0; i < syncedObjectsToRemove.Count; i++)
            {
                SyncedObject syncedObject = syncedObjectsToRemove[i];
                if (syncedObject != null)
                {
                    syncedObject.UpdateObject(new CompressedTransform(new Vector3(0, 100, 0), Quaternion.identity));
                    syncedObject.DestroySyncable(false);
                    if (syncedObject.spawnedObject)
                    {
                        Transform parent = syncedObject.transform;
                        while (parent.parent != null)
                        {
                            parent = parent.parent;
                        }
                        GameObject.Destroy(parent);
                    }
                }
            }
            syncedObjectsToRemove.Clear();

            SyncedObject.queuedObjectsToDelete.Remove(groupId);
            MelonLogger.Msg("Destroyed group Id: "+groupId);
            MelonLogger.Msg("Size: "+SyncedObject.queuedObjectsToDelete.Count);
        }
    }

    public class GroupDestroyData : MessageData
    {
        public ushort groupId;
        public ushort backupObjectId;
    }
}