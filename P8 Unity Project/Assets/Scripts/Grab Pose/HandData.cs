using UnityEngine;

[System.Serializable]
public class HandData
{
    public FingerData thumb;
    public FingerData index;
    public FingerData middle;
    public FingerData ring;
    public FingerData pinky;
}

[System.Serializable]
public class FingerData
{
    public Transform[] phalanges = new Transform[3];
    public Vector3[] phalanxValues = new Vector3[3];
}