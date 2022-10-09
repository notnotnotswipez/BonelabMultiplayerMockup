using System.Collections.Generic;
using BonelabMultiplayerMockup.Object;
using HBMP.DataType;

namespace BonelabMultiplayerMockup.Messages.Handlers.Object
{
    public class TransformUpdateMessage : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            var transformUpdateData = (TransformUpdateData)messageData;
            var packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteUShort(transformUpdateData.objectId);
            packetByteBuf.WriteByte(DiscordIntegration.GetByteId(transformUpdateData.userId));
            packetByteBuf.WriteSimpleTransform(transformUpdateData.sTransform);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            var objectId = packetByteBuf.ReadUShort();
            var syncedObject = SyncedObject.GetSyncedObject(objectId);
            if (syncedObject == null)  return;

            var userId = DiscordIntegration.GetLongId(packetByteBuf.ReadByte());
            var transformBytes = new List<byte>();
            for (var i = packetByteBuf.byteIndex; i < packetByteBuf.getBytes().Length; i++)
                transformBytes.Add(packetByteBuf.getBytes()[i]);
            var simpleTransform = SimplifiedTransform.FromBytes(transformBytes.ToArray());

            syncedObject.UpdateObject(simpleTransform);
        }
    }

    public class TransformUpdateData : MessageData
    {
        public ushort objectId;
        public SimplifiedTransform sTransform;
        public long userId;
    }
}