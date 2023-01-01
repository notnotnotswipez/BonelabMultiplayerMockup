using BonelabMultiplayerMockup.NetworkData;
using BonelabMultiplayerMockup.Representations;
using Steamworks;

namespace BonelabMultiplayerMockup.Packets.Player
{
    public class PlayerColliderPacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            PlayerColliderData data = (PlayerColliderData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(SteamIntegration.GetByteId(data.userId));
            packetByteBuf.WriteByte(data.colliderIndex);
            packetByteBuf.WriteCompressedTransform(data.CompressedTransform);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            SteamId userId = SteamIntegration.GetLongId(packetByteBuf.ReadByte());
            byte colliderIndex = packetByteBuf.ReadByte();
            CompressedTransform compressedTransform = packetByteBuf.ReadCompressedTransform();
            
            if (compressedTransform == null)
            {
                return;
            }
            
            if (PlayerRepresentation.representations.ContainsKey(userId))
            {
                var playerRepresentation = PlayerRepresentation.representations[userId];
                playerRepresentation.updateColliderTransform(colliderIndex, compressedTransform);
            }
        }
    }

    public class PlayerColliderData : MessageData
    {
        public SteamId userId;
        public byte colliderIndex;
        public CompressedTransform CompressedTransform;
    }
}