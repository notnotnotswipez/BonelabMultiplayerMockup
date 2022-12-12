using BonelabMultiplayerMockup.Representations;
using SLZ;
using Steamworks;

namespace BonelabMultiplayerMockup.Packets.Player
{
    public class PlayerEndGrabPacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            PlayerEndGrabData playerEndGrabData = (PlayerEndGrabData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(SteamIntegration.GetByteId(playerEndGrabData.userIdGrabber));
            packetByteBuf.WriteByte(playerEndGrabData.hand);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            SteamId userId = SteamIntegration.GetLongId(packetByteBuf.ReadByte());
            byte hand = packetByteBuf.ReadByte();

            var handedness = hand == 1 ? Handedness.RIGHT : Handedness.LEFT;
            if (PlayerRepresentation.representations.ContainsKey(userId))
            {
                PlayerRepresentation playerRepresentation = PlayerRepresentation.representations[userId];
                playerRepresentation.LetGoOfClientCollider(handedness);
            }
        }
    }

    public class PlayerEndGrabData : MessageData
    {
        public SteamId userIdGrabber;
        public byte hand;
    }
}