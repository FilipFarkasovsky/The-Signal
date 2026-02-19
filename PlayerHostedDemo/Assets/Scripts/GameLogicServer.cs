using Riptide;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Riptide.Demos.PlayerHosted
{
    public class GameLogicServer : GameLogicShared
    {
        private static GameLogicServer _singleton;
        public static GameLogicServer Singleton
        {
            get => _singleton;
            private set
            {
                if (_singleton == null)
                    _singleton = value;
                else if (_singleton != value)
                {
                    Debug.Log($"{nameof(GameLogicServer)} instance already exists, destroying duplicate!");
                    Destroy(value);
                }
            }
        }

        public bool IsGameInProgress => activeScene == 2;
        public Transform GreenSpawn { get; set; }
        public Transform OrangeSpawn { get; set; }

        [SerializeField] private float roundLengthSeconds = 300;

        public GameObject ServerPlayerPrefab => serverPlayerPrefab;
        public GameObject BulletPrefabServer => bullePrefabServer;
        public GameObject TeleporterPrefabServer => teleportePrefabServer;
        public GameObject LaserPrefabServer => laserPrefabServer;

        [Header("Prefabs")]
        [SerializeField] private GameObject serverPlayerPrefab;

        [SerializeField] private GameObject bullePrefabServer;
        [SerializeField] private GameObject teleportePrefabServer;
        [SerializeField] private GameObject laserPrefabServer;

        private void Awake()
        {
            Singleton = this;
        }

        public void PlayerCountChanged(ushort clientId)
        {
            SendActiveScene(clientId, activeScene);

            if (NetworkManager.Singleton.Server.ClientCount >= 2 && activeScene == 1)
                StartCoroutine(LobbyCountdown()); // Start game when 2 or more players are connected
        }

        private IEnumerator LobbyCountdown()
        {
            yield return new WaitForSeconds(10f);

            if (NetworkManager.Singleton.Server.ClientCount >= 2)
            {
                StartCoroutine(SetSceneAndRespawn(2));
                StartCoroutine(GameCountdown());
            }
        }

        private IEnumerator GameCountdown()
        {
            yield return new WaitForSeconds(roundLengthSeconds);

            StartCoroutine(SetSceneAndRespawn(1));
            StartCoroutine(LobbyCountdown());
        }

        private IEnumerator SetSceneAndRespawn(byte sceneIndex)
        {
            SendNewActiveScene(sceneIndex);
            yield return SetSceneCoroutine(sceneIndex);

            foreach (PlayerServer player in PlayerServer.List.Values)
                player.InstantRespawn();
        }

        private void SendActiveScene(ushort toClientId, byte sceneIndex)
        {
            Message message = Message.Create(MessageSendMode.Reliable, MessageId.activeScene);
            message.AddByte(sceneIndex);
            NetworkManager.Singleton.Server.Send(message, toClientId);
        }

        private void SendNewActiveScene(byte sceneIndex)
        {
            Message message = Message.Create(MessageSendMode.Reliable, MessageId.activeScene);
            message.AddByte(sceneIndex);
            NetworkManager.Singleton.Server.SendToAll(message);
        }
    }
}