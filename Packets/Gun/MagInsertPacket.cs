using BonelabMultiplayerMockup.Object;
using BonelabMultiplayerMockup.Patches;
using BonelabMultiplayerMockup.Utils;
using MelonLoader;
using SLZ.Interaction;
using SLZ.Props.Weapons;
using UnityEngine;

namespace BonelabMultiplayerMockup.Packets.Gun
{
    public class MagInsertPacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            MagInsertMessageData magInsertMessageData = (MagInsertMessageData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteUShort(magInsertMessageData.gunId);
            packetByteBuf.WriteUShort(magInsertMessageData.magId);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            ushort gunId = packetByteBuf.ReadUShort();
            ushort magId = packetByteBuf.ReadUShort();
            
            SyncedObject gunSynced = SyncedObject.GetSyncedObject(gunId);
            SyncedObject magSynced = SyncedObject.GetSyncedObject(magId);
            
            if (gunSynced && magSynced)
            {
                SLZ.Props.Weapons.Gun gun = PoolManager.GetComponentOnObject<SLZ.Props.Weapons.Gun>(gunSynced.gameObject);
                if (gun)
                {
                    Magazine magazine = PoolManager.GetComponentOnObject<Magazine>(magSynced.gameObject);
                    if (magazine)
                    {
                        gun.InstantLoad();
                        gun.CeaseFire();
                        gun.Charge();
                        GameObject.Destroy(magazine.gameObject);
                    }
                }
            }
        }
    }

    public class MagInsertMessageData : MessageData
    {
        public ushort gunId;
        public ushort magId;
    }
}