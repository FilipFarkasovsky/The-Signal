using Riptide;
using Riptide.Demos.PlayerHosted;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileClient : MonoBehaviour
{
    public static Dictionary<ushort, ProjectileClient> list = new Dictionary<ushort, ProjectileClient>();

    [SerializeField] private WeaponType type;
    private ushort id;

    private void OnDestroy()
    {
        list.Remove(id);
    }

    public static void Spawn(ushort id, WeaponType type, ushort shooterId, Vector3 position, Vector3 direction)
    {
        PlayerClient.List[shooterId].WeaponManagerClient.Shot(type);

        ProjectileClient projectile;
        switch (type)
        {
            case WeaponType.pistol:
                projectile = Instantiate(GameLogicClient.Singleton.BulletPrefabClient, position, Quaternion.LookRotation(direction)).GetComponent<ProjectileClient>();
                break;
            case WeaponType.teleporter:
                projectile = Instantiate(GameLogicClient.Singleton.TeleporterPrefabClient, position, Quaternion.LookRotation(direction)).GetComponent<ProjectileClient>();
                break;
            case WeaponType.laser:
                projectile = Instantiate(GameLogicClient.Singleton.LaserPrefabClient, position, Quaternion.LookRotation(direction)).GetComponent<ProjectileClient>();
                break;
            default:
                Debug.LogError($"Can't spawn unknown projectile type '{type}'!");
                return;
        }

        projectile.name = $"Projectile {id}";
        projectile.id = id;

        list.Add(id, projectile);
    }

    #region Messages
    [MessageHandler((ushort)MessageId.projectileSpawned)]
    private static void ProjectileSpawned(Message message)
    {
        Spawn(message.GetUShort(), (WeaponType)message.GetByte(), message.GetUShort(), message.GetVector3(), message.GetVector3());
    }

    [MessageHandler((ushort)MessageId.projectileMovement)]
    private static void ProjectileMovement(Message message)
    {
        if (list.TryGetValue(message.GetUShort(), out ProjectileClient projectile))
            projectile.transform.position = message.GetVector3();
    }

    [MessageHandler((ushort)MessageId.projectileCollided)]
    private static void ProjectileCollided(Message message)
    {
        if (list.TryGetValue(message.GetUShort(), out ProjectileClient projectile))
            Destroy(projectile.gameObject);
    }

    [MessageHandler((ushort)MessageId.projectileHitmarker)]
    private static void ProjectileHitmarker(Message message)
    {
        //UIManager.Singleton.ShowHitmarker();
    }
    #endregion
}
