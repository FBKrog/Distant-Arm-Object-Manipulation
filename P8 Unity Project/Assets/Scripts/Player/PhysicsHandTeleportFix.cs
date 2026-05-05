using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PhysicsHandTeleportFix : MonoBehaviour
{
    Rigidbody rb;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        TeleportationActivator.teleport += ResetRigidbody;
    }

    void OnDisable()
    {
        TeleportationActivator.teleport -= ResetRigidbody;
    }

    void ResetRigidbody()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    void OnCollisionEnter(Collision collision)
    {
        rb.freezeRotation = true;
    }

    void OnCollisionExit(Collision collision)
    {
        rb.freezeRotation = false;
    }
}