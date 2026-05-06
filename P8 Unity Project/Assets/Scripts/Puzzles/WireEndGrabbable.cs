using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Makes a WireBuilder EndAnchor grabbable in VR without breaking the physics chain.
///
/// Setup:
///   1. Add this component to the EndAnchor GameObject.
///   2. Also add an XRGrabInteractable component to the same object (required).
///   3. Change the EndAnchor's layer (and its children) from "wire" to "Default",
///      OR add the "wire" layer to your XR Interactor's Interaction Layer Mask.
///
/// Behaviour:
///   - While not grabbed: Rigidbody is kinematic — the end stays pinned to the wall.
///   - While grabbed: rb.MovePosition() snaps the anchor directly to the controller.
///     No spring forces are used, so there is no oscillation against the joints.
///     The ConfigurableJoint on the last wire segment pulls the cable chain along.
///   - On release: kinematic is disabled so the cable hangs naturally.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class WireEndGrabbable : MonoBehaviour
{
    public WireController wireController;
    private Rigidbody rb;
    private XRGrabInteractable grab;

    public int id; // set in Inspector to match with the corresponding PlugController's id

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();

        grab.throwOnDetach = false;

        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleased);
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        rb.isKinematic = true;
        wireController.SetRadius(8f);
        wireController.SetDrag(3f, 1f);
    }

    void OnReleased(SelectExitEventArgs args)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = false;
        wireController.SetRadius(2f);
        wireController.SetDrag(1f, 0.5f);
    }

    public void OnHOMERGrab()
    {
        rb.isKinematic = true;
        wireController.SetRadius(8f);
        wireController.SetDrag(3f, 1f);
    }

    public void OnHOMERRelease()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = false;
        wireController.SetRadius(2f);
        wireController.SetDrag(1f, 0.5f);
    }

    //void FixedUpdate()
    //{
    //    if (heldBy == null) return;

    //    // MovePosition on a kinematic Rigidbody moves directly to the controller
    //    // with no spring oscillation. Kinematic bodies ignore joint constraint
    //    // forces on themselves, so nothing fights the movement. The joint on the
    //    // last wire segment then pulls the cable chain along behind the anchor.
    //    rb.MovePosition(heldBy.GetAttachTransform(grab).position);
    //}
}
