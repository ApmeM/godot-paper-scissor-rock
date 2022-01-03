using IsometricGame.Logic.Models;

namespace IsometricGame.Logic.ScriptHelpers
{
    public static class UnitUtils
    {
        public static long GetFullUnitId(GameData.ServerUnit unit)
        {
            return GetFullUnitId(unit.Player.PlayerId, unit.UnitId);
        }

        public static long GetFullUnitId(int playerId, int unitId)
        {
            return ((long)playerId << 32) | ((long)unitId & 0xFFFFFFFFL);
        }

        public static int GetPlayerId(long abilityFullUnitId)
        {
            return (int)(abilityFullUnitId >> 32);
        }

        public static int GetUnitId(long abilityFullUnitId)
        {
            return (int)abilityFullUnitId;
        }
    }
}
