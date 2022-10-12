using BonelabMultiplayerMockup.Nodes;

namespace BonelabMultiplayerMockup.Packets.Player
{
    public class RequestIdsPacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            var requestIdsMessageData = (RequestIdsData)messageData;

            var packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(DiscordIntegration.GetByteId(requestIdsMessageData.userId));
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            long userId = DiscordIntegration.GetByteId(packetByteBuf.ReadByte());
            if (Server.instance != null)
                foreach (var valuePair in DiscordIntegration.byteIds)
                {
                    var addMessageData = new ShortIdData
                    {
                        userId = valuePair.Value,
                        byteId = valuePair.Key
                    };
                    var shortBuf =
                        PacketHandler.CompressMessage(NetworkMessageType.ShortIdUpdatePacket, addMessageData);
                    Server.instance.SendMessage(userId, (byte)NetworkChannel.Reliable, shortBuf.getBytes());
                }
        }
    }

    public class RequestIdsData : MessageData
    {
        public long userId;
    }
}