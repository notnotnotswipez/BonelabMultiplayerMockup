using BonelabMultiplayerMockup.Nodes;
using BonelabMultiplayerMockup.Utils;
using MelonLoader;
using SLZ.Rig;
using Steamworks;

namespace BonelabMultiplayerMockup.Packets.Player
{
    public class AvatarQuestionPacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            // Doesnt matter, question packet.
            AvatarQuestionData avatarQuestionData = (AvatarQuestionData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(SteamIntegration.GetByteId(avatarQuestionData.questioner));
            packetByteBuf.create();
            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            DebugLogger.Msg("Asked a question about this clients avatar, sending response to the server.");
            var userIdToSend = SteamIntegration.GetLongId(packetByteBuf.ReadByte());
            AvatarChangeData avatarChangeData = new AvatarChangeData()
            {
                userId = SteamIntegration.currentId,
                barcode = BoneLib.Player.rigManager._avatarCrate._barcode._id
            };

            PacketByteBuf newBuffer =
                PacketHandler.CompressMessage(NetworkMessageType.AvatarChangePacket, avatarChangeData);
            
            SteamPacketNode.SendMessage(userIdToSend, NetworkChannel.Transaction, newBuffer.getBytes());
        }
    }

    public class AvatarQuestionData : MessageData
    {
        public SteamId questioner;
    }
}