using Riptide.Utils;
using System;
using UnityEngine;

namespace Riptide.Demos.PlayerHosted
{
    internal enum MessageId : ushort
    {
        // Relay - Client to Server to Other Clients
        SpawnPlayer = 1,
        PlayerMovement,

        // Server to client
        activeScene,
        playerSpawned,
        playerMovement,
        playerHealthChanged,
        playerActiveWeaponUpdated,
        playerAmmoChanged,
        playerDied,
        playerRespawned,
        projectileSpawned,
        projectileMovement,
        projectileCollided,
        projectileHitmarker,
        sync,

        // Client to Server 
        name,
        registerPlayer,
        input,
        switchActiveWeapon,
        primaryUse,
        reload,
    }

    public class NetworkManager : MonoBehaviour
    {
        private static NetworkManager _singleton;
        public static NetworkManager Singleton
        {
            get => _singleton;
            private set
            {
                if (_singleton == null)
                    _singleton = value;
                else if (_singleton != value)
                {
                    Debug.Log($"{nameof(NetworkManager)} instance already exists, destroying object!");
                    Destroy(value);
                }
            }
        }

        public bool IsHost => Server.IsRunning;
        public ushort CurrentTick { get; private set; } = 0;


        [SerializeField] private ushort port = 7777;
        [SerializeField] private ushort maxPlayers = 10;
        [Header("Prefabs")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject localPlayerPrefab;

        public GameObject PlayerPrefab => playerPrefab;
        public GameObject LocalPlayerPrefab => localPlayerPrefab;

        internal Server Server { get; private set; }
        internal Client Client { get; private set; }

        private ushort _serverTick;
        public ushort ServerTick
        {
            get => _serverTick;
            private set
            {
                _serverTick = value;
                InterpolationTick = (ushort)(value - TicksBetweenPositionUpdates);
            }
        }
        public ushort InterpolationTick { get; private set; }
        private ushort _ticksBetweenPositionUpdates = 2;
        public ushort TicksBetweenPositionUpdates
        {
            get => _ticksBetweenPositionUpdates;
            private set
            {
                _ticksBetweenPositionUpdates = value;
                InterpolationTick = (ushort)(ServerTick - value);
            }
        }

        [SerializeField] private ushort tickDivergenceTolerance = 1;


        private void Awake()
        {
            Singleton = this;
        }

        private void Start()
        {
            RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

            Server = new Server();
            Server.ClientConnected += S_NewPlayerConnected;
            // Server.ClientDisconnected += S_PlayerLeft;
            Server.RelayFilter = new MessageRelayFilter(typeof(MessageId), MessageId.SpawnPlayer, MessageId.PlayerMovement);

            Client = new Client();
            Client.Connected += DidConnect;
            Client.ConnectionFailed += FailedToConnect;
            Client.Disconnected += DidDisconnect;
            Client.ClientDisconnected += PlayerLeft;
        }

        private void FixedUpdate()
        {
            if (Server.IsRunning)
                Server.Update();

            if (CurrentTick % 200 == 0 & Server.IsRunning)
                SendSync();

            CurrentTick++;

            Client.Update();
            ServerTick++;

        }

        private void OnApplicationQuit()
        {
            Server.Stop();
            Client.Disconnect();
        }

        internal void StartHost()
        {
            Server.Start(port, maxPlayers);
            Client.Connect($"127.0.0.1:{port}");
            GameLogicServer.Singleton.LoadScene(1);
        }

        internal void JoinGame(string ipString)
        {
            Client.Connect($"{ipString}:{port}");
        }

        internal void LeaveGame()
        {
            Server.Stop();
            Client.Disconnect();
        }

        private void DidConnect(object sender, EventArgs e)
        {
            PlayerClient.RegisterPlayer(UIManager.Singleton.Username);
            //Player.Spawn(Client.Id, UIManager.Singleton.Username, Vector3.zero, true);
        }

        private void FailedToConnect(object sender, EventArgs e)
        {
            UIManager.Singleton.BackToMain();
        }

        private void S_NewPlayerConnected(object sender, ServerConnectedEventArgs e)
        {
            GameLogicServer.Singleton.PlayerCountChanged(e.Client.Id);
        }

        private void PlayerLeft(object sender, ClientDisconnectedEventArgs e)
        {
            Destroy(PlayerServer.List[e.Id].gameObject);
            Destroy(PlayerClient.List[e.Id].gameObject);
        }

        private void DidDisconnect(object sender, DisconnectedEventArgs e)
        {
            GameLogicShared.UnloadActiveScene();
            foreach (PlayerClient player in PlayerClient.List.Values)
                Destroy(player.gameObject);

            UIManager.Singleton.BackToMain();
        }

        private void SendSync()
        {
            Message message = Message.Create(MessageSendMode.Unreliable, (ushort)MessageId.sync);
            message.AddUShort(CurrentTick);

            Server.SendToAll(message);
        }

        private void SetTick(ushort serverTick)
        {
            if (Mathf.Abs(ServerTick - serverTick) > tickDivergenceTolerance)
            {
                Debug.Log($"Client tick: {ServerTick} -> {serverTick}");
                ServerTick = serverTick;
            }
        }

        [MessageHandler((ushort)MessageId.sync)]
        public static void Sync(Message message)
        {
            Singleton.SetTick(message.GetUShort());
        }
    }
}
