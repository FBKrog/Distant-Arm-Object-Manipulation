using UnityEngine;

public class RespawnZone : MonoBehaviour
{
    [SerializeField] string zoneName;
    [SerializeField] RespawnType respawnType;

    public enum RespawnType { OnEnter, OnExit }

    void Start()
    {
        zoneName = zoneName.ToLower();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Respawnable>(out var respawnable) && (respawnable.zoneName == zoneName || zoneName == ""))
        {
            if(respawnType == RespawnType.OnEnter)
            {
                print($"[Respawner]: {gameObject.name} has respawned {other.name}");
                respawnable.TryRespawn();
            }
            if(respawnType == RespawnType.OnExit)
            {
                print($"[Respawner]: {gameObject.name} cancelled respawn for {other.name}");
                respawnable.CancelRespawn();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<Respawnable>(out var respawnable) && (respawnable.zoneName == zoneName || zoneName == ""))
        {
            if (respawnType == RespawnType.OnExit)
            {
                print($"[Respawner]: {gameObject.name} has respawned {other.name}");
                respawnable.TryRespawn();
            }
            if (respawnType == RespawnType.OnEnter)
            {
                print($"[Respawner]: {gameObject.name} cancelled respawn for {other.name}");
                respawnable.CancelRespawn();
            }
        }
    }
}
