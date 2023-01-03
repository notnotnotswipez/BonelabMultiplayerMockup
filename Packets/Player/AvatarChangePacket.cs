using BonelabMultiplayerMockup.Representations;
using BonelabMultiplayerMockup.Utils;
using MelonLoader;
using Steamworks;

namespace BonelabMultiplayerMockup.Packets.Player
{
    public class AvatarChangePacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            AvatarChangeData avatarChangeData = (AvatarChangeData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(SteamIntegration.GetByteId(avatarChangeData.userId));
            packetByteBuf.WriteString(avatarChangeData.barcode);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            var userId = SteamIntegration.GetLongId(packetByteBuf.ReadByte());
            string barcode = packetByteBuf.ReadString();
            
            MelonLogger.Msg(userId+" sent a request to change their avatar to barcode: "+barcode);
            
            
            if (PlayerRepresentation.representations.ContainsKey(userId))
            {
                var playerRepresentation = PlayerRepresentation.representations[userId];
                MelonLogger.Msg("Changing avatar on player rep");
                playerRepresentation.SetAvatar(barcode);
            }
        }
    }

    public class AvatarChangeData : MessageData
    {
        public string barcode;
        public SteamId userId;
    }
}