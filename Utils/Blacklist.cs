namespace BonelabMultiplayerMockup.Utils
{
    public class Blacklist
    {
        public static bool isBlacklisted(string path)
        {
            if (path.Contains("[RigManager (Blank)]")) return true;
            if (path.ToLower().Contains("(playerrep)")) return true;
            if (path.ToLower().StartsWith("cartridge")) return true;

            return false;
        }
    }
}