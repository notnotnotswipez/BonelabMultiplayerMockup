using System.Collections.Generic;
using System.Linq;
using BonelabMultiplayerMockup.Object;
using HBMP.DataType;
using MelonLoader;
using UnityEngine;

namespace BonelabMultiplayerMockup.Messages.Handlers.Object
{
    public class GroupDestroyMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            GroupDestroyMessageData groupDestroyMessageData = (GroupDestroyMessageData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteUShort(groupDestroyMessageData.groupId);
            packetByteBuf.WriteUShort(groupDestroyMessageData.backupObjectId);
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
                syncedObject.UpdateObject(new SimplifiedTransform(new Vector3(0, 100, 0), Quaternion.identity));
                syncedObject.DestroySyncable(false);
                if (syncedObject.mainReference)
                {
                    GameObject.Destroy(syncedObject.mainReference);
                    syncedObject.mainReference = null;
                }
            }
            syncedObjectsToRemove.Clear();

            SyncedObject.queuedObjectsToDelete.Remove(groupId);
            MelonLogger.Msg("Destroyed group Id: "+groupId);
            MelonLogger.Msg("Size: "+SyncedObject.queuedObjectsToDelete.Count);
        }
    }

    public class GroupDestroyMessageData : MessageData
    {
        public ushort groupId;
        public ushort backupObjectId;
    }
}