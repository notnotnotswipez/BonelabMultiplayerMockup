using BonelabMultiplayerMockup.Object;
using BonelabMultiplayerMockup.Utils;
using SLZ.AI;
using UnityEngine.SceneManagement;

namespace BonelabMultiplayerMockup.Packets.Object
{
    public class NpcDeathPacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            NpcDeathData npcDeathData = (NpcDeathData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteUShort(npcDeathData.npcId);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            SyncedObject syncedObject = SyncedObject.GetSyncedObject(packetByteBuf.ReadUShort());
            if (syncedObject)
            {
                AIBrain aiBrain = PoolManager.GetComponentOnObject<AIBrain>(syncedObject.gameObject);
                if (aiBrain != null)
                {
                    aiBrain.puppetMaster.Kill();
                }
            }
        }
    }
    
    public class NpcDeathData : MessageData
    {
        public ushort npcId;
    }
}