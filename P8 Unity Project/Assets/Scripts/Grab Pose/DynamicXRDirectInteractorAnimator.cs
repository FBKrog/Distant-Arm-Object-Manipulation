using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class DynamicXRDirectInteractorAnimator : XRDirectInteractor
{
    [Header("Hand Data")]
    [SerializeField] protected HandData handData;
    protected GrabPose currentGrabPose;

    protected override void Awake()
    {
        if (handData == null)
        {
            Debug.LogError("Hand data is not assigned or empty.");
            return;
        }
        SetPhalanxValues();
        base.Awake();
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (args != null)
        {
            base.OnSelectEntered(args);
            if (args.interactableObject.transform.TryGetComponent(out currentGrabPose))
            {
                BendPhalanges(currentGrabPose.data.handData);
                attachTransform.localPosition = currentGrabPose.data.positionOffset;
                attachTransform.localRotation = Quaternion.Euler(currentGrabPose.data.rotationOffset);
                print(currentGrabPose.gameObject.name);
            }
        }
        else
        {
            Debug.LogWarning("Selected object does not have a GrabPose component. Hand pose will reset.");
            BendPhalanges(handData);
        }
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        if(args != null)
            base.OnSelectExited(args);
        // Reset hand pose to initial values when releasing the object
        BendPhalanges(handData);
        currentGrabPose = null;
    }

    protected virtual void SetPhalanxValues()
    {
        for (int i = 0; i < handData.thumb.phalanges.Length; i++)
            handData.thumb.phalanxValues[i] = GetPhalanxRotation(handData.thumb.phalanges[i]);
        for (int i = 0; i < handData.index.phalanges.Length; i++)
        {
            handData.index.phalanxValues[i] = GetPhalanxRotation(handData.index.phalanges[i]);
            handData.middle.phalanxValues[i] = GetPhalanxRotation(handData.middle.phalanges[i]);
            handData.ring.phalanxValues[i] = GetPhalanxRotation(handData.ring.phalanges[i]);
            handData.pinky.phalanxValues[i] = GetPhalanxRotation(handData.pinky.phalanges[i]);
        }
    }

    public void BendPhalanges(HandData newPose)
    {
        for (int i = 0; i < handData.thumb.phalanges.Length; i++)
            BendPhalanx(handData.thumb.phalanges[i], newPose.thumb.phalanxValues[i]);
        for (int i = 0; i < handData.index.phalanges.Length; i++)
        {
            BendPhalanx(handData.index.phalanges[i], newPose.index.phalanxValues[i]);
            BendPhalanx(handData.middle.phalanges[i], newPose.middle.phalanxValues[i]);
            BendPhalanx(handData.ring.phalanges[i], newPose.ring.phalanxValues[i]);
            BendPhalanx(handData.pinky.phalanges[i], newPose.pinky.phalanxValues[i]);
        }
    }

    void BendPhalanx(Transform phalanx, Vector3 value)
    {
        phalanx.localRotation = Quaternion.Euler(value.x, value.y, value.z);
    }

    Vector3 GetPhalanxRotation(Transform phalanx)
    {
        return phalanx.localRotation.eulerAngles;
    }
}
