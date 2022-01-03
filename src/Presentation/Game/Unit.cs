using Godot;
using IsometricGame.Business.Plugins.Enums;
using IsometricGame.Presentation;
using IsometricGame.Presentation.Game.UnitAction;
using System.Collections.Generic;

[SceneReference("Unit.tscn")]
public partial class Unit : Node2D
{
    private Queue<IUnitAction> PendingActions = new Queue<IUnitAction>();

    public long FullUnitId = -1;

    private UnitType unitType = UnitType.Paper;
    private bool unitTypeRaw = true;
    private int playerNumber = 0;
    private bool playerNumberRaw = true;
    public Vector2? TargetPositionMap;

    private AtlasTexture shipTexture;
    private AtlasTexture shipTypeTexture;

    [Export]
    public UnitType UnitType
    {
        get => this.unitType;
        set { this.unitType = value; this.unitTypeRaw = true; }
    }

    [Export]
    public int PlayerNumber
    {
        get => this.playerNumber;
        set { this.playerNumber = value; this.playerNumberRaw = true; }
    }

    [Export]
    public bool IsSelected { get; set; }

    public bool IsDead { get; private set; }

    public override void _Ready()
    {
        base._Ready();
        this.FillMembers();
        this.shipTexture = (AtlasTexture)this.ship.Texture;
        this.shipTypeTexture = (AtlasTexture)this.shipTypeFlag.Texture;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        this.flag.Visible = this.IsSelected;
        if (this.playerNumberRaw)
        {
            this.playerNumberRaw = false;
            this.shipTexture.Region = new Rect2(0, 120 * this.playerNumber, 66, 113);
        }

        if (this.unitTypeRaw)
        {
            this.unitTypeRaw = false;
            switch (this.unitType)
            {
                case UnitType.Unknown:
                    this.shipTypeTexture.Region = new Rect2(280, 170, 20, 20);
                    break;
                case UnitType.Stone:
                    this.shipTypeTexture.Region = new Rect2(300, 170, 20, 20);
                    break;
                case UnitType.Scissor:
                    this.shipTypeTexture.Region = new Rect2(280, 190, 20, 20);
                    break;
                case UnitType.Paper:
                    this.shipTypeTexture.Region = new Rect2(300, 190, 20, 20);
                    break;
                case UnitType.Flag:
                    this.shipTypeTexture.Region = new Rect2(320, 170, 20, 20);
                    break;
            }
        }

        if (PendingActions.Count > 0)
        {
            var action = PendingActions.Peek();
            var isActionDone = action.Process(delta);
            if (isActionDone)
            {
                PendingActions.Dequeue();
            }
        }
    }

    public void UnitHit()
    {
        this.IsDead = true;
        this.PendingActions.Enqueue(new SinkUnitAction(this, this.shipTexture));
    }

    public void Attack()
    {
        this.PendingActions.Enqueue(new ShootUnitAction(this.cannon));
    }

    public void MoveUnitTo(Vector2 newCell)
    {
        var maze = this.GetParent<Maze>();
        this.TargetPositionMap = newCell;
        this.PendingActions.Enqueue(new MoveUnitAction(this, maze.GetSpritePositionForCell(newCell)));
    }

    public void RotateUnitTo(Vector2 lookAtCell)
    {
        var maze = this.GetParent<Maze>();
        this.PendingActions.Enqueue(new RotateUnitAction(this, maze.GetSpritePositionForCell(lookAtCell)));
    }

    public void CancelActions()
    {
        this.PendingActions.Clear();
    }
}
