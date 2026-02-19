using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UIElements;

namespace Riptide.Demos.PlayerHosted
{
    public enum Team : byte
    {
        none,
        green,
        orange
    }

    public class Player : MonoBehaviour
    {
        internal static Dictionary<ushort, Player> List = new Dictionary<ushort, Player>();

        public ushort Id { get; private set; }
        public string Username { get; private set; }
        public bool IsAlive => health > 0f;
        public bool IsLocal { get; private set; }
        public WeaponManager WeaponManager => weaponManager;



        [SerializeField] private float respawnSeconds;
        [SerializeField] private float health;
        [SerializeField] private float maxHealth = 100;
        [SerializeField] private GameObject model;
        [SerializeField] private WeaponManager weaponManager;
        [SerializeField] private PlayerController movement;
        [SerializeField] private PlayerAnimationManager animationManager;
        [SerializeField] private Transform camTransform;
        [SerializeField] private Interpolator interpolator;
        

        [SerializeField]private Team team;

        private void OnDestroy()
        {
            List.Remove(Id);
        }

        private void OnValidate() 
        {
            if (movement == null)
                movement = GetComponent<PlayerController>();
            if (weaponManager == null)
                weaponManager = GetComponent<WeaponManager>();
        }
        private void Start() 
        {
            health = maxHealth;
            DontDestroyOnLoad(gameObject);
        }

        public void TakeDamage(float amount)
        {
            health -= amount;
            if (health <= 0f)
            {
                health = 0f;
                Die();
            }
            else
                SendHealthChanged();
        }

        private void Die()
        {
            StartCoroutine(DelayedRespawn());
            SendDied();
        }

        public void Died(Vector3 position)
        {
            transform.position = position;
            health = 0f;
            model.SetActive(false);
            weaponManager.DisableWeapons();

            if (IsLocal)
                UIManager.Singleton.HealthUpdated(health, maxHealth, true);
        }

        public void Respawned(Vector3 position)
        {
            Debug.Log($"Should respawn on position {position}");
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
            weaponManager.EnableWeapons();

            if (IsLocal)
                UIManager.Singleton.HealthUpdated(health, maxHealth, false);
        }

        private void SendDied()
        {
            Message message = Message.Create(MessageSendMode.Reliable, MessageId.playerDied);
            message.AddUShort(Id);
            message.AddVector3(transform.position);
            NetworkManager.Singleton.Server.SendToAll(message);
        }

        private void SendHealthChanged()
        {
            Message message = Message.Create(MessageSendMode.Reliable, MessageId.playerHealthChanged);
            message.AddFloat(health);
            NetworkManager.Singleton.Server.Send(message, Id);
        }

        [MessageHandler((ushort)MessageId.playerDied)]
        private static void PlayerDied(Message message)
        {
            if (List.TryGetValue(message.GetUShort(), out Player player))
                player.Died(message.GetVector3());
        }
        public void InstantRespawn()
        {
            TeleportToTeamSpawnpoint();
            health = maxHealth;
            SendRespawned();
        }

