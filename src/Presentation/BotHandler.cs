using Godot;
using IsometricGame.Business.Logic;
using IsometricGame.Business.Models.TransferData;
using IsometricGame.Business.Plugins;
using IsometricGame.Logic.Utils;
using IsometricGame.Repository;
using Newtonsoft.Json;
using System.Collections.Generic;

public class BotHandler : Node
{
    private readonly PluginUtils pluginUtils;
    private ServerLogic serverLogic;
    private GamesRepository gameRepository;
    private BotRepository botRepository;
    private Communicator communicator;

    public BotHandler()
    {
        this.pluginUtils = DependencyInjector.pluginUtils;
        this.serverLogic = DependencyInjector.serverLogic;
        this.gameRepository = DependencyInjector.gamesRepository;
        this.botRepository = DependencyInjector.botRepository;
    }

    public override void _Ready()
    {
        base._Ready();

        this.communicator = GetNode<Communicator>("/root/Communicator");
        this.communicator.Connect(nameof(Communicator.StartGameDone), this, nameof(StartGameDone));
        this.communicator.Connect(nameof(Communicator.Initialize), this, nameof(Initialize));
        this.communicator.Connect(nameof(Communicator.TurnDone), this, nameof(TurnDone));
        this.communicator.Connect(nameof(Communicator.GameOver), this, nameof(GameOver));
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
    }

    public void StartGameDone(string data)
    {
        var config = JsonConvert.DeserializeObject<TransferStartGameData>(data);
        var playerId = config.PlayerId;
        if (playerId >= 0)
        {
            return;
        }

        var botData = this.botRepository.GetBot(playerId);

        var botInstance = this.pluginUtils.FindBot(botData.Bot);
        var connectData = botInstance.StartGame(config);
        this.communicator.ConnectToGameOnServer(1, playerId, connectData);
    }

    public void Initialize(string data)
    {
        var initialData = JsonConvert.DeserializeObject<TransferInitialData>(data);
        var playerId = initialData.YourPlayerId;
        if (playerId >= 0)
        {
            return;
        }

        var botData = this.botRepository.GetBot(playerId);
        var botInstance = this.pluginUtils.FindBot(botData.Bot);
        var turnDoneData = botInstance.Initialize(initialData, botData.Cache);
        this.botRepository.SetBotCache(playerId, botData.Cache);
        if (turnDoneData != null)
        {
            this.communicator.PlayerMovedOnServer(playerId, turnDoneData);
        }
    }

    public void TurnDone(string data)
    {
        var turnData = JsonConvert.DeserializeObject<TransferTurnData>(data);
        var playerId = turnData.YourPlayerId;
        if (playerId >= 0)
        {
            return;
        }

        var botData = this.botRepository.GetBot(playerId);
        var botInstance = this.pluginUtils.FindBot(botData.Bot);

        var turnDoneData = botInstance.TurnDone(turnData, botData.Cache);
        this.botRepository.SetBotCache(playerId, botData.Cache);
        if (turnDoneData != null)
        {
            this.communicator.PlayerMovedOnServer(playerId, turnDoneData);
        }
    }

    public void GameOver(string data)
    {
        var gameOverData = JsonConvert.DeserializeObject<TransferGameOverData>(data);
        var playerId = gameOverData.YourPlayerId;
        if (playerId >= 0)
        {
            return;
        }

        this.botRepository.RemoveBot(playerId);
    }
}
