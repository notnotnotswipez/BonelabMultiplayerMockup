using System.Collections.Generic;

namespace BonelabMultiplayerMockup.Messages
{
    public abstract class MessageReader
    {
        public abstract PacketByteBuf CompressData(MessageData messageData);
        public abstract void ReadData(PacketByteBuf packetByteBuf, long sender);

        public byte[] WriteTypeToBeginning(NetworkMessageType type, PacketByteBuf packetByteBuf)
        {
            var allBytes = new List<byte>();
            allBytes.Add((byte)type);
            foreach (var b in packetByteBuf.getBytes()) allBytes.Add(b);
            var byteArray = allBytes.ToArray();

            return byteArray;
        }
    }

    public abstract class MessageData
    {
    }
}