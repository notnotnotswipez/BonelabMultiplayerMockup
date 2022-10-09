using BonelabMultiplayerMockup.Object;
using BonelabMultiplayerMockup.Patches;
using BonelabMultiplayerMockup.Utils;
using MelonLoader;
using SLZ.Interaction;
using UnityEngine;

namespace BonelabMultiplayerMockup.Messages.Handlers.Gun
{
    public class MagInsertMessage : MessageReader
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
                PatchVariables.shouldIgnoreGunEvents = true;
                MelonLogger.Msg("Mag inserted!");
                
                SLZ.Props.Weapons.Gun gun = PoolManager.GetComponentOnObject<SLZ.Props.Weapons.Gun>(gunSynced.gameObject);
                gun.InstantLoad();
                gun.CeaseFire();
                gun.Charge();

                magSynced.DestroySyncable(false);
                if (magSynced.mainReference)
                {
                    GameObject.Destroy(magSynced.mainReference);
                    magSynced.mainReference = null;
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