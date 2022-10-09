namespace BonelabMultiplayerMockup.Utils
{
    public class Blacklist
    {
        public static bool isBlacklisted(string path)
        {
            if (path.Contains("[RigManager (Blank)]")) return true;

            return false;
        }
    }
}