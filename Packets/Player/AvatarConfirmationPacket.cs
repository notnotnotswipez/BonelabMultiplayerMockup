using MelonLoader;
using Steamworks;

namespace BonelabMultiplayerMockup.Packets.Player
{
    public class AvatarConfirmationPacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            AvatarConfirmationData avatarConfirmationData = (AvatarConfirmationData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(SteamIntegration.GetByteId(avatarConfirmationData.userId));
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            SteamId steamId = SteamIntegration.GetLongId(packetByteBuf.ReadByte());
            BonelabMultiplayerMockup.idsReadyForPlayerInfo.Add(steamId);
            MelonLogger.Msg(steamId+" is ready for us to send them packets for our player.");
        }
    }

    public class AvatarConfirmationData : MessageData
    {
        public SteamId userId;
    }
}