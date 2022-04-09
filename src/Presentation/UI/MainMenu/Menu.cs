using BrainAI.Pathfinding.BreadthFirst;
using Godot;
using GodotAnalysers;
using IsometricGame.Business.Plugins;
using IsometricGame.Business.Plugins.Enums;
using IsometricGame.Logic.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[SceneReference("Menu.tscn")]
public partial class Menu : Node2D
{
    [Export]
    public PackedScene MenuItemScene;

    private Communicator communicator;
    private PluginUtils pluginUtils;

    public Menu()
    {
        this.pluginUtils = DependencyInjector.pluginUtils;
    }

    public override void _Ready()
    {
        base._Ready();
        this.FillMembers();
        this.communicator = GetNode<Communicator>("/root/Communicator");

        var allGameTypes = this.pluginUtils.GetGameTypes().OfType<IPresentationGameType>();
        foreach (var gameType in allGameTypes)
        {
            if (gameType.Position == null)
            {
                continue;
            }

            var item = (MenuItem)MenuItemScene.Instance();
            item.Points = gameType.Position;
            item.Text = gameType.Text;
            item.ShootPosition = gameType.ShootPosition;
            item.Connect(nameof(MenuItem.ItemSelected), this, nameof(ItemSelected), new Godot.Collections.Array {(int)gameType.GameType });
            this.AddChild(item);

            this.maze.MapGraph.Walls.Remove(gameType.ShootPosition);
        }

        this.maze.Connect(nameof(Maze.CellSelected), this, nameof(CellSelected));
        this.menuItem.Connect(nameof(MenuItem.ItemSelected), this, nameof(ItemSelected), new Godot.Collections.Array { -1 });

        this.maze.MapGraph.Walls.Remove(this.menuItem.ShootPosition);

        this.waveGenerator.Start(new List<Node2D> { this.unit });
    }

    public void ItemSelected(MenuItem menuItem, int gameType)
    {
        Action<Unit> callback = null;
        if (gameType == -1)
        {
            callback = unit => OnLadderPressed();
        }
        else
        {
            var item = pluginUtils.FindGameType((GameType)gameType);
            callback = unit => OnCampaignPressed(item);
        }

        MoveUnitTo(menuItem.ShootPosition, callback);
    }

    public void CellSelected(Vector2 cell)
    {
        MoveUnitTo(cell, null);
    }

    private void MoveUnitTo(Vector2 cell, Action<Unit> callback)
    {
        var path = BreadthFirstPathfinder.Search(maze.MapGraph, maze.WorldToMap(this.unit.Position), cell);

        if (path == null)
        {
            return;
        }

        this.unit.CancelActions();
        foreach (var point in path.Skip(1).Take(path.Count - 2))
        {
            this.unit.RotateUnitTo(point);
            this.unit.MoveUnitTo(point);
        }

        this.unit.RotateUnitTo(path[path.Count - 1]);
        if (callback != null)
        {
            this.unit.Attack();
            this.unit.CallbackForUnit(callback);
        }
        else
        {
            this.unit.MoveUnitTo(cell);
        }
    }

    #region Dashboard

    private async void OnLadderPressed()
    {
        if (!await this.CreateConnection())
        {
            return;
        }
        this.ladderDialog.PopupCentered();
    }

    private async void OnCampaignPressed(IGameType gameType)
    {
        if (!await this.CreateConnection(true))
        {
            return;
        }

        this.communicator.CreateAndJoinLobby(gameType.GameType);
        var joinSuccess = (bool)(await this.ToSignal(this.communicator, nameof(Communicator.CreateLobbyDone)))[1];
        if (!joinSuccess)
        {
            GD.Print("Join lobby failed in CampaignDialog. Not implemented yet.");
            return;
        }

        this.communicator.StartGame();
        this.Hide();
    }

    private void OnSettingsPressed()
    {
        this.settingsDialog.PopupCentered();
    }

    private async Task<bool> CreateConnection(bool createAsServer = false)
    {
        if (createAsServer || this.settingsDialog.IsServer)
        {
            this.communicator.CreateServer();
        }
        else
        {
            this.communicator.CreateClient(this.settingsDialog.Server, this.settingsDialog.Login, this.settingsDialog.Password);
        }

        var isSuccess = (bool)(await ToSignal(this.communicator, nameof(Communicator.LoginDone)))[0];

        if (isSuccess)
        {
            return true;
        }

        var dialog = new AcceptDialog
        {
            WindowTitle = "Connection issue",
            DialogText = $"Failed to connect to {this.settingsDialog.Server}. \nPlease check: \n- your internet connection;\n- game settings.",
            PopupExclusive = true,
        };
        this.AddChild(dialog);
        dialog.PopupCenteredMinsize();
        await ToSignal(dialog, "popup_hide");
        dialog.QueueFree();
        return false;
    }

    #endregion
}
