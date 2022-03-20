using FateRandom;
using Godot;
using IsometricGame.Business.Models.TransferData;
using IsometricGame.Business.Plugins.Enums;
using System.Collections.Generic;
using System.Linq;

namespace IsometricGame.Business.Plugins.Bots
{
    public class VeryEasyBot : IBot
    {
        public Bot Bot => Bot.VeryEasy;

        public TransferConnectData StartGame(TransferStartGameData startGameData)
        {
            var units = new List<TransferConnectData.UnitData>
            {
                new TransferConnectData.UnitData
                {
                    UnitType = UnitType.Flag,
                    X = 0,
                    Y = 0
                },
                new TransferConnectData.UnitData
                {
                    UnitType = UnitType.Scissor,
                    X = 1,
                    Y = 1
                }
            };

            return new TransferConnectData { Units = units };
        }

        public TransferTurnDoneData Initialize(TransferInitialData initialData, Dictionary<int, object> botCache)
        {
            var initData = InnerData.Convert(initialData);
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
    }
}