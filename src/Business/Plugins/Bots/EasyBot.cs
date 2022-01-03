using FateRandom;
using Godot;
using IsometricGame.Business.Models.TransferData;
using IsometricGame.Business.Plugins.Enums;
using IsometricGame.Logic.ScriptHelpers;
using System.Collections.Generic;
using System.Linq;

namespace IsometricGame.Business.Plugins.Bots
{
    public class EasyBot : IBot
    {
        public class InnerData
        {
            public List<Unit> Units;
            public int MapHeight;
            public int MapWidth;
        }

        public class Unit
        {
            public long FullUnitId;
            public int PlayerId;
            public Vector2 Position;
            public int UnitId;
        }

        public Bot Bot => Bot.Easy;

        public TransferConnectData StartGame(TransferStartGameData startGameData)
        {
            Fate.GlobalFate.Shuffle(startGameData.AvailableUnits);
            var units = new List<TransferConnectData.UnitData>();

            for (var x = 0; x < startGameData.MapWidth; x++)
            {
                for (var y = 0; y < 2; y++)
                {
                    var idx = y * startGameData.MapWidth + x;
                    if (startGameData.AvailableUnits.Count > idx)
                    {
                        var unit = startGameData.AvailableUnits[idx];
                        units.Add(new TransferConnectData.UnitData
                        {
                            UnitType = unit,
                            X = x,
                            Y = y
                        });
                    }
                }
            }

            var tmpUnits = new List<TransferConnectData.UnitData>
            {
                new TransferConnectData.UnitData
                {
                    UnitType = UnitType.Flag, X = 1, Y = 1
                },
                new TransferConnectData.UnitData
                {
                    UnitType = UnitType.Flag, X = 1, Y = 2
                }
            };

            return new TransferConnectData { Units = units };
        }

        public TransferTurnDoneData Initialize(TransferInitialData initialData, Dictionary<int, object> botCache)
        {
            var initData = Convert(initialData);
            botCache[0] = initData;

            if (!initialData.YourTurn)
            {
                return null;
            }

            var myUnits = initData.Units.Where(a => a.PlayerId == initialData.YourPlayerId).ToArray();

            while (true)
            {
                var unit = Fate.GlobalFate.Choose(initialData.YourUnits.ToArray());
                var newPosition = Fate.GlobalFate.Choose(Vector2.Down, Vector2.Down, Vector2.Left, Vector2.Right) + unit.Position;
                if (myUnits.Any(a => a.Position == newPosition))
                {
                    continue;
                }

                if (newPosition.x < 0 || newPosition.y < 0 || newPosition.x >= initData.MapWidth || newPosition.y >= initData.MapHeight)
                {
                    continue;
                }

                return new TransferTurnDoneData
                {
                    UnitId = unit.UnitId,
                    NewX = newPosition.x,
                    NewY = newPosition.y
                };
            }

        }

        public TransferTurnDoneData TurnDone(TransferTurnData turnData, Dictionary<int, object> botCache)
        {
            var initData = (InnerData)botCache[0];
            if (turnData.Moved)
            {
                var movedUnit = initData.Units.First(a => a.FullUnitId == turnData.MovedFullUnitId);
                movedUnit.Position = new Vector2(turnData.MovedX, turnData.MovedY);
            }

            if (turnData.Battle)
            {
                var attackerUnit = initData.Units.First(a => a.FullUnitId == turnData.AttackerFullUnitId);
                var defenderUnit = initData.Units.First(a => a.FullUnitId == turnData.DefenderFullUnitId);

                switch (turnData.BattleWinner)
                {
                    case TransferTurnData.BattleResult.Attacker:
                        initData.Units.Remove(defenderUnit);
                        break;
                    case TransferTurnData.BattleResult.Defender:
                        initData.Units.Remove(attackerUnit);
                        break;
                    case TransferTurnData.BattleResult.Draw:
                        break;
                }
            }

            var myUnits = initData.Units.Where(a => a.PlayerId == turnData.YourPlayerId).ToArray();

            if (!turnData.YourTurn)
            {
                return null;
            }

            while (true)
            {
                var unit = Fate.GlobalFate.Choose(myUnits);
                var newPosition = Fate.GlobalFate.Choose(Vector2.Down, Vector2.Down, Vector2.Left, Vector2.Right) + unit.Position;
                if (myUnits.Any(a => a.Position == newPosition))
                {
                    continue;
                }

                if (newPosition.x < 0 || newPosition.y < 0 || newPosition.x >= initData.MapWidth || newPosition.y >= initData.MapHeight)
                {
                    continue;
                }

                return new TransferTurnDoneData
                {
                    UnitId = unit.UnitId,
                    NewX = newPosition.x,
                    NewY = newPosition.y
                };
            }
        }

        private InnerData Convert(TransferInitialData initialData)
        {
            if (initialData == null)
            {
                return null;
            }

            return new InnerData
            {
                MapWidth = initialData.VisibleMap.GetLength(0),
                MapHeight = initialData.VisibleMap.GetLength(1),
                Units = initialData.YourUnits.Select(a => new Unit
                {
                    FullUnitId = UnitUtils.GetFullUnitId(initialData.YourPlayerId, a.UnitId),
                    PlayerId = initialData.YourPlayerId,
                    UnitId = a.UnitId,
                    Position = a.Position
                }).Concat(initialData.OtherPlayers.SelectMany(a => a.Units.Select(b => new Unit
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