using System.Collections;
using BonelabMultiplayerMockup.Object;
using BonelabMultiplayerMockup.Patches;
using BonelabMultiplayerMockup.Utils;
using MelonLoader;
using SLZ.Interaction;
using UnityEngine;

namespace BonelabMultiplayerMockup.Packets.Gun
{
    public class GunStatePacket : NetworkPacket
    {
        private bool runningEnumerator = false;
        public IEnumerator IgnoreGunReference()
        {
            if (runningEnumerator)
            {
                yield break;
            }

            runningEnumerator = true;
            yield return new WaitForSeconds(1f);
            PatchVariables.shouldIgnoreGunEvents = false;
            runningEnumerator = false;
        }
        
        public IEnumerator ShootGun(SLZ.Props.Weapons.Gun gun)
        {
            yield return new WaitForSeconds(0.1f);
            gun.Fire();
        }

        public override PacketByteBuf CompressData(MessageData messageData)
        {
            GunStateData gunStateData = (GunStateData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(gunStateData.state);
            packetByteBuf.WriteUShort(gunStateData.objectid);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            byte state = packetByteBuf.ReadByte();
            ushort objectId = packetByteBuf.ReadUShort();
            
            SyncedObject syncedObject = SyncedObject.GetSyncedObject(objectId);
            if (syncedObject != null)
            {
                MelonLogger.Msg("Got gun with state "+state);
                SLZ.Props.Weapons.Gun gun =
                    PoolManager.GetComponentOnObject<SLZ.Props.Weapons.Gun>(syncedObject.gameObject);
                if (gun != null)
                {
                    AmmoSocket ammoSocket = PoolManager.GetComponentOnObject<AmmoSocket>(syncedObject.gameObject);
                    PatchVariables.shouldIgnoreGunEvents = true;
                    MelonLogger.Msg("Sent state: "+state);
                    if (state == 0)
                    {
                        if (ammoSocket.hasMagazine)
                        {
                            GameObject.Destroy(ammoSocket._magazinePlug.magazine.gameObject);
                        }

                        gun.InstantLoad();
                        gun.CeaseFire();
                        gun.Charge();
                        MelonCoroutines.Start(ShootGun(gun));
                    }
                    else if (state == 1)
                    {
                        gun.Charge();
                    }
                    else if (state == 2)
                    {
                        if (gun.HasMagazine())
                        {
                            //PoolManager.GetComponentOnObject<AmmoSocket>(gun.gameObject).EjectMagazine();
                        }
                    }

                    MelonCoroutines.Start(IgnoreGunReference());
                }
            }
        }
    }

    public class GunStateData : MessageData
    {
        public ushort objectid;
        public byte state;
    }
    
    public enum GunStates : byte
    {
        Fire = 0,
        Charge = 1,
        Eject = 2
    }
}