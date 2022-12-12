using MelonLoader;

namespace BonelabMultiplayerMockup.Packets.Player
{
    public class PlayerGreetingPacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            Utils.Utils.EmptyMessageData emptyMessageData = (Utils.Utils.EmptyMessageData)messageData;
            return emptyMessageData.internalData;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            // This is some weird initialization thing I need to do for steam networking. If I dont send a few packets before anyone else sends one to me,
            // The game freaks out and goes 1 fps.
        }
    }
}