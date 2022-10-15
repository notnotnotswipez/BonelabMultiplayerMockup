using System;
using System.Linq;
using BonelabMultiplayerMockup.Representations;
using BoneLib;
using Discord;
using MelonLoader;
using SLZ.Rig;

namespace BonelabMultiplayerMockup.Nodes
{
    public class Client : Node
    {
        // Boilerplate connection code, thanks Entanglement.
        
        public static Client instance;

        public User hostUser;

        public Client()
        {
            DiscordIntegration.activityManager.OnActivityJoin += secret =>
                DiscordIntegration.lobbyManager.ConnectLobbyWithActivitySecret(secret, DiscordJoinLobby);
        }

        public static void StartClient()
        {
            if (instance != null)
                throw new Exception("Can't create another client instance!");
            if (DiscordIntegration.isConnected)
            {
                MelonLogger.Error("Already in a server!");
                return;
            }

            MelonLogger.Msg("Client started!");
            activeNode = instance = new Client();
        }

        public void DiscordJoinLobby(Result result, ref Lobby lobby)
        {
            if (DiscordIntegration.hasLobby)
            {
                MelonLogger.Error("You are already in a lobby!");
                return;
            }

            if (result != Result.Ok)
                return;

            DiscordIntegration.lobby = lobby;
            MelonLogger.Msg("Joined lobby with id: " + lobby.Id);
            MelonLogger.Msg("Connecting to Discord Servers");
            ConnectToDiscordServer();

            DiscordIntegration.userManager.GetUser(lobby.OwnerId, OnDiscordHostUserFetched);

            DiscordIntegration.lobbyManager.OnNetworkMessage += OnDiscordMessageRecieved;
            DiscordIntegration.lobbyManager.OnMemberConnect += OnDiscordUserJoined;
            DiscordIntegration.lobbyManager.OnMemberDisconnect += OnDiscordUserLeft;

            var users = DiscordIntegration.lobbyManager.GetMemberUsers(lobby.Id);

            foreach (var user in users)
                if (user.Id != DiscordIntegration.currentUser.Id && user.Id != lobby.OwnerId)
                    CreatePlayerRep(user.Id);

            DiscordIntegration.activity.Party = new ActivityParty
            {
                Id = lobby.Id.ToString(),
                Size = new PartySize { CurrentSize = users.Count(), MaxSize = (int)lobby.Capacity }
            };

            DiscordIntegration.activity.Details = "This user is connected to a BLMP server!";
            DiscordIntegration.activity.State = "Killing with friends";

            DiscordIntegration.activity.Secrets = new ActivitySecrets
            {
                Join = DiscordIntegration.lobbyManager.GetLobbyActivitySecret(lobby.Id)
            };
            BonelabMultiplayerMockup.PopulateCurrentAvatarData();

            DiscordIntegration.activity.Instance = true;

            DiscordIntegration.UpdateActivity();
        }

        public void OnDiscordHostUserFetched(Result result, ref User user)
        {
            PlayerRepresentation.representations.Add(user.Id, new PlayerRepresentation(user));
            userDatas.Add(user.Id, user);

            hostUser = user;
            MelonLogger.Log($"Joined {hostUser.Username}'s server!");

            DiscordIntegration.RegisterUser(hostUser.Id, 0);
        }

        public override void BroadcastMessage(byte channel, byte[] data)
        {
            BroadcastMessageP2P(channel, data);
        }

        public override void UserConnectedEvent(long lobbyId, long userId)
        {
            DiscordIntegration.activity.Party.Size.CurrentSize = 1 + connectedUsers.Count;
            DiscordIntegration.activityManager.UpdateActivity(DiscordIntegration.activity, res => { });
        }

        public override void UserDisconnectEvent(long lobbyId, long userId)
        {
            DiscordIntegration.activity.Party.Size.CurrentSize = 1 + connectedUsers.Count;
            DiscordIntegration.activityManager.UpdateActivity(DiscordIntegration.activity, res => { });
        }

        public void DisconnectFromServer()
        {
            DiscordIntegration.lobbyManager.DisconnectLobby(DiscordIntegration.lobby.Id, res => { });
            DiscordIntegration.lobby = new Lobby(); // Clear the lobby
            DiscordIntegration.DefaultRichPresence();

            CleanData();
        }

        public override void Shutdown()
        {
            DisconnectFromServer();
        }
    }
}