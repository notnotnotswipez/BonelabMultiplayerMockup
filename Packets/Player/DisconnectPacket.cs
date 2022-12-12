using BonelabMultiplayerMockup.Nodes;
using Steamworks;

namespace BonelabMultiplayerMockup.Packets.Player
{
    public class DisconnectPacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            var disconnectMessageData = (DisconnectData)messageData;
            var packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(SteamIntegration.GetByteId(disconnectMessageData.userId));
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            if (SteamIntegration.hasLobby)
                SteamIntegration.Disconnect(false);
        }
    }

    public class DisconnectData : MessageData
    {
        public SteamId userId;
    }
}