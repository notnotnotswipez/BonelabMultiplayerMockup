using System.IO;
using System.Reflection;

namespace BonelabMultiplayerMockup.Utils
{
    public class GameSDK
    {
        // Thanks Entanglement
        public static void LoadGameSDK()
        {
            var sdkPath = DataDirectory.GetPath("discord_game_sdk.dll");
            if (!File.Exists(sdkPath))
                File.WriteAllBytes(sdkPath,
                    EmbeddedAssetBundle.LoadFromAssembly(Assembly.GetExecutingAssembly(),
                        "BonelabMultiplayerMockup.Resources.discord_game_sdk.dll"));
            _ = DllTools.LoadLibrary(sdkPath);
        }
    }
}