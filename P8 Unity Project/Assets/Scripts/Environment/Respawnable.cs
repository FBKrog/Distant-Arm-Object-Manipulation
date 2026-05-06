using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody))]
public class Respawnable : MonoBehaviour
{
    public string zoneName;
    [SerializeField] [Tooltip("Leaving this empty will use the object's position and rotation when Start() is called as the respawn point.")] Transform respawnPointTransform;
    Vector3 respawnPoint;
    Quaternion respawnRotation;
    Rigidbody rb;
    bool wasKinematic;
    bool canRespawn;

    void Start()
    {
        respawnPoint = respawnPointTransform ? respawnPointTransform.position : transform.position;
        respawnRotation = respawnPointTransform ? respawnPointTransform.rotation : transform.rotation;
        rb = GetComponent<Rigidbody>();
        wasKinematic = rb.isKinematic ? true : false;
        zoneName = zoneName.ToLower();
        canRespawn = true;
    }

    void OnEnable()
    {
        if (TryGetComponent<XRGrabInteractable>(out var grabInteractable))
        {
            grabInteractable.selectEntered.AddListener(_ => canRespawn = false);
            grabInteractable.selectExited.AddListener(_ => canRespawn = true);
        }
    }

    void OnDisable()
    {
        if (TryGetComponent<XRGrabInteractable>(out var grabInteractable))
        {
            grabInteractable.selectEntered.RemoveListener(_ => canRespawn = false);
            grabInteractable.selectExited.RemoveListener(_ => canRespawn = true);
        }
    }

    public void Respawn()
    {
        print($"[Respawnable]: {gameObject.name} attempted to respawn");
        if (!canRespawn) return;
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = respawnPoint;
        transform.rotation = respawnRotation;
        rb.isKinematic = wasKinematic;
    }
}
