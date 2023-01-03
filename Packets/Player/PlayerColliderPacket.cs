using System.Collections.Generic;
using BonelabMultiplayerMockup.NetworkData;
using BonelabMultiplayerMockup.Representations;
using BonelabMultiplayerMockup.Utils;
using Steamworks;

namespace BonelabMultiplayerMockup.Packets.Player
{
    public class PlayerColliderPacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            PlayerColliderData data = (PlayerColliderData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(SteamIntegration.GetByteId(data.userId));
            byte size = (byte) data.bones.Count;
            packetByteBuf.WriteByte(size);

            foreach (BoneCacheData playerBone in data.bones) {
                packetByteBuf.WriteByte(playerBone.boneId);
                packetByteBuf.WriteCompressedTransform(playerBone.transform);
            }
            
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            SteamId userId = SteamIntegration.GetLongId(packetByteBuf.ReadByte());
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
                    playerRepresentation.updateColliderTransform(boneCacheData.boneId, boneCacheData.transform.position, boneCacheData.transform.rotation);
                }
                //ThreadedCalculator.QueueCalculation(playerRepresentation, boneId, PlayerPosVariant.BONE, compressedTransform);
            }
        }
    }

    public class PlayerColliderData : MessageData
    {
        public List<BoneCacheData> bones;
        public SteamId userId;
    }
}