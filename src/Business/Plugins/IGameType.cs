using Godot;
using IsometricGame.Business.Plugins.Enums;
using IsometricGame.Logic.Models;
using System.Collections.Generic;

namespace IsometricGame.Business.Plugins
{
    public interface IGameType
    {
        GameType GameType { get; }

        void PopulateConfig(GameData.GameConfiguration gameConfiguration);
        IEnumerable<Bot> GetPredefinedBots();
    }
}
