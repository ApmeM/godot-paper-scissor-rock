using Godot;
using IsometricGame.Business.Models.TransferData;
using IsometricGame.Business.Plugins;
using IsometricGame.Logic.Models;
using IsometricGame.Logic.ScriptHelpers;
using IsometricGame.Repository;
using System;
using System.Linq;
using Vector2 = Godot.Vector2;

namespace IsometricGame.Business.Logic
{
    public class GameLogic
    {
        private static readonly TransferTurnDoneData emptyMoves = new TransferTurnDoneData();
        private readonly PluginUtils pluginUtils;
        private readonly TurnLogic turnLogic;
        private readonly BotRepository botRepository;

        public GameLogic(
            PluginUtils pluginUtils,
            TurnLogic turnLogic,
            BotRepository botRepository)
        {
            this.pluginUtils = pluginUtils;
            this.turnLogic = turnLogic;
            this.botRepository = botRepository;
        }

        public GameData StartForLobby(LobbyData lobby)
        {
            var game = new GameData(lobby.Id);
            game.Configuration.MapType = lobby.Configuration.MapType;
            game.Configuration.TurnTimeout = lobby.Configuration.TurnTimeout;
            var gameType = this.pluginUtils.FindGameType(lobby.Configuration.MapType);
            gameType.PopulateConfig(game.Configuration);

            for (var i = 0; i < lobby.Players.Count; i++)
            {
                var player = lobby.Players[i];

                game.Players.Add(player.PlayerId, new GameData.ServerPlayer
                {
                    ClientId = player.ClientId,
                    PlayerId = player.PlayerId,
                    PlayerNumber = i,
                    PlayerName = player.PlayerName
                });

                if (player.PlayerId < 0)
                {
                    this.botRepository.CreateBot(player.PlayerId, player.Bot);
                }
            }

            return game;
        }

        public bool ConnectToGame(
            GameData game,
            int playerId,
            TransferConnectData connectData,
            Action<TransferInitialData> initialize,
            Action<TransferTurnData> turnDone,
            Action<TransferGameOverData> gameOver)
        {
            if (!game.Players.ContainsKey(playerId))
            {
                GD.PrintErr("Player trying to connect to the game where this player not listed.");
                return false;
            }

            var player = game.Players[playerId];
            if (player.IsConnected)
            {
                GD.Print("Player already connected to this game.");
                return true;
            }

            player.IsConnected = true;
            player.InitializeMethod = (data) => initialize(data);
            player.TurnDoneMethod = (data) => turnDone(data);
            player.GameOverMethod = (data) => gameOver(data);

            var connectedPlayers = game.Players.Count(a => a.Value.IsConnected);
            var newList = game.Configuration.AvailableUnits.ToList();
            foreach (var u in connectData.Units)
            {
                if (u.X >= game.Configuration.Map.GetLength(0) || u.X < 0 || u.Y < 0 || u.Y >= game.Configuration.StartHeight)
                {
                    continue;
                }

                var removed = newList.Remove(u.UnitType);
                if (!removed)
                {
                    continue;
                }

                var unitId = player.Units.Count + 1;
                var unit = new GameData.ServerUnit
                {
                    UnitType = u.UnitType,
                    UnitId = unitId,
                    Player = player,
                    Position = turnLogic.RotateToPlayer(new Vector2(u.X, u.Y), new Vector2(game.Configuration.Map.GetLength(0), game.Configuration.Map.GetLength(1)), player.PlayerNumber)
                };
                player.Units.Add(unitId, unit);
            }

            if (connectedPlayers != game.Players.Count())
            {
                return true;
            }

            foreach (var p in game.Players)
            {
                p.Value.InitializeMethod(GetInitialData(game, p.Key));
            }

            return true;
        }

