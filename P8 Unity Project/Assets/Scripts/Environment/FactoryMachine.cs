using UnityEngine;
using UnityEngine.VFX;

public class FactoryMachine : MonoBehaviour
{
    [SerializeField] VisualEffect spark;
    [SerializeField] GameObject ikTarget;
    [SerializeField] float speed = 1f;
    [SerializeField] float distanceOffset = 0.5f;
    [SerializeField] float sparkDistanceThreshold = 0.2f;
    [SerializeField] Vector3 lookOffset = new(180, 0, 0);
    Vector3 initialPos;
    Quaternion initialRot;
    float distance;
    float currentDistance;
    float closestDistance = Mathf.Infinity;
    GameObject currentPart;
    bool isSparking;

    void Start()
    {
        initialPos = ikTarget.transform.position;
        initialRot = ikTarget.transform.rotation;
        spark.Stop();
    }

    void Update()
    {
        if (currentPart != null)
        {
            distance = Vector3.Distance(transform.position, currentPart.transform.position);
            if (distance < closestDistance)
                closestDistance = distance;
            if(currentDistance - distanceOffset < sparkDistanceThreshold)
                ToggleSpark(true);
            currentDistance = Mathf.Clamp(distance - distanceOffset, 0, distance);
            var targetPos = Vector3.Lerp(initialPos, currentPart.transform.position, 1 - (currentDistance / distance));
            ikTarget.transform.position = Vector3.Lerp(ikTarget.transform.position, targetPos, Time.deltaTime * speed);
            ikTarget.transform.rotation = Quaternion.Lerp(ikTarget.transform.rotation, LookDirection(currentPart.transform.position) * Quaternion.Euler(lookOffset), Time.deltaTime * speed);
        }
        else
        {
            ToggleSpark(false);
            closestDistance = Mathf.Infinity;
            ikTarget.transform.position = Vector3.Lerp(ikTarget.transform.position, initialPos, Time.deltaTime * speed);
            ikTarget.transform.rotation = Quaternion.Lerp(ikTarget.transform.rotation, initialRot, Time.deltaTime * speed);
        }
    }

    void ToggleSpark(bool state)
    {
        if (state && !isSparking)
        {
            spark.Play();
            AudioManager.Play(SfxType.FactoryMachine, transform);
            isSparking = true;
        }
        else if (!state && isSparking)
        { 
            spark.Stop();
            isSparking = false;
        }
    }

    /// <summary>
    /// Calculates the rotation required to face the specified target position from this transform's position.
    /// </summary>
    Quaternion LookDirection(Vector3 target)
    {
        var direction = (target - transform.position).normalized;
        var rotation = Quaternion.LookRotation(direction, Vector3.up);
        return rotation;
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent<RobotPart>(out var robotPart))
        {
            currentPart = robotPart.gameObject;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<RobotPart>(out var robotPart))
        {
            if (currentPart == robotPart.gameObject)
                currentPart = null;
        }
    }
}
