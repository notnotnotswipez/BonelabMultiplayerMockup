using System;
using System.IO;

namespace BonelabMultiplayerMockup.Utils
{
    public class DataDirectory
    {
        // Thanks Entanglement
        public static string persistentPath { get; private set; }

        public static void Initialize()
        {
            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            persistentPath = appdata + "/BLMPDemo/";
            ValidateDirectory(persistentPath);
        }

        public static void ValidateDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public static string GetPath(string appended)
        {
            return persistentPath + appended;
        }
    }
}