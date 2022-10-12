using System;

namespace BonelabMultiplayerMockup.Packets.Player
{
    public class ShortIdPacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            var shortIdMessageData = (ShortIdData)messageData;
            var packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteLong(shortIdMessageData.userId);
            packetByteBuf.WriteByte(shortIdMessageData.byteId);
            packetByteBuf.create();
            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            if (packetByteBuf.getBytes().Length <= 0)
                throw new IndexOutOfRangeException();

            var index = 0;
            var userId = BitConverter.ToInt64(packetByteBuf.getBytes(), index);
            index += sizeof(long);

            var byteId = packetByteBuf.getBytes()[index++];

            if (userId == DiscordIntegration.currentUser.Id)
                DiscordIntegration.localByteId = byteId;
            DiscordIntegration.RegisterUser(userId, byteId);
        }
    }

    public class ShortIdData : MessageData
    {
        public byte byteId;
        public long userId;
    }
}