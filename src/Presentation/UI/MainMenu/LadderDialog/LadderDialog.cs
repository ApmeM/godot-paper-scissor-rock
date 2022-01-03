using Godot;
using System;

public class LadderDialog : WindowDialog
{
    private Communicator communicator;

    public override void _Ready()
    {
        this.communicator = GetNode<Communicator>("/root/Communicator");

        this.Connect("about_to_show", this, nameof(StartConnection));
        this.Connect("popup_hide", this, nameof(StopConnection));
        this.communicator.Connect(nameof(Communicator.StartGameDone), this, nameof(MatchFound));
    }

    private void MatchFound()
    {
        this.Hide();
    }

    private void StopConnection()
    {
        this.communicator.LeaveLadder();
    }

    private void StartConnection()
    {
        this.communicator.JoinLadder();
    }
}
