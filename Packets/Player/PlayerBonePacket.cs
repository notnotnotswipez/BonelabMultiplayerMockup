using System;
using System.Collections.Generic;
using BonelabMultiplayerMockup.NetworkData;
using BonelabMultiplayerMockup.Nodes;
using BonelabMultiplayerMockup.Representations;
using BonelabMultiplayerMockup.Utils;
using MelonLoader;
using Steamworks;

namespace BonelabMultiplayerMockup.Packets.Player
{
    public class PlayerBonePacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            var playerSyncMessageData = (PlayerBoneData)messageData;
            var packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(SteamIntegration.GetByteId(playerSyncMessageData.userId));
            byte size = (byte) playerSyncMessageData.bones.Count;
            packetByteBuf.WriteByte(size);

            foreach (BoneCacheData playerBone in playerSyncMessageData.bones) {
                packetByteBuf.WriteByte(playerBone.boneId);
                packetByteBuf.WriteCompressedTransform(playerBone.transform);
            }

            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            if (packetByteBuf.getBytes().Length <= 0)
                throw new IndexOutOfRangeException();

            var userId = SteamIntegration.GetLongId(packetByteBuf.ReadByte());
            
            var size = packetByteBuf.ReadByte();
            List<BoneCacheData> boneCacheDatas = new List<BoneCacheData>();
            for (int i = 0; i < size; i++)
            {
                BoneCacheData boneCacheData = new BoneCacheData();
                boneCacheData.boneId = packetByteBuf.ReadByte();
                boneCacheData.transform = packetByteBuf.ReadCompressedTransform();
                boneCacheDatas.Add(boneCacheData);
            }

            if (PlayerRepresentation.representations.ContainsKey(userId))
            {
                var playerRepresentation = PlayerRepresentation.representations[userId];
                foreach (BoneCacheData boneCacheData in boneCacheDatas) {
                    boneCacheData.transform.Read();
                    playerRepresentation.updateIkTransform(boneCacheData.boneId, boneCacheData.transform.position, boneCacheData.transform.rotation);
                }
                //ThreadedCalculator.QueueCalculation(playerRepresentation, boneId, PlayerPosVariant.BONE, compressedTransform);
            }
        }
    }

    public class BoneCacheData
    {
        public byte boneId;
        public CompressedTransform transform;
    }

    public class PlayerBoneData : MessageData
    {
        public List<BoneCacheData> bones;
        public SteamId userId;
    }
}