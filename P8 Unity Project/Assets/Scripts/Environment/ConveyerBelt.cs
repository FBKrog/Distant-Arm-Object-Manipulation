using UnityEngine;

public class ConveyerBelt : MonoBehaviour
{
    [SerializeField] bool isActive = true;
    [SerializeField] GameObject[] belt;
    [SerializeField] float speed = 1f;

    void Update()
    {
        if(!isActive) return;
        foreach(var cyllinder in belt)
        {
            cyllinder.transform.localRotation *= Quaternion.Euler(0, speed * Time.deltaTime * -360, 0);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if(isActive && other != null && !other.CompareTag("Immovable"))
        {
            var rb = other.attachedRigidbody;
            var moveDir = transform.forward * Mathf.Abs(speed);
            if(speed < 0)
                moveDir = -transform.forward * Mathf.Abs(speed);
            var clampedMoveDir = Vector3.ClampMagnitude(rb.linearVelocity + moveDir, Mathf.Abs(speed));
            rb.linearVelocity = new Vector3(clampedMoveDir.x, rb.linearVelocity.y, clampedMoveDir.z);
        }
    }
}
