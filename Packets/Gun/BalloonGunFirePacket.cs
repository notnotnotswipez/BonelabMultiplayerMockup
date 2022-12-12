using BonelabMultiplayerMockup.Object;
using BonelabMultiplayerMockup.Utils;
using SLZ.Props;
using SLZ.Props.Weapons;

namespace BonelabMultiplayerMockup.Packets.Gun
{
    public class BalloonGunFirePacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            BalloonGunFireData balloonGunFireData = (BalloonGunFireData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteUShort(balloonGunFireData.objectId);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            ushort objectId = packetByteBuf.ReadUShort();
            SyncedObject syncedObject = SyncedObject.GetSyncedObject(objectId);
            if (syncedObject)
            {
                BalloonGun balloonGun = PoolManager.GetComponentOnObject<BalloonGun>(syncedObject.gameObject);
                if (balloonGun)
                {
                    balloonGun.Fire();
                }
            }
        }
    }

    public class BalloonGunFireData : MessageData
    {
        public ushort objectId;
    }
}