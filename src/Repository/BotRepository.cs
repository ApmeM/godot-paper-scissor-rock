using IsometricGame.Business.Plugins.Enums;
using System.Collections.Generic;

namespace IsometricGame.Repository
{
    public class BotRepository
    {
        public class BotData
        {
            public readonly Bot Bot;
            public Dictionary<int, object> Cache;

            public BotData(Bot bot)
            {
                this.Bot = bot;
                this.Cache = new Dictionary<int, object>();
            }
        }

        private static int CurrentBotPlayerId = 0;
        private static readonly Dictionary<int, BotData> BotsData = new Dictionary<int, BotData>();

        public void CreateBot(int botId, Bot bot)
        {
            BotsData[botId] = new BotData(bot);
        }

        public BotData GetBot(int botId)
        {
            if (!BotsData.ContainsKey(botId))
            {
                return null;
            }

            return BotsData[botId];
        }

        public void SetBotCache(int botId, Dictionary<int, object> cache)
        {
            if (!BotsData.ContainsKey(botId))
            {
                return;
            }

            BotsData[botId].Cache = cache;
        }

        public void RemoveBot(int botId)
        {
            BotsData.Remove(botId);
        }

        public int GetPlayerIdForNewBot()
        {
            CurrentBotPlayerId--;
            return CurrentBotPlayerId;
        }
    }
}