        private void TeleportToTeamSpawnpoint()
        {         
            if (team == Team.green )
                transform.position = GameLogicServer.Singleton.GreenSpawn.position;
            else if (team == Team.orange)
                transform.position = GameLogicServer.Singleton.OrangeSpawn.position;
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

        private IEnumerator DelayedRespawn()
        {
            yield return new WaitForSeconds(respawnSeconds);

            InstantRespawn();
        }

        internal static void Spawn(ushort id, string username)
        {
            foreach (Player otherPlayer in List.Values)
                otherPlayer.SendSpawned(id);

            Team team = id % 2 == 0 ? Team.orange : Team.green;

            Vector3 position = Vector3.zero;
            if (GameLogicServer.Singleton.IsGameInProgress)
            {
                if (team == Team.green)
                    position = GameLogicServer.Singleton.GreenSpawn.position;
                else if (team == Team.orange)
                    position = GameLogicServer.Singleton.OrangeSpawn.position;
            }

            SendSpawned(id, username, position, (byte)team);
        }

        internal static void OnSpawn(ushort id, string username, Vector3 position, byte team)
        {
            Player player;
            if (id == NetworkManager.Singleton.Client.Id)
            {
                player = Instantiate(NetworkManager.Singleton.LocalPlayerPrefab, position, Quaternion.identity).GetComponent<Player>();
                player.IsLocal = true;
            }
            else
            {
                player = Instantiate(NetworkManager.Singleton.PlayerPrefab, position, Quaternion.identity).GetComponent<Player>();
                player.IsLocal = false;
            }

            player.Id = id;
            player.Username = username;
            player.name = $"Player {id} ({(string.IsNullOrEmpty(username) ? "Guest" : username)})";
            player.team = (Team)team; 

            List.Add(id, player);
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
            if (List.TryGetValue(NetworkManager.Singleton.Client.Id, out Player player))
                player.SetHealth(message.GetFloat());
        }

        private void SendRespawned()
        {
            Message message = Message.Create(MessageSendMode.Reliable, MessageId.playerRespawned);
            message.AddUShort(Id);
            message.AddVector3(transform.position);
            NetworkManager.Singleton.Server.SendToAll(message);
        }

        [MessageHandler((ushort)MessageId.playerRespawned)]
        private static void PlayerRespawned(Message message)
        {
            if (List.TryGetValue(message.GetUShort(), out Player player))
                player.Respawned(message.GetVector3());
        }

        private static void SendSpawned(ushort id, string username, Vector3 position, byte team)
        {
            Message message = Message.Create(MessageSendMode.Reliable, MessageId.playerSpawned);
            message.AddUShort(id);
            message.AddString(username);
            message.AddVector3(position);
            message.AddByte((byte)team);
            NetworkManager.Singleton.Server.SendToAll(message);
        }

        private void SendSpawned(ushort toClientId)
        {
            Message message = Message.Create(MessageSendMode.Reliable, MessageId.playerSpawned);
            message.AddUShort(Id);
            message.AddString(Username);
            message.AddVector3(transform.position);
            message.AddByte((byte)team);
            NetworkManager.Singleton.Server.Send(message, toClientId);
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
            if (List.TryGetValue(playerId, out Player player))
                player.Move(message.GetUShort(),message.GetVector3(), message.GetVector3());
        }

        public static void RegisterPlayer(string username)
        {
            Message message = Message.Create(MessageSendMode.Reliable, MessageId.registerPlayer);
            message.AddString(username);
            NetworkManager.Singleton.Client.Send(message);
        }

        [MessageHandler((ushort)MessageId.registerPlayer)]
        private static void OnRegisterPlayer(ushort fromClientId, Message message)
        {

            Spawn(fromClientId, message.GetString());
        }

        [MessageHandler((ushort)MessageId.switchActiveWeapon)]
        private static void SwitchActiveWeapon(ushort fromClientId, Message message)
        {
            if (List.TryGetValue(fromClientId, out Player player))
                player.weaponManager.SetActiveWeapon((WeaponType)message.GetByte());
        }

        [MessageHandler((ushort)MessageId.primaryUse)]
        private static void PrimaryUse(ushort fromClientId, Message message)
        {
            if (List.TryGetValue(fromClientId, out Player player))
                player.weaponManager.PrimaryUsePressed();
        }

        [MessageHandler((ushort)MessageId.reload)]
        private static void Reload(ushort fromClientId, Message message)
        {
            if (List.TryGetValue(fromClientId, out Player player))
                player.weaponManager.Reload();
        }

        [MessageHandler((ushort)MessageId.playerActiveWeaponUpdated)]
        private static void PlayerActiveWeaponUpdated(Message message)
        {
            if (List.TryGetValue(message.GetUShort(), out Player player))
            {
                WeaponType newType = (WeaponType)message.GetByte();
                player.WeaponManager.SetWeaponActive(newType);

                if (player.IsLocal)
                    UIManager.Singleton.ActiveWeaponUpdated(newType);
            }
        }

        [MessageHandler((ushort)MessageId.playerAmmoChanged)]
        private static void PlayerAmmoChanged(Message message)
        {
            UIManager.Singleton.AmmoUpdated((WeaponType)message.GetByte(), message.GetByte(), message.GetUShort());
        }

        #endregion




    }
}
