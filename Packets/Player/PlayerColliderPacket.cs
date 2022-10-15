using BonelabMultiplayerMockup.NetworkData;
using BonelabMultiplayerMockup.Representations;

namespace BonelabMultiplayerMockup.Packets.Player
{
    public class PlayerColliderPacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            PlayerColliderData data = (PlayerColliderData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(DiscordIntegration.GetByteId(data.userId));
            packetByteBuf.WriteByte(data.colliderIndex);
            packetByteBuf.WriteCompressedTransform(data.CompressedTransform);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            long userId = DiscordIntegration.GetLongId(packetByteBuf.ReadByte());
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
        public long userId;
        public byte colliderIndex;
        public CompressedTransform CompressedTransform;
    }
}