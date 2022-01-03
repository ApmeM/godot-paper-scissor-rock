using Godot;
using IsometricGame.Business.Plugins.Enums;
using IsometricGame.Presentation;

[SceneReference("CampaignDialog.tscn")]
public partial class CampaignDialog : WindowDialog
{
    private Communicator communicator;

    public override void _Ready()
    {
        base._Ready();
        this.FillMembers();
        this.communicator = GetNode<Communicator>("/root/Communicator");

        this.campaignNewIcon.Connect("pressed", this, nameof(OnNewGamePressed));
    }

    private async void OnNewGamePressed()
    {
        this.communicator.CreateAndJoinLobby(GameType.CampaignLevel1);
        var joinSuccess = (bool)(await this.ToSignal(this.communicator, nameof(Communicator.CreateLobbyDone)))[1];
        if (!joinSuccess)
        {
            GD.Print("Join lobby failed in CampaignDialog. Not implemented yet.");
            return;
        }

        this.communicator.StartGame();
        this.Hide();
    }
}
