using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Respawnable : MonoBehaviour
{
    [SerializeField] [Tooltip("Leaving this empty will use the object's position and rotation when Start() is called as the respawn point.")] Transform respawnPointTransform;
    Vector3 respawnPoint;
    Quaternion respawnRotation;
    Rigidbody rb;
    bool wasKinematic;

    void Start()
    {
        respawnPoint = respawnPointTransform ? respawnPointTransform.position : transform.position;
        respawnRotation = respawnPointTransform ? respawnPointTransform.rotation : transform.rotation;
        rb = GetComponent<Rigidbody>();
        wasKinematic = rb.isKinematic ? true : false;
    }

    public void Respawn()
    {
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = respawnPoint;
        transform.rotation = respawnRotation;
        rb.isKinematic = wasKinematic;
    }
}
