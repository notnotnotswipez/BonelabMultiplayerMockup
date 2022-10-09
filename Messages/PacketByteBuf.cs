using System;
using System.Collections.Generic;
using System.Text;
using HBMP.DataType;

namespace BonelabMultiplayerMockup.Messages
{
    public class PacketByteBuf
    {
        public int byteIndex;
        private readonly List<byte> byteList = new List<byte>();
        private byte[] bytes;

        public PacketByteBuf(byte[] bytes)
        {
            this.bytes = bytes;
        }

        public PacketByteBuf()
        {
            bytes = new byte[] { };
        }

        public byte[] getBytes()
        {
            return bytes;
        }

        public void WriteBytes(byte[] bytesToAdd)
        {
            foreach (var b in bytesToAdd) byteList.Add(b);
        }

        public byte ReadByte()
        {
            return getBytes()[byteIndex++];
        }

        public SimplifiedTransform ReadSimpleTransform()
        {
            var transformBytes = new List<byte>();
            var finalIndex = byteIndex + SimplifiedTransform.size;
            for (var i = byteIndex; i < finalIndex; i++) transformBytes.Add(getBytes()[i]);
            var simpleTransform = SimplifiedTransform.FromBytes(transformBytes.ToArray());
            byteIndex += SimplifiedTransform.size;

            return simpleTransform;
        }

        public void WriteSimpleTransform(SimplifiedTransform simplifiedTransform)
        {
            foreach (var b in simplifiedTransform.GetBytes()) byteList.Add(b);
        }

        public string ReadString()
        {
            var pathBytes = new byte[getBytes().Length - byteIndex];
            for (var i = 0; i < pathBytes.Length; i++)
                pathBytes[i] = getBytes()[byteIndex++];
            return Encoding.UTF8.GetString(pathBytes);
        }

        public float ReadLong()
        {
            var longNum = BitConverter.ToInt64(getBytes(), byteIndex);
            byteIndex += sizeof(long);
            return longNum;
        }

        public int ReadInt()
        {
            var longNum = BitConverter.ToInt32(getBytes(), byteIndex);
            byteIndex += sizeof(int);
            return longNum;
        }

        public bool ReadBoolean()
        {
            return Convert.ToBoolean(getBytes()[byteIndex++]);
        }

        public ushort ReadUShort()
        {
            var longNum = BitConverter.ToUInt16(getBytes(), byteIndex);
            byteIndex += sizeof(ushort);
            return longNum;
        }

        public void WriteType(NetworkMessageType networkMessageType)
        {
            byteList.Add((byte)networkMessageType);
        }

        public void WriteString(string str)
        {
            var utf8 = Encoding.UTF8.GetBytes(str);
            foreach (var b in utf8) byteList.Add(b);
        }

        public void WriteBool(bool boolean)
        {
            byteList.Add(Convert.ToByte(boolean));
        }

        public void WriteInt(int integer)
        {
            foreach (var b in BitConverter.GetBytes(integer)) byteList.Add(b);
        }

        public void WriteDouble(double doub)
        {
            foreach (var b in BitConverter.GetBytes(doub)) byteList.Add(b);
        }

        public void WriteUShort(ushort shor)
        {
            foreach (var b in BitConverter.GetBytes(shor)) byteList.Add(b);
        }

        public void WriteByte(byte b)
        {
            byteList.Add(b);
        }

        public void WriteLong(long longNum)
        {
            foreach (var b in BitConverter.GetBytes(longNum)) byteList.Add(b);
        }

        public void WriteFloat(float floatNum)
        {
            foreach (var b in BitConverter.GetBytes(floatNum)) byteList.Add(b);
        }

        public void create()
        {
            bytes = byteList.ToArray();
        }
    }
}