using System.Collections;
using BonelabMultiplayerMockup.NetworkData;
using BonelabMultiplayerMockup.Object;
using BonelabMultiplayerMockup.Utils;
using MelonLoader;
using UnityEngine;

namespace BonelabMultiplayerMockup.Packets.Object
{
    public class SpawnRequestPacket : NetworkPacket
    {

        public IEnumerator WaitForSpawn(GameObject gameObject, string barcode, long userId)
        {
            yield return new WaitForSecondsRealtime(2);
            SyncedObject synced = SyncedObject.Sync(gameObject, false, barcode);
        }

        public override PacketByteBuf CompressData(MessageData messageData)
        {
            SpawnRequestData spawnRequestData = (SpawnRequestData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(DiscordIntegration.GetByteId(spawnRequestData.userId));
            packetByteBuf.WriteBytePosition(spawnRequestData.position);
            packetByteBuf.WriteString(spawnRequestData.barcode);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            if (DiscordIntegration.isHost)
            {
                long userId = DiscordIntegration.GetLongId(packetByteBuf.ReadByte());
                BytePosition bytePosition = packetByteBuf.ReadBytePosition();
                string barcode = packetByteBuf.ReadString();
                
                MelonLogger.Msg(userId+$" is asking us to spawn an object ({barcode}) for them.");
            
                PoolManager.SpawnGameObject(barcode, bytePosition.position, Quaternion.identity, o =>
                {
                    MelonCoroutines.Start(WaitForSpawn(o, barcode, userId));
                }); 
            }
        }
    }

    public class SpawnRequestData : MessageData
    {
        public long userId;
        public BytePosition position;
        public string barcode;
    }
}