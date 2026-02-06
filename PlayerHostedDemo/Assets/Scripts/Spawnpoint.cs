using Riptide.Demos.PlayerHosted;
using UnityEngine;

public class Spawnpoint : MonoBehaviour
{
    [SerializeField] private Team team;

    private void Start()
    {
        switch (team)
        {
            case Team.none:
                GameLogicServer.Singleton.GreenSpawn = transform;
                GameLogicServer.Singleton.OrangeSpawn = transform;
                break;
            case Team.green:
                GameLogicServer.Singleton.GreenSpawn = transform;
                break;
            case Team.orange:
                GameLogicServer.Singleton.OrangeSpawn = transform;
                break;
            default:
                break;
        }
    }
}
