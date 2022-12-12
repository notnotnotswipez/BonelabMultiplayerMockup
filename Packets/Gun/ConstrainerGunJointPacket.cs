using BonelabMultiplayerMockup.NetworkData;
using BonelabMultiplayerMockup.Object;
using BonelabMultiplayerMockup.Utils;
using SLZ.Props;
using SLZ.Props.Weapons;

namespace BonelabMultiplayerMockup.Packets.Gun
{
    public class ConstrainerGunJointPacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            ConstrainerGunJointData constrainerGunJointData = (ConstrainerGunJointData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte((byte)constrainerGunJointData.type);
            packetByteBuf.WriteUShort(constrainerGunJointData.constrainerId);
            packetByteBuf.WriteUShort(constrainerGunJointData.main);
            packetByteBuf.WriteUShort(constrainerGunJointData.cb);
            packetByteBuf.WriteBytePosition(constrainerGunJointData.anchor);
            packetByteBuf.WriteBytePosition(constrainerGunJointData.connectedA);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            byte constrainType = packetByteBuf.ReadByte();
            SyncedObject constrainerGun = SyncedObject.GetSyncedObject(packetByteBuf.ReadUShort());
            SyncedObject mainBody = SyncedObject.GetSyncedObject(packetByteBuf.ReadUShort());
            SyncedObject connectedBody = SyncedObject.GetSyncedObject(packetByteBuf.ReadUShort());
            BytePosition anchorPos = packetByteBuf.ReadBytePosition();
            BytePosition connectedAnchor = packetByteBuf.ReadBytePosition();

            // Make sure the only things we actually connect are... synced.
            if (constrainerGun != null && mainBody != null && connectedBody != null)
            {
                Constrainer constrainer = PoolManager.GetComponentOnObject<Constrainer>(constrainerGun.gameObject);
                if (constrainer)
                {
                    if (constrainType == 0)
                    {
                        constrainer.JointTether(mainBody._rigidbody, connectedBody._rigidbody, anchorPos.position,
                            connectedAnchor.position);
                    }
                    if (constrainType == 1)
                    {
                        constrainer.JointWeld(mainBody._rigidbody, connectedBody._rigidbody, anchorPos.position,
                            connectedAnchor.position);
                    }
                    if (constrainType == 2)
                    {
                        // I honestly do NOT feel like adding an extra byte just for this one joint, if this is an issue I will change it.
                        constrainer.JointBallSocket(mainBody._rigidbody, connectedBody._rigidbody, anchorPos.position,
                            connectedAnchor.position, false);
                    }
                    if (constrainType == 3)
                    {
                        constrainer.JointElastic(mainBody._rigidbody, connectedBody._rigidbody, anchorPos.position,
                            connectedAnchor.position);
                    }
                    if (constrainType == 4)
                    {
                        constrainer.JointEntangleRotation(mainBody._rigidbody, connectedBody._rigidbody, anchorPos.position,
                            connectedAnchor.position);
                    }
                    if (constrainType == 5)
                    {
                        constrainer.JointEntangleVelocity(mainBody._rigidbody, connectedBody._rigidbody, anchorPos.position,
                            connectedAnchor.position);
                    }
                }
            }
        }
    }

    public class ConstrainerGunJointData : MessageData
    {
        public ConstrainerJointTypes type;
        public ushort constrainerId;
        public ushort main;
        public ushort cb;
        public BytePosition anchor;
        public BytePosition connectedA;
    }

    public enum ConstrainerJointTypes : byte
    {
        Tether = 0,
        Weld = 1,
        BallSocket = 2,
        Elastic = 3,
        // THEY PUT ENTANGLEMENT INTO BONELAB?????? multiplayer confirmed.....
        EntangleRotation = 4,
        EntangleVelocity = 5
    }
}