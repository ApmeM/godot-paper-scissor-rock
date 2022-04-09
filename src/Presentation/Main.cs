using Godot;
using GodotAnalysers;

[SceneReference("Main.tscn")]
public partial class Main : Node
{
    private Communicator communicator;

    public override void _Ready()
    {
        base._Ready();
        this.FillMembers();
        this.communicator = GetNode<Communicator>("/root/Communicator");
        this.communicator.Connect(nameof(Communicator.StartGameDone), this, nameof(StartGameDone));
        this.communicator.Connect(nameof(Communicator.GameOver), this, nameof(GameOver));

        this.game.Hide();
    }

    public void StartGameDone(string data)
    {
        this.game.Show();
        this.menu.Hide();
        this.draggableCamera.enabled = false;
        this.draggableCamera.Position = Vector2.Zero;
        this.draggableCamera.NormalizedZoom = 0;
    }

    public void GameOver(string data)
    {
        this.game.Hide();
        this.menu.Show();
        this.draggableCamera.enabled = true;
    }
}
