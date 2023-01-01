using System;
using BonelabMultiplayerMockup.NetworkData;
using BonelabMultiplayerMockup.Nodes;
using BonelabMultiplayerMockup.Representations;
using MelonLoader;
using Steamworks;

namespace BonelabMultiplayerMockup.Packets.Player
{
    public class PlayerBonePacket : NetworkPacket
    {

        public static bool hasAskedAlready = false;

        public override PacketByteBuf CompressData(MessageData messageData)
        {
            var playerSyncMessageData = (PlayerBoneData)messageData;
            var packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(SteamIntegration.GetByteId(playerSyncMessageData.userId));
            packetByteBuf.WriteByte(playerSyncMessageData.boneId);
            packetByteBuf.WriteCompressedTransform(playerSyncMessageData.transform);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            if (packetByteBuf.getBytes().Length <= 0)
                throw new IndexOutOfRangeException();

            var userId = SteamIntegration.GetLongId(packetByteBuf.ReadByte());
            var boneId = packetByteBuf.ReadByte();
            var simplifiedTransform = packetByteBuf.ReadCompressedTransform();

            if (simplifiedTransform == null)
            {
                return;
            }

            if (PlayerRepresentation.representations.ContainsKey(userId))
            {
                var playerRepresentation = PlayerRepresentation.representations[userId];
                playerRepresentation.updateIkTransform(boneId, simplifiedTransform);
            }
        }
    }

    public class PlayerBoneData : MessageData
    {
        public byte boneId;
        public CompressedTransform transform;
        public SteamId userId;
    }
}