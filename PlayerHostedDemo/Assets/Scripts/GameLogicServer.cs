using Riptide;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Riptide.Demos.PlayerHosted
{
    public class GameLogicServer : MonoBehaviour
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

        [SerializeField] private float roundLengthSeconds;

        public GameObject LocalPlayerPrefab => localPlayerPrefab;
        public GameObject PlayerPrefab => playerPrefab;
        public GameObject BulletPrefabClient => bulletPrefabClient;
        public GameObject TeleporterPrefabClient => teleporterPrefabClient;
        public GameObject LaserPrefabClient => laserPrefabClient;
        public GameObject BulletPrefabServer => bullePrefabServer;
        public GameObject TeleporterPrefabServer => teleportePrefabServer;
        public GameObject LaserPrefabServer => lasePrefabServer;

        [Header("Prefabs")]
        [SerializeField] private GameObject localPlayerPrefab;
        [SerializeField] private GameObject playerPrefab;

        [SerializeField] private GameObject bulletPrefabClient;
        [SerializeField] private GameObject teleporterPrefabClient;
        [SerializeField] private GameObject laserPrefabClient;

        [SerializeField] private GameObject bullePrefabServer;
        [SerializeField] private GameObject teleportePrefabServer;
        [SerializeField] private GameObject lasePrefabServer;

        private byte activeScene;

        private void Awake()
        {
            Singleton = this;
        }

        public void PlayerCountChanged(ushort clientId)
        {
            SendActiveScene(clientId);

            if (NetworkManager.Singleton.Server.ClientCount >= 2 && activeScene == 1)
                StartCoroutine(LobbyCountdown()); // Start game when 2 or more players are connected
        }

        private IEnumerator LobbyCountdown()
        {
            yield return new WaitForSeconds(10f);

            if (NetworkManager.Singleton.Server.ClientCount >= 2)
            {
                StartCoroutine(LoadSceneInBackground(2));
                StartCoroutine(GameCountdown());
            }
        }

        private IEnumerator GameCountdown()
        {
            yield return new WaitForSeconds(roundLengthSeconds);

            StartCoroutine(LoadSceneInBackground(1));
            StartCoroutine(LobbyCountdown());
        }

        public void LoadScene(byte sceneBuildIndex)
        {
            StartCoroutine(LoadSceneInBackground(sceneBuildIndex));
        }

        private IEnumerator LoadSceneInBackground(byte sceneBuildIndex)
        {
            if (activeScene > 0)
                SceneManager.UnloadSceneAsync(activeScene);

            activeScene = sceneBuildIndex;
            SendNewActiveScene();
 

            AsyncOperation loadingScene = SceneManager.LoadSceneAsync(sceneBuildIndex, LoadSceneMode.Additive);
            while (!loadingScene.isDone)
                yield return new WaitForSeconds(0.25f);
            SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(sceneBuildIndex));
            
            foreach (Player player in Player.List.Values)
                player.InstantRespawn();
        }
        private void SendActiveScene(ushort toClientId)
        {
            Message message = Message.Create(MessageSendMode.Reliable, MessageId.activeScene);
            message.AddByte(activeScene);
            NetworkManager.Singleton.Server.Send(message, toClientId);
        }

        private void SendNewActiveScene()
        {
            Message message = Message.Create(MessageSendMode.Reliable, MessageId.activeScene);
            message.AddByte(activeScene);
            NetworkManager.Singleton.Server.SendToAll(message);
        }
        [MessageHandler((ushort)MessageId.activeScene)]
        private static void OnActiveScene(Message message)
        {
            GameLogicServer.Singleton.StartCoroutine(
                GameLogicServer.Singleton.SetScene(message.GetByte()));
        }

        private IEnumerator SetScene(byte sceneBuildIndex)
        {
            if(sceneBuildIndex == activeScene &&
                !SceneManager.GetSceneByBuildIndex(sceneBuildIndex).isLoaded)
                yield break;

            if(activeScene == 0 && sceneBuildIndex == 0)
                yield break;

            if (activeScene > 0)
            {
                SceneManager.UnloadSceneAsync(activeScene);
                activeScene = 0;
            }
            activeScene = sceneBuildIndex;
            AsyncOperation loadingScene = SceneManager.LoadSceneAsync(sceneBuildIndex, LoadSceneMode.Additive);
            while (!loadingScene.isDone)
                yield return new WaitForSeconds(0.25f);

            SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(sceneBuildIndex));
        }
    }
}