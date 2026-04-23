using System;
using UnityEngine;

public class HandCollision : MonoBehaviour
{
    [Header("Transforms")]
    [SerializeField] Transform fingerTip;
    [SerializeField] Transform shoulder;
    [Header("Collider")]
    [SerializeField] CapsuleCollider armCollider;
    [SerializeField] float radius = 0.1f;
    [Header("Toggles")]
    [SerializeField] MonoBehaviour[] scriptsToToggle;
    [SerializeField] GameObject[] objectsToToggle;

    void Start()
    {
        if(armCollider == null)
            armCollider = GetComponent<CapsuleCollider>();
    }

    void Update()
    {
        SetCollider();
    }

    void SetCollider()
    {
        Vector3 start = shoulder.position;
        Vector3 end = fingerTip.position;

        Vector3 dir = end - start;
        float distance = dir.magnitude;

        // Position in the middle
        transform.position = start + dir * 0.5f;

        // Rotate to match direction
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);

        // Set capsule size
        armCollider.height = distance + radius * 2f;
        armCollider.radius = radius;
    }


    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Environment") || other.CompareTag("Immovable"))
            SetScripts(false);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Environment") || other.CompareTag("Immovable"))
            SetScripts(true);
    }

    void SetScripts(bool state)
    {
        for (int i = 0; i < scriptsToToggle.Length; i++)
        {
            if (scriptsToToggle[i] != null)
                scriptsToToggle[i].enabled = state;
        }
    }
}
