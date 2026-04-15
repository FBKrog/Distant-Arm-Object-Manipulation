using UnityEngine;

public class RobotPart : MonoBehaviour
{
    public Parts part;
    public enum Parts
    {
        Body,
        TPArm,
        DAOMArm,
        LeftLeg,
        RightLeg,
        Head,
        Rings
    }
}
