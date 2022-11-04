using BonelabMultiplayerMockup.Object;

namespace BonelabMultiplayerMockup.Packets.Object
{
    public class GrabStatePacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            var grabStateData = (GrabStateData)messageData;

            var packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteUShort(grabStateData.objectId);
            packetByteBuf.WriteByte(grabStateData.state);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            ushort objectId = packetByteBuf.ReadUShort();
            byte state = packetByteBuf.ReadByte();
            bool grabbed = state == 1;

            var syncedObject = SyncedObject.GetSyncedObject(objectId);
            if (syncedObject == null) return;

            syncedObject.SetGrabbed(grabbed);

            if (SyncedObject.relatedSyncedObjects.ContainsKey(syncedObject.groupId))
                foreach (var relatedSync in SyncedObject.relatedSyncedObjects[syncedObject.groupId])
                    relatedSync.SetGrabbed(grabbed);
        }
    }

    public class GrabStateData : MessageData
    {
        public ushort objectId;
        public byte state;
    }
}