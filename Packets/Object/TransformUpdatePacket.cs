using BonelabMultiplayerMockup.NetworkData;
using BonelabMultiplayerMockup.Object;
using Steamworks;

namespace BonelabMultiplayerMockup.Packets.Object
{
    public class TransformUpdatePacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            var transformUpdateData = (TransformUpdateData)messageData;
            var packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteUShort(transformUpdateData.objectId);
            packetByteBuf.WriteByte(SteamIntegration.GetByteId(transformUpdateData.userId));
            packetByteBuf.WriteCompressedTransform(transformUpdateData.compressedTransform);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            var objectId = packetByteBuf.ReadUShort();
            var syncedObject = SyncedObject.GetSyncedObject(objectId);
            if (syncedObject == null)  return;

            var userId = SteamIntegration.GetLongId(packetByteBuf.ReadByte());
            var compressedTransform = packetByteBuf.ReadCompressedTransform();

            syncedObject.UpdateObject(compressedTransform);
        }
    }

    public class TransformUpdateData : MessageData
    {
        public ushort objectId;
        public CompressedTransform compressedTransform;
        public SteamId userId;
    }
}