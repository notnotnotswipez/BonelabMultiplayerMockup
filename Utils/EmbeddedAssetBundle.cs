using System.IO;
using System.Linq;
using System.Reflection;
using MelonLoader;

namespace BonelabMultiplayerMockup.Utils
{
    public static class EmbeddedAssetBundle
    {
        // Thanks Entanglement
        public static byte[] LoadFromAssembly(Assembly assembly, string name)
        {
            var manifestResources = assembly.GetManifestResourceNames();

            if (manifestResources.Contains(name))
            {
                MelonLogger.Msg("Contains: " + name + " and was found.");
                using (var str = assembly.GetManifestResourceStream(name))
                using (var memoryStream = new MemoryStream())
                {
                    str.CopyTo(memoryStream);
                    return memoryStream.ToArray();
                }
            }

            return null;
        }
    }
}