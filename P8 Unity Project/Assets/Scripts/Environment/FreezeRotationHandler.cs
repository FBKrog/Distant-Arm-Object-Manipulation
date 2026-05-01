using UnityEngine;
using System.Collections.Generic;

public class FreezeRotationHandler : MonoBehaviour
{
    List<Collider> colliders = new();
    void Awake()
    {
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.freezeRotation = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<ConveyorBelt>(out var conveyorBelt))
        {
            if (TryGetComponent<Rigidbody>(out var rb))
            {
                rb.freezeRotation = true;
                colliders.Add(other);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<ConveyorBelt>(out var conveyorBelt))
        {
            if (TryGetComponent<Rigidbody>(out var rb))
            {
                if(colliders.Contains(other))
                    colliders.Remove(other);
                if (colliders.Count > 0) return;
                rb.freezeRotation = false;
            }
        }
    }
}
