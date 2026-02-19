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

    public abstract class Player<T> : MonoBehaviour where T : Player<T>
    {
        internal static Dictionary<ushort, T> List = new Dictionary<ushort, T>();

        public ushort Id { get; protected set; }
        public string Username { get; protected set; }
        public bool IsAlive => health > 0f;
        public bool IsLocal { get; protected set; }


        public float health;
        public float maxHealth = 100;
        public Transform camTransform;
        protected float respawnSeconds = 2f;

        public Team team;

        protected virtual void OnDestroy()
        {
            List.Remove(Id);
        }

        protected virtual void Start() 
        {
            health = maxHealth;
            DontDestroyOnLoad(gameObject);
        }

    }
}
