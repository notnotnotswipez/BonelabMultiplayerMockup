using BonelabMultiplayerMockup.Object;
using MelonLoader;

namespace BonelabMultiplayerMockup.Messages.Handlers.Player
{
    public class JoinCatchupMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            var joinCatchupData = (IdCatchupData)messageData;
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

    public class IdCatchupData : MessageData
    {
        public ushort lastGroupId;
        public ushort lastId;
    }
}