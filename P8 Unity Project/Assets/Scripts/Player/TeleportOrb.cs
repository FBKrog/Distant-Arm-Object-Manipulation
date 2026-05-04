using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Marker component on the TP Orb. Tag the GameObject "TPOrb".
/// XRGrabInteractable + Rigidbody required (sibling components).
/// HandTPOrbConnect.cs handles snap detection and teleport toggling.
/// Future: expose OnPlacedOnPad() for TeleportPad puzzle integration.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class TeleportOrb : MonoBehaviour
{
    private XRGrabInteractable interactable;
    public OrbType orbType;

    private void Awake()
    {
        interactable = GetComponent<XRGrabInteractable>();
    }

    /// <summary>Called by OrbPedestal when the orb is snapped onto the pedestal.</summary>
    public void OnPlacedOnPad() 
    { 
        interactable.enabled = false; // Disable grabbing while on the pedestal
        AudioManager.Play(SfxType.OrbPlaced, transform); 
    }

    /// <summary>
    /// Enable grabbing again after being locked in place. Called by OrbPedestal when the puzzle is completed.
    /// </summary>
    public void ReEnableOrb()
    {
        interactable.enabled = true;
    }
}

public enum OrbType
{
    Blue,
    Red
}
