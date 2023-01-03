using System.Linq;
using BonelabMultiplayerMockup.NetworkData;
using BonelabMultiplayerMockup.Representations;
using SLZ;
using Steamworks;

namespace BonelabMultiplayerMockup.Packets.Player
{
    public class PlayerStartGrabPacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            PlayerStartGrabData playerStartGrabData = (PlayerStartGrabData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte(SteamIntegration.GetByteId(playerStartGrabData.userIdGrabber));
            packetByteBuf.WriteByte(playerStartGrabData.hand);
            packetByteBuf.WriteByte(playerStartGrabData.colliderIndex);
            packetByteBuf.WriteCompressedTransform(playerStartGrabData.pelvisAtGrabEvent);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            SteamId userId = SteamIntegration.GetLongId(packetByteBuf.ReadByte());
            byte hand = packetByteBuf.ReadByte();
            byte colliderIndex = packetByteBuf.ReadByte();
            CompressedTransform compressedTransform = packetByteBuf.ReadCompressedTransform();
            compressedTransform.Read();

            var handedness = hand == 1 ? Handedness.RIGHT : Handedness.LEFT;
            if (PlayerRepresentation.representations.ContainsKey(userId))
            {
                BonelabMultiplayerMockup.pelvis.transform.position = compressedTransform.position;
                BonelabMultiplayerMockup.pelvis.transform.rotation = compressedTransform.rotation;
                PlayerRepresentation playerRepresentation = PlayerRepresentation.representations[userId];
                playerRepresentation.GrabClientCollider(colliderIndex, handedness);
            }
        }
    }

    public class PlayerStartGrabData : MessageData
    {
        public SteamId userIdGrabber;
        public byte hand;
        public byte colliderIndex;
        public CompressedTransform pelvisAtGrabEvent;
    }
}