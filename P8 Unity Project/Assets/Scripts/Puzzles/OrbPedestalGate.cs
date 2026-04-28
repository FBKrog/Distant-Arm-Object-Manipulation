using UnityEngine;
using System.Collections;

public class OrbPedestalGate : MonoBehaviour
{
    [SerializeField] OrbPedestal orbPedestal;

    Vector3 targetPosition;

    void Awake()
    {
        targetPosition = transform.position;
        transform.position = new(targetPosition.x, targetPosition.y - 1.3f, targetPosition.z); // start below the floor
        orbPedestal.enabled = false;
    }

    public void Activate()
    {
        StartCoroutine(ActivatePedestal());
    }

    /// <summary>
    /// Raises the pedestal up to its target position and activates it when its fully raised.
    /// </summary>
    IEnumerator ActivatePedestal()
    {
        float elapsedTime = 0f;
        float duration = 1f;
        Vector3 startPosition = transform.position;
        while (elapsedTime < duration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPosition;
        orbPedestal.enabled = true;
    }
}
