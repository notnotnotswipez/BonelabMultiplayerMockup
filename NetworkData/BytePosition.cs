using BonelabMultiplayerMockup.Packets;
using UnityEngine;

namespace BonelabMultiplayerMockup.NetworkData
{
    public class BytePosition
    {
        public Vector3 position;

        private PacketByteBuf _packetByteBuf = null;
        public static int length = 12;

        public BytePosition(byte[] bytes)
        {
            PacketByteBuf packetByteBuf = new PacketByteBuf(bytes);
            Read(packetByteBuf);
            _packetByteBuf = packetByteBuf;
        }

        public BytePosition(Vector3 position)
        {
            this.position = position;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteBytes(CompressPosition());
            packetByteBuf.create();

            _packetByteBuf = packetByteBuf;
        }

        public byte[] GetBytes()
        {
            return _packetByteBuf.getBytes();
        }

        private byte[] CompressPosition()
        {
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            // Float is 4 bytes, 4 * 3 is 12. 12 Total bytes for just the position. Quaternion must compress pretty well.
            packetByteBuf.WriteFloat(position.x);
            packetByteBuf.WriteFloat(position.y);
            packetByteBuf.WriteFloat(position.z);
            packetByteBuf.create();

            return packetByteBuf.getBytes();
        }

        private void Read(PacketByteBuf packetByteBuf)
        {
            position.x = packetByteBuf.ReadFloat();
            position.y = packetByteBuf.ReadFloat();
            position.z = packetByteBuf.ReadFloat();
        }
    }
}