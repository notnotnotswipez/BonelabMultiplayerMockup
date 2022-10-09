using System;
using BonelabMultiplayerMockup.Nodes;
using BonelabMultiplayerMockup.Representations;
using HBMP.DataType;
using MelonLoader;

namespace BonelabMultiplayerMockup.Messages.Handlers.Player
{
    public class PlayerSyncReader : MessageReader
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            var playerSyncMessageData = (PlayerSyncMessageData)messageData;
            var packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(DiscordIntegration.GetByteId(playerSyncMessageData.userId));
            packetByteBuf.WriteByte(playerSyncMessageData.boneId);
            packetByteBuf.WriteSimpleTransform(playerSyncMessageData.transform);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            if (packetByteBuf.getBytes().Length <= 0)
                throw new IndexOutOfRangeException();

            var userId = DiscordIntegration.GetLongId(packetByteBuf.ReadByte());
            var boneId = packetByteBuf.ReadByte();
            var simplifiedTransform = packetByteBuf.ReadSimpleTransform();

            if (PlayerRepresentation.representations.ContainsKey(userId))
            {
                var playerRepresentation = PlayerRepresentation.representations[userId];
                playerRepresentation.updateIkTransform(boneId, simplifiedTransform);
            }
            else
            {
                MelonLogger.Error(
                    "Something is wrong, player representation sent update but doesnt exist, requesting updates from host.");
                var requestIdsMessageData = new RequestIdsMessageData
                {
                    userId = DiscordIntegration.currentUser.Id
                };
                var shortBuf =
                    MessageHandler.CompressMessage(NetworkMessageType.RequestIdsMessage, requestIdsMessageData);
                Node.activeNode.BroadcastMessage((byte)NetworkChannel.Reliable, shortBuf.getBytes());
            }
        }
    }

    public class PlayerSyncMessageData : MessageData
    {
        public byte boneId;
        public SimplifiedTransform transform;
        public long userId;
    }
}