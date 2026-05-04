using UnityEngine;

public class GrabPose : MonoBehaviour
{
    public GrabPoseScriptableObject data;

    [Header("HOMER Overrides")]
    [Tooltip("Added on top of the ScriptableObject's rotationOffset when grabbed via HOMER.")]
    public Vector3 homerRotationOffset;
    [Tooltip("Added on top of the ScriptableObject's positionOffset when grabbed via HOMER.")]
    public Vector3 homerPositionOffset;
}
