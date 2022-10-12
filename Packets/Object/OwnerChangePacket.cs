using BonelabMultiplayerMockup.Object;

namespace BonelabMultiplayerMockup.Packets.Object
{
    public class OwnerChangePacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            var ownerQueueChangeData = (OwnerChangeData)messageData;

            var packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(DiscordIntegration.GetByteId(ownerQueueChangeData.userId));
            packetByteBuf.WriteUShort(ownerQueueChangeData.objectId);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            var userId = DiscordIntegration.GetLongId(packetByteBuf.ReadByte());
            var objectId = packetByteBuf.ReadUShort();

            var syncedObject = SyncedObject.GetSyncedObject(objectId);
            if (syncedObject == null) return;

            syncedObject.SetOwner(userId);

            if (SyncedObject.relatedSyncedObjects.ContainsKey(syncedObject.groupId))
                foreach (var relatedSync in SyncedObject.relatedSyncedObjects[syncedObject.groupId])
                    relatedSync.SetOwner(userId);
        }
    }

    public class OwnerChangeData : MessageData
    {
        public ushort objectId;
        public long userId;
    }
}