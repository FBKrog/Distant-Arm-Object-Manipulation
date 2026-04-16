using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Two-step orb pickup objective:
///   0 → Orb grabbed (direct XRI grab OR DAOM auto-grab) → step 1
///   1 → Orb snapped to hand                             → step 2  (hands off to next tutorial steps)
///
/// Both direct grabbing and technique-assisted grabbing (HOMER / GoGo / DAOM) satisfy step 0.
/// Wire launchArm if DAOM is active — its GrabbedGameObject static event is used alongside
/// the standard selectEntered to cover DAOM's non-XRI grab path.
/// </summary>
public class OrbTutorialObjective : MonoBehaviour
{
    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private LaunchArm launchArm;                  // optional — enables DAOM grab detection
    [SerializeField] private HandTPOrbConnect orbConnect;
    [SerializeField] private XRGrabInteractable orbGrabInteractable;
    [SerializeField] private bool autoStart = true;

    private int _step = -1;

    // -------------------------------------------------------------------------

    private void Start()
    {
        if (autoStart) StartObjective();
    }

    public void StartObjective()
    {
        if (_step >= 0) return;
        _step = 0;
        tutorialManager.StartTutorial();

        orbGrabInteractable.selectEntered.AddListener(OnOrbGrabbed);
        orbConnect.OrbSnapped += OnOrbSnapped;

        if (launchArm != null)
            LaunchArm.GrabbedGameObject += OnDAOMGrabbed;
    }

    // ── Step 0 → 1: orb grabbed ───────────────────────────────────────────────

    // Covers direct grab, HOMER virtual-hand grab, and GoGo grab via XRI selectEntered
    private void OnOrbGrabbed(SelectEnterEventArgs args)
    {
        if (_step != 0) return;
        _step = 1;
        tutorialManager.AdvanceToNextStep();
    }

    // Covers DAOM auto-grab (fires before selectEntered on the DAOM path)
    private void OnDAOMGrabbed(IXRSelectInteractable grabbed)
    {
        if (_step != 0) return;
        if (grabbed != orbGrabInteractable) return;
        _step = 1;
        tutorialManager.AdvanceToNextStep();
    }

    // ── Step 1 → 2: orb snapped to hand ──────────────────────────────────────

    private void OnOrbSnapped()
    {
        if (_step != 1) return;
        _step = 2;
        tutorialManager.AdvanceToNextStep();
        Cleanup();
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    private void Cleanup()
    {
        orbGrabInteractable.selectEntered.RemoveListener(OnOrbGrabbed);
        orbConnect.OrbSnapped -= OnOrbSnapped;

        if (launchArm != null)
            LaunchArm.GrabbedGameObject -= OnDAOMGrabbed;
    }
}
