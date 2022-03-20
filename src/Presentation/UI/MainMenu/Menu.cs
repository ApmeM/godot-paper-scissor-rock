using Godot;
using IsometricGame.Business.Plugins;
using IsometricGame.Logic.Utils;
using IsometricGame.Presentation;
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

            this.tileMap3.SetCellv(gameType.Position, Maze.wallCell);

            this.tileMap3.UpdateBitmaskRegion(gameType.Position, Vector2.One);
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
            GetTree().SetInputAsHandled();

            var position = this.GetGlobalMousePosition(); // eventMouseButton.Position;
            var cell = this.tileMap.WorldToMap(position);

            if (cell.x > -1 && cell.x < 5 && cell.y > 2 && cell.y < 8)
            {
                OnLadderPressed();
                return;
            }

            if (this.tileMap3.GetCellv(cell) != Maze.wallCell)
            {
                return;
            }

            var allGameTypes = this.pluginUtils.GetGameTypes();
            foreach (var gameType in allGameTypes)
            {
                if (gameType.Position != cell)
                {
                    continue;
                }
                
                OnCampaignPressed(gameType);
                return;
            }
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

    private void OnExitPressed()
    {
        this.GetTree().Quit();
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
