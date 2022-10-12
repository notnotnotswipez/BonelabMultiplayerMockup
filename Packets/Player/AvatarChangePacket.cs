using BonelabMultiplayerMockup.Representations;
using MelonLoader;

namespace BonelabMultiplayerMockup.Packets.Player
{
    public class AvatarChangePacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            AvatarChangeData avatarChangeData = (AvatarChangeData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(DiscordIntegration.GetByteId(avatarChangeData.userId));
            packetByteBuf.WriteString(avatarChangeData.barcode);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            long userId = DiscordIntegration.GetLongId(packetByteBuf.ReadByte());
            string barcode = packetByteBuf.ReadString();
            
            MelonLogger.Msg(userId+" sent a request to change their avatar to barcode: "+barcode);
            
            if (PlayerRepresentation.representations.ContainsKey(userId))
            {
                var playerRepresentation = PlayerRepresentation.representations[userId];
                playerRepresentation.SetAvatar(barcode);
            }
        }
    }

    public class AvatarChangeData : MessageData
    {
        public string barcode;
        public long userId;
    }
}