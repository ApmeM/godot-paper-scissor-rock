using Godot;
using IsometricGame.Business.Models.TransferData;
using IsometricGame.Logic.ScriptHelpers;
using System.Collections.Generic;
using System.Linq;

namespace IsometricGame.Business.Plugins.Bots
{
    public class InnerData
    {
        public List<Unit> Units;
        public int MapHeight;
        public int MapWidth;
        public class Unit
        {
            public long FullUnitId;
            public int PlayerId;
            public Vector2 Position;
            public int UnitId;
        }

        public static InnerData Convert(TransferInitialData initialData)
        {
            if (initialData == null)
            {
                return null;
            }

            return new InnerData
            {
                MapWidth = initialData.VisibleMap.GetLength(0),
                MapHeight = initialData.VisibleMap.GetLength(1),
                Units = initialData.YourUnits.Select(a => new InnerData.Unit
                {
                    FullUnitId = UnitUtils.GetFullUnitId(initialData.YourPlayerId, a.UnitId),
                    PlayerId = initialData.YourPlayerId,
                    UnitId = a.UnitId,
                    Position = a.Position
                }).Concat(initialData.OtherPlayers.SelectMany(a => a.Units.Select(b => new InnerData.Unit
                {
                    FullUnitId = UnitUtils.GetFullUnitId(a.PlayerId, b.UnitId),
                    PlayerId = a.PlayerId,
                    UnitId = b.UnitId,
                    Position = b.Position
                }))).ToList()
            };
        }

    }
}