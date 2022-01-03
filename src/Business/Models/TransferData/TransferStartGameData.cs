using IsometricGame.Business.Plugins.Enums;
using System.Collections.Generic;

namespace IsometricGame.Business.Models.TransferData
{
    public class TransferStartGameData
    {
        public float? TurnTimeout;
        public List<UnitType> AvailableUnits;
        public int MapWidth;
        public int StartHeight;
        public int PlayerId;
    }
}
