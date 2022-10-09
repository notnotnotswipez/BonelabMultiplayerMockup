using System;
using UnityEngine;

namespace HBMP.Extensions
{
    public static class Vector3Extensions
    {
        // This class is needed for the "SimplifiedTransforms" class by Entanglement devs.
        public static byte[] GetBytes(this Vector3 vector3)
        {
            var bytes = new byte[sizeof(float) * 3];

            var index = 0;
            bytes = bytes.AddBytes(BitConverter.GetBytes(vector3.x), ref index);

            bytes = bytes.AddBytes(BitConverter.GetBytes(vector3.y), ref index);

            bytes = bytes.AddBytes(BitConverter.GetBytes(vector3.z), ref index);

            return bytes;
        }

        public static byte[] GetShortBytes(this Vector3 vector3, float decimal_precision = 1000f)
        {
            var bytes = new byte[sizeof(short) * 3];

            var index = 0;
            bytes = bytes.AddBytes(BitConverter.GetBytes((short)(vector3.x * decimal_precision)), ref index);

            bytes = bytes.AddBytes(BitConverter.GetBytes((short)(vector3.y * decimal_precision)), ref index);

            bytes = bytes.AddBytes(BitConverter.GetBytes((short)(vector3.z * decimal_precision)), ref index);

            return bytes;
        }

        public static void FromBytes(this Vector3 vector3, byte[] bytes, int offset = 0)
        {
            var index = offset;

            vector3.x = BitConverter.ToSingle(bytes, index);
            index += sizeof(float);
            vector3.y = BitConverter.ToSingle(bytes, index);
            index += sizeof(float);
            vector3.z = BitConverter.ToSingle(bytes, index);
        }

        public static Vector3 FromShortBytes(this byte[] bytes, int offset = 0, float decimal_precision = 1000f)
        {
            var index = offset;

            var vector3 = Vector3.zero;
            vector3.x = BitConverter.ToInt16(bytes, index);
            index += sizeof(short);

            vector3.y = BitConverter.ToInt16(bytes, index);
            index += sizeof(short);

            vector3.z = BitConverter.ToInt16(bytes, index);
            index += sizeof(short);

            vector3.x /= decimal_precision;

            vector3.y /= decimal_precision;

            vector3.z /= decimal_precision;

            return vector3;
        }

        // Credits to https://forum.unity.com/threads/encoding-vector2-and-vector3-variables-into-single-int-or-float-and-back.448346/
        public static ulong ToULong(this Vector3 vector3)
        {
            //Vectors must stay within the -320.00 to 320.00 range per axis - no error handling is coded here
            //Adds 32768 to get numbers into the 0-65536 range rather than -32768 to 32768 range to allow unsigned
            //Multiply by 100 to get two decimal place
            var xcomp = (ulong)(Mathf.RoundToInt(vector3.x * 100f) + 32768);
            var ycomp = (ulong)(Mathf.RoundToInt(vector3.y * 100f) + 32768);
            var zcomp = (ulong)(Mathf.RoundToInt(vector3.z * 100f) + 32768);

            return xcomp + ycomp * 65536 + zcomp * 4294967296;
        }

        // Credits to https://forum.unity.com/threads/encoding-vector2-and-vector3-variables-into-single-int-or-float-and-back.448346/
        public static Vector3 ToVector3(this ulong u)
        {
            //Get the leftmost bits first. The fractional remains are the bits to the right.
            // 1024 is 2 ^ 10 - 1048576 is 2 ^ 20 - just saving some calculation time doing that in advance
            var z = u / 4294967296;
            var y = (u - z * 4294967296) / 65536;
            var x = u - y * 65536 - z * 4294967296;

            // subtract 512 to move numbers back into the -512 to 512 range rather than 0 - 1024
            return new Vector3((x - 32768f) / 100f, (y - 32768f) / 100f, (z - 32768f) / 100f);
        }
    }
}