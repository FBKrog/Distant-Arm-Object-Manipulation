using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

public class ConveyorRobotAssembler : MonoBehaviour
{
    [SerializeField] GameObject robotPrefab;
    [SerializeField] GameObject spawnPoint;
    [SerializeField] GameObject robotsParent;
    [SerializeField] List<Parts> acquiredParts = new();

    void Awake()
    {
        if(spawnPoint == null)
            spawnPoint = gameObject;
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent<RobotPart>(out var robotPart))
        {
            var part = robotPart.part;
            acquiredParts.Add(part);

            AudioManager.PlaySound(SfxType.Assembly, transform);

            if (AreAllPartsAcquired())
            {
                AssembleRobot();
            }
            Destroy(other.gameObject);
        }
    }

    bool AreAllPartsAcquired()
    {
        // Check if all required parts are acquired
        return Enum.GetValues(typeof(Parts)).Cast<Parts>().All(part => acquiredParts.Contains(part));
    }

    void AssembleRobot()
    {
        // Instantiate the robot at the assembler's position
        Instantiate(robotPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation, robotsParent.transform);
        // Clear one of each acquired part for the next assembly
        foreach (var part in Enum.GetValues(typeof(Parts)).Cast<Parts>())
            acquiredParts.Remove(part);
        AudioManager.PlaySound(SfxType.AssemblyComplete, transform);
    }
}