        public void PlayerMove(GameData game, int forPlayerId, TransferTurnDoneData moves)
        {
            if (!game.Players.ContainsKey(forPlayerId))
            {
                return;
            }

            var player = game.Players[forPlayerId];

            if (game.CurrentPlayerNumber != player.PlayerNumber)
            {
                return;
            }

            /* Initialize turn delta. */
            var unitsTurnDelta = CalculateTurnDelta(game, player, moves);
            if (unitsTurnDelta.Moved)
            {
                player.Units[UnitUtils.GetUnitId(unitsTurnDelta.MovedFullUnitId)].Position = unitsTurnDelta.MovedPosition;
            }

            /* Turn calculation done */
            game.Timeout = game.Configuration.TurnTimeout;
            game.CurrentPlayerNumber = turnLogic.GetNextPlayerNumber(game.CurrentPlayerNumber);
            foreach (var p in game.Players.Values)
            {
                p.TurnDoneMethod(GetTurnData(game, p.PlayerId, unitsTurnDelta));
            }

            /* Check survived units */
            foreach (var p in game.Players.Values.Where(CheckGameOver).ToList())
            {
                p.GameOverMethod(new TransferGameOverData
                {
                    YouWin = false,
                    YourPlayerId = p.PlayerId
                });
                game.Players.Remove(p.PlayerId);
            }

            if (game.Players.Count == 1)
            {
                var p = game.Players.Values.First();
                p.GameOverMethod(new TransferGameOverData
                {
                    YouWin = false,
                    YourPlayerId = p.PlayerId
                });
                game.Players.Clear();
            }
        }

        private ServerTurnDelta CalculateTurnDelta(GameData game, GameData.ServerPlayer player, TransferTurnDoneData moves)
        {
            var result = new ServerTurnDelta();

            var unitId = UnitUtils.GetUnitId(moves.UnitId);

            if (!player.Units.ContainsKey(unitId))
            {
                GD.PrintErr("Invalid move (unit not found) - skip turn.");
                return result;
            }

            var unit = player.Units[unitId];
            var unitType = pluginUtils.FindUnitType(unit.UnitType);

            var move = turnLogic.RotateToPlayer(new Vector2(moves.NewX, moves.NewY), new Vector2(game.Configuration.Map.GetLength(0), game.Configuration.Map.GetLength(1)), player.PlayerNumber);

            if (unit.Position.DistanceSquaredTo(move) > unitType.MoveDistance)
            {
                GD.PrintErr("Invalid move (too far) - skip turn.");
                return result;
            }

            if (move.x < 0 || move.y < 0 || move.x >= game.Configuration.Map.GetLength(0) || move.y >= game.Configuration.Map.GetLength(1))
            {
                GD.PrintErr("Invalid move (outside of map) - skip turn.");
                return result;
            }

            var unitOnCell = game.Players.SelectMany(a => a.Value.Units).Select(a => a.Value).Where(a => a.Position == move).FirstOrDefault();
            if (unitOnCell != null)
            {
                if (unitOnCell.Player.PlayerId == player.PlayerId)
                {
                    GD.PrintErr("Invalid move (move with ship of the same player) - skip turn.");
                    return result;
                }

                result.Battle = true;
                result.AttackerFullUnitId = UnitUtils.GetFullUnitId(unit);
                result.AttackerUnitType = unit.UnitType;
                result.DefenderFullUnitId = UnitUtils.GetFullUnitId(unitOnCell);
                result.DefenderUnitType = unitOnCell.UnitType;
                result.BattleWinner = unitType.Attack(unitOnCell.UnitType);

                switch (result.BattleWinner)
                {
                    case ServerTurnDelta.BattleResult.Draw:
                        {
                            break;
                        }
                    case ServerTurnDelta.BattleResult.Attacker:
                        {
                            unitOnCell.Player.Units.Remove(unitOnCell.UnitId);
                            break;
                        }
                    case ServerTurnDelta.BattleResult.Defender:
                        {
                            unit.Player.Units.Remove(unit.UnitId);
                            break;
                        }
                }
            }
            else
            {
                result.Moved = true;
                result.MovedFullUnitId = UnitUtils.GetFullUnitId(unit);
                result.MovedPosition = move;
            }

            return result;
        }

