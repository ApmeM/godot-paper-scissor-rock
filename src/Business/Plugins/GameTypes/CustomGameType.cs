using IsometricGame.Business.Plugins.Enums;
using IsometricGame.Logic.Enums;
using IsometricGame.Logic.Models;
using System.Collections.Generic;

namespace IsometricGame.Business.Plugins.GameTypes
{
    public class CustomGameType : IGameType
    {
        public GameType GameType => GameType.Custom;

        public int StartHeight => 2;

        public IEnumerable<Bot> GetPredefinedBots()
        {
            yield break;
        }

        public void PopulateConfig(GameData.GameConfiguration gameConfiguration)
        {
            var map = new MapTile[9, 9];
            for (var x = 0; x < map.GetLength(0); x++)
                for (var y = 0; y < map.GetLength(1); y++)
                {
                    map[x, y] = MapTile.Path;
                }

            gameConfiguration.Map = map;
            gameConfiguration.AvailableUnits = new List<UnitType>
            {
                UnitType.Paper, UnitType.Scissor, UnitType.Stone,
                UnitType.Paper, UnitType.Scissor, UnitType.Stone,
                UnitType.Paper, UnitType.Scissor, UnitType.Stone,
                UnitType.Paper, UnitType.Scissor, UnitType.Stone,
                UnitType.Paper, UnitType.Scissor, UnitType.Stone,
                UnitType.Flag,
            };
            gameConfiguration.StartHeight = 2;
        }
    }
}