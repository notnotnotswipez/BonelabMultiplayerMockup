using System.Collections;
using System.Collections.Generic;
using BonelabMultiplayerMockup.Object;
using BonelabMultiplayerMockup.Packets.Gun;
using BonelabMultiplayerMockup.Packets.Object;
using BonelabMultiplayerMockup.Packets.Player;
using BonelabMultiplayerMockup.Packets.Reset;
using BonelabMultiplayerMockup.Packets.World;
using MelonLoader;
using SLZ.Marrow.SceneStreaming;
using UnityEngine.SceneManagement;

namespace BonelabMultiplayerMockup.Packets
{
    public class PacketHandler
    {
        public static Dictionary<NetworkMessageType, NetworkPacket> MessageReaders =
            new Dictionary<NetworkMessageType, NetworkPacket>();

        public static bool isQueueingPackets = false;
        private static List<QueuedPacket> _queuedPackets = new List<QueuedPacket>();

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
            MessageReaders.Add(NetworkMessageType.NpcDeathPacket, new NpcDeathPacket());
            MessageReaders.Add(NetworkMessageType.LevelResponsePacket, new LoadedLevelResponsePacket());
            MessageReaders.Add(NetworkMessageType.SceneChangePacket, new SceneChangePacket());
            MessageReaders.Add(NetworkMessageType.SimpleGripEventPacket, new SimpleGripEventPacket());
            MessageReaders.Add(NetworkMessageType.BalloonFirePacket, new BalloonGunFirePacket());
            MessageReaders.Add(NetworkMessageType.PlayerDamagePacket, new PlayerDamagePacket());
            MessageReaders.Add(NetworkMessageType.GrabStatePacket, new GrabStatePacket());
            MessageReaders.Add(NetworkMessageType.PlayerStartGrabPacket, new PlayerStartGrabPacket());
            MessageReaders.Add(NetworkMessageType.PlayerEndGrabPacket, new PlayerEndGrabPacket());
            MessageReaders.Add(NetworkMessageType.GameControlPacket, new GameControlPacket());
            MessageReaders.Add(NetworkMessageType.SpawnRequestPacket, new SpawnRequestPacket());
        }

        public static void ReadMessage(NetworkMessageType messageType, PacketByteBuf packetByteBuf, long sender)
        {
            // Dont read packets if we're in a loading screen. It causes a huge amount of lag and memory issues.
            if (SceneStreamer.Session.Status == StreamStatus.LOADING)
            {
                return;
            }

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

        private class QueuedPacket
        {
            public NetworkMessageType _type;
            public PacketByteBuf buf;
            public long sender;
        }
    }
}