using BonelabMultiplayerMockup.Nodes;

namespace BonelabMultiplayerMockup.Messages.Handlers.Player
{
    public class DisconnectMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            var disconnectMessageData = (DisconnectMessageData)messageData;
            var packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(DiscordIntegration.GetByteId(disconnectMessageData.userId));
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            if (DiscordIntegration.hasLobby)
                if (Client.instance != null)
                    Client.instance.Shutdown();
        }
    }

    public class DisconnectMessageData : MessageData
    {
        public long userId;
    }
}