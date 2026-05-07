using System.Collections;
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
    [Header("Reference:")]
    [SerializeField] private TutorialManager tutorialManager;
    [Header("Settings:")]
    [SerializeField] private HOMERArm homerArm;                 // optional — enables HOMER grab detection
    [SerializeField] private LaunchArm launchArm;                  // optional — enables DAOM grab detection
    [SerializeField] private HandTPOrbConnect orbConnect;
    [SerializeField] private XRGrabInteractable orbGrabInteractable;
    [SerializeField] private bool autoStart = true;
    [Tooltip("stepId of the TutorialManager step that instructs the player to grab the orb.")]
    [SerializeField] private string grabStepId;
    [Tooltip("stepId of the TutorialManager step that instructs the player to snap the orb to their hand.")]
    [SerializeField] private string snapStepId;
    [Tooltip("Seconds to display a pre-completed step before auto-advancing past it.")]
    [SerializeField] private float alreadyCompletedDisplayDuration = 1f;

    private int _step = -1;

    // -------------------------------------------------------------------------

    private void Start()
    {
        if (autoStart) StartObjective();
    }

    public void SetTutorialManager(TutorialManager tm) => tutorialManager = tm;

    public void StartObjective()
    {
        if (_step >= 0) return;
        _step = 1;  // step 0 is the intro, auto-advanced by TutorialManager
        tutorialManager.StartTutorial();

        orbGrabInteractable.selectEntered.AddListener(OnOrbGrabbed);
        orbConnect.OrbSnapped += OnOrbSnapped;

        if (homerArm != null)
            homerArm.GrabStarted += OnHOMERGrabbed;

        if (launchArm != null)
            LaunchArm.GrabbedGameObject += OnDAOMGrabbed;

        tutorialManager.OnStepShown += OnStepShownHandler;
    }

    // ── Step 0 → 1: orb grabbed ───────────────────────────────────────────────

    // Covers direct grab, HOMER virtual-hand grab, and GoGo grab via XRI selectEntered
    private void OnOrbGrabbed(SelectEnterEventArgs args)
    {
        if (_step != 1) return;
        _step = 2;
        AdvanceUntilStep(snapStepId);
    }

    // Covers HOMER grab (fires GrabStarted, not selectEntered)
    private void OnHOMERGrabbed(GameObject obj)
    {
        if (_step != 1) return;
        if (obj != orbGrabInteractable.gameObject) return;
        _step = 2;
        AdvanceUntilStep(snapStepId);
    }

    // Covers DAOM auto-grab (fires before selectEntered on the DAOM path)
    private void OnDAOMGrabbed(IXRSelectInteractable grabbed)
    {
        if (_step != 1) return;
        if (grabbed != orbGrabInteractable) return;
        _step = 2;
        AdvanceUntilStep(snapStepId);
    }

    // ── Step 2 → 3: orb snapped to hand ──────────────────────────────────────

    private void OnOrbSnapped()
    {
        if (_step != 2) return;
        _step = 3;
        tutorialManager.AdvanceToNextStep();
        Cleanup();
    }

    // Advances through timer steps until the target stepId is reached.
    private void AdvanceUntilStep(string targetId)
    {
        do { tutorialManager.AdvanceToNextStep(); }
        while (!string.IsNullOrEmpty(targetId) && tutorialManager.CurrentStepId != targetId);
    }

    // ── Pre-completion detection ──────────────────────────────────────────────

    private void OnStepShownHandler(string stepId)
    {
        if (!string.IsNullOrEmpty(grabStepId) && stepId == grabStepId && _step > 1)
            StartCoroutine(BriefDisplayThenAdvance(grabStepId));
        else if (!string.IsNullOrEmpty(snapStepId) && stepId == snapStepId && _step > 2)
            StartCoroutine(BriefDisplayThenAdvance(snapStepId));
    }

    private IEnumerator BriefDisplayThenAdvance(string expectedStepId)
    {
        yield return new WaitForSeconds(alreadyCompletedDisplayDuration);
        if (tutorialManager.CurrentStepId == expectedStepId)
            tutorialManager.AdvanceToNextStep();
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    private void Cleanup()
    {
        tutorialManager.OnStepShown -= OnStepShownHandler;
        orbGrabInteractable.selectEntered.RemoveListener(OnOrbGrabbed);
        orbConnect.OrbSnapped -= OnOrbSnapped;

        if (launchArm != null)
            LaunchArm.GrabbedGameObject -= OnDAOMGrabbed;
    }
}
