using System.Collections.Generic;
using BonelabMultiplayerMockup.Messages.Handlers.Gun;
using BonelabMultiplayerMockup.Messages.Handlers.Object;
using BonelabMultiplayerMockup.Messages.Handlers.Player;

namespace BonelabMultiplayerMockup.Messages
{
    public class MessageHandler
    {
        public static Dictionary<NetworkMessageType, MessageReader> MessageReaders =
            new Dictionary<NetworkMessageType, MessageReader>();

        public static void RegisterHandlers()
        {
            MessageReaders.Add(NetworkMessageType.PlayerUpdateMessage, new PlayerSyncReader());
            MessageReaders.Add(NetworkMessageType.ShortIdUpdateMessage, new ShortIdMessage());
            MessageReaders.Add(NetworkMessageType.InitializeSyncMessage, new InitializeSyncMessage());
            MessageReaders.Add(NetworkMessageType.TransformUpdateMessage, new TransformUpdateMessage());
            MessageReaders.Add(NetworkMessageType.OwnerChangeMessage, new OwnerChangeMessage());
            MessageReaders.Add(NetworkMessageType.DisconnectMessage, new DisconnectMessage());
            MessageReaders.Add(NetworkMessageType.RequestIdsMessage, new RequestIdsMessage());
            MessageReaders.Add(NetworkMessageType.IdCatchupMessage, new JoinCatchupMessage());
            MessageReaders.Add(NetworkMessageType.AvatarChangeMessage, new AvatarChangeMessage());
            MessageReaders.Add(NetworkMessageType.GunStateMessage, new GunStateMessage());
            MessageReaders.Add(NetworkMessageType.MagInsertMessage, new MagInsertMessage());
            MessageReaders.Add(NetworkMessageType.GroupDestroyMessage, new GroupDestroyMessage());
            MessageReaders.Add(NetworkMessageType.AvatarQuestionMessage, new AvatarQuestionMessage());
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