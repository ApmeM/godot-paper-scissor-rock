using Godot;
using IsometricGame.Business.Plugins.Enums;
using IsometricGame.Logic.Enums;
using IsometricGame.Logic.Models;
using System.Collections.Generic;

namespace IsometricGame.Business.Plugins.GameTypes
{
    public class CampaignLevel1GameType : IPresentationGameType
    {
        public GameType GameType => GameType.CampaignLevel1;

        public Vector2 ShootPosition => new Vector2(6,3);

        public string Text => "Level 1";

        public List<Vector2> Position => new List<Vector2>
        {
            new Vector2(5,1),
            new Vector2(7,1),
            new Vector2(7,3),
            new Vector2(5,3),
        };

        public IEnumerable<Bot> GetPredefinedBots()
        {
            yield return Bot.VeryEasy;
        }

        public void PopulateConfig(GameData.GameConfiguration gameConfiguration)
        {
            var map = new MapTile[8, 8];
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