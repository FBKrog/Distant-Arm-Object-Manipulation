using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class ConveyorRobotAssembler : MonoBehaviour
{
    public bool isCool;
    [SerializeField] GameObject coolRobotPrefab;
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

            AudioManager.Play(SfxType.Assembly, transform);
            Destroy(other.gameObject);
        }
    }

    void Update()
    {
        if(AreAllPartsAcquired())
        {
            AssembleRobot();
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
        if (isCool)
        {
            var robot = Instantiate(coolRobotPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation, robotsParent.transform);
            robot.GetComponent<Animator>().SetBool("dance", true);
        }
        else
        {
            Instantiate(robotPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation, robotsParent.transform);
        }
        // Clear one of each acquired part for the next assembly
        foreach (var part in Enum.GetValues(typeof(Parts)).Cast<Parts>())
                acquiredParts.Remove(part);
        AudioManager.Play(SfxType.AssemblyComplete, transform);
    }
}
