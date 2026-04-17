using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Three-step orb pickup objective:
///   0 → Intro text (auto-advances via TutorialManager displayDuration — no action needed here)
///   1 → Orb grabbed (direct XRI grab OR DAOM auto-grab) → step 2
///   2 → Orb snapped to hand                             → step 3  (hands off to next tutorial steps)
///
/// Step 0 is owned entirely by TutorialManager's auto-advance timer (set displayDuration on step 0
/// in the Inspector). OrbTutorialObjective starts at _step = 1 and handles steps 1 and 2 only.
/// Both direct grabbing and technique-assisted grabbing (HOMER / GoGo / DAOM) satisfy step 1.
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
        _step = 1;  // step 0 is the intro, auto-advanced by TutorialManager
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
        if (_step != 1) return;
        _step = 2;
        tutorialManager.AdvanceToNextStep();
    }

    // Covers DAOM auto-grab (fires before selectEntered on the DAOM path)
    private void OnDAOMGrabbed(IXRSelectInteractable grabbed)
    {
        if (_step != 1) return;
        if (grabbed != orbGrabInteractable) return;
        _step = 2;
        tutorialManager.AdvanceToNextStep();
    }

    // ── Step 2 → 3: orb snapped to hand ──────────────────────────────────────

    private void OnOrbSnapped()
    {
        if (_step != 2) return;
        _step = 3;
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
