using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UIElements;

namespace Riptide.Demos.PlayerHosted
{
    public class PlayerServer : Player<PlayerServer>
    {
        public WeaponManagerServer WeaponManagerServer => weaponManagerServer;

        [SerializeField] private WeaponManagerServer weaponManagerServer;
        [SerializeField] private PlayerController movement;

        private void OnValidate() 
        {
            if (movement == null)
                movement = GetComponent<PlayerController>();
            if (weaponManagerServer == null)
                weaponManagerServer = GetComponent<WeaponManagerServer>();
        }

        public void AddToList(ushort id, string username, Team team)
        {
            this.Id = id;
            this.Username = username;
            this.team = (Team)team;
            PlayerServer.List.Add(id, this);
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

        public void InstantRespawn()
        {
            TeleportToTeamSpawnpoint();

            health = maxHealth;
            weaponManagerServer.ResetWeapons();
            SendRespawned();
        }

        private void TeleportToTeamSpawnpoint()
        {         
            if (team == Team.green )
                transform.position = GameLogicServer.Singleton.GreenSpawn.position;
            else if (team == Team.orange)
                transform.position = GameLogicServer.Singleton.OrangeSpawn.position;
        }

        private IEnumerator DelayedRespawn()
        {
            yield return new WaitForSeconds(respawnSeconds);

            InstantRespawn();
        }

        internal static void Spawn(ushort id, string username)
        {
            foreach (PlayerServer otherPlayer in List.Values)
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




        #region Messages
        private void SendRespawned()
        {
            Message message = Message.Create(MessageSendMode.Reliable, MessageId.playerRespawned);
            message.AddUShort(Id);
            message.AddVector3(transform.position);
            NetworkManager.Singleton.Server.SendToAll(message);
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

        [MessageHandler((ushort)MessageId.registerPlayer)]
        private static void OnRegisterPlayer(ushort fromClientId, Message message)
        {

            Spawn(fromClientId, message.GetString());
        }
        #endregion

    }
}
