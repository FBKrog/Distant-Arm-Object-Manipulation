using UnityEngine;

[CreateAssetMenu(fileName = "New Grab Pose", menuName = "Grab Pose")]
public class GrabPoseScriptableObject : ScriptableObject
{
    public Transform grabPoint;
    public Vector3 rotationOffset;
    public Vector3 positionOffset;
    public HandData handData;
}
