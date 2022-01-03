using IsometricGame.Business.Plugins.Enums;
using System.Collections.Generic;
using System.Linq;

namespace IsometricGame.Business.Plugins
{
    public class PluginUtils
    {
        public void Initialize(
            List<IBot> bots,
            List<IGameType> gameTypes,
            List<IUnitType> unitTypes)
        {
            this.SupportedBots = bots.ToDictionary(a => a.Bot);
            this.SupportedGameTypes = gameTypes.ToDictionary(a => a.GameType);
            this.SupportedUnitTypes = unitTypes.ToDictionary(a => a.UnitType);
        }

        private Dictionary<Bot, IBot> SupportedBots;
        private Dictionary<GameType, IGameType> SupportedGameTypes;
        private Dictionary<UnitType, IUnitType> SupportedUnitTypes;

        public IBot FindBot(Bot bot)
        {
            if (!SupportedBots.ContainsKey(bot))
            {
                return null;
            }

            return SupportedBots[bot];
        }

        public IGameType FindGameType(GameType gameType)
        {
            if (!SupportedGameTypes.ContainsKey(gameType))
            {
                return null;
            }

            return SupportedGameTypes[gameType];
        }

        public IUnitType FindUnitType(UnitType unitType)
        {
            if (!SupportedUnitTypes.ContainsKey(unitType))
            {
                return null;
            }

            return SupportedUnitTypes[unitType];
        }
    }
}
