using BonelabMultiplayerMockup.Nodes;
using MelonLoader;
using SLZ.Rig;

namespace BonelabMultiplayerMockup.Packets.Player
{
    public class AvatarQuestionPacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            // Doesnt matter, question packet.
            AvatarQuestionData avatarQuestionData = (AvatarQuestionData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(DiscordIntegration.GetByteId(avatarQuestionData.questioner));
            packetByteBuf.create();
            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            MelonLogger.Msg("Asked a question about this clients avatar, sending response to the server.");
            long userIdToSend = DiscordIntegration.GetLongId(packetByteBuf.ReadByte());
            AvatarChangeData avatarChangeData = new AvatarChangeData()
            {
                userId = DiscordIntegration.currentUser.Id,
                barcode = BoneLib.Player.GetRigManager().GetComponentInChildren<RigManager>()._avatarCrate._barcode._id
            };

            PacketByteBuf newBuffer =
                PacketHandler.CompressMessage(NetworkMessageType.AvatarChangeMessage, avatarChangeData);
            
            Node.activeNode.SendMessage(userIdToSend, (byte)NetworkChannel.Transaction, newBuffer.getBytes());
        }
    }

    public class AvatarQuestionData : MessageData
    {
        public long questioner;
    }
}