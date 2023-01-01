using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BonelabMultiplayerMockup.Nodes;
using BonelabMultiplayerMockup.Object;
using BonelabMultiplayerMockup.Packets;
using BonelabMultiplayerMockup.Packets.Player;
using BonelabMultiplayerMockup.Representations;
using Discord;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using Lobby = Steamworks.Data.Lobby;
using Result = Steamworks.Result;

namespace BonelabMultiplayerMockup
{
    public class SteamIntegration
    {
        public static SteamIntegration Instance;
        public static uint gameAppId = 1592190;

        public string currentName { get; set; }
        public static SteamId currentId { get; set; }
        private string playerSteamIdString;

        public static bool hasLobby = false;

        private string ownerIdIdentifier = "ownerId";

        private bool connectedToSteam = false;

        public static List<ulong> connectedIds = new List<ulong>();
        public static Dictionary<SteamId, Friend> userData = new Dictionary<SteamId, Friend>();
        public static Dictionary<byte, ulong> byteIds = new Dictionary<byte, ulong>();
        public static Dictionary<NetworkChannel, P2PSend> networkChannels = new Dictionary<NetworkChannel, P2PSend>();
        public static Dictionary<NetworkChannel, P2PSend> reliableChannels = new Dictionary<NetworkChannel, P2PSend>();

        public Lobby currentLobby;
        private Lobby hostedMultiplayerLobby;

        public static bool isHost = false;

        private bool applicationHasQuit = false;

        public static byte localByteId = 0;
        public static byte lastByteId = 1;

        public SteamIntegration()
        {
            Instance = this;
            currentName = "";
            // Create client
            SteamClient.Init(gameAppId, true);

            if (!SteamClient.IsValid)
            {
                MelonLogger.Msg("Steam client not valid");
                throw new Exception();
            }

            currentName = SteamClient.Name;
            currentId = SteamClient.SteamId;
            playerSteamIdString = currentId.ToString();
            connectedToSteam = true;
            MelonLogger.Msg("Steam initialized: " + currentName);
            OpenNetworkChannels();
            MelonLogger.Msg("Opened network channels");
            Instance.Start();
            SteamNetworking.AllowP2PPacketRelay(true);
        }

        public IEnumerator WaitToSendIndexes(SteamId friend)
        {
            yield return new WaitForSecondsRealtime(3);
            MelonLogger.Msg("Sending byte indexes to user!");
            foreach (var valuePair in byteIds)
            {
                if (valuePair.Value == currentId) continue;

                var addMessageData = new ShortIdData
                {
                    userId = valuePair.Value,
                    byteId = valuePair.Key
                };
                var packetByteBuf =
                    PacketHandler.CompressMessage(NetworkMessageType.ShortIdUpdatePacket, addMessageData);
                SteamPacketNode.BroadcastMessage((byte)NetworkChannel.Reliable, packetByteBuf.getBytes());
            }

            var idMessageData = new ShortIdData
            {
                userId = friend,
                byteId = RegisterUser(friend)
            };
            var secondBuff = PacketHandler.CompressMessage(NetworkMessageType.ShortIdUpdatePacket, idMessageData);
            SteamPacketNode.BroadcastMessage((byte)NetworkChannel.Reliable, secondBuff.getBytes());

            var joinCatchupData = new JoinCatchupData
            {
                lastId = SyncedObject.lastId,
                lastGroupId = SyncedObject.lastGroupId
            };
            var catchupBuff = PacketHandler.CompressMessage(NetworkMessageType.IdCatchupPacket, joinCatchupData);
            SteamPacketNode.SendMessage(friend, (byte)NetworkChannel.Reliable, catchupBuff.getBytes());
        }

        public static void RegisterUser(byte byteId, ulong longId)
        {
            if (!byteIds.ContainsKey(byteId))
            {
                MelonLogger.Msg("Registered "+longId+" as byte: "+byteId);
                byteIds.Add(byteId, longId);
            }
        }

        public static void Init()
        {
            if (Instance == null)
            {
                SteamIntegration steamIntegration = new SteamIntegration();
            }
        }

        public static void Disconnect(bool fullyShutDown)
        {
            if (isHost)
            {
                DiscordRichPresence.lobbyManager.DeleteLobby(DiscordRichPresence.currentLobby.Id, result => { });
                DiscordRichPresence.currentLobby = new Discord.Lobby();

                DiscordRichPresence.hasLobby = false;
                DiscordRichPresence.DefaultRichPresence();
            }
            else
            {
                DiscordRichPresence.lobbyManager.DisconnectLobby(DiscordRichPresence.currentLobby.Id, result => { });
                DiscordRichPresence.currentLobby = new Discord.Lobby();

                DiscordRichPresence.hasLobby = false;
                DiscordRichPresence.DefaultRichPresence();
            }

            hasLobby = false;
            isHost = false;
            Instance.CleanData();
            Instance.LeaveLobby();
            if (fullyShutDown)
            {
                SteamClient.Shutdown();
            }
        }

