using BrainAI.Pathfinding.BreadthFirst;
using Godot;
using IsometricGame.Business.Logic;
using IsometricGame.Business.Models.TransferData;
using IsometricGame.Business.Plugins;
using IsometricGame.Business.Plugins.Enums;
using IsometricGame.Logic.Enums;
using IsometricGame.Logic.ScriptHelpers;
using IsometricGame.Logic.Utils;
using IsometricGame.Presentation;
using System.Collections.Generic;
using System.Linq;

[SceneReference("Battle.tscn")]
public partial class Battle : Node2D
{
    private float? Timeout;
    private float? MaxTimeout;
    private MapTile[,] visibleMap;

    [Export]
    public PackedScene UnitScene;

    private Communicator communicator;
    private readonly TurnLogic turnLogic;
    private readonly PluginUtils pluginUtils;

    public Battle()
    {
        this.turnLogic = DependencyInjector.turnLogic;
        this.pluginUtils = DependencyInjector.pluginUtils;
    }

    public override void _Ready()
    {
        base._Ready();
        this.FillMembers();
        
        this.communicator = GetNode<Communicator>("/root/Communicator");
        this.maze.Connect(nameof(Maze.CellSelected), this, nameof(CellSelected));
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
        if (Timeout.HasValue)
        {
            Timeout -= delta;
            this.timeoutLabel.Text = $"{(int)Timeout}";
        }
    }

    public void Initialize(TransferInitialData initialData)
    {
        this.Timeout = initialData.Timeout;
        this.MaxTimeout = initialData.Timeout;
        this.visibleMap = initialData.VisibleMap;
        this.maze.Initialize();

        this.maze.AddWater(new Rect2(-1, -1, 12, 14));

        for (var x = 0; x < initialData.VisibleMap.GetLength(0); x++)
            for (var y = 0; y < initialData.VisibleMap.GetLength(1); y++)
            {
                if (initialData.VisibleMap[x, y] == MapTile.Wall)
                {
                    this.maze.AddBeach(new Vector2(x, y) + Vector2.One);
                }
            }

        this.maze.AddBeach(new Rect2(-1, -1, 2, 15));
        this.maze.AddBeach(new Rect2(initialData.VisibleMap.GetLength(0) + 1, -1, 2, 15));
        this.maze.AddWater(new Rect2(-1, -1, initialData.VisibleMap.GetLength(0) + 3, 2), true);
        this.maze.AddWater(new Rect2(-1, initialData.VisibleMap.GetLength(1) + 1, initialData.VisibleMap.GetLength(0) + 3, 2), true);

        var allUnits = new List<Unit>();

        foreach (var unit in initialData.YourUnits)
        {
            var mapPos = this.turnLogic.RotateToPlayer(unit.Position, new Vector2(visibleMap.GetLength(0), visibleMap.GetLength(1)), 1) + Vector2.One;
            
            var unitSceneInstance = (Unit)UnitScene.Instance();
            unitSceneInstance.FullUnitId = UnitUtils.GetFullUnitId(initialData.YourPlayerId, unit.UnitId);
            unitSceneInstance.UnitType = unit.UnitType;
            unitSceneInstance.PlayerNumber = 1;
            unitSceneInstance.Rotation = Mathf.Pi;
            unitSceneInstance.Position = this.maze.GetSpritePositionForCell(mapPos + Vector2.Down * 4);
            unitSceneInstance.AddToGroup(Groups.MyUnits);
            unitSceneInstance.AddToGroup(Groups.AllUnits);
            this.maze.AddChild(unitSceneInstance);
            allUnits.Add(unitSceneInstance);
            unitSceneInstance.MoveUnitTo(mapPos);
        }

        foreach (var player in initialData.OtherPlayers)
        {
            foreach (var unit in player.Units)
            {
                var mapPos = this.turnLogic.RotateToPlayer(unit.Position, new Vector2(visibleMap.GetLength(0), visibleMap.GetLength(1)), 1) + Vector2.One;
                
                var unitSceneInstance = (Unit)UnitScene.Instance();
                unitSceneInstance.FullUnitId = UnitUtils.GetFullUnitId(player.PlayerId, unit.UnitId);
                unitSceneInstance.UnitType = UnitType.Unknown;
                unitSceneInstance.PlayerNumber = 0;
                unitSceneInstance.Position = this.maze.GetSpritePositionForCell(mapPos + Vector2.Up * 4);
                unitSceneInstance.AddToGroup(Groups.OtherUnits);
                unitSceneInstance.AddToGroup(Groups.AllUnits);
                this.maze.AddChild(unitSceneInstance);
                allUnits.Add(unitSceneInstance);
                unitSceneInstance.MoveUnitTo(mapPos);
            }
        }

        this.waveGenerator.Start(allUnits.Cast<Node2D>().ToList());
    }

