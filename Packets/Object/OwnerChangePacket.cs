using BonelabMultiplayerMockup.Object;
using Steamworks;

namespace BonelabMultiplayerMockup.Packets.Object
{
    public class OwnerChangePacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            var ownerQueueChangeData = (OwnerChangeData)messageData;

            var packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(SteamIntegration.GetByteId(ownerQueueChangeData.userId));
            packetByteBuf.WriteUShort(ownerQueueChangeData.objectId);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            SteamId userId = SteamIntegration.GetLongId(packetByteBuf.ReadByte());
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
        public SteamId userId;
    }
}