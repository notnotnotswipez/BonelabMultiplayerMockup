using System;
using MelonLoader;
using UnityEngine;

namespace HBMP.DataType
{
    public struct SimplifiedQuaternion
    {
        // This is a very useful class I have been given permission to use. Its from Entanglement. Thank you Lakatrazz.
        
        public short c1, c2, c3;
        public byte loss; // Lost component in compression

        public const ushort size = sizeof(short) * 3 + sizeof(byte);

        // The amount we multiply / divide by to preserve precision when using shorts
        public const float PRECISION_OFFSET = 10000.0f;

        public static SimplifiedQuaternion SimplifyQuat(Quaternion quat)
        {
            SimplifiedQuaternion simplified;

            // Based on https://gafferongames.com/post/snapshot_compression/
            // Basically compression works by dropping a component that is the lowest absolute value
            // We first add each component to an array, then sort said array from largest to smallest absolute value

            float[] components = { quat.x, quat.y, quat.z, quat.w };

            byte dropped = 0;
            var biggest = 0.0f;
            var sign = 0.0f;
            for (byte c = 0; c < 4; c++)
                if (Math.Abs(components[c]) > biggest)
                {
                    sign = components[c] < 0 ? -1 : 1;

                    dropped = c;
                    biggest = components[c];
                }

            var compressed = new short[3];

            var compIndex = 0;
            for (var c = 0; c < 4; c++)
            {
                if (c == dropped)
                    continue;

                compressed[compIndex++] = (short)(components[c] * sign * PRECISION_OFFSET);
            }

            simplified.c1 = compressed[0];
            simplified.c2 = compressed[1];
            simplified.c3 = compressed[2];
            simplified.loss = dropped;

            return simplified;
        }

        public Quaternion ExpandQuat()
        {
            if (loss >= 4)
            {
                MelonLogger.Error("Too much data loss!");
                return Quaternion.identity;
            }

            float Pow(float x)
            {
                return x * x;
            }

            var f1 = c1 / PRECISION_OFFSET;
            var f2 = c2 / PRECISION_OFFSET;
            var f3 = c3 / PRECISION_OFFSET;

            var f4 = Mathf.Sqrt(1f - Pow(f1) - Pow(f2) - Pow(f3));

            // Still dumb...
            switch (loss)
            {
                case 0:
                    return new Quaternion(f4, f1, f2, f3);

                case 1:
                    return new Quaternion(f1, f4, f2, f3);

                case 2:
                    return new Quaternion(f1, f2, f4, f3);

                case 3:
                    return new Quaternion(f1, f2, f3, f4);
            }

            return Quaternion.identity;
        }
    }
}