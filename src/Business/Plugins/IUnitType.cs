using IsometricGame.Business.Plugins.Enums;
using IsometricGame.Logic.Models;

namespace IsometricGame.Business.Plugins
{
    public interface IUnitType
    {
        UnitType UnitType { get; }
        int MoveDistance { get; }
        ServerTurnDelta.BattleResult Attack(UnitType defenderUnitType);
    }
}
