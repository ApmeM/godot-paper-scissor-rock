using IsometricGame.Logic.Models;
using System.Collections.Generic;
using System.Linq;

namespace IsometricGame.Repository
{
    public class GamesRepository
    {
        private static int LobbyIndex = 0;
        private static readonly Dictionary<string, LobbyData> Lobbies = new Dictionary<string, LobbyData>();
        private static readonly Dictionary<string, GameData> Games = new Dictionary<string, GameData>();
        private static readonly Dictionary<int, string> PlayerLobbies = new Dictionary<int, string>();
        
        public void ClearAllLobby()
        {
            Lobbies.Clear();
        }

        public LobbyData CreateLobby()
        {
            var lobbyId = "Lobby" + LobbyIndex;
            LobbyIndex++;
            var lobby = new LobbyData(lobbyId);
            Lobbies.Add(lobbyId, lobby);
            return lobby;
        }

        public LobbyData FindByIdLobby(string lobbyId)
        {
            if (!Lobbies.ContainsKey(lobbyId))
            {
                return null;
            }

            return Lobbies[lobbyId];
        }

        public void RemoveLobby(string lobbyId)
        {
            Lobbies.Remove(lobbyId);
        }

        public GameData FindForPlayerGame(int playerId)
        {
            if (!PlayerLobbies.ContainsKey(playerId))
            {
                return null;
            }

            var lobbyId = PlayerLobbies[playerId];
            return this.FindByIdGame(lobbyId);
        }

        public List<GameData> GetAllGames()
        {
            return Games.Values.ToList();
        }

        public void RemoveGame(string id)
        {
            Games.Remove(id);
        }

        public GameData FindByIdGame(string lobbyId)
        {
            if (!Games.ContainsKey(lobbyId))
            {
                return null;
            }

            return Games[lobbyId];
        }

        public void AddGame(GameData game)
        {
            Games[game.Id] = game;
        }

        public LobbyData FindForPlayerLobby(int playerId)
        {
            if (!PlayerLobbies.ContainsKey(playerId))
            {
                return null;
            }

            var lobbyId = PlayerLobbies[playerId];
            return this.FindByIdLobby(lobbyId);
        }

        public void DetachPlayerFromGameLobby(int playerId)
        {
            PlayerLobbies.Remove(playerId);
        }

        public void AttachPlayerToGameLobby(int playerId, string lobbyId)
        {
            PlayerLobbies[playerId] = lobbyId;
        }
    }
}
