using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using BonelabMultiplayerMockup.Packets;
using MelonLoader;
using Steamworks;

namespace BonelabMultiplayerMockup.Nodes
{
    public class SteamPacketNode
    {
        public static ConcurrentQueue<QueuedPacket> queuedBufs = new ConcurrentQueue<QueuedPacket>();
        public static ConcurrentQueue<QueuedReceived> receivedPackets = new ConcurrentQueue<QueuedReceived>();
        public static ConcurrentQueue<QueuedReceived> cachedUnreliable = new ConcurrentQueue<QueuedReceived>();

        public static long callbackMsTime = 0;
        public static long flushMsTime = 0;

        public static void BroadcastMessage(NetworkChannel channel, byte[] packetByteBuf)
        {
            foreach (var connectedUser in SteamIntegration.connectedIds)
            {
                if (connectedUser == SteamIntegration.currentId) continue;
                queuedBufs.Enqueue(new QueuedPacket()
                {
                    _packetByteBuf = new PacketByteBuf(packetByteBuf),
                    _steamId = connectedUser,
                    channel = channel
                });
            }
        }

        public static void SendMessage(SteamId steamId, NetworkChannel channel, byte[] packetByteBuf)
        {
            if (steamId == SteamIntegration.currentId) return;
            queuedBufs.Enqueue(new QueuedPacket()
            {
                _packetByteBuf = new PacketByteBuf(packetByteBuf),
                _steamId = steamId,
                channel = channel
            });
        }

        public static void Callbacks()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (cachedUnreliable.Count > 0)
            {
                QueuedReceived packets;
                while (!cachedUnreliable.TryDequeue(out packets)) continue;
                try
                {
                    PacketHandler.ReadMessage((NetworkMessageType)packets.networkMessageType, packets.packetByteBuf,
                        0);
                }
                catch (Exception e)
                {
                    MelonLogger.Error(e.ToString());
                }
            }
            
            while (receivedPackets.Count > 0)
            {
                QueuedReceived packets;
                while (!receivedPackets.TryDequeue(out packets)) continue;
                try
                {
                    PacketHandler.ReadMessage((NetworkMessageType)packets.networkMessageType, packets.packetByteBuf,
                        0);
                }
                catch (Exception e)
                {
                    MelonLogger.Error(e.ToString());
                }
            }

            stopwatch.Stop();
            callbackMsTime = stopwatch.ElapsedMilliseconds;
        }

        public class QueuedReceived
        {
            public PacketByteBuf packetByteBuf;
            public NetworkMessageType networkMessageType;
        }

        public class QueuedPacket
        {
            public PacketByteBuf _packetByteBuf;
            public SteamId _steamId;
            public NetworkChannel channel;
        }
    }
}