using Godot;
using IsometricGame.Business.Plugins.Enums;
using IsometricGame.Logic.Enums;
using System.Collections.Generic;

namespace IsometricGame.Business.Models.TransferData
{
    public class TransferInitialData
    {
        public int YourPlayerId;
        public float? Timeout;
        public List<OtherPlayerData> OtherPlayers;
        public List<YourUnitsData> YourUnits;
        public MapTile[,] VisibleMap;
        public bool YourTurn;

        public class OtherPlayerData
        {
            public int PlayerId;
            public string PlayerName;
            public List<OtherUnitsData> Units;
        }

        public class OtherUnitsData
        {
            public int UnitId;
            public Vector2 Position;
        }

        public class YourUnitsData
        {
            public Vector2 Position;
            public int UnitId;
            public UnitType UnitType;
        }
    }
}
