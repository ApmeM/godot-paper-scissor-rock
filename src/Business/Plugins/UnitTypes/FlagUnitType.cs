using IsometricGame.Business.Plugins.Enums;
using IsometricGame.Logic.Models;
using System;

namespace IsometricGame.Business.Plugins.UnitTypes
{
    public class FlagUnitType : IUnitType
    {
        public UnitType UnitType => UnitType.Flag;

        public int MoveDistance => 0;

        public ServerTurnDelta.BattleResult Attack(UnitType defenderUnitType)
        {
            switch (defenderUnitType)
            {
                case UnitType.Stone:
                    return ServerTurnDelta.BattleResult.Defender;
                case UnitType.Scissor:
                    return ServerTurnDelta.BattleResult.Defender;
                case UnitType.Paper:
                    return ServerTurnDelta.BattleResult.Defender;
                case UnitType.Flag:
                    return ServerTurnDelta.BattleResult.Draw;
            }

            throw new NotSupportedException($"FindWinner for UnitType {UnitType}");
        }
    }
}