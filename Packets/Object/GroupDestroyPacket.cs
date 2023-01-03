using System.Collections.Generic;
using System.Linq;
using BonelabMultiplayerMockup.NetworkData;
using BonelabMultiplayerMockup.Object;
using BonelabMultiplayerMockup.Utils;
using MelonLoader;
using SLZ.Marrow.Pool;
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

            if (SyncedObject.cachedSpawnedObjects.ContainsKey(groupId))
            {
                SyncedObject backup = SyncedObject.GetSyncedObject(backupObjectId);

                if (SyncedObject.npcWithRoots.ContainsKey(groupId))
                {
                    SyncedObject.npcWithRoots.Remove(groupId);
                }

                GameObject gameObject = SyncedObject.cachedSpawnedObjects[groupId];

                if (gameObject)
                {
                    GameObject.Destroy(gameObject);
                    SyncedObject.cachedSpawnedObjects.Remove(groupId);
                }
                else
                {
                    AssetPoolee poolee = PoolManager.GetComponentOnObject<AssetPoolee>(backup.gameObject);
                    if (poolee)
                    {
                        poolee.Despawn();
                    }
                }
            }
        }
    }

    public class GroupDestroyData : MessageData
    {
        public ushort groupId;
        public ushort backupObjectId;
    }
}