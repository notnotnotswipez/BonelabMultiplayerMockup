using BonelabMultiplayerMockup.Object;
using MelonLoader;

namespace BonelabMultiplayerMockup.Packets.Reset
{
    public class SyncResetPacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            return new PacketByteBuf();
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            MelonLogger.Msg("Cleaned all data!");
            SyncedObject.CleanData(true);
        }
    }

    public class SyncResetData : MessageData
    {
        // empty.
    }
}