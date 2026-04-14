using UnityEngine;

[CreateAssetMenu(fileName = "New Grab Pose", menuName = "Grab Pose")]
public class GrabPoseScriptableObject : ScriptableObject
{
    public Vector3 rotationOffset;
    public Vector3 positionOffset;
    public HandData handData;
}
