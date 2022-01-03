using Godot;
using IsometricGame.Business.Logic;
using IsometricGame.Business.Models.TransferData;
using IsometricGame.Business.Plugins.Enums;
using IsometricGame.Logic.Models;
using IsometricGame.Logic.Utils;
using IsometricGame.Repository;
using Newtonsoft.Json;
using System.Threading.Tasks;

public class Communicator : Node
{
    private const int ConnectionPort = 12345;
    private const int ClientConnectionTimeout = 5;

    private GamesRepository gamesRepository;
    private AccountRepository accountRepository;
    private ServerLogic serverLogic;

    private float clientConnectingTime = 0;

    private bool IsHtml => OS.HasFeature("HTML5");
    private bool isServer = false;

    public override void _Ready()
    {
        base._Ready();
        this.gamesRepository = DependencyInjector.gamesRepository;
        this.accountRepository = DependencyInjector.accountRepository;
        this.serverLogic = DependencyInjector.serverLogic;

        GetTree().Connect("network_peer_connected", this, nameof(PlayerConnected));
        GetTree().Connect("network_peer_disconnected", this, nameof(PlayerDisconnected));
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
        if (GetTree().NetworkPeer is WebSocketServer server && server.IsListening())
        {
            server.Poll();
        }
        else if (GetTree().NetworkPeer is WebSocketClient client &&
           (
           client.GetConnectionStatus() == NetworkedMultiplayerPeer.ConnectionStatus.Connected ||
           client.GetConnectionStatus() == NetworkedMultiplayerPeer.ConnectionStatus.Connecting
           ))
        {
            if(client.GetConnectionStatus() == NetworkedMultiplayerPeer.ConnectionStatus.Connecting)
            {
                clientConnectingTime += delta;
                if(clientConnectingTime > ClientConnectionTimeout)
                {
                    GetTree().NetworkPeer = null;
                    clientConnectingTime = 0;
                    this.IncorrectLogin();
                }
            }
            client.Poll();
        }

        this.serverLogic.ProcessTick(delta, (int p1, int p2, LobbyData lobby) => {
            foreach (var newPlayer in lobby.Players)
            {
                if (newPlayer.ClientId > 0)
                {
                    MyRpcId(newPlayer.ClientId, nameof(JoinLobbyOnClientDone), lobby.Id);
                }
            }
            
            foreach (var newPlayer in lobby.Players)
            {
                SendAllNewPlayerJoinedLobby(lobby, newPlayer);
            }

            this.StartGameOnServer(lobby.CreatorPlayerId, lobby.CreatorPlayerId);
        });
    }

    #region Peers connection

    public void CreateServer()
    {
        this.isServer = true;
        if (!IsHtml)
        {
            var peer = new WebSocketServer();
            peer.Listen(ConnectionPort, null, true);
            GetTree().NetworkPeer = peer;
        }
        else
        {
            GD.Print("Server (not) started on HTML5 - you cant connect to this server, but current instance will be connected to itself.");
        }

        this.serverLogic.InitializeServer();
        
        LoginSuccess();
    }

    public void CreateClient(string serverAddress, string login, string password)
    {
        this.isServer = false;
        this.clientConnectingTime = 0;

        var peer = new WebSocketClient();
        peer.ConnectToUrl($"ws://{serverAddress}:{ConnectionPort}", null, true);
        GetTree().NetworkPeer = peer;

        GetTree().Disconnect("connected_to_server", this, nameof(PlayerConnectedToServer));
        GetTree().Connect("connected_to_server", this, nameof(PlayerConnectedToServer), new Godot.Collections.Array { login, password });
    }

    [RemoteSync]
    private void LoginOnServer(string login, string password)
    {
        var clientId = MyGetRpcSenderId();
        bool isSuccess = this.serverLogic.Login(clientId, login, password);
        if (!isSuccess)
        {
            MyRpcId(clientId, nameof(IncorrectLogin));
        }
        else
        {
            MyRpcId(clientId, nameof(LoginSuccess));
        }
    }

    [RemoteSync]
    private async void LoginSuccess()
    {
        await this.ArtificialDelayForAsyncCall();
        this.EmitSignal(nameof(LoginDone), true);
    }

    [RemoteSync]
    private async void IncorrectLogin()
    {
        GetTree().NetworkPeer = null;

        await this.ArtificialDelayForAsyncCall();
        this.EmitSignal(nameof(LoginDone), true);
    }

    [Signal]
    public delegate void LoginDone(bool loginSuccess);

    private void PlayerConnected(int clientId)
    {
    }

    private void PlayerConnectedToServer(string login, string password)
    {
        MyRpcId(1, nameof(LoginOnServer), login, password);
    }

