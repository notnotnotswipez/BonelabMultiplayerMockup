using BonelabMultiplayerMockup.Nodes;
using BonelabMultiplayerMockup.Object;
using BonelabMultiplayerMockup.Packets.Player;

namespace BonelabMultiplayerMockup.Packets.Reset
{
    public class LoadedLevelResponsePacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            return new PacketByteBuf();
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            if (DiscordIntegration.isHost)
            {
                var joinCatchupData = new JoinCatchupData
                {
                    lastId = SyncedObject.lastId,
                    lastGroupId = SyncedObject.lastGroupId
                };
                var catchupBuff = PacketHandler.CompressMessage(NetworkMessageType.IdCatchupPacket, joinCatchupData);
                Node.activeNode.SendMessage(sender, (byte)NetworkChannel.Reliable, catchupBuff.getBytes());
            }
        }
    }

    public class LoadedLevelResponseData : MessageData
    {
        
    }
}