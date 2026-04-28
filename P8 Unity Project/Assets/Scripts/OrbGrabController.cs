using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Attach to the TP Orb GameObject.
/// Prevents the left-hand interactor from directly grabbing the orb (right-hand only).
/// Also disables the grab interactable while any technique is in its aiming phase.
/// </summary>
public class OrbGrabController : MonoBehaviour, IXRSelectFilter
{
    [SerializeField] private XRGrabInteractable orbInteractable;
    [Tooltip("The left controller's XR Direct Interactor — blocked from grabbing the orb.")]
    [SerializeField] private XRBaseInteractor leftHandInteractor;
    [Tooltip("Optional — assign even if DAOM is inactive.")]
    [SerializeField] private LaunchArm launchArm;
    [Tooltip("Optional — assign even if HOMER is inactive.")]
    [SerializeField] private HOMERArm homerRaycast;

    public bool canProcess => isActiveAndEnabled;

    private void Awake()
    {
        if (orbInteractable == null)
            orbInteractable = GetComponent<XRGrabInteractable>();

        orbInteractable.selectFilters.Add(this);
    }

    private void OnDestroy()
    {
        if (orbInteractable != null)
            orbInteractable.selectFilters.Remove(this);
    }

    private void Update()
    {
        bool isAiming = (launchArm    != null && launchArm.IsAiming)
                     || (homerRaycast != null && homerRaycast.IsAiming);

        orbInteractable.enabled = !isAiming;
    }

    // IXRSelectFilter — returns false to block the left-hand interactor
    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        if (leftHandInteractor != null && interactor is XRBaseInteractor baseInteractor
            && baseInteractor == leftHandInteractor)
            return false;

        return true;
    }
}
