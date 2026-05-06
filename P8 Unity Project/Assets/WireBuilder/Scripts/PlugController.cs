using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;


public class PlugController : MonoBehaviour
{
    public bool isConected = false;
    public UnityEvent OnWirePlugged;
    public Transform plugPosition;

    public int id; // set in Inspector to match with the corresponding WireEndGrabbable's id

    public Transform endAnchor;
    public Rigidbody endAnchorRB;
    [HideInInspector]
    public WireController wireController;

    public void OnPlugged()
    {
        OnWirePlugged.Invoke();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isConected) return;
        if (other.gameObject != endAnchor.gameObject) return;
        if (other.TryGetComponent<WireEndGrabbable>(out var wireEnd) && 
            wireEnd.id != id) return;
        StartCoroutine(SnapWire());
    }

    private IEnumerator SnapWire()
    {
        isConected = true; // guard against re-entry immediately
        AudioManager.Play(SfxType.WirePlug, transform);
        var grab = endAnchor.GetComponent<XRGrabInteractable>();
        var wireEndGrabbable = endAnchor.GetComponent<WireEndGrabbable>();
        var wc = wireEndGrabbable != null ? wireEndGrabbable.wireController : wireController;

        // Boost drag immediately to help the chain settle before freezing
        wc?.SetDrag(8f, 4f);

        // Force-release from the player's hand
        if (grab != null && grab.isSelected)
        {
            var interactors = new List<IXRSelectInteractor>(grab.interactorsSelecting);
            foreach (var interactor in interactors)
                grab.interactionManager.SelectExit(interactor, grab);
        }

        // Wait until XRI has cleared the selection
        int safetyFrames = 0;
        while (grab != null && grab.isSelected && safetyFrames < 10)
        {
            yield return null;
            safetyFrames++;
        }

        // Freeze all chain segments immediately — stops the shaking
        wc?.FreezeWire();

        // Lock endAnchor for kinematic lerp
        endAnchorRB.linearVelocity  = Vector3.zero;
        endAnchorRB.angularVelocity = Vector3.zero;
        endAnchorRB.isKinematic = true;

        // Permanently disable re-grabbing
        if (grab != null)
            grab.enabled = false;

        // Smoothly slide endAnchor into the socket over 0.2 s
        Vector3 startPos = endAnchor.position;
        Quaternion startRot = endAnchor.rotation;
        Quaternion targetRot = Quaternion.Euler(transform.eulerAngles.x + 90,
                                                transform.eulerAngles.y,
                                                transform.eulerAngles.z);
        float duration = 0.2f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            endAnchorRB.MovePosition(Vector3.Lerp(startPos, plugPosition.position, t));
            endAnchorRB.MoveRotation(Quaternion.Slerp(startRot, targetRot, t));
            yield return null;
        }
        // Final exact snap to avoid float drift
        endAnchorRB.MovePosition(plugPosition.position);
        endAnchorRB.MoveRotation(targetRot);

        OnPlugged();
    }

    public void Reset()
    {
        isConected = false;
        var grab = endAnchor != null ? endAnchor.GetComponent<XRGrabInteractable>() : null;
        if (grab != null)
            grab.enabled = true;
    }

    //private void Update()
    //{
    //    if (isConected && endAnchorRB != null)
    //        endAnchorRB.isKinematic = true;
    //}
}
