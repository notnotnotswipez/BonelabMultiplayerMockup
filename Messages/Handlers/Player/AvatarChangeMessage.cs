using System;
using BonelabMultiplayerMockup.Representations;
using MelonLoader;

namespace BonelabMultiplayerMockup.Messages.Handlers.Player
{
    public class AvatarChangeMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            AvatarChangeMessageData avatarChangeMessageData = (AvatarChangeMessageData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(DiscordIntegration.GetByteId(avatarChangeMessageData.userId));
            packetByteBuf.WriteString(avatarChangeMessageData.barcode);
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

    public class AvatarChangeMessageData : MessageData
    {
        public string barcode;
        public long userId;
    }
}