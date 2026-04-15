using UnityEngine;

public class ConveyorBelt : MonoBehaviour
{
    [SerializeField] Transform direction; // Transform reference for the forward direction of the belt, since the new model is weird and forward is left and right is back, hehe :D
    [SerializeField] float speed = 1f;
    public bool isActive = true;

    void OnEnable()
    {
        ConveyorProductionManager.OnAllConveyorBeltsStateChanged += (state) => isActive = state;
    }

    void OnDisable()
    {
        ConveyorProductionManager.OnAllConveyorBeltsStateChanged -= (state) => isActive = state;
    }

    void Update()
    {
        MoveBelt();
    }

    void MoveBelt()
    {
        if(!isActive) return;
        // Move the belt visually by scrolling the texture or smth
    }

    void OnTriggerStay(Collider other)
    {
        if(isActive && other != null && !other.CompareTag("Immovable"))
        {
            var rb = other.attachedRigidbody;
            var moveDir = direction.forward * Mathf.Abs(speed);
            if (speed < 0)
                moveDir = -direction.forward * Mathf.Abs(speed);
            var clampedMoveDir = Vector3.ClampMagnitude(rb.linearVelocity + moveDir, Mathf.Abs(speed));
            rb.linearVelocity = new Vector3(clampedMoveDir.x, rb.linearVelocity.y, clampedMoveDir.z);
        }
    }
}
