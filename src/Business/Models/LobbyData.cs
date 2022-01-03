using IsometricGame.Business.Plugins.Enums;
using System.Collections.Generic;

namespace IsometricGame.Logic.Models
{
    public class LobbyData
    {
        public string Id { get; private set; }
        public int CreatorPlayerId;
        public List<PlayerData> Players = new List<PlayerData>();
        public LobbyConfiguration Configuration = new LobbyConfiguration();

        public LobbyData(string id)
        {
            this.Id = id;
        }

        public class PlayerData
        {
            public int ClientId;
            public int PlayerId;
            public Bot Bot;
            public string PlayerName;
        }

        public class LobbyConfiguration
        {
            public static float DefaultTurnTimeout = 60;

            public float? TurnTimeout = DefaultTurnTimeout;
            public GameType MapType = GameType.Custom;
        }
    }
}