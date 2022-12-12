using BonelabMultiplayerMockup.Nodes;
using BonelabMultiplayerMockup.Packets;
using BonelabMultiplayerMockup.Packets.World;
using HarmonyLib;
using MelonLoader;
using SLZ.Bonelab;

namespace BonelabMultiplayerMockup.Patches
{
    public class GameControlPatches
    {
        public class GameControlVariables
        {
            public static bool shouldIgnoreGameEvents = false;
        }

        [HarmonyPatch(typeof(GameControl_Descent), "SEQUENCE", typeof(int))]
        private class DescentGameControlPatch
        {
            public static void Prefix(GameControl_Descent __instance, int gate_index)
            {
                if (SteamIntegration.hasLobby && !GameControlVariables.shouldIgnoreGameEvents)
                {
                    MelonLogger.Msg("Descent sequence triggered: "+gate_index);
                    var gameControl = new GameControlData()
                    {
                        type = GameControlTypes.DESCENT,
                        sequence = gate_index
                    };
                    var packetByteBuf = PacketHandler.CompressMessage(NetworkMessageType.GameControlPacket,
                        gameControl);
                    SteamPacketNode.BroadcastMessage(NetworkChannel.Object, packetByteBuf.getBytes());
                }
            }
        }
    }
}