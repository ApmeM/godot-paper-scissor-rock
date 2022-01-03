using Godot;
using IsometricGame.Business.Plugins.Enums;

namespace IsometricGame.Logic.Models
{
    public class ServerTurnDelta
    {
        public enum BattleResult
        {
            Attacker,
            Defender,
            Draw
        }

        public bool Moved;
        public long MovedFullUnitId;
        public Vector2 MovedPosition;

        public bool Battle;
        public long DefenderFullUnitId;
        public UnitType DefenderUnitType;
        public long AttackerFullUnitId;
        public UnitType AttackerUnitType;
        public BattleResult BattleWinner;
    }
}