using Godot;
using IsometricGame.Presentation;
using System.Collections.Generic;

[Tool]
[SceneReference("MenuItem.tscn")]
public partial class MenuItem : TileMap
{
    private List<Vector2> points = new List<Vector2>();
    private bool pointsDirty;
    private string text;
    private bool textDirty;

    [Signal]
    public delegate void ItemSelected(MenuItem menuItem);

    [Export]
    public List<Vector2> Points
    {
        get
        {
            return this.points;
        }

        set
        {
            this.points = value;
            this.pointsDirty = true;
        }
    }

    [Export]
    public string Text
    {
        get
        {
            return this.text;
        }

        set
        {
            this.text = value;
            this.textDirty = true;
        }
    }

    [Export]
    public Vector2 ShootPosition { get; set; }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (pointsDirty)
        {
            this.Clear();
            
            pointsDirty = false;
            if (points != null)
            {
                var fromPoint = points[points.Count - 1];

                var mapSize = new Rect2(fromPoint, Vector2.One);

                for (var i = 0; i < points.Count; i++)
                {
                    var toPoint = points[i];

                    mapSize = mapSize.Expand(toPoint);

                    if (fromPoint.x == toPoint.x)
                    {
                        var x = fromPoint.x;
                        for (var y = Mathf.Min(fromPoint.y, toPoint.y); y <= Mathf.Max(fromPoint.y, toPoint.y); y++)
                        {
                            this.SetCellv(new Vector2(x, y), 3);
                        }
                    }
                    else
                    if (fromPoint.y == toPoint.y)
                    {
                        var y = fromPoint.y;
                        for (var x = Mathf.Min(fromPoint.x, toPoint.x); x <= Mathf.Max(fromPoint.x, toPoint.x); x++)
                        {
                            this.SetCellv(new Vector2(x, y), 3);
                        }
                    }
                    else
                    {
                        GD.PrintErr($"Cant build stright line from {fromPoint} to {toPoint}.");
                    }

                    fromPoint = toPoint;
                }

                this.UpdateBitmaskRegion(mapSize.Position, mapSize.End);

                this.FillMembers();
                this.label.RectPosition = (mapSize.Position) * 64;
                this.label.RectSize = mapSize.Size * 64;
            }
        }

        if (this.textDirty)
        {
            this.textDirty = false;
            this.FillMembers();
            this.label.Text = text;
        }
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
            if (this.label.GetRect().HasPoint(position))
            {
                EmitSignal(nameof(ItemSelected), this);
                GetTree().SetInputAsHandled();
            }
        }
    }
}
