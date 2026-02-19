using Riptide;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Riptide.Demos.PlayerHosted
{
    public class GameLogicClient : GameLogicShared
    {
        private static GameLogicClient _singleton;
        public static GameLogicClient Singleton
        {
            get => _singleton;
            private set
            {
                if (_singleton == null)
                    _singleton = value;
                else if (_singleton != value)
                {
                    Debug.Log($"{nameof(GameLogicClient)} instance already exists, destroying duplicate!");
                    Destroy(value);
                }
            }
        }

        public GameObject LocalPlayerPrefab => localPlayerPrefab;
        public GameObject PlayerPrefab => playerPrefab;
        public GameObject BulletPrefabClient => bulletPrefabClient;
        public GameObject TeleporterPrefabClient => teleporterPrefabClient;
        public GameObject LaserPrefabClient => laserPrefabClient;

        [Header("Prefabs")]
        [SerializeField] private GameObject localPlayerPrefab;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject bulletPrefabClient;
        [SerializeField] private GameObject teleporterPrefabClient;
        [SerializeField] private GameObject laserPrefabClient;

        private void Awake()
        {
            Singleton = this;
        }

        [MessageHandler((ushort)MessageId.activeScene)]
        private static void OnActiveScene(Message message)
        {
            GameLogicServer.Singleton.StartCoroutine(
                GameLogicClient.Singleton.SetSceneCoroutine(message.GetByte()));
        }
    }
}