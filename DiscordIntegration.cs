using System.Collections.Generic;
using System.Linq;
using Discord;
using MelonLoader;

namespace BonelabMultiplayerMockup
{
    public class DiscordIntegration
    {
        private static Discord.Discord discord;
        public static UserManager userManager;
        public static ActivityManager activityManager;
        public static LobbyManager lobbyManager;
        public static User currentUser;
        public static Activity activity;
        public static Lobby lobby;
        
        // Boilerplate connection code, thanks Entanglement.

        public static Dictionary<byte, long> byteIds = new Dictionary<byte, long>();
        public static byte localByteId = 0;
        public static byte lastByteId = 1;
        public static bool hasLobby => lobby.Id != 0;

        public static bool isHost => hasLobby && lobby.OwnerId == currentUser.Id;

        public static bool isConnected => hasLobby && lobby.OwnerId != currentUser.Id;

        public static void Init()
        {
            discord = new Discord.Discord(1026695411144081518, (ulong)CreateFlags.Default);
            userManager = discord.GetUserManager();
            activityManager = discord.GetActivityManager();
            lobbyManager = discord.GetLobbyManager();
            userManager.OnCurrentUserUpdate += () =>
            {
                currentUser = userManager.GetCurrentUser();
                MelonLogger.Msg($"Current Discord User: {currentUser.Username}");
            };
            DefaultRichPresence();
        }

        public static void Update()
        {
            discord.RunCallbacks();
        }

        public static void Flush()
        {
            lobbyManager.FlushNetwork();
        }

        public static void Tick()
        {
            Update();
            Flush();
        }

        public static void DefaultRichPresence()
        {
            activity = new Activity
            {
                State = "Playing alone",
                Details = "Not connected to a server",
                Instance = true,
                Assets =
                {
                    LargeImage = "blmp"
                }
            };

            activity.Instance = false;

            UpdateActivity();
        }

        public static void RegisterUser(long userId, byte byteId)
        {
            if (byteIds.ContainsKey(byteId)) return;

            MelonLogger.Msg("Registered " + userId + " to byte id: " + byteId);

            byteIds.Add(byteId, userId);
        }

        public static byte CreateByteId()
        {
            return lastByteId++;
        }

        public static void RemoveUser(long userId)
        {
            byteIds.Remove(GetByteId(userId));
        }

        public static byte GetByteId(long longId)
        {
            if (longId == currentUser.Id) return localByteId;

            return byteIds.FirstOrDefault(o => o.Value == longId).Key;
        }

        public static long GetLongId(byte shortId)
        {
            if (shortId == 0) return lobby.OwnerId;

            if (byteIds.ContainsKey(shortId))
            {
                return byteIds[shortId];
            }

            return 1278;
        }

        public static byte RegisterUser(long userId)
        {
            var byteId = CreateByteId();
            RegisterUser(userId, byteId);
            return byteId;
        }


        public static void UpdateActivity()
        {
            activityManager.UpdateActivity(activity, result => { });
        }
    }
}