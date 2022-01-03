using IsometricGame.Business.Plugins.Enums;

namespace IsometricGame.Business.Models.TransferData
{
    public class TransferTurnData
    {
        public bool YourTurn;
        public int YourPlayerId;

        public bool Moved;
        public long MovedFullUnitId;
        public float MovedY;
        public float MovedX;

        public bool Battle;
        public long DefenderFullUnitId;
        public UnitType DefenderUnitType;
        public long AttackerFullUnitId;
        public UnitType AttackerUnitType;
        public BattleResult BattleWinner;

        public enum BattleResult
        {
            Attacker,
            Defender,
            Draw
        }
    }
}
