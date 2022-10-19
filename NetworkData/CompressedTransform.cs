using System;
using BonelabMultiplayerMockup.Packets;
using MelonLoader;
using UnityEngine;

namespace BonelabMultiplayerMockup.NetworkData
{
    public class CompressedTransform
    {
	    
        public Vector3 position;
        public Quaternion rotation;
        
        private const float FLOAT_PRECISION_MULT = 32767f;
        private PacketByteBuf _packetByteBuf = null;
        public static int length = 19;

        public CompressedTransform(byte[] bytes)
        {
	        PacketByteBuf packetByteBuf = new PacketByteBuf(bytes);
	        Read(packetByteBuf);
	        _packetByteBuf = packetByteBuf;
        }

        public CompressedTransform(Vector3 position, Quaternion rotation)
        {
	        this.position = position;
	        this.rotation = rotation;
	        PacketByteBuf packetByteBuf = new PacketByteBuf();
	        packetByteBuf.WriteBytes(CompressPosition());
	        packetByteBuf.WriteBytes(CompressQuaternion());
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

        public void Read(PacketByteBuf packetByteBuf)
        {
	        position.x = packetByteBuf.ReadFloat();
	        position.y = packetByteBuf.ReadFloat();
	        position.z = packetByteBuf.ReadFloat();
	        rotation = ReadCompressedRotation(packetByteBuf);
        }
        
        // https://gist.github.com/StagPoint/bb7edf61c2e97ce54e3e4561627f6582 -- Thanks for this! (Smallest 3 Compression!)
        private byte[] CompressQuaternion()
        {
	        PacketByteBuf packetByteBuf = new PacketByteBuf();
	        var maxIndex = (byte)0;
	        var maxValue = float.MinValue;
	        var sign = 1f;

	        for (int i = 0; i < 4; i++)
	        {
		        var element = rotation[i];
		        var abs = Mathf.Abs(rotation[i]);
		        if (abs > maxValue)
		        {
			        sign = (element < 0) ? -1 : 1;
			        maxIndex = (byte)i;
			        maxValue = abs;
		        }
	        }

	        if (Mathf.Approximately(maxValue, 1f))
	        {
		        packetByteBuf.WriteByte((byte)(maxIndex + 4));
		        packetByteBuf.create();
		        return packetByteBuf.getBytes();
	        }

	        var a = (short)0;
	        var b = (short)0;
	        var c = (short)0;

	        if (maxIndex == 0)
	        {
		        a = (short)(rotation.y * sign * FLOAT_PRECISION_MULT);
		        b = (short)(rotation.z * sign * FLOAT_PRECISION_MULT);
		        c = (short)(rotation.w * sign * FLOAT_PRECISION_MULT);
	        }
	        else if (maxIndex == 1)
	        {
		        a = (short)(rotation.x * sign * FLOAT_PRECISION_MULT);
		        b = (short)(rotation.z * sign * FLOAT_PRECISION_MULT);
		        c = (short)(rotation.w * sign * FLOAT_PRECISION_MULT);
	        }
	        else if (maxIndex == 2)
	        {
		        a = (short)(rotation.x * sign * FLOAT_PRECISION_MULT);
		        b = (short)(rotation.y * sign * FLOAT_PRECISION_MULT);
		        c = (short)(rotation.w * sign * FLOAT_PRECISION_MULT);
	        }
	        else
	        {
		        a = (short)(rotation.x * sign * FLOAT_PRECISION_MULT);
		        b = (short)(rotation.y * sign * FLOAT_PRECISION_MULT);
		        c = (short)(rotation.z * sign * FLOAT_PRECISION_MULT);
	        }

	        packetByteBuf.WriteByte(maxIndex);
	        packetByteBuf.WriteShort(a);
	        packetByteBuf.WriteShort(b);
	        packetByteBuf.WriteShort(c);
	        packetByteBuf.create();

	        return packetByteBuf.getBytes();
        }

        public static Quaternion ReadCompressedRotation( PacketByteBuf packetByteBuf )
        {
	        var maxIndex = packetByteBuf.ReadByte();
	        
	        if( maxIndex >= 4 && maxIndex <= 7 )
	        {
		        var x = ( maxIndex == 4 ) ? 1f : 0f;
		        var y = ( maxIndex == 5 ) ? 1f : 0f;
		        var z = ( maxIndex == 6 ) ? 1f : 0f;
		        var w = ( maxIndex == 7 ) ? 1f : 0f;

		        return new Quaternion( x, y, z, w );
	        }
	        
	        var a = (float)packetByteBuf.ReadShort() / FLOAT_PRECISION_MULT;
	        var b = (float)packetByteBuf.ReadShort() / FLOAT_PRECISION_MULT;
	        var c = (float)packetByteBuf.ReadShort() / FLOAT_PRECISION_MULT;
	        var d = Mathf.Sqrt( 1f - ( a * a + b * b + c * c ) );

	        if( maxIndex == 0 )
		        return new Quaternion( d, a, b, c ); 
	        if( maxIndex == 1 )
		        return new Quaternion( a, d, b, c );
	        if( maxIndex == 2 )
		        return new Quaternion( a, b, d, c );

	        return new Quaternion( a, b, c, d );
        }
    }
}