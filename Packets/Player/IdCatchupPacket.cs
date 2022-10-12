using BonelabMultiplayerMockup.Object;

namespace BonelabMultiplayerMockup.Packets.Player
{
    public class JoinCatchupPacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            var joinCatchupData = (JoinCatchupData)messageData;
            var packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteUShort(joinCatchupData.lastId);
            packetByteBuf.WriteUShort(joinCatchupData.lastGroupId);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            var lastId = packetByteBuf.ReadUShort();
            var lastGroupId = packetByteBuf.ReadUShort();
            SyncedObject.lastId = lastId;
            SyncedObject.lastGroupId = lastGroupId;
        }
    }

    public class JoinCatchupData : MessageData
    {
        public ushort lastGroupId;
        public ushort lastId;
    }
}