using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UIElements;

namespace Riptide.Demos.PlayerHosted
{
    public class PlayerClient : Player<PlayerClient>
    {
        public WeaponManagerClient WeaponManagerClient => weaponManagerClient;
        public WeaponManagerServer WeaponManagerServer => weaponManagerServer;



        [SerializeField] private float respawnSeconds;
        [SerializeField] private GameObject model;
        [SerializeField] private WeaponManagerClient weaponManagerClient;
        [SerializeField] private WeaponManagerServer weaponManagerServer;
        [SerializeField] private PlayerController movement;
        [SerializeField] private PlayerAnimationManager animationManager;
        [SerializeField] private Interpolator interpolator;

        private void OnValidate() 
        {
            if (movement == null)
                movement = GetComponent<PlayerController>();
            if (weaponManagerClient == null)
                weaponManagerClient = GetComponent<WeaponManagerClient>();
        }

        public void Died(Vector3 position)
        {
            transform.position = position;
            health = 0f;
            model.SetActive(false);
            weaponManagerClient.DisableWeapons();

            if (IsLocal)
                UIManager.Singleton.HealthUpdated(health, maxHealth, true);
        }

        public void Respawned(Vector3 position)
        {
            CharacterController cc = GetComponent<CharacterController>();

            if (cc != null)
            {
                cc.enabled = false;
                transform.position = position;
                cc.enabled = true;
            }
            else
            {
                transform.position = position; // fallback
            }

            transform.position = position;
            health = maxHealth;


            model.SetActive(true);
            weaponManagerClient.EnableWeapons();

            if (IsLocal)
                UIManager.Singleton.HealthUpdated(health, maxHealth, false);
        }

        [MessageHandler((ushort)MessageId.playerDied)]
        private static void PlayerDied(Message message)
        {
            if (List.TryGetValue(message.GetUShort(), out PlayerClient player))
                player.Died(message.GetVector3());
        }

        private void Move(ushort tick, Vector3 newPosition, Vector3 forward, bool isTeleport = false)
        {
            if (!IsLocal)
            {
                interpolator.NewUpdate(tick, isTeleport, newPosition, forward);
                animationManager.AnimateBasedOnSpeed();

            }

            /*
            transform.position = newPosition;
            forward.y = 0;
            transform.forward = forward.normalized;
            */
        }

        internal static void OnSpawn(ushort id, string username, Vector3 position, byte team)
        {
            PlayerClient player;
            if (id == NetworkManager.Singleton.Client.Id)
            {
                player = Instantiate(NetworkManager.Singleton.LocalPlayerPrefab, position, Quaternion.identity).GetComponent<PlayerClient>();
                player.IsLocal = true;
            }
            else
            {
                player = Instantiate(NetworkManager.Singleton.PlayerPrefab, position, Quaternion.identity).GetComponent<PlayerClient>();
                player.IsLocal = false;
            }

            player.Id = id;
            player.Username = username;
            player.name = $"Player {id} ({(string.IsNullOrEmpty(username) ? "Guest" : username)})";
            player.team = (Team)team; 


            PlayerClient.List.Add(id, player);

            PlayerServer playerServer = player.GetComponent<PlayerServer>();

            if (playerServer != null)
                playerServer.AddToList(id, username, (Team)team);
        }


        #region Messages
  


        public void SetHealth(float amount)
        {
            health = Mathf.Clamp(amount, 0f, maxHealth);
            UIManager.Singleton.HealthUpdated(health, maxHealth, true);
        }

        [MessageHandler((ushort)MessageId.playerHealthChanged)]
        private static void PlayerHealthChanged(Message message)
        {
            if (List.TryGetValue(NetworkManager.Singleton.Client.Id, out PlayerClient player))
                player.SetHealth(message.GetFloat());
        }


        [MessageHandler((ushort)MessageId.playerRespawned)]
        private static void PlayerRespawned(Message message)
        {
            if (List.TryGetValue(message.GetUShort(), out PlayerClient player))
                player.Respawned(message.GetVector3());
        }


        [MessageHandler((ushort)MessageId.playerSpawned)]
        private static void SpawnPlayer(Message message)
        {
            OnSpawn(message.GetUShort(), message.GetString(), message.GetVector3(), message.GetByte());
        }

        [MessageHandler((ushort)MessageId.PlayerMovement)]
        private static void OnPlayerMovement(Message message)
        {
            ushort playerId = message.GetUShort();
            if (List.TryGetValue(playerId, out PlayerClient player))
                player.Move(message.GetUShort(),message.GetVector3(), message.GetVector3());
        }

        public static void RegisterPlayer(string username)
        {
            Message message = Message.Create(MessageSendMode.Reliable, MessageId.registerPlayer);
            message.AddString(username);
            NetworkManager.Singleton.Client.Send(message);
        }
        #endregion

    }
}
