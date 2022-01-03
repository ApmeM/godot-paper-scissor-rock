using IsometricGame.Business.Plugins.Enums;
using IsometricGame.Logic.Models;
using System;

namespace IsometricGame.Business.Plugins.UnitTypes
{
    public class PaperUnitType : IUnitType
    {
        public UnitType UnitType => UnitType.Paper;

        public int MoveDistance => 1;

        public ServerTurnDelta.BattleResult Attack(UnitType defenderUnitType)
        {
            switch (defenderUnitType)
            {
                case UnitType.Stone:
                    return ServerTurnDelta.BattleResult.Attacker;
                case UnitType.Scissor:
                    return ServerTurnDelta.BattleResult.Defender;
                case UnitType.Paper:
                    return ServerTurnDelta.BattleResult.Draw;
                case UnitType.Flag:
                    return ServerTurnDelta.BattleResult.Attacker;
            }

            throw new NotSupportedException($"FindWinner for UnitType {UnitType}");
        }
    }
}