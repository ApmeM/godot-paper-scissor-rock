using Godot;
using IsometricGame.Business.Models.TransferData;
using IsometricGame.Business.Plugins;
using IsometricGame.Business.Plugins.Enums;
using IsometricGame.Logic.Models;
using IsometricGame.Repository;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace IsometricGame.Business.Logic
{
    public class ServerLogic
    {
        private readonly GameLogic gameLogic;
        private readonly PluginUtils pluginUtils;
        private readonly AccountRepository accountRepository;
        private readonly GamesRepository gamesRepository;
        private readonly BotRepository botRepository;
        private readonly LadderRepository ladderRepository;

        public ServerLogic(
            GameLogic gameLogic,
            PluginUtils pluginUtils,
            AccountRepository accountRepository,
            GamesRepository gamesRepository,
            BotRepository botRepository,
            LadderRepository ladderRepository)
        {
            this.gameLogic = gameLogic;
            this.pluginUtils = pluginUtils;
            this.accountRepository = accountRepository;
            this.gamesRepository = gamesRepository;
            this.botRepository = botRepository;
            this.ladderRepository = ladderRepository;
        }

        public void ProcessTick(float delta, Action<int, int, LobbyData> ladderFound)
        {
            var games = this.gamesRepository.GetAllGames();
            foreach (var game in games)
            {
                this.gameLogic.CheckTimeout(game, delta);
            }

            var pair = this.ladderRepository.FindPair();
            if (pair != null)
            {
                var lobby = this.CreateAndJoinLobby(pair.Item1, pair.Item1, GameType.Custom);
                this.JoinLobby(pair.Item2, lobby.Id);
                ladderFound(pair.Item1, pair.Item2, lobby);
            }
        }

        public void InitializeServer()
        {
            this.gamesRepository.ClearAllLobby();
            this.accountRepository.InitializeActiveLogins();
        }

        public bool Login(int clientId, string login, string password)
        {
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            var credentials = this.accountRepository.LoadCredentials();
            var hash = Convert.ToBase64String(MD5.Create().ComputeHash(Encoding.Unicode.GetBytes($"{login}_{password}")));
            if (!credentials.ContainsKey(login))
            {
                credentials[login] = hash;
                this.accountRepository.AddActiveLogin(clientId, login);
                this.accountRepository.SaveCredentials(credentials);
                return true;
            }
            else if (credentials[login] == hash)
            {
                this.accountRepository.AddActiveLogin(clientId, login);
                return true;
            }
            else
            {
                return false;
            }
        }

        public LobbyData CreateAndJoinLobby(int clientId, int serverId, GameType gameType)
        {
            if(this.gamesRepository.FindForPlayerLobby(clientId) != null ||
                this.gamesRepository.FindForPlayerGame(clientId) != null)
            {
                return null;
            }

            var lobby = this.gamesRepository.CreateLobby();
            lobby.CreatorPlayerId = clientId;
            lobby.Configuration.MapType = gameType;
            this.JoinLobby(clientId, lobby.Id);

            var bots = this.pluginUtils.FindGameType(gameType).GetPredefinedBots();
            foreach (var bot in bots)
            {
                this.AddBot(clientId, serverId, bot);
            }

            return lobby;
        }

        public LobbyData.PlayerData JoinLobby(int clientId, string lobbyId)
        {
            var lobby = this.gamesRepository.FindByIdLobby(lobbyId);
            if (lobby == null)
            {
                return null;
            }

            this.gamesRepository.AttachPlayerToGameLobby(clientId, lobby.Id);
            var playerName = this.accountRepository.FindForClientActiveLogin(clientId);
            var newPlayer = new LobbyData.PlayerData { ClientId = clientId, PlayerId = clientId, PlayerName = playerName };
            lobby.Players.Add(newPlayer);
            return newPlayer;
        }

        public LobbyData.PlayerData AddBot(int creatorPlayerId, int serverId, Bot bot)
        {
            if (!this.IsCreator(creatorPlayerId))
            {
                return null;
            }

            var lobby = this.gamesRepository.FindForPlayerLobby(creatorPlayerId);
            var botPlayerId = botRepository.GetPlayerIdForNewBot();
            this.gamesRepository.AttachPlayerToGameLobby(botPlayerId, lobby.Id);
            var newPlayer = new LobbyData.PlayerData { ClientId = serverId, PlayerId = botPlayerId, PlayerName = bot.ToString(), Bot = bot};
            lobby.Players.Add(newPlayer);
            return newPlayer;
        }

        public bool IsCreator(int playerId)
        {
            var lobby = this.gamesRepository.FindForPlayerLobby(playerId);
            return lobby?.CreatorPlayerId == playerId;
        }

        public void Logout(int playerId)
        {
            this.LeaveLobby(playerId);
            this.LeaveGame(playerId);
            this.ladderRepository.LeaveLadder(playerId);
            this.accountRepository.RemoveActiveLogin(playerId);
        }

        private void LeaveGame(int playerId)
        {
            var game = this.gamesRepository.FindForPlayerGame(playerId);
            if (game == null)
            {
                return;
            }

            this.gameLogic.PlayerExitGame(game, playerId);
            this.gamesRepository.DetachPlayerFromGameLobby(playerId);
        }

        public void LeaveLobby(int playerId)
        {
            var lobby = this.gamesRepository.FindForPlayerLobby(playerId);
            if (lobby == null)
            {
                return;
            }

            lobby.Players.RemoveAll(a => a.PlayerId == playerId);
            this.gamesRepository.DetachPlayerFromGameLobby(playerId);

            if (!lobby.Players.Any() || lobby.Players.All(a => a.PlayerId < 0))
            {
                foreach (var player in lobby.Players)
                {
                    this.gamesRepository.DetachPlayerFromGameLobby(player.PlayerId);
                }

                this.gamesRepository.RemoveLobby(lobby.Id);
            }
        }

        public bool UpdateConfig(int playerId, TransferSyncConfigData newConfiguration)
        {
            if (!this.IsCreator(playerId))
            {
                return false;
            }

            var lobby = this.gamesRepository.FindForPlayerLobby(playerId);
            // Update lobby with newConfiguration
            return true;
        }

        public GameData StartGame(int playerId)
        {
            if (!this.IsCreator(playerId))
            {
                return null;
            }

            var lobby = this.gamesRepository.FindForPlayerLobby(playerId);
            if (lobby == null)
            {
                return null;
            }

            var game = this.gameLogic.StartForLobby(lobby);

            this.gamesRepository.RemoveLobby(lobby.Id);
            this.gamesRepository.AddGame(game);
            return game;
        }

        public void ConnectToGame(int playerId, TransferConnectData connectData,
            Action<TransferInitialData> initialize,
            Action<TransferTurnData> turnDone,
            Action<TransferGameOverData> gameOver
            )
        {
            var game = this.gamesRepository.FindForPlayerGame(playerId);
            this.gameLogic.ConnectToGame(game, playerId, connectData, 
                initialize, 
                turnDone,
                (data) =>
                {
                    gameOver(data);
                    this.gamesRepository.DetachPlayerFromGameLobby(playerId);
                    if (!game.Players.Any())
                    {
                        this.gamesRepository.RemoveGame(game.Id);
                    }
                });
        }

        public void PlayerMove(int playerId, TransferTurnDoneData turnData)
        {
            var game = this.gamesRepository.FindForPlayerGame(playerId);
            this.gameLogic.PlayerMove(game, playerId, turnData);
        }

        public void JoinLadder(int clientId)
        {
            this.ladderRepository.JoinLadder(clientId);
        }

        public void LeaveLadder(int clientId)
        {
            this.ladderRepository.LeaveLadder(clientId);
        }
    }
}
