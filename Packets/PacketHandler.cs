using System.Collections.Generic;
using BonelabMultiplayerMockup.Packets.Gun;
using BonelabMultiplayerMockup.Packets.Object;
using BonelabMultiplayerMockup.Packets.Player;
using BonelabMultiplayerMockup.Packets.Reset;

namespace BonelabMultiplayerMockup.Packets
{
    public class PacketHandler
    {
        public static Dictionary<NetworkMessageType, NetworkPacket> MessageReaders =
            new Dictionary<NetworkMessageType, NetworkPacket>();

        public static void RegisterPackets()
        {
            MessageReaders.Add(NetworkMessageType.PlayerUpdateMessage, new PlayerBonePacket());
            MessageReaders.Add(NetworkMessageType.ShortIdUpdateMessage, new ShortIdPacket());
            MessageReaders.Add(NetworkMessageType.InitializeSyncMessage, new InitializeSyncPacket());
            MessageReaders.Add(NetworkMessageType.TransformUpdateMessage, new TransformUpdatePacket());
            MessageReaders.Add(NetworkMessageType.OwnerChangeMessage, new OwnerChangePacket());
            MessageReaders.Add(NetworkMessageType.DisconnectMessage, new DisconnectPacket());
            MessageReaders.Add(NetworkMessageType.RequestIdsMessage, new RequestIdsPacket());
            MessageReaders.Add(NetworkMessageType.IdCatchupMessage, new JoinCatchupPacket());
            MessageReaders.Add(NetworkMessageType.AvatarChangeMessage, new AvatarChangePacket());
            MessageReaders.Add(NetworkMessageType.GunStateMessage, new GunStatePacket());
            MessageReaders.Add(NetworkMessageType.MagInsertMessage, new MagInsertMessage());
            MessageReaders.Add(NetworkMessageType.GroupDestroyMessage, new GroupDestroyPacket());
            MessageReaders.Add(NetworkMessageType.AvatarQuestionMessage, new AvatarQuestionPacket());
            MessageReaders.Add(NetworkMessageType.SyncResetMessage, new SyncResetPacket());
        }

        public static void ReadMessage(NetworkMessageType messageType, PacketByteBuf packetByteBuf, long sender)
        {
            var reader = MessageReaders[messageType];
            reader.ReadData(packetByteBuf, sender);
        }

        public static PacketByteBuf CompressMessage(NetworkMessageType messageType, MessageData messageData)
        {
            var packetByteBuf = MessageReaders[messageType].CompressData(messageData);
            var taggedBytes = new List<byte>();
            taggedBytes.Add((byte)messageType);
            foreach (var b in packetByteBuf.getBytes()) taggedBytes.Add(b);
            var finalArray = taggedBytes.ToArray();
            return new PacketByteBuf(finalArray);
        }
    }
}