        public void OpenNetworkChannels()
        {
            networkChannels.Add(NetworkChannel.Unreliable, P2PSend.UnreliableNoDelay);
            networkChannels.Add(NetworkChannel.Reliable, P2PSend.Reliable);
            networkChannels.Add(NetworkChannel.Object, P2PSend.Reliable);
            networkChannels.Add(NetworkChannel.Attack, P2PSend.Reliable);
            networkChannels.Add(NetworkChannel.Transaction, P2PSend.Reliable);
            
            reliableChannels.Add(NetworkChannel.Reliable, P2PSend.Reliable);
            reliableChannels.Add(NetworkChannel.Object, P2PSend.Reliable);
            reliableChannels.Add(NetworkChannel.Attack, P2PSend.Reliable);
            reliableChannels.Add(NetworkChannel.Transaction, P2PSend.Reliable);
        }

        public void CleanData()
        {
            foreach (PlayerRepresentation rep in PlayerRepresentation.representations.Values)  {
                GameObject.Destroy(rep.playerRep);
            }
            connectedIds.Clear();
            PlayerRepresentation.representations.Clear();
            SyncedObject.CleanData();
            byteIds.Clear();
            userData.Clear();
            lastByteId = 0;
        }

        public bool ConnectedToSteam()
        {
            return connectedToSteam;
        }

        void Start()
        {
            MelonLogger.Msg("Running start method.");
            // Callbacks
            SteamMatchmaking.OnLobbyGameCreated += OnLobbyGameCreatedCallback;
            SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
            SteamMatchmaking.OnLobbyEntered += OnLobbyEnteredCallback;
            SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
            SteamMatchmaking.OnChatMessage += OnChatMessageCallback;
            SteamMatchmaking.OnLobbyMemberDisconnected += OnLobbyMemberDisconnectedCallback;
            SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeaveCallback;
            SteamNetworking.OnP2PSessionRequest += ((steamId) =>
            {
                SteamNetworking.AcceptP2PSessionWithUser(steamId);
            });
            SteamFriends.OnGameLobbyJoinRequested += OnInviteClicked;

            MelonLogger.Msg("Finished registering start method.");
        }

        public void Update()
        {
            SteamClient.RunCallbacks();
        }
        
        void OnLobbyMemberDisconnectedCallback(Lobby lobby, Friend friend)
        {
            ProcessMemberLeft(friend);
        }

        void OnLobbyMemberLeaveCallback(Lobby lobby, Friend friend)
        {
            ProcessMemberLeft(friend);
        }

        private void ProcessMemberLeft(Friend friend)
        {
            if (friend.Id != currentId)
            {
                try
                {
                    PlayerRepresentation playerRepresentation = PlayerRepresentation.representations[friend.Id];
                    playerRepresentation.DeleteRepresentation();
                    connectedIds.Remove(friend.Id);
                    userData.Remove(friend.Id);
                    byteIds.Remove(GetByteId(friend.Id));
                    SteamNetworking.CloseP2PSessionWithUser(friend.Id);
                }
                catch
                {
                    MelonLogger.Msg("Unable to update disconnected player nameplate / process disconnect cleanly");
                }
            }
        }

        void OnLobbyGameCreatedCallback(Lobby lobby, uint ip, ushort port, SteamId steamId)
        {
            // MelonLogger.Msg("Created game.");
        }

        private void AcceptP2P(SteamId playerId)
        {
            try
            {
                SteamNetworking.AcceptP2PSessionWithUser(playerId);
            }
            catch
            {
                MelonLogger.Msg("Unable to accept P2P Session with user");
            }
        }

        void OnChatMessageCallback(Lobby lobby, Friend friend, string message)
        {
            if (friend.Id != currentId)
            {
                MelonLogger.Msg("incoming chat message");
                MelonLogger.Msg(message);
            }
        }
        
        void OnLobbyEnteredCallback(Lobby lobby)
        {
            if (lobby.MemberCount !=
                1)
            {
                hasLobby = true;
                lobby.SendChatString("I have connected to the lobby!");
                currentLobby = lobby;
        
                foreach (var connectedFriend in currentLobby.Members)
                {
                    if (connectedFriend.Id != currentId)
                    {
                        HandlePlayerConnection(connectedFriend);
                    }
                }
            }
        }
        async void OnInviteClicked(Lobby joinedLobby, SteamId id)
        {
            RoomEnter joinedLobbySuccess = await joinedLobby.Join();
            if (joinedLobbySuccess != RoomEnter.Success)
            {
                MelonLogger.Error("Lobby could not be joined.");
            }
            else
            {
                isHost = false;
                hasLobby = true;
                MelonLogger.Msg("Joined lobby.");
                currentLobby = joinedLobby;
            }
        }

