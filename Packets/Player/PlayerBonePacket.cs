using System;
using BonelabMultiplayerMockup.NetworkData;
using BonelabMultiplayerMockup.Nodes;
using BonelabMultiplayerMockup.Representations;
using MelonLoader;

namespace BonelabMultiplayerMockup.Packets.Player
{
    public class PlayerBonePacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            var playerSyncMessageData = (PlayerBoneData)messageData;
            var packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(DiscordIntegration.GetByteId(playerSyncMessageData.userId));
            packetByteBuf.WriteByte(playerSyncMessageData.boneId);
            packetByteBuf.WriteCompressedTransform(playerSyncMessageData.transform);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            if (packetByteBuf.getBytes().Length <= 0)
                throw new IndexOutOfRangeException();

            var userId = DiscordIntegration.GetLongId(packetByteBuf.ReadByte());
            var boneId = packetByteBuf.ReadByte();
            var simplifiedTransform = packetByteBuf.ReadCompressedTransform();

            if (PlayerRepresentation.representations.ContainsKey(userId))
            {
                var playerRepresentation = PlayerRepresentation.representations[userId];
                playerRepresentation.updateIkTransform(boneId, simplifiedTransform);
            }
            else
            {
                MelonLogger.Error(
                    "Something is wrong, player representation sent update but doesnt exist, requesting updates from host.");
                var requestIdsMessageData = new RequestIdsData
                {
                    userId = DiscordIntegration.currentUser.Id
                };
                var shortBuf =
                    PacketHandler.CompressMessage(NetworkMessageType.RequestIdsPacket, requestIdsMessageData);
                Node.activeNode.BroadcastMessage((byte)NetworkChannel.Reliable, shortBuf.getBytes());
            }
        }
    }

    public class PlayerBoneData : MessageData
    {
        public byte boneId;
        public CompressedTransform transform;
        public long userId;
    }
}