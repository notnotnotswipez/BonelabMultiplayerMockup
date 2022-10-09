using MelonLoader;

namespace BonelabMultiplayerMockup.Utils
{
    public class DebugLogger
    {
        private static bool debug = false;
        
        public static void Msg(string message)
        {
            if (debug)
            {
                MelonLogger.Msg(message); 
            }
        }
        
        public static void Error(string message)
        {
            if (debug)
            {
                MelonLogger.Error(message); 
            }
        }
    }
}