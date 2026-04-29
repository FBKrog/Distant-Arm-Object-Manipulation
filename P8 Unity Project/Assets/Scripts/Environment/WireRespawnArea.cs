using UnityEngine;

public class WireRespawnArea : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent<WireRespawnable>(out var respawnable))
        {
            respawnable.Respawn();
        }
    }
}
