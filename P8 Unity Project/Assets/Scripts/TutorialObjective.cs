using System.Collections;
using UnityEngine;

/// <summary>
/// General-purpose tutorial objective sequencer. Wire trigger sources to
/// AdvanceIfStep() in the Inspector, setting the expected step index as the
/// static int value on each UnityEvent entry.
/// </summary>
public class TutorialObjective : MonoBehaviour
{
    [Header("Reference:")]
    [SerializeField] private TutorialManager tutorialManager;

    [Header("Adjustments:")]
    [Tooltip("Activate when ObjectivesManager fires an event with this name.")]
    [SerializeField] private string activateOnObjective;

    [Tooltip("Seconds to display a pre-completed step before auto-advancing past it.")]
    [SerializeField] private float alreadyCompletedDisplayDuration = 1f;

    private int _step = -1;   // -1 = not yet activated
    private string _pendingStepId;

    private void Start()
    {
        if (tutorialManager != null)
            tutorialManager.OnStepShown += OnStepShownHandler;
    }

    private void OnDestroy()
    {
        if (tutorialManager != null)
            tutorialManager.OnStepShown -= OnStepShownHandler;
    }

    /// <summary>Wire to ObjectivesManager.onObjectiveCompleted.</summary>
    public void ActivateIfName(string completedObjectiveName)
    {
        if (completedObjectiveName == activateOnObjective)
        {
            Debug.Log($"[TutorialObjective] '{gameObject.name}' — ActivateIfName matched '{completedObjectiveName}'. Activating.");
            Activate();
        }
        else
        {
            Debug.Log($"[TutorialObjective] '{gameObject.name}' — ActivateIfName ignored '{completedObjectiveName}' (waiting for '{activateOnObjective}').");
        }
    }

    public void SetTutorialManager(TutorialManager tm) => tutorialManager = tm;

    /// <summary>Activate immediately (skip name filter).</summary>
    public void Activate()
    {
        if (_step >= 0)
        {
            Debug.Log($"[TutorialObjective] '{gameObject.name}' — Activate called but already active (step={_step}).");
            return;
        }

        if (tutorialManager == null)
        {
            Debug.LogError($"[TutorialObjective] '{gameObject.name}' has no TutorialManager assigned.");
            return;
        }

        _step = 0;
        Debug.Log($"[TutorialObjective] '{gameObject.name}' activated. Now listening for step advances.");
    }

    /// <summary>
    /// Advance if the TutorialManager is currently on a step whose stepId matches id.
    /// </summary>
    public void AdvanceIfStepId(string id)
    {
        if (_step < 0)
        {
            Debug.Log($"[TutorialObjective] '{gameObject.name}' — AdvanceIfStepId('{id}') ignored: not yet activated.");
            return;
        }
        if (string.IsNullOrEmpty(id) || tutorialManager.CurrentStepId != id)
        {
            if (_step >= 0 && !string.IsNullOrEmpty(id))
                _pendingStepId = id;
            Debug.Log($"[TutorialObjective] '{gameObject.name}' — AdvanceIfStepId('{id}') ignored: current step ID is '{tutorialManager.CurrentStepId}'. Stored as pending.");
            return;
        }

        _pendingStepId = null;
        _step++;
        Debug.Log($"[TutorialObjective] '{gameObject.name}' — AdvanceIfStepId('{id}') matched. Advancing tutorial (internal step now {_step}).");
        tutorialManager.AdvanceToNextStep();
    }

    private void OnStepShownHandler(string stepId)
    {
        if (_step < 0 || string.IsNullOrEmpty(_pendingStepId)) return;
        if (stepId != _pendingStepId) return;
        var pending = _pendingStepId;
        _pendingStepId = null;
        StartCoroutine(BriefDisplayThenAdvance(pending));
    }

    private IEnumerator BriefDisplayThenAdvance(string expectedStepId)
    {
        yield return new WaitForSeconds(alreadyCompletedDisplayDuration);
        if (_step >= 0 && tutorialManager.CurrentStepId == expectedStepId)
        {
            _step++;
            Debug.Log($"[TutorialObjective] '{gameObject.name}' — pre-completed step '{expectedStepId}' auto-advanced after brief display.");
            tutorialManager.AdvanceToNextStep();
        }
    }
}
