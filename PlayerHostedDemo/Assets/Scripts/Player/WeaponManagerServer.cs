using Riptide;
using Riptide.Demos.PlayerHosted;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManagerServer : MonoBehaviour
{
    [SerializeField] private PlayerServer player;
    [SerializeField] private Gun pistol;
    [SerializeField] private Gun teleporter;
    [SerializeField] private Gun laser;

    private WeaponType activeType;
    private Gun activeWeapon;

    private void OnValidate()
    {
        if (player == null)
            player = GetComponent<PlayerServer>();
    }

    public void SetActiveWeapon(WeaponType type)
    {
        if (activeType == type)
            return;

        switch (type)
        {
            case WeaponType.none:
                activeWeapon = null;
                break;
            case WeaponType.pistol:
                activeWeapon = pistol;
                break;
            case WeaponType.teleporter:
                activeWeapon = teleporter;
                break;
            case WeaponType.laser:
                activeWeapon = laser;
                break;
            default:
                Debug.LogError($"Can't set unknown weapon type '{type}' as active!");
                return;
        }

        activeType = type;
        SendActiveWeaponUpdate(type);
    }

    public void PrimaryUsePressed()
    {
        if (activeWeapon == null)
            return;

        activeWeapon.Shoot();
    }

    public void Reload()
    {
        if (activeWeapon == null)
            return;

        activeWeapon.Reload();
    }

    public void ResetWeapons()
    {
        pistol.ResetAmmo();
        teleporter.ResetAmmo();
        laser.ResetAmmo();
    }

    private void SendActiveWeaponUpdate(WeaponType type)
    {
        Message message = Message.Create(MessageSendMode.Reliable, MessageId.playerActiveWeaponUpdated);
        message.AddUShort(player.Id);
        message.AddByte((byte)type);
        NetworkManager.Singleton.Server.SendToAll(message);
    }

    [MessageHandler((ushort)MessageId.primaryUse)]
    private static void PrimaryUse(ushort fromClientId, Message message)
    {
        if (PlayerServer.List.TryGetValue(fromClientId, out PlayerServer player))
            player.WeaponManagerServer.PrimaryUsePressed();
    }

    [MessageHandler((ushort)MessageId.reload)]
    private static void Reload(ushort fromClientId, Message message)
    {
        if (PlayerServer.List.TryGetValue(fromClientId, out PlayerServer player))
            player.WeaponManagerServer.Reload();
    }

    [MessageHandler((ushort)MessageId.switchActiveWeapon)]
    private static void SwitchActiveWeapon(ushort fromClientId, Message message)
    {
        if (PlayerServer.List.TryGetValue(fromClientId, out PlayerServer player))
            player.WeaponManagerServer.SetActiveWeapon((WeaponType)message.GetByte());
    }
}