    private void PlayerDisconnected(int clientId)
    {
        SendAllPlayerLeftLobby(clientId, clientId);
        this.serverLogic.Logout(clientId);
    }

    #endregion

    #region Joining lobby

    public void CreateAndJoinLobby(GameType gameType)
    {
        MyRpcId(1, nameof(CreateAndJoinLobbyOnServer), (int)gameType);
    }

    [RemoteSync]
    private void CreateAndJoinLobbyOnServer(int gameType)
    {
        var creatorClientId = MyGetRpcSenderId();
        var serverId = MyGetNetworkUniqueId();
        var lobby = this.serverLogic.CreateAndJoinLobby(creatorClientId, serverId, (GameType)gameType);

        if (lobby == null)
        {
            MyRpcId(creatorClientId, nameof(CreateLobbyDoneOnClient), lobby.Id, false);
            return;
        }

        MyRpcId(creatorClientId, nameof(CreateLobbyDoneOnClient), lobby.Id, true);
        MyRpcId(creatorClientId, nameof(JoinLobbyOnClientDone), lobby.Id, true);
        foreach (var newPlayer in lobby.Players)
        {
            SendAllNewPlayerJoinedLobby(lobby, newPlayer);
        }
    }

    [RemoteSync]
    private async void CreateLobbyDoneOnClient(string lobbyId, bool success)
    {
        await this.ArtificialDelayForAsyncCall();
        this.EmitSignal(nameof(CreateLobbyDone), lobbyId, success);
    }

    [Signal]
    public delegate void CreateLobbyDone(string lobbyId, bool success);

    public void JoinLobby(string lobbyId)
    {
        MyRpcId(1, nameof(JoinLobbyOnServer), lobbyId);
    }

    [RemoteSync]
    private void JoinLobbyOnServer(string lobbyId)
    {
        var clientId = MyGetRpcSenderId();
        var newPlayer = this.serverLogic.JoinLobby(clientId, lobbyId);
        if (newPlayer == null)
        {
            MyRpcId(clientId, nameof(JoinLobbyOnClientDone), lobbyId, false);
            return;
        }

        var lobby = this.gamesRepository.FindForPlayerLobby(clientId);
        MyRpcId(clientId, nameof(JoinLobbyOnClientDone), lobbyId, true);
        SendAllNewPlayerJoinedLobby(lobby, newPlayer);
    }

    [RemoteSync]
    private async void JoinLobbyOnClientDone(string lobbyId, bool success)
    {
        await this.ArtificialDelayForAsyncCall();
        this.EmitSignal(nameof(JoinLobbyDone), lobbyId, success);
    }

    [Signal]
    public delegate void JoinLobbyDone(string lobbyId, bool success);

    public void AddBot(Bot bot)
    {
        MyRpcId(1, nameof(AddBotOnServer), bot);
    }

    [RemoteSync]
    private void AddBotOnServer(Bot bot)
    {
        var clientId = MyGetRpcSenderId();
        var serverId = MyGetNetworkUniqueId();
        var newPlayer = this.serverLogic.AddBot(clientId, serverId, bot);
        if (newPlayer == null)
        {
            return;
        }

        var lobby = this.gamesRepository.FindForPlayerLobby(clientId);
        SendAllNewPlayerJoinedLobby(lobby, newPlayer);
    }

    private void SendAllNewPlayerJoinedLobby(LobbyData lobby, LobbyData.PlayerData newPlayer)
    {
        if (newPlayer.PlayerId > 0)
        {
            MyRpcId(newPlayer.ClientId, nameof(JoinLobbyOnClientYourData), newPlayer.PlayerId == lobby.CreatorPlayerId, lobby.Id, newPlayer.PlayerId);
            MyRpcId(newPlayer.ClientId, nameof(SyncConfigOnClientDone), JsonConvert.SerializeObject(lobby.Configuration));
        }

        if (newPlayer.ClientId > 0)
        {
            foreach (var player in lobby.Players)
            {
                MyRpcId(newPlayer.ClientId, nameof(JoinLobbyOnClientPlayerAdded), player.PlayerName, player.PlayerId);
            }
        }

        foreach (var player in lobby.Players)
        {
            if (player.PlayerId <= 0)
            {
                continue;
            }

            if (player.PlayerId == newPlayer.PlayerId)
            {
                continue;
            }

            MyRpcId(player.ClientId, nameof(JoinLobbyOnClientPlayerAdded), newPlayer.PlayerName, newPlayer.PlayerId);
        }
    }

    [Signal]
    public delegate void JoinLobbyYourData(bool creator, string lobbyId, int playerId);

