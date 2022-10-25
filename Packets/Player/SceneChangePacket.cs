using MelonLoader;
using SLZ.Marrow.SceneStreaming;
using UnityEngine.SceneManagement;

namespace BonelabMultiplayerMockup.Packets.Player
{
    public class SceneChangePacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            SceneChangeData sceneChangeData = (SceneChangeData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteString(sceneChangeData.barcode);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            string barcode = packetByteBuf.ReadString();
            if (SceneStreamer.Session.Level._barcode._id != barcode)
            {
                MelonLogger.Msg("Loading to: "+barcode);
                SceneStreamer.Load(barcode);
            }
        }
    }

    public class SceneChangeData : MessageData
    {
        public string barcode;
    }
}