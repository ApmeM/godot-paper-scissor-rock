using BrainAI.Pathfinding.BreadthFirst;
using Godot;
using IsometricGame.Business.Plugins;
using IsometricGame.Logic.Utils;
using IsometricGame.Presentation;
using System;
using System.Linq;
using System.Threading.Tasks;

[SceneReference("Menu.tscn")]
public partial class Menu : Node2D
{
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

        var allGameTypes = this.pluginUtils.GetGameTypes();
        foreach (var gameType in allGameTypes)
        {
            if (gameType.Position == Vector2.Zero)
            {
                continue;
            }

            this.maze.AddWall(gameType.Position, false);
        }

        this.maze.AddWall(new Vector2(5,5), false);

        this.maze.Connect(nameof(Maze.CellSelected), this, nameof(CellSelected));
    }

    public void CellSelected(Vector2 cell)
    {
        var path = BreadthFirstPathfinder.Search(maze.MapGraph, maze.WorldToMap(this.unit.Position), cell);

        if (path == null)
        {
            return;
        }

        Action<Unit> callback = null;
        if (cell.x == 5 && cell.y == 5)
        {
            callback = unit => OnLadderPressed();
        }

        var allGameTypes = this.pluginUtils.GetGameTypes();
        foreach (var gameType in allGameTypes)
        {
            if (gameType.Position != cell)
            {
                continue;
            }

            callback = unit => OnCampaignPressed(gameType);
            break;
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