    [RemoteSync]
    private async void JoinLobbyOnClientYourData(bool creator, string lobbyId, int playerId)
    {
        await this.ArtificialDelayForAsyncCall();
        this.EmitSignal(nameof(JoinLobbyYourData), creator, lobbyId, playerId);
    }

    [Signal]
    public delegate void JoinLobbyPlayerAdded(string playerName, int playerId);

    [RemoteSync]
    private async void JoinLobbyOnClientPlayerAdded(string playerName, int playerId)
    {
        await this.ArtificialDelayForAsyncCall();
        this.EmitSignal(nameof(JoinLobbyPlayerAdded), playerName, playerId);
    }

    public void LeaveLobby()
    {
        MyRpcId(1, nameof(LeaveLobbyOnServer));
    }

    [RemoteSync]
    private void LeaveLobbyOnServer()
    {
        var clientId = MyGetRpcSenderId();
        SendAllPlayerLeftLobby(clientId, clientId);
        this.serverLogic.LeaveLobby(clientId);
    }

    private void SendAllPlayerLeftLobby(int clientId, int playerId)
    {
        var lobby = this.gamesRepository.FindForPlayerLobby(playerId);
        var playerName = this.accountRepository.FindForClientActiveLogin(playerId);

        if (lobby == null)
        {
            return;
        }

        foreach (var player in lobby.Players)
        {
            if (player.PlayerId <= 0)
            {
                continue;
            }

            MyRpcId(player.ClientId, nameof(LeaveLobbyOnClientPlayerLeft), playerId);
        }

        if (playerId > 0)
        {
            MyRpcId(clientId, nameof(LeaveLobbyOnClientYouLeft));
        }
    }

    [Signal]
    public delegate void LeaveLobbyPlayerLeft(int playerId);

    [RemoteSync]
    private async void LeaveLobbyOnClientPlayerLeft(int playerId)
    {
        await this.ArtificialDelayForAsyncCall();
        this.EmitSignal(nameof(LeaveLobbyPlayerLeft), playerId);
    }

    [Signal]
    public delegate void LeaveLobbyYouLeft();

    [RemoteSync]
    private async void LeaveLobbyOnClientYouLeft()
    {
        await this.ArtificialDelayForAsyncCall();
        this.EmitSignal(nameof(LeaveLobbyYouLeft));
    }

    public void SyncConfig(TransferSyncConfigData serverConfiguration)
    {
        MyRpcId(1, nameof(SyncConfigOnServer), JsonConvert.SerializeObject(serverConfiguration));
    }

    [RemoteSync]
    private void SyncConfigOnServer(string data)
    {
        var clientId = MyGetRpcSenderId();

        var configuration = JsonConvert.DeserializeObject<TransferSyncConfigData>(data);

        if (!this.serverLogic.UpdateConfig(clientId, configuration))
        {
            return;
        }

        SendAllNewConfig(clientId);
    }

    private void SendAllNewConfig(int clientId)
    {
        var lobbyData = this.gamesRepository.FindForPlayerLobby(clientId);
        foreach (var player in lobbyData.Players)
        {
            if (player.PlayerId < 0)
            {
                continue;
            }

            MyRpcId(player.ClientId, nameof(SyncConfigOnClientDone), JsonConvert.SerializeObject(new TransferSyncConfigData()));
        }
    }

    [Signal]
    public delegate void SyncConfigDone(string data);

    [RemoteSync]
    private async void SyncConfigOnClientDone(string data)
    {
        await this.ArtificialDelayForAsyncCall();
        this.EmitSignal(nameof(SyncConfigDone), data);
    }

    #endregion

    #region Start game

    public void StartGame()
    {
        MyRpcId(1, nameof(StartGameOnServerRemote));
    }

    [RemoteSync]
    private void StartGameOnServerRemote()
    {
        var clientId = MyGetRpcSenderId();
        StartGameOnServer(clientId, clientId);
    }

    public void StartGameOnServer(int clientId, int playerId)
    {
        var game = this.serverLogic.StartGame(playerId);
        if (game == null)
        {
            return;
        }

        foreach (var player in game.Players)
        {
            var transferData = new TransferStartGameData
            {
                AvailableUnits = game.Configuration.AvailableUnits,
                TurnTimeout = game.Configuration.TurnTimeout,
                MapWidth = game.Configuration.Map.GetLength(0),
                StartHeight = game.Configuration.StartHeight,
                PlayerId = player.Value.PlayerId
            };
            var data = JsonConvert.SerializeObject(transferData);

            MyRpcId(player.Value.ClientId, nameof(StartGameOnClientDone), data);
        }
    }

    [Signal]
    public delegate void StartGameDone(string data);

    [RemoteSync]
    private async void StartGameOnClientDone(string data)
    {
        await this.ArtificialDelayForAsyncCall();
        this.EmitSignal(nameof(StartGameDone), data);
    }

