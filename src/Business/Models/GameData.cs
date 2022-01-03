using System.Collections.Generic;
using IsometricGame.Logic.Enums;
using IsometricGame.Business.Models.TransferData;
using System;
using Godot;
using IsometricGame.Business.Plugins.Enums;

namespace IsometricGame.Logic.Models
{
    public class GameData
    {
        public readonly string Id;

        public readonly GameConfiguration Configuration;

        public int CurrentPlayerNumber;

        public readonly Dictionary<int, ServerPlayer> Players = new Dictionary<int, ServerPlayer>();
		
		public float? Timeout;

        public GameData(string id)
        {
            this.Id = id;
            this.Configuration = new GameConfiguration();
        }

        public class ServerPlayer
        {
            public int ClientId;
            public int PlayerId;
            public string PlayerName;
            public Dictionary<int, ServerUnit> Units = new Dictionary<int, ServerUnit>();
            public bool IsConnected;
            public Action<TransferInitialData> InitializeMethod = (a) => { };
            public Action<TransferTurnData> TurnDoneMethod = (a) => { };
            public Action<TransferGameOverData> GameOverMethod = (a) => { };
            public int PlayerNumber;
        }

        public class ServerUnit
        {
            public int UnitId;
            public ServerPlayer Player;

            public Vector2 Position;
            public UnitType UnitType;
        }

        public class GameConfiguration
        {
            public float? TurnTimeout;
            public GameType MapType;
            public List<UnitType> AvailableUnits;
            public int StartHeight;
            public MapTile[,] Map;
        }
    }
}