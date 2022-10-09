using System;
using System.Collections.Generic;
using HBMP.Extensions;
using UnityEngine;

namespace HBMP.DataType
{
    public struct SimplifiedTransform
    {
        // This is a very useful class I have been given permission to use. Its from Entanglement. Thank you Lakatrazz.
        
        public const ushort size = sizeof(float) * 3 + SimplifiedQuaternion.size;
        public const ushort size_small = sizeof(short) * 3 + SimplifiedQuaternion.size;

        public Vector3 position;
        public SimplifiedQuaternion rotation;

        public SimplifiedTransform(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.rotation = SimplifiedQuaternion.SimplifyQuat(rotation);
        }

        public SimplifiedTransform(Transform transform)
        {
            position = transform.position;
            rotation = SimplifiedQuaternion.SimplifyQuat(transform.rotation);
        }

        public byte[] GetBytes()
        {
            var bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(position.x));
            bytes.AddRange(BitConverter.GetBytes(position.y));
            bytes.AddRange(BitConverter.GetBytes(position.z));

            bytes.AddRange(BitConverter.GetBytes(rotation.c1));
            bytes.AddRange(BitConverter.GetBytes(rotation.c2));
            bytes.AddRange(BitConverter.GetBytes(rotation.c3));
            bytes.Add(rotation.loss);

            return bytes.ToArray();
        }

        public byte[] GetSmallBytes(Vector3 root)
        {
            var bytes = new List<byte>();

            bytes.AddRange(root.InverseTransformPosition(position).GetShortBytes());

            bytes.AddRange(BitConverter.GetBytes(rotation.c1));
            bytes.AddRange(BitConverter.GetBytes(rotation.c2));
            bytes.AddRange(BitConverter.GetBytes(rotation.c3));
            bytes.Add(rotation.loss);

            return bytes.ToArray();
        }

        public static SimplifiedTransform SimplyTransform(Transform transform)
        {
            return SimplyTransform(transform.position, transform.rotation);
        }

        public static SimplifiedTransform SimplyTransform(Vector3 position, Quaternion rotation)
        {
            var simplified = new SimplifiedTransform();

            simplified.position = position;
            simplified.rotation = SimplifiedQuaternion.SimplifyQuat(rotation);

            return simplified;
        }

        public static SimplifiedTransform FromBytes(byte[] bytes)
        {
            var transform = new SimplifiedTransform();

            var index = 0;
            transform.position.x = BitConverter.ToSingle(bytes, index);
            index += sizeof(float);
            transform.position.y = BitConverter.ToSingle(bytes, index);
            index += sizeof(float);
            transform.position.z = BitConverter.ToSingle(bytes, index);
            index += sizeof(float);

            transform.rotation.c1 = BitConverter.ToInt16(bytes, index);
            index += sizeof(short);
            transform.rotation.c2 = BitConverter.ToInt16(bytes, index);
            index += sizeof(short);
            transform.rotation.c3 = BitConverter.ToInt16(bytes, index);
            index += sizeof(short);
            transform.rotation.loss = bytes[index];

            return transform;
        }

        public static SimplifiedTransform FromSmallBytes(byte[] bytes, Vector3 root)
        {
            var transform = new SimplifiedTransform();

            var index = 0;
            transform.position = root.TransformPosition(bytes.FromShortBytes(index));
            index += sizeof(short) * 3;

            transform.rotation.c1 = BitConverter.ToInt16(bytes, index);
            index += sizeof(short);
            transform.rotation.c2 = BitConverter.ToInt16(bytes, index);
            index += sizeof(short);
            transform.rotation.c3 = BitConverter.ToInt16(bytes, index);
            index += sizeof(short);
            transform.rotation.loss = bytes[index];

            return transform;
        }
    }
}