    #endregion

    #region Game steps

    public void ConnectToGame(TransferConnectData data)
    {
        MyRpcId(1, nameof(ConnectToGameOnServerRemote), JsonConvert.SerializeObject(data));
    }

    [RemoteSync]
    private void ConnectToGameOnServerRemote(string data)
    {
        var connectData = JsonConvert.DeserializeObject<TransferConnectData>(data);
        var clientId = MyGetRpcSenderId();
        ConnectToGameOnServer(clientId, clientId, connectData);
    }

    public void ConnectToGameOnServer(int clientId, int playerId, TransferConnectData connectData)
    {
        this.serverLogic.ConnectToGame(playerId, connectData,
            (initData) => { MyRpcId(clientId, nameof(InitializeOnClient), JsonConvert.SerializeObject(initData)); },
            (turnData) => { MyRpcId(clientId, nameof(TurnDoneOnClient), JsonConvert.SerializeObject(turnData)); },
            (gameOverData) => { MyRpcId(clientId, nameof(GameOverOnClient), JsonConvert.SerializeObject(gameOverData)); }
            );
    }

    [Signal]
    public delegate void Initialize(string data);

    [RemoteSync]
    private async void InitializeOnClient(string data)
    {
        await this.ArtificialDelayForAsyncCall();
        this.EmitSignal(nameof(Initialize), data);
    }

    public void PlayerMoved(TransferTurnDoneData data)
    {
        MyRpcId(1, nameof(PlayerMovedOnServerRemote), JsonConvert.SerializeObject(data));
    }

    [RemoteSync]
    private void PlayerMovedOnServerRemote(string data)
    {
        var turnData = JsonConvert.DeserializeObject<TransferTurnDoneData>(data);
        var clientId = MyGetRpcSenderId();
        PlayerMovedOnServer(clientId, turnData);
    }

    public void PlayerMovedOnServer(int playerId, TransferTurnDoneData turnData)
    {
        this.serverLogic.PlayerMove(playerId, turnData);
    }

    [Signal]
    public delegate void TurnDone(string data);

    [RemoteSync]
    private async void TurnDoneOnClient(string data)
    {
        await this.ArtificialDelayForAsyncCall();
        this.EmitSignal(nameof(TurnDone), data);
    }

    [Signal]
    public delegate void GameOver(string data);

    [RemoteSync]
    private async void GameOverOnClient(string data)
    {
        await this.ArtificialDelayForAsyncCall();
        this.EmitSignal(nameof(GameOver), data);
    }

    #endregion


    #region Joining ladder

    public void JoinLadder()
    {
        MyRpcId(1, nameof(JoinLadderOnServer));
    }

    [RemoteSync]
    private void JoinLadderOnServer()
    {
        var clientId = MyGetRpcSenderId();
        this.serverLogic.JoinLadder(clientId);

        MyRpcId(clientId, nameof(JoinLadderOnClientDone), true);
    }

    [RemoteSync]
    private async void JoinLadderOnClientDone(bool success)
    {
        await this.ArtificialDelayForAsyncCall();
        this.EmitSignal(nameof(JoinLadderDone), success);
    }

    [Signal]
    public delegate void JoinLadderDone(bool success);

    
    public void LeaveLadder()
    {
        MyRpcId(1, nameof(LeaveLadderOnServer));
    }

    [RemoteSync]
    private void LeaveLadderOnServer()
    {
        var clientId = MyGetRpcSenderId();
        this.serverLogic.LeaveLadder(clientId);

        MyRpcId(clientId, nameof(LeaveLadderOnClientDone), true);
    }

    [RemoteSync]
    private async void LeaveLadderOnClientDone(bool success)
    {
        await this.ArtificialDelayForAsyncCall();
        this.EmitSignal(nameof(LeaveLadderDone), success);
    }

    [Signal]
    public delegate void LeaveLadderDone(bool success);

    #endregion

    private int MyGetRpcSenderId()
    {
        if (isServer && IsHtml)
        {
            return 1;
        }
        else
        {
            return GetTree().GetRpcSenderId();
        }
    }

    private int MyGetNetworkUniqueId()
    {
        if (isServer && IsHtml)
        {
            return 1;
        }
        else
        {
            return GetTree().GetNetworkUniqueId();
        }
    }

    private void MyRpcId(int clientId, string method, params object[] args)
    {
        if(isServer && IsHtml)
        {
            Call(method, args);
        }
        else
        {
            RpcId(clientId, method, args);
        }
    }

    private async Task ArtificialDelayForAsyncCall()
    {
        await this.ToSignal(this.GetTree().CreateTimer(0.1f), "timeout");
    }
}
