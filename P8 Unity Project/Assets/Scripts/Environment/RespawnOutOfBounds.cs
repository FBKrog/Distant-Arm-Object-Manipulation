using UnityEngine;

public class RespawnOutOfBounds : MonoBehaviour
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
        if (respawnType == RespawnType.OnExit) return;
        if (other.TryGetComponent<Respawnable>(out var respawnable) && (respawnable.zoneName == zoneName || zoneName == ""))
        {
            respawnable.Respawn();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if(respawnType == RespawnType.OnEnter) return;
        if (other.TryGetComponent<Respawnable>(out var respawnable) && (respawnable.zoneName == zoneName || zoneName == ""))
        {
            respawnable.Respawn();
        }
    }
}
