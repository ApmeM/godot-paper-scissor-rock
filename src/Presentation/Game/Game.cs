using Godot;
using IsometricGame.Business.Models.TransferData;
using IsometricGame.Presentation;
using Newtonsoft.Json;

[SceneReference("Game.tscn")]
public partial class Game : Node2D
{
    private Communicator communicator;

    public override void _Ready()
    {
        base._Ready();
        this.FillMembers();

        this.communicator = GetNode<Communicator>("/root/Communicator");
        this.communicator.Connect(nameof(Communicator.StartGameDone), this, nameof(StartGameDone));
        this.communicator.Connect(nameof(Communicator.Initialize), this, nameof(Initialize));
        this.communicator.Connect(nameof(Communicator.TurnDone), this, nameof(TurnDone));
        this.communicator.Connect(nameof(Communicator.GameOver), this, nameof(GameOver));
    }

    public void StartGameDone(string data)
    {
        var config = JsonConvert.DeserializeObject<TransferStartGameData>(data);
        if (config.PlayerId <= 0)
        {
            return;
        }

        this.preparation.Show();
        this.preparation.StartGame(config);
    }

    public void Initialize(string data)
    {
        var initialData = JsonConvert.DeserializeObject<TransferInitialData>(data);
        if (initialData.YourPlayerId <= 0)
        {
            return;
        }

        this.preparation.BeforeHide();
        this.preparation.Hide();
        
        this.battle.Show();
        this.battle.Initialize(initialData);
    }

    public void TurnDone(string data)
    {
        var turnData = JsonConvert.DeserializeObject<TransferTurnData>(data);
        if (turnData.YourPlayerId <= 0)
        {
            return;
        }
        
        this.battle.TurnDone(turnData);
    }

    public void GameOver(string data)
    {
        this.battle.BeforeHide();
        this.battle.Hide();
    }
}
