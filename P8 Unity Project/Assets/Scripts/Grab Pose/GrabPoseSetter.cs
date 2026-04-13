using System.Collections;
using UnityEngine;

public class GrabPoseSetter : DynamicXRDirectInteractorAnimator
{
    public void SetGrabPose()
    {
        base.SetPhalanxValues();
        StartCoroutine(SetGrabPoseVariables());
    }

    IEnumerator SetGrabPoseVariables()
    {
        yield return new WaitForSeconds(0.5f); // ensure the for loop in SetPhalanxValues has completed and values are updated
        currentGrabPose.data.handData = handData;
        currentGrabPose.data.rotationOffset = attachTransform.localRotation.eulerAngles;
        currentGrabPose.data.positionOffset = attachTransform.localPosition;
    }
}
