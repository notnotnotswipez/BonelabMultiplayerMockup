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
            MessageReaders.Add(NetworkMessageType.PlayerUpdatePacket, new PlayerBonePacket());
            MessageReaders.Add(NetworkMessageType.ShortIdUpdatePacket, new ShortIdPacket());
            MessageReaders.Add(NetworkMessageType.InitializeSyncPacket, new InitializeSyncPacket());
            MessageReaders.Add(NetworkMessageType.TransformUpdatePacket, new TransformUpdatePacket());
            MessageReaders.Add(NetworkMessageType.OwnerChangePacket, new OwnerChangePacket());
            MessageReaders.Add(NetworkMessageType.DisconnectPacket, new DisconnectPacket());
            MessageReaders.Add(NetworkMessageType.RequestIdsPacket, new RequestIdsPacket());
            MessageReaders.Add(NetworkMessageType.IdCatchupPacket, new JoinCatchupPacket());
            MessageReaders.Add(NetworkMessageType.AvatarChangePacket, new AvatarChangePacket());
            MessageReaders.Add(NetworkMessageType.GunStatePacket, new GunStatePacket());
            MessageReaders.Add(NetworkMessageType.MagInsertPacket, new MagInsertPacket());
            MessageReaders.Add(NetworkMessageType.GroupDestroyPacket, new GroupDestroyPacket());
            MessageReaders.Add(NetworkMessageType.AvatarQuestionPacket, new AvatarQuestionPacket());
            MessageReaders.Add(NetworkMessageType.SyncResetPacket, new SyncResetPacket());
            MessageReaders.Add(NetworkMessageType.PlayerColliderPacket, new PlayerColliderPacket());
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