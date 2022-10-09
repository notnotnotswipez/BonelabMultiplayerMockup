using BonelabMultiplayerMockup.Nodes;

namespace BonelabMultiplayerMockup.Messages.Handlers.Player
{
    public class RequestIdsMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            var requestIdsMessageData = (RequestIdsMessageData)messageData;

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
                    var addMessageData = new ShortIdMessageData
                    {
                        userId = valuePair.Value,
                        byteId = valuePair.Key
                    };
                    var shortBuf =
                        MessageHandler.CompressMessage(NetworkMessageType.ShortIdUpdateMessage, addMessageData);
                    Server.instance.SendMessage(userId, (byte)NetworkChannel.Reliable, shortBuf.getBytes());
                }
        }
    }

    public class RequestIdsMessageData : MessageData
    {
        public long userId;
    }
}