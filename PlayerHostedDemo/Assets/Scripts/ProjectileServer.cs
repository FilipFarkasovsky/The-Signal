using Riptide;
using Riptide.Demos.PlayerHosted;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileServer : MonoBehaviour
{
    public static Dictionary<ushort, ProjectileServer> list = new Dictionary<ushort, ProjectileServer>();

    [SerializeField] private WeaponType type;
    [SerializeField] private float gravity;
    [SerializeField] private float damage;
    [SerializeField] private int laserBounces;

    private ushort id;
    private PlayerServer shooter;
    private float gravityAcceleration;
    private Vector3 velocity;

    private void Start()
    {
        gravityAcceleration = gravity * Time.fixedDeltaTime * Time.fixedDeltaTime;
        transform.rotation = Quaternion.LookRotation(velocity);
    }

    private void FixedUpdate()
    {
        velocity.y += gravityAcceleration;
        Vector3 nextPosition = transform.position + velocity;
        
        if (Physics.Raycast(transform.position, velocity.normalized, out RaycastHit hitInfo, velocity.magnitude))
        {
            PlayerServer hitPlayer = hitInfo.collider.GetComponent<PlayerServer>();

            if (hitPlayer == null) // Hit a non player
            {
                Hit(hitInfo);
                return;
            }
            else if (hitPlayer.Id != shooter.Id) // Hit another player
            {
                Hit(hitInfo, hitPlayer);
                return;
            }
        }

        transform.position = nextPosition;
        SendMovement();
    }

    private void Collide(Vector3 position)
    {
        transform.position = position;
        SendCollided();
        Destroy(gameObject);
    }

    private void Hit(RaycastHit hitInfo)
    {
        //if (type == WeaponType.teleporter)
        //    shooter.Movement.Teleport(hitInfo.point + hitInfo.normal);
        if (type == WeaponType.laser && laserBounces > 0)
        {
            // Allow lasers to bounce off objects, doing more damage with each bounce
            transform.position = hitInfo.point;
            velocity = Vector3.Reflect(velocity, hitInfo.normal);
            damage *= 2f;
            laserBounces--;
            return;
        }

        Collide(hitInfo.point);
    }
    private void Hit(RaycastHit hitInfo, PlayerServer player)
    {
        Collide(hitInfo.point);

        switch (type)
        {
            case WeaponType.pistol:
            case WeaponType.laser:
                player.TakeDamage(damage);
                SendHitmarker();
                break;
            case WeaponType.teleporter:
                //shooter.Movement.Teleport(hitInfo.point + hitInfo.normal);
                break;
            default:
                Debug.LogError($"Can't execute hit logic for unknown projectile type '{type}'!");
                break;
        }
    }

    private void OnDestroy()
    {
        list.Remove(id);
    }

    public static void Spawn(PlayerServer shooter, WeaponType type, Vector3 position, Vector3 initialVelocity)
    {
        ProjectileServer projectile;
        switch (type)
        {
            case WeaponType.pistol:
                projectile = Instantiate(GameLogicServer.Singleton.BulletPrefabServer, position, Quaternion.LookRotation(initialVelocity)).GetComponent<ProjectileServer>();
                break;
            case WeaponType.teleporter:
                projectile = Instantiate(GameLogicServer.Singleton.TeleporterPrefabServer, position, Quaternion.LookRotation(initialVelocity)).GetComponent<ProjectileServer>();
                break;
            case WeaponType.laser:
                projectile = Instantiate(GameLogicServer.Singleton.LaserPrefabServer, position, Quaternion.LookRotation(initialVelocity)).GetComponent<ProjectileServer>();
                break;
            default:
                Debug.LogError($"Can't spawn unknown projectile type '{type}'!");
                return;
        }

        ushort id = NextId;
        projectile.name = $"Projectile {id}";
        projectile.id = id;
        projectile.shooter = shooter;
        projectile.type = type;
        projectile.velocity = initialVelocity;

        projectile.SendSpawned();
        list.Add(id, projectile);
    }

    private static ushort _nextId;
    private static ushort NextId
    {
        get => _nextId++;
    }

    #region Messages
    private void SendSpawned()
    {
        Message message = Message.Create(MessageSendMode.Reliable, MessageId.projectileSpawned);
        message.AddUShort(id);
        message.AddByte((byte)type);
        message.AddUShort(shooter.Id);
        message.AddVector3(transform.position);
        message.AddVector3(transform.forward);
        NetworkManager.Singleton.Server.SendToAll(message);
    }

    private void SendMovement()
    {
        Message message = Message.Create(MessageSendMode.Unreliable, MessageId.projectileMovement);
        message.AddUShort(id);
        message.AddVector3(transform.position);
        NetworkManager.Singleton.Server.SendToAll(message);
    }

    private void SendCollided()
    {
        Message message = Message.Create(MessageSendMode.Reliable, MessageId.projectileCollided);
        message.AddUShort(id);
        message.AddVector3(transform.position);
        NetworkManager.Singleton.Server.SendToAll(message);
    }

    private void SendHitmarker()
    {
        Message message = Message.Create(MessageSendMode.Reliable, MessageId.projectileHitmarker);
        NetworkManager.Singleton.Server.Send(message, shooter.Id);
    }
    #endregion
}
