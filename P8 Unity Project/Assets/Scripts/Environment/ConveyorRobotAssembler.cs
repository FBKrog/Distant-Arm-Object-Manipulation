using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

public class ConveyorRobotAssembler : MonoBehaviour
{
    [SerializeField] GameObject robotPrefab;
    [SerializeField] List<RobotPart.Parts> acquiredParts = new();
    
    [SerializeField] AudioClip assemblySound;
    [SerializeField] AudioClip assemblyComplete;

    void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent<RobotPart>(out var robotPart))
        {
            var part = robotPart.part;
            acquiredParts.Add(part);

            AudioManager.PlaySound(assemblySound, transform, 1);

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
        return Enum.GetValues(typeof(RobotPart.Parts)).Cast<RobotPart.Parts>().All(part => acquiredParts.Contains(part));
    }

    void AssembleRobot()
    {
        // Instantiate the robot at the assembler's position
        Instantiate(robotPrefab, transform.position, Quaternion.identity);
        // Clear one of each acquired part for the next assembly
        foreach (var part in Enum.GetValues(typeof(RobotPart.Parts)).Cast<RobotPart.Parts>())
            acquiredParts.Remove(part);
        AudioManager.PlaySound(assemblyComplete, transform, 1);
    }
}
