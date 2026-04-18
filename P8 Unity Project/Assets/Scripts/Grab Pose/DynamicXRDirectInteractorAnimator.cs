using UnityEngine;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System;

public class DynamicXRDirectInteractorAnimator : XRDirectInteractor
{
    [Header("Hand Data")]
    [SerializeField] HandData handData;
    [SerializeField] GrabPoseScriptableObject defaultPose;
    [SerializeField] bool leftHand = false;
    GrabPose currentGrabPose;

    protected override void Awake()
    {
        if (handData == null)
        {
            Debug.LogError("Hand data is not assigned or empty.");
            return;
        }
        BendPhalanges(defaultPose.handData);
        base.Awake();
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (args != null)
        {
            base.OnSelectEntered(args);

            AudioManager.PlaySound(SfxType.Grab, transform.position, AudioManager.instance.sfxs[(int)SfxType.Grab].volume);

            if (args.interactableObject.transform.TryGetComponent(out GrabPose grabPose))
            {
                currentGrabPose = grabPose;
                if (grabPose.data == null)
                {
                    Debug.LogWarning("GrabPose component found, but data is not assigned. Hand pose will reset.");
                    BendPhalanges(defaultPose.handData);
                    return;
                }
                BendPhalanges(currentGrabPose.data.handData);
                if(leftHand)
                {
                    var newRot = new Vector3(currentGrabPose.data.rotationOffset.x, -currentGrabPose.data.rotationOffset.y, -currentGrabPose.data.rotationOffset.z);
                    var newPos = new Vector3(-currentGrabPose.data.positionOffset.x, currentGrabPose.data.positionOffset.y, currentGrabPose.data.positionOffset.z);
                    attachTransform.localRotation = Quaternion.Euler(newRot);
                    attachTransform.localPosition = newPos;
                }
                else
                {
                    attachTransform.localPosition = currentGrabPose.data.positionOffset;
                    attachTransform.localRotation = Quaternion.Euler(currentGrabPose.data.rotationOffset);
                }
            }
        }
        else
        {
            Debug.LogWarning("Selected object does not have a GrabPose component. Hand pose will reset.");
            BendPhalanges(defaultPose.handData);
        }
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        if (args != null)
        {
            base.OnSelectExited(args);
            AudioManager.PlaySound(SfxType.Release, transform.position, AudioManager.instance.sfxs[(int)SfxType.Release].volume);
        }
        // Reset hand pose to initial values when releasing the object
        BendPhalanges(defaultPose.handData);
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
        if(leftHand)
            phalanx.localRotation = Quaternion.Euler(value.x, -value.y, -value.z);
        else
            phalanx.localRotation = Quaternion.Euler(value.x, value.y, value.z);
    }

    Vector3 GetPhalanxRotation(Transform phalanx)
    {
        return phalanx.localRotation.eulerAngles;
    }


#if UNITY_EDITOR
    public void SetGrabPose()
    {
        print("Setting grab pose values...");
        SetPhalanxValues();
        StartCoroutine(SetGrabPoseVariables());
    }
    
    void SetPhalanxValues()
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

    IEnumerator SetGrabPoseVariables()
    {
        if (currentGrabPose.data != null)
        {
            Debug.LogWarning($"Current grab pose ({currentGrabPose.gameObject.name}) already has a GrabPoseScriptableObject.");
            yield break;
        }
        else
        {
            yield return new WaitForSeconds(0.5f); // ensure the for loop in SetPhalanxValues has completed and values are updated
            var so = ScriptableObject.CreateInstance<GrabPoseScriptableObject>();
            so.handData = handData;
            so.rotationOffset = attachTransform.localRotation.eulerAngles;
            so.positionOffset = attachTransform.localPosition;
            currentGrabPose.data = so;
            print($"Pose values set for + {currentGrabPose.gameObject.name}");
        }
    }
#endif
}
