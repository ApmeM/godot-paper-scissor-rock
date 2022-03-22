using Godot;
using IsometricGame.Business.Utils;
using IsometricGame.Presentation;
using System;
using System.Collections.Generic;
using System.Linq;

[SceneReference("Maze.tscn")]
public partial class Maze : Node2D
{
    private const int waterCell = 4;
    private const int beachCell = 2;
    private const int wallCell = 3;

    private const int attackCell = 1;
    private const int moveCell = 2;
    private const int move2Cell = 3;
    private const int fogCell = 4;
    private const int damageRadiusCell = 8;

    public enum HighliteType
    {
        Move = moveCell,
        HighlitedMove = move2Cell
    }

    private readonly Dictionary<HighliteType, List<Vector2>> highlitedCells;
    private int? attackRadius;

    [Signal]
    public delegate void CellSelected(Vector2 cell);
    public readonly MapGraphData MapGraph = new MapGraphData();

    public Maze()
    {
        this.highlitedCells = Enum.GetValues(typeof(HighliteType)).Cast<HighliteType>().ToDictionary(a => a, a => new List<Vector2>());
    }

    public override void _Ready()
    {
        base._Ready();
        this.FillMembers();

        foreach (Vector2 water in this.level1.GetUsedCells())
        {
            AddWater(water);
        }
        foreach (Vector2 beach in this.level2.GetUsedCells())
        {
            AddBeach(beach);
        }
        foreach (Vector2 wall in this.level3.GetUsedCells())
        {
            AddWall(wall);
        }
    }

    public void AddWall(Vector2 pos, bool isWall = true)
    {
        AddCellToLevel(this.level3, pos, wallCell, isWall);
    }

    public void AddWall(Rect2 pos, bool isWall = true)
    {
        AddCellToLevel(this.level3, pos, wallCell, isWall);
    }

    public void AddBeach(Vector2 pos, bool isWall = true)
    {
        AddCellToLevel(this.level2, pos, beachCell, isWall);
    }

    public void AddBeach(Rect2 pos, bool isWall = true)
    {
        AddCellToLevel(this.level2, pos, beachCell, isWall);
    }

    public void AddWater(Vector2 pos, bool isWall = false)
    {
        AddCellToLevel(this.level1, pos, waterCell, isWall);
    }

    public void AddWater(Rect2 pos, bool isWall = false)
    {
        AddCellToLevel(this.level1, pos, waterCell, isWall);
    }

    public void AddCellToLevel(TileMap map, Vector2 pos, int cellType, bool isWall)
    {
        MapGraph.MapSize = MapGraph.MapSize.Expand(pos);
        map.SetCellv(pos, cellType);
        
        if (isWall)
        {
            MapGraph.Walls.Add(pos);
        }

        map.UpdateBitmaskRegion(MapGraph.MapSize.Position, MapGraph.MapSize.End);
    }

    public void AddCellToLevel(TileMap map, Rect2 pos, int cellType, bool isWall)
    {
        MapGraph.MapSize = MapGraph.MapSize.Merge(pos);
        
        for (var x = 0; x < pos.Size.x; x++)
            for (var y = 0; y < pos.Size.y; y++)
            {
                map.SetCellv(pos.Position + new Vector2(x, y), cellType);
                if (isWall)
                {
                    MapGraph.Walls.Add(pos.Position + new Vector2(x, y));
                }
            }

        map.UpdateBitmaskRegion(MapGraph.MapSize.Position, MapGraph.MapSize.End);
    }

    public Vector2 GetSpritePositionForCell(Vector2 mapPos)
    {
        var worldPos = this.highliteLevel.MapToWorld(mapPos);
        worldPos += Vector2.Down * this.highliteLevel.CellSize.y / 2;
        worldPos += Vector2.Right * this.highliteLevel.CellSize.x / 2;
        return worldPos;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        var mouse = this.highliteLevel.GetGlobalMousePosition();
        var cell = this.highliteLevel.WorldToMap(mouse);

        BeginHighliting(HighliteType.HighlitedMove, this.attackRadius);
        HighlitePoint(cell);
        EndHighliting();
    }

    public void Initialize()
    {
        this.level1.Clear();
        this.level2.Clear();
        this.level3.Clear();
        this.highliteLevel.Clear();
        this.MapGraph.Walls.Clear();
        RemoveHighliting();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);
        if (!IsVisibleInTree())
        {
            return;
        }

        if (@event is InputEventScreenTouch eventMouseButton && eventMouseButton.Pressed)
        {
            var position = this.GetGlobalMousePosition(); // eventMouseButton.Position;
            var cell = this.highliteLevel.WorldToMap(position);

            EmitSignal(nameof(CellSelected), cell);
            GetTree().SetInputAsHandled();
        }
    }

    public void RemoveHighliting()
    {
        foreach (var cells in highlitedCells.Values)
        {
            cells.Clear();
        }
        RehighliteCells();
    }

    #region HighlitingInternal

    private HighliteType? currentHighliteType;

    public void BeginHighliting(HighliteType highliteType, int? attackRadius)
    {
        if (currentHighliteType != null)
        {
            throw new Exception("Previous highliting not finished.");
        }

        this.attackRadius = attackRadius;
        this.currentHighliteType = highliteType;
        highlitedCells[highliteType].Clear();
    }

    public void HighlitePoint(Vector2 fromPoint)
    {
        EnsureHighliting();
        highlitedCells[this.currentHighliteType.Value].Add(fromPoint);
    }

    public void EndHighliting()
    {
        EnsureHighliting();
        RehighliteCells();
        this.currentHighliteType = null;
    }

    private void EnsureHighliting()
    {
        if (this.currentHighliteType == null)
        {
            throw new Exception("Highliting not started.");
        }
    }

    private void RehighliteCells()
    {
        this.highliteLevel.Clear();

        foreach (var highlitedCell in highlitedCells)
        {
            foreach (var cell in highlitedCell.Value)
            {
                highliteLevel.SetCellv(cell, (int)highlitedCell.Key);
            }
        }
    }

    #endregion

    public int Distance(Vector2 from, Vector2 to)
    {
        var vector = from - to;
        return (int)(Math.Abs(vector.x) + Math.Abs(vector.y));
    }

    public Vector2 WorldToMap(Vector2 position)
    {
        return this.highliteLevel.WorldToMap(position);
    }

    public Vector2 MapToWorld(Vector2 position)
    {
        return this.highliteLevel.MapToWorld(position);
    }
}
