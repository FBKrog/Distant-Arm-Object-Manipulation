using UnityEngine;

public class RespawnOutOfBounds : MonoBehaviour
{
    void OnTriggerStay(Collider other)
    {
        if(other.TryGetComponent<Respawnable>(out var respawnable))
        {
            print("nogen dummede sig lol");
            respawnable.Respawn();
        }
    }
}
