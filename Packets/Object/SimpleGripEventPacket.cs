using System.Collections;
using BonelabMultiplayerMockup.Object;
using MelonLoader;
using UnityEngine;

namespace BonelabMultiplayerMockup.Packets.Object
{
    public class SimpleGripEventPacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            SimpleGripEventData simpleGripEventData = (SimpleGripEventData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteUShort(simpleGripEventData.objectId);
            packetByteBuf.WriteByte(simpleGripEventData.gripIndex);
            packetByteBuf.WriteByte(simpleGripEventData.eventIndex);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            ushort objectId = packetByteBuf.ReadUShort();
            byte gripIndex = packetByteBuf.ReadByte();
            byte eventIndex = packetByteBuf.ReadByte();
            
            SyncedObject syncedObject = SyncedObject.GetSyncedObject(objectId);
            if (syncedObject != null)
            {
                SimpleGripEvents selectedGripEvent = null;
                foreach (var storedEvents in syncedObject.gripEvents)
                {
                    if (storedEvents.Value == gripIndex)
                    {
                        selectedGripEvent = storedEvents.Key;
                    }
                }

                if (selectedGripEvent != null)
                {
                    if (eventIndex == 1)
                    {
                        selectedGripEvent.OnIndexDown?.Invoke();
                    }
                    if (eventIndex == 2)
                    {
                        selectedGripEvent.OnMenuTapDown?.Invoke();
                    }
                }
            }
        }
    }

    public class SimpleGripEventData : MessageData
    {
        public ushort objectId;
        public byte gripIndex;
        public byte eventIndex;
    }
}