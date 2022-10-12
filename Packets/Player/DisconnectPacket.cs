using BonelabMultiplayerMockup.Nodes;

namespace BonelabMultiplayerMockup.Packets.Player
{
    public class DisconnectPacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            var disconnectMessageData = (DisconnectData)messageData;
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

    public class DisconnectData : MessageData
    {
        public long userId;
    }
}