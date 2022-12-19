using System.Collections.Generic;
using BonelabMultiplayerMockup.NetworkData;
using BonelabMultiplayerMockup.Object;
using Steamworks;

namespace BonelabMultiplayerMockup.Packets.Object
{
    public class TransformUpdatePacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            var transformUpdateData = (TransformUpdateData)messageData;
            var packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte((byte) transformUpdateData.datas.Count);

            foreach (TransformObjectData transformObjectData in transformUpdateData.datas) {
                packetByteBuf.WriteUShort(transformObjectData.objectId);
                packetByteBuf.WriteCompressedTransform(transformObjectData.compressedTransform);
            }
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {

            var size = packetByteBuf.ReadByte();
            
            for (int i = 0; i < size; i++)
            {
                TransformObjectData transformObjectData = new TransformObjectData();
                transformObjectData.objectId = packetByteBuf.ReadUShort();
                transformObjectData.compressedTransform = packetByteBuf.ReadCompressedTransform();
                transformObjectData.compressedTransform.Read();
                
                var syncedObject = SyncedObject.GetSyncedObject(transformObjectData.objectId);
                if (syncedObject == null)  return;
                
                syncedObject.UpdateObject(transformObjectData.compressedTransform);
            }
        }
    }

    public class TransformObjectData
    {
        public ushort objectId;
        public CompressedTransform compressedTransform;
    }

    public class TransformUpdateData : MessageData
    {
        public List<TransformObjectData> datas;
    }
}