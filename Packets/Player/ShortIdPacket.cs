using System;
using BonelabMultiplayerMockup.Nodes;

namespace BonelabMultiplayerMockup.Packets.Player
{
    public class ShortIdPacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            var shortIdMessageData = (ShortIdData)messageData;
            var packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteULong(shortIdMessageData.userId);
            packetByteBuf.WriteByte(shortIdMessageData.byteId);
            packetByteBuf.create();
            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            if (packetByteBuf.getBytes().Length <= 0)
                throw new IndexOutOfRangeException();

            ulong userId = packetByteBuf.ReadULong();
            var byteId = packetByteBuf.ReadByte();

            if (userId == SteamIntegration.currentId)
                SteamIntegration.localByteId = byteId;

            SteamIntegration.RegisterUser(byteId, userId);
        }
    }

    public class ShortIdData : MessageData
    {
        public byte byteId;
        public ulong userId;
    }
}