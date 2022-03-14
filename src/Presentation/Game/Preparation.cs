using FateRandom;
using Godot;
using IsometricGame.Business.Models.TransferData;
using IsometricGame.Business.Plugins;
using IsometricGame.Logic.Enums;
using IsometricGame.Logic.Utils;
using IsometricGame.Presentation;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[SceneReference("Preparation.tscn")]
public partial class Preparation : Node2D
{
    private float? Timeout;

    [Export]
    public PackedScene UnitScene;

    private Communicator communicator;
    private PluginUtils pluginUtils;
    private TransferStartGameData config;

    public Preparation()
    {
        this.pluginUtils = DependencyInjector.pluginUtils;
    }

    public override void _Ready()
    {
        base._Ready();
        this.FillMembers();
        
        this.startButton.Connect("pressed", this, nameof(StartPressed));
        this.randomButton.Connect("pressed", this, nameof(RandomPressed));
        this.communicator = GetNode<Communicator>("/root/Communicator");
        this.maze.Connect(nameof(Maze.CellSelected), this, nameof(CellSelected));
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
        if (Timeout.HasValue)
        {
            Timeout -= delta;
            startButton.Text = $"Start ({(int)Timeout})";

            if (Timeout < 0)
            {
                StartPressed();
            }
        }
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

    public void StartGame(TransferStartGameData config)
    {
        this.config = config;
        this.Timeout = config.TurnTimeout - 1;

        this.startButton.Visible = true;
        this.randomButton.Visible = true;
        this.maze.Initialize();
        this.maze.AddWater(new Rect2(-1, -1, 12, 14));
        this.maze.AddBeach(new Rect2(-1, 8, 12, 7));
        this.maze.AddBeach(new Rect2(-1, 5, 2, 3));
        this.maze.AddBeach(new Rect2(config.MapWidth + 1, 5, 12 - config.MapWidth - 2, 3));
        this.maze.AddWall(new Rect2(-1, 9, 6, 1));
        this.maze.AddWall(new Rect2(6, 9, 5, 1));

        var allUnits = new List<Unit>();
        for (var i = 0; i < config.AvailableUnits.Count; i++)
        {
            var unitType = config.AvailableUnits[i];
            var unit = (Unit)UnitScene.Instance();
            unit.UnitType = unitType;
            unit.PlayerNumber = 1;
            unit.Position = this.maze.GetSpritePositionForCell(Vector2.Down * 7 + Vector2.Right * 2 + Vector2.Right * (int)unitType);
            unit.RotationDegrees = 180;
            unit.AddToGroup(nameof(Groups.MyUnits));
            unit.AddToGroup(nameof(Groups.AllUnits));
            this.maze.AddChild(unit);
            allUnits.Add(unit);
        }

        this.waveGenerator.Start(allUnits.Cast<Node2D>().ToList());
    }

    public async void StartPressed()
    {
        var myUnits = this.GetTree().GetNodesInGroup(Groups.MyUnits).Cast<Unit>().ToList();
        foreach (var unit in myUnits)
        {
            unit.IsSelected = false;
        }

        this.maze.RemoveHighliting();
        this.startButton.Visible = false;
        this.randomButton.Visible = false;

        var data = new TransferConnectData
        {
            Units = myUnits
            .Where(a => a.TargetPositionMap != null)
            .Select(a => new TransferConnectData.UnitData
            {
                UnitType = a.UnitType,
                X = (config.MapWidth - 1) - ((int)a.TargetPositionMap.Value.x - 1),
                Y = (config.StartHeight - 1) - ((int)a.TargetPositionMap.Value.y - 1)
            })
            .ToList()
        };
        this.Timeout = null;

        foreach (var unit in myUnits)
        {
            unit.MoveUnitTo(new Vector2(unit.TargetPositionMap.Value.x, -1));
        }

        await Task.WhenAll(myUnits.Select(async unit => await this.GetTree().ToSignal(unit, nameof(Unit.AllActionsDone))));

        this.communicator.ConnectToGame(data);
    }

    public void RandomPressed()
    {
        var myUnits = this.GetTree().GetNodesInGroup(Groups.MyUnits).Cast<Unit>().ToList();
        Fate.GlobalFate.Shuffle(myUnits);
        foreach (var unit in myUnits)
        {
            unit.IsSelected = false;
        }

        for (var x = 0; x < config.MapWidth; x++)
        {
            for (var y = 0; y < 2; y++)
            {
                var idx = y * config.MapWidth + x;
                if (myUnits.Count > idx)
                {
                    var unit = myUnits[idx];
                    unit.CancelActions();
                    unit.RotateUnitTo(new Vector2(x + 1, y + 1));
                    unit.MoveUnitTo(new Vector2(x + 1, y + 1));
                    unit.RotateUnitTo(new Vector2(x + 1, y + 1) + Vector2.Up);
                }
            }
        }

        this.maze.RemoveHighliting();
    }

    private void CellSelected(Vector2 cell)
    {
        var myUnits = this.GetTree().GetNodesInGroup(Groups.MyUnits).Cast<Unit>().ToList();
        var otherUnits = this.GetTree().GetNodesInGroup(Groups.OtherUnits).Cast<Unit>().ToList();

        var currentUnit = myUnits.FirstOrDefault(a => a.IsSelected);
     
        var clickOnUnit = myUnits.FirstOrDefault(a => this.maze.WorldToMap(a.Position) == cell);
        if (currentUnit != null)
        {
            currentUnit.IsSelected = false;
            this.maze.RemoveHighliting();

            if (cell.x >= 1 && cell.y >= 1 && cell.x < config.MapWidth+1 && cell.y < config.StartHeight+1)
            {
                var clickOnUnitToReplace = myUnits.FirstOrDefault(a => a.TargetPositionMap == cell);
                if (clickOnUnitToReplace != null)
                {
                    var targetPositionMap = currentUnit.TargetPositionMap.HasValue
                        ? currentUnit.TargetPositionMap.Value
                        : Vector2.Down * 7 + Vector2.Right * 2 + Vector2.Right * (int)clickOnUnitToReplace.UnitType;

                    clickOnUnitToReplace.CancelActions();
                    clickOnUnitToReplace.RotateUnitTo(targetPositionMap);
                    clickOnUnitToReplace.MoveUnitTo(targetPositionMap);
                    clickOnUnitToReplace.RotateUnitTo(targetPositionMap + Vector2.Up);
                    clickOnUnitToReplace.TargetPositionMap = currentUnit.TargetPositionMap;
                }

                currentUnit.CancelActions();
                currentUnit.RotateUnitTo(cell);
                currentUnit.MoveUnitTo(cell);
                currentUnit.RotateUnitTo(cell + Vector2.Up);
            }
        }
        else if (clickOnUnit != null)
        {
            clickOnUnit.IsSelected = true;
            
            maze.BeginHighliting(Maze.HighliteType.Move, null);

            for (var x = 0; x < config.MapWidth; x++)
            {
                for (var y = 0; y < 2; y++)
                {
                    maze.HighlitePoint(new Vector2(x+1, y+1));
                }
            }
            maze.EndHighliting();
        } 
        
    }
}
