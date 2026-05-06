using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PhysicsHandTeleportFix : MonoBehaviour
{
    Rigidbody rb;

    Vector3 previousPosition;
    Quaternion previousRotation;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        TeleportBlink.teleportStarted += PrepareForTeleport;
        TeleportBlink.teleportEnded += ResetRigidbody;
    }

    void OnDisable()
    {
        TeleportBlink.teleportStarted -= PrepareForTeleport;
        TeleportBlink.teleportEnded -= ResetRigidbody;
    }

    void PrepareForTeleport()
    {
        previousPosition = transform.position;
        previousRotation = transform.rotation;
        rb.isKinematic = true;
    }

    void ResetRigidbody()
    {
        transform.position = previousPosition;
        transform.rotation = previousRotation;
        rb.isKinematic = false;
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