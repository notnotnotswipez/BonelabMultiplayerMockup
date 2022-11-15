using System.Linq;
using BonelabMultiplayerMockup.Patches;
using SLZ.Bonelab;
using UnityEngine;

namespace BonelabMultiplayerMockup.Packets.World
{
    public class GameControlPacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            GameControlData gameControlData = (GameControlData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteByte((byte)gameControlData.type);
            packetByteBuf.WriteByte((byte)gameControlData.sequence);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            byte type = packetByteBuf.ReadByte();
            int sequence = (int)packetByteBuf.ReadByte();

            if (type == 0)
            {
                GameControl_Descent descentControl = Resources.FindObjectsOfTypeAll<GameControl_Descent>().First();
                if (descentControl != null)
                {
                    GameControlPatches.GameControlVariables.shouldIgnoreGameEvents = true;
                    descentControl.SEQUENCE(sequence);
                    GameControlPatches.GameControlVariables.shouldIgnoreGameEvents = false;
                }
            }
        }
    }

    public enum GameControlTypes : byte
    {
        DESCENT = 0
    }

    public class GameControlData : MessageData
    {
        public GameControlTypes type;
        public int sequence;
    }
}