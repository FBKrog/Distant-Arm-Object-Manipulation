using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Attach to a child transform of Left Arm named "OrbSnapPoint".
/// Detects when a TPOrb (tagged "TPOrb") enters the snap radius,
/// force-releases it from the right hand, snaps it to this transform,
/// and enables teleportation via TeleportationActivator.
/// Grabbing the orb off the snap point disables teleportation again.
/// </summary>
public class HandTPOrbConnect : MonoBehaviour, IXRSelectFilter
{
    [SerializeField] private TeleportationActivator teleportationActivator;
    [SerializeField] private float snapRadius = 0.12f;
    [SerializeField] private string orbTag = "TPOrb";
    [SerializeField] private HOMERArm homerRaycast; // optional — assign if HOMER is active
    [Tooltip("The left controller's XRDirectInteractor. Blocked from re-grabbing the orb while it is snapped to this hand.")]
    [SerializeField] private XRBaseInteractor leftHandInteractor;

    // IXRSelectFilter — active only while the orb is snapped to this hand.
    // Blocks the left hand interactor at the XRI level so selectEntered never fires
    // and no grab audio plays when the left hand tries to pick the orb off itself.
    public bool canProcess => _snappedOrb != null;
    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        if (leftHandInteractor != null && interactor as XRBaseInteractor == leftHandInteractor)
            return false;
        return true;
    }

    public event System.Action OrbSnapped;

    // Read by OrbPedestal to check whether this hand controls a given orb
    public XRGrabInteractable SnappedOrb => _snappedOrb;

    private XRGrabInteractable _snappedOrb;
    private bool _isSnapping;
    private float _snapCooldown;

    private void Update()
    {
        if (_snappedOrb != null)
        {
            _snappedOrb.transform.position = transform.position;
            _snappedOrb.transform.rotation = transform.rotation;
            return;
        }

        if (_snapCooldown > 0f)
        {
            _snapCooldown -= Time.deltaTime;
            return;
        }

        if (_isSnapping)
            return;

        Collider[] hits = Physics.OverlapSphere(transform.position, snapRadius);
        foreach (Collider col in hits)
        {
            if (!col.CompareTag(orbTag))
                continue;

            XRGrabInteractable grab = col.GetComponentInParent<XRGrabInteractable>();
            bool isHeld = (grab != null && grab.isSelected)
                       || (grab != null && homerRaycast != null && homerRaycast.GrabbedObject == col.gameObject);
            if (isHeld)
            {
                StartCoroutine(SnapOrb(grab));
                break;
            }
        }
    }

    private IEnumerator SnapOrb(XRGrabInteractable orb)
    {
        _isSnapping = true;

        // Force-release from whichever interactor is holding it
        UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor interactor = orb.firstInteractorSelecting;
        if (interactor != null)
            orb.interactionManager.SelectExit(interactor, orb);

        // Wait one frame for XRI to process the release
        yield return null;

        Rigidbody rb = orb.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        _snappedOrb = orb;
        _isSnapping = false;
        AudioManager.PlaySound(SfxType.OrbPlaced, transform);


        if (teleportationActivator != null)
            teleportationActivator.orbConnected = true;

        // Register this script as a select filter on the orb.
        // canProcess returns true only while _snappedOrb != null, so the filter
        // is effectively inactive before snap and after unsnap.
        orb.selectFilters.Add(this);

        OrbSnapped?.Invoke();

        // Listen for re-grab so we can detach
        orb.selectEntered.AddListener(OnOrbRegrabbed);
    }

    /// <summary>
    /// Called by OrbPedestal when the orb is claimed by the pedestal.
    /// Clears the snap without re-enabling physics (pedestal keeps the orb kinematic).
    /// </summary>
    public void DisconnectOrb()
    {
        if (_snappedOrb == null) return;
        _snappedOrb.selectEntered.RemoveListener(OnOrbRegrabbed);
        _snappedOrb.selectFilters.Remove(this);
        _snappedOrb = null;
        if (teleportationActivator != null)
            teleportationActivator.orbConnected = false;
    }

    private void OnOrbRegrabbed(SelectEnterEventArgs args)
    {
        if (_snappedOrb == null)
            return;

        _snappedOrb.selectEntered.RemoveListener(OnOrbRegrabbed);
        _snappedOrb.selectFilters.Remove(this);

        Rigidbody rb = _snappedOrb.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = false;

        _snappedOrb = null;
        _snapCooldown = 0.5f;

        if (teleportationActivator != null)
            teleportationActivator.orbConnected = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, snapRadius);
    }
}
