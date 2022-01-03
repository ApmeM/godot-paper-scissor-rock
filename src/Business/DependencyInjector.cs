using IsometricGame.Business.Logic;
using IsometricGame.Business.Plugins;
using IsometricGame.Business.Plugins.Bots;
using IsometricGame.Business.Plugins.GameTypes;
using IsometricGame.Business.Plugins.UnitTypes;
using IsometricGame.Repository;
using System.Collections.Generic;

namespace IsometricGame.Logic.Utils
{
    public static class DependencyInjector
    {
        static DependencyInjector()
        {
            gamesRepository = new GamesRepository();
            accountRepository = new AccountRepository();
            botRepository = new BotRepository();
            ladderRepository = new LadderRepository();
            pluginUtils = new PluginUtils();
            turnLogic = new TurnLogic();
            gameLogic = new GameLogic(pluginUtils, turnLogic, botRepository);
            serverLogic = new ServerLogic(gameLogic, pluginUtils, accountRepository, gamesRepository, botRepository, ladderRepository);

            pluginUtils.Initialize(
                new List<IBot>
                {
                    new EasyBot(),
                },
                new List<IGameType>
                {
                    new CustomGameType(),
                    new CampaignLevel1GameType(),
                },
                new List<IUnitType>
                {
                    new StoneUnitType(),
                    new ScissorUnitType(),
                    new PaperUnitType(),
                    new FlagUnitType(),
                });
        }

        public static PluginUtils pluginUtils { get; }
        public static TurnLogic turnLogic { get; }
        public static GamesRepository gamesRepository { get; }
        public static BotRepository botRepository { get; }
        public static LadderRepository ladderRepository { get; }
        public static AccountRepository accountRepository { get; }
        public static GameLogic gameLogic { get; }
        public static ServerLogic serverLogic { get; }
    }
}
