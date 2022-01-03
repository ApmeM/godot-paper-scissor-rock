using IsometricGame.Business.Models.TransferData;
using IsometricGame.Business.Plugins.Enums;
using IsometricGame.Logic.Models;
using System.Collections.Generic;

namespace IsometricGame.Business.Plugins
{
    public interface IBot
    {
        Bot Bot { get; }
        TransferConnectData StartGame(TransferStartGameData startGameData);
        TransferTurnDoneData Initialize(TransferInitialData initialData, Dictionary<int, object> botCache);
        TransferTurnDoneData TurnDone(TransferTurnData turnData, Dictionary<int, object> botCache);
    }
}
