using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class WireRespawnable : MonoBehaviour
{
    [SerializeField] Rigidbody[] rbs;
    Vector3[] rbInitialPositions;
    Quaternion[] rbInitialRotations;
    bool[] wasKinematic;
    XRGrabInteractable wireEnd;
    
    void Awake()
    {
        wasKinematic = new bool[rbs.Length];
        rbInitialPositions = new Vector3[rbs.Length];
        rbInitialRotations = new Quaternion[rbs.Length];

        for(int i = 0; i < rbs.Length; i++)
        {
            if (rbs[i].TryGetComponent<XRGrabInteractable>(out var grabInteractable))
                wireEnd = grabInteractable;
            wasKinematic[i] = rbs[i].isKinematic ? true : false;
            rbInitialPositions[i] = rbs[i].transform.localPosition;
            rbInitialRotations[i] = rbs[i].transform.localRotation;
        }
    }

    public void Respawn()
    {
        if(wireEnd && wireEnd.isSelected)
        {
            wireEnd.interactionManager.SelectExit(wireEnd.firstInteractorSelecting, wireEnd);
        }
        for (int i = 0; i < rbs.Length; i++)
        {
            rbs[i].isKinematic = true;
            rbs[i].linearVelocity = Vector3.zero;
            rbs[i].angularVelocity = Vector3.zero;
            rbs[i].transform.localPosition = rbInitialPositions[i];
            rbs[i].transform.localRotation = rbInitialRotations[i];
            rbs[i].isKinematic = wasKinematic[i];
        }
    }


}