        void OnLobbyCreated(Result result, Lobby lobby)
        {
            if (result != Result.OK)
            {
                MelonLogger.Msg("lobby creation result not ok");
                MelonLogger.Msg(result.ToString());
            }
            else
            {
                MelonLogger.Msg("Created lobby successfully");
                isHost = true;
                hasLobby = true;
            }
        }

        void OnLobbyMemberJoined(Lobby lobby, Friend friend)
        {
            // Not us, means its another player.
            if (friend.Id != currentId)
            {
                MelonLogger.Msg(friend.Name+" has joined the lobby.");
                HandlePlayerConnection(friend);

                // Send byte indexing data if we are currently the host.
                if (isHost)
                {
                    MelonCoroutines.Start(WaitToSendIndexes(friend.Id));
                }
            }
        }
        
        public static byte RegisterUser(ulong userId)
        {
            var byteId = lastByteId++;
            RegisterUser(byteId, userId);
            return byteId;
        }
        
        public void HandlePlayerConnection(Friend friend)
        {
            if (connectedIds.Contains(friend.Id))
                return;
             
            AcceptP2P(friend.Id);
            MelonLogger.Msg("Added "+friend.Name+" to connected users.");
            connectedIds.Add(friend.Id);
            
            MelonLogger.Msg("Fetched user: "+friend.Name);
            PlayerRepresentation.representations.Add(friend.Id, new PlayerRepresentation(friend));
            MelonLogger.Msg("Added representation");
            userData.Add(friend.Id, friend);
            MelonLogger.Msg("Added userdata");
            
            
            // This is pointless, but necessary. Its a sort of initializer packet (Dont ask me why this is the way it is.), it prevents a weird amount of lag.
            // Has to be filled with bytes, cannot be empty.
            PacketByteBuf byteFilled = new PacketByteBuf();
            byteFilled.WriteByte(2);
            byteFilled.WriteULong(23456);
            byteFilled.WriteByte(2);
            byteFilled.WriteULong(23456);
            byteFilled.create();

            PacketByteBuf packetByteBuf = PacketHandler.CompressMessage(NetworkMessageType.PlayerGreetingPacket,
                new Utils.Utils.EmptyMessageData(byteFilled));
            SteamPacketNode.SendMessage(friend.Id, NetworkChannel.Reliable, packetByteBuf.getBytes());
        }
        private void LeaveLobby()
        {
            isHost = false;
            hasLobby = false;
            try
            {
                currentLobby.Leave();
            }
            catch
            {
                MelonLogger.Error("Error leaving current lobby");
                return;
            }

            try
            {
                foreach (SteamId connectedId in connectedIds) {
                    SteamNetworking.CloseP2PSessionWithUser(connectedId);
                }
                connectedIds.Clear();
                userData.Clear();
                Instance.CleanData();
            }
            catch
            {
                MelonLogger.Error("Something went wrong when a P2P session was trying to be closed.");
            }
        }

        public async Task CreateLobby()
        {
            if (hasLobby)
            {
                MelonLogger.Msg("Already in a lobby! Cannot create another one.");
                return;
            }

            try
            {
                var createLobbyOutput = await SteamMatchmaking.CreateLobbyAsync(10);
                if (!createLobbyOutput.HasValue)
                {
                    MelonLogger.Error("Lobby was not created properly!");
                    return;
                }

                hostedMultiplayerLobby = createLobbyOutput.Value;
                hostedMultiplayerLobby.SetFriendsOnly();

                currentLobby = hostedMultiplayerLobby;

                isHost = true;
                hasLobby = true;
                
                MelonLogger.Msg("Created lobby.");
                
                DiscordRichPresence.MakeDiscordLobby();
            }
            catch (Exception exception)
            {
                MelonLogger.Msg("Failed to create multiplayer lobby");
                MelonLogger.Msg(exception.ToString());
            }
        }

        public static byte GetByteId(SteamId longId) {
            if (longId == currentId) return localByteId;
            
            return byteIds.FirstOrDefault(o => o.Value == longId).Key;
        }
        public static SteamId GetLongId(byte shortId) {
            if (shortId == 0) return Instance.currentLobby.Owner.Id;

            if (byteIds.ContainsKey(shortId))
            {
                return byteIds[shortId];
            }
            return 1278;
        }
    }
}