using IsometricGame.Business.Plugins.Enums;
using System.Collections.Generic;

namespace IsometricGame.Business.Models.TransferData
{
    public class TransferConnectData
    {
        public List<UnitData> Units;

        public class UnitData
        {
            public UnitType UnitType;
            public int X;
            public int Y;
        }
    }
}