    public void BeforeHide()
    {
        this.waveGenerator.Stop();

        var allUnits = this.GetTree().GetNodesInGroup(Groups.AllUnits).Cast<Unit>().ToList();
        foreach (var unit in allUnits)
        {
            unit.QueueFree();
        }
    }

    public void CellSelected(Vector2 cell)
    {
        var myUnits = this.GetTree().GetNodesInGroup(Groups.MyUnits).Cast<Unit>().Where(a => !a.IsDead).ToList();
        var otherUnits = this.GetTree().GetNodesInGroup(Groups.OtherUnits).Cast<Unit>().Where(a => !a.IsDead).ToList();

        var currentUnit = myUnits.FirstOrDefault(a => a.IsSelected);
        var clickOnUnit = myUnits.FirstOrDefault(a => this.maze.WorldToMap(a.Position) == cell);

        if (currentUnit != null)
        {
            currentUnit.IsSelected = false;
            this.maze.RemoveHighliting();
        }

        if (clickOnUnit != null)
        {
            clickOnUnit.IsSelected = true;

            maze.BeginHighliting(Maze.HighliteType.Move, null);

            BreadthFirstPathfinder.Search(maze.MapGraph, cell, pluginUtils.FindUnitType(clickOnUnit.UnitType).MoveDistance, out var comeFrom);

            foreach (var newTarget in comeFrom.Keys)
            {
                if (newTarget == cell)
                {
                    continue;
                }

                maze.HighlitePoint(newTarget);
            }
            
            maze.EndHighliting();
        }
        else if (currentUnit != null && (this.maze.WorldToMap(currentUnit.Position) - cell).Length() <= pluginUtils.FindUnitType(currentUnit.UnitType).MoveDistance)
        {
            cell = this.turnLogic.RotateToPlayer(cell - Vector2.One, new Vector2(visibleMap.GetLength(0), visibleMap.GetLength(1)), 1);

            this.communicator.PlayerMoved(new TransferTurnDoneData
            {
                UnitId = UnitUtils.GetUnitId(currentUnit.FullUnitId),
                NewX = cell.x,
                NewY = cell.y
            });
        }
    }

    public void TurnDone(TransferTurnData turnData)
    {
        this.Timeout = this.MaxTimeout;
        var allUnits = this.GetTree().GetNodesInGroup(Groups.AllUnits);
        if (turnData.Moved)
        {
            var target = this.turnLogic.RotateToPlayer(new Vector2(turnData.MovedX, turnData.MovedY), new Vector2(visibleMap.GetLength(0), visibleMap.GetLength(1)), 1) + Vector2.One;

            var movedUnit = allUnits.Cast<Unit>().First(a => a.FullUnitId == turnData.MovedFullUnitId);
            movedUnit.RotateUnitTo(target);
            movedUnit.MoveUnitTo(target);
        }

        if (turnData.Battle)
        {
            var defenderUnit = allUnits.Cast<Unit>().First(a => a.FullUnitId == turnData.DefenderFullUnitId);
            var attackerUnit = allUnits.Cast<Unit>().First(a => a.FullUnitId == turnData.AttackerFullUnitId);

            defenderUnit.UnitType = turnData.DefenderUnitType;
            attackerUnit.UnitType = turnData.AttackerUnitType;

            switch (turnData.BattleWinner)
            {
                case TransferTurnData.BattleResult.Attacker:
                    {
                        attackerUnit.RotateUnitTo(defenderUnit.Position - attackerUnit.Position);
                        attackerUnit.Attack();
                        defenderUnit.UnitHit();
                    }
                    break;
                case TransferTurnData.BattleResult.Defender:
                    {
                        defenderUnit.RotateUnitTo(attackerUnit.Position - defenderUnit.Position);
                        defenderUnit.Attack();
                        attackerUnit.UnitHit();
                    }
                    break;
                case TransferTurnData.BattleResult.Draw:
                    break;
            }
        }
    }
}
