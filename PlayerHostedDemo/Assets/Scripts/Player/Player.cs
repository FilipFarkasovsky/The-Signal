using System.Collections;
using System.Collections.Generic;
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


        [SerializeField] private float respawnSeconds;
        [SerializeField] private float health;
        [SerializeField] private float maxHealth;
        //[SerializeField] private PlayerMovement movement;
        //[SerializeField] private WeaponManager weaponManager;

        private Team team;

        private void OnDestroy()
        {
            List.Remove(Id);
        }

        private void OnValidate() //S
        {
            //if (movement == null)
            //    movement = GetComponent<PlayerMovement>();
            //if (weaponManager == null)
            //    weaponManager = GetComponent<WeaponManager>();
        }
        private void Start() //S
        {
            DontDestroyOnLoad(gameObject);
        }

        public void InstantRespawn()
        {
            TeleportToTeamSpawnpoint();
            //if (movement != null)
            //    movement.Enabled(true);

            health = maxHealth;
            SendRespawned();
        }

        private void TeleportToTeamSpawnpoint()
        {
            //if (movement == null) 
            //    return;

            //if (team == Team.green )
            //    movement.Teleport(GameLogicServer.Singleton.GreenSpawn.position);
            //else if (team == Team.orange)
            //    movement.Teleport(GameLogicServer.Singleton.OrangeSpawn.position);            

            if (team == Team.green )
                Move(GameLogicServer.Singleton.GreenSpawn.position, Vector3.forward);
            else if (team == Team.orange)
                Move(GameLogicServer.Singleton.OrangeSpawn.position, Vector3.forward);
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
                player = Instantiate(NetworkManager.Singleton.LocalPlayerPrefab, position, Quaternion.identity).GetComponent<Player>();
            else
                player = Instantiate(NetworkManager.Singleton.PlayerPrefab, position, Quaternion.identity).GetComponent<Player>();

            player.Id = id;
            player.Username = username;
            player.name = $"Player {id} ({(string.IsNullOrEmpty(username) ? "Guest" : username)})";
            player.team = (Team)team; 

            List.Add(id, player);
        }


        #region Messages
        [MessageHandler((ushort)MessageId.playerRespawned)]
        private static void PlayerRespawned(Message message)
        {
            if (List.TryGetValue(message.GetUShort(), out Player player))
                player.Respawned(message.GetVector3());
        }

        [MessageHandler((ushort)MessageId.playerSpawned)]
        private static void SpawnPlayer(Message message)
        {
            OnSpawn(message.GetUShort(), message.GetString(), message.GetVector3(), message.GetByte());
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

        public void Respawned(Vector3 position)
        {
            CharacterController cc = GetComponent<CharacterController>();

            if (cc != null)
            {
                // Disable the CharacterController to avoid collision issues
                cc.enabled = false;

                // Teleport
                transform.position = position;

                // Re-enable the CharacterController
                cc.enabled = true;
            }
            else
            {
                transform.position = position; // fallback
            }

            transform.position = position;
            health = maxHealth;
            //model.SetActive(true);
            //weaponManager.EnableWeapons();

            //if (IsLocal)
            //    UIManager.Singleton.HealthUpdated(health, maxHealth, false);
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

        private void SendRespawned()
        {
            Message message = Message.Create(MessageSendMode.Reliable, MessageId.playerRespawned);
            message.AddUShort(Id);
            message.AddVector3(transform.position);
            Debug.Log(transform.position);
            NetworkManager.Singleton.Server.SendToAll(message);
        }

        [MessageHandler((ushort)MessageId.registerPlayer)]
        private static void OnRegisterPlayer(ushort fromClientId, Message message)
        {

            Spawn(fromClientId, message.GetString());
        }
        #endregion
 
        private void Move(Vector3 newPosition, Vector3 forward)
        {
            transform.position = newPosition;
            forward.y = 0;
            transform.forward = forward.normalized;
        }



        #region Messages


        internal void SendSpawn(ushort newPlayerId)
        {
            Message message = Message.Create(MessageSendMode.Reliable, MessageId.SpawnPlayer);
            message.AddUShort(Id);
            message.AddString(Username);
            message.AddVector3(transform.position);
            NetworkManager.Singleton.Server.Send(message, newPlayerId);
        }

        [MessageHandler((ushort)MessageId.PlayerMovement)]
        private static void PlayerMovement(Message message)
        {
            ushort playerId = message.GetUShort();
            if (List.TryGetValue(playerId, out Player player))
                player.Move(message.GetVector3(), message.GetVector3());
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