        public void PlayerExitGame(GameData gameData, int playerId)
        {
            gameData.Players[playerId].Units.Clear();
            PlayerMove(gameData, playerId, emptyMoves);
        }

        private TransferInitialData GetInitialData(GameData game, int forPlayerId)
        {
            var player = game.Players[forPlayerId];
            return new TransferInitialData
            {
                Timeout = game.Configuration.TurnTimeout,
                YourTurn = player.PlayerNumber == game.CurrentPlayerNumber,
                YourPlayerId = forPlayerId,
                YourUnits = player.Units.Select(a => new TransferInitialData.YourUnitsData
                {
                    UnitId = a.Key,
                    Position = this.turnLogic.RotateToPlayer(a.Value.Position, new Vector2(game.Configuration.Map.GetLength(0), game.Configuration.Map.GetLength(1)), game.Players[forPlayerId].PlayerNumber),
                    UnitType = a.Value.UnitType,
                }).ToList(),
                VisibleMap = game.Configuration.Map,
                OtherPlayers = game.Players.Where(a => a.Key != forPlayerId).Select(a => new TransferInitialData.OtherPlayerData
                {
                    PlayerId = a.Key,
                    PlayerName = a.Value.PlayerName,
                    Units = a.Value.Units.Select(b => new TransferInitialData.OtherUnitsData
                    {
                        UnitId = b.Key,
                        Position = this.turnLogic.RotateToPlayer(b.Value.Position, new Vector2(game.Configuration.Map.GetLength(0), game.Configuration.Map.GetLength(1)), game.Players[forPlayerId].PlayerNumber),
                    }).ToList(),
                }).ToList()
            };
        }

        private TransferTurnData GetTurnData(
            GameData game,
            int forPlayer,
            ServerTurnDelta delta)
        {
            var player = game.Players[forPlayer];
            var deltaMove = turnLogic.RotateToPlayer(delta.MovedPosition, new Vector2(game.Configuration.Map.GetLength(0), game.Configuration.Map.GetLength(1)), player.PlayerNumber);
            return new TransferTurnData
            {
                YourTurn = player.PlayerNumber == game.CurrentPlayerNumber,
                YourPlayerId = player.PlayerId,

                Moved = delta.Moved,
                MovedFullUnitId = delta.MovedFullUnitId,
                MovedX = deltaMove.x,
                MovedY = deltaMove.y,

                Battle = delta.Battle,
                DefenderFullUnitId = delta.DefenderFullUnitId,
                DefenderUnitType = delta.DefenderUnitType,
                AttackerFullUnitId = delta.AttackerFullUnitId,
                AttackerUnitType = delta.AttackerUnitType,
                BattleWinner = (TransferTurnData.BattleResult)(int)delta.BattleWinner
            };
        }

        private bool CheckGameOver(GameData.ServerPlayer player)
        {
            return !player.Units.Any(u => u.Value.UnitType == Plugins.Enums.UnitType.Flag);
        }

        public void CheckTimeout(GameData game, float delta)
        {
            if (!game.Configuration.TurnTimeout.HasValue)
            {
                return;
            }

            game.Timeout = game.Timeout ?? game.Configuration.TurnTimeout;
            game.Timeout -= delta;
            if (game.Timeout > 0)
            {
                return;
            }

            // ToDo: if game is in preparetion step - add 0 units and game will be over.
            game.Timeout = game.Configuration.TurnTimeout;
            // ToDo: current player do random move.
            // ToDo: after 2 turn skips - kick player.
            var forceMoveForPlayerId = game.Players.Keys.ToList();
            foreach (var player in forceMoveForPlayerId)
            {
                this.PlayerMove(game, player, emptyMoves);
            }
        }
    }
}
