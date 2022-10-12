namespace BonelabMultiplayerMockup.Packets
{
    public class DebugAssistance
    {
        public static void SimulatePacket(NetworkMessageType netType, MessageData messageData)
        {
            var packetByteBuf = PacketHandler.CompressMessage(netType, messageData);

            var data = packetByteBuf.getBytes();
            var messageType = data[0];
            var realData = new byte[data.Length - sizeof(byte)];

            for (var b = sizeof(byte); b < data.Length; b++)
                realData[b - sizeof(byte)] = data[b];

            var secondBuf = new PacketByteBuf(realData);

            PacketHandler.ReadMessage((NetworkMessageType)messageType, secondBuf, DiscordIntegration.currentUser.Id);
        }
    }
}