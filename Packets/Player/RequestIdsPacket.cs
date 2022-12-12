using BonelabMultiplayerMockup.Nodes;
using Steamworks;

namespace BonelabMultiplayerMockup.Packets.Player
{
    public class RequestIdsPacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            var requestIdsMessageData = (RequestIdsData)messageData;

            var packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(SteamIntegration.GetByteId(requestIdsMessageData.userId));
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            SteamId userId = SteamIntegration.GetByteId(packetByteBuf.ReadByte());
            if (SteamIntegration.Instance.ConnectedToSteam() && SteamIntegration.isHost)
                foreach (var valuePair in SteamIntegration.byteIds)
                {
                    var addMessageData = new ShortIdData
                    {
                        userId = valuePair.Value,
                        byteId = valuePair.Key
                    };
                    var shortBuf =
                        PacketHandler.CompressMessage(NetworkMessageType.ShortIdUpdatePacket, addMessageData);
                    SteamPacketNode.SendMessage(userId, NetworkChannel.Reliable, shortBuf.getBytes());
                }
        }
    }

    public class RequestIdsData : MessageData
    {
        public SteamId userId;
    }
}