using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Simon Says puzzle manager. Drives a 3×3 grid of 9 SimonButton objects through
/// a sequence-display → player-input loop.
///
/// Call BeginPuzzle() (e.g. from AllTriggersCompleted.OnAllTriggersCompleted or an
/// Inspector event) to start. Wire OnPuzzleCompleted to a DoorTrigger or
/// ObjectivesManager.CompleteObjective.
///
/// Flow:
///   Idle ──BeginPuzzle()──► ShowingSequence ──► WaitingForInput
///                                                 ├─ correct press ──► (all done) Completed
///                                                 └─ wrong order  ──► WrongInput ──► ShowingSequence
/// </summary>
public class SimonSaysPuzzle : MonoBehaviour
{
    [Header("Buttons")]
    [Tooltip("All 9 buttons in the grid, indexed 0–8.")]
    [SerializeField] private SimonButton[] buttons;
    [Tooltip("Indices into the buttons array that define the required press order (e.g. {3,7,1}).")]
    [SerializeField] private int[] sequence;

    [Header("Colors")]
    [Tooltip("Emission colour flashed on each button during the sequence preview.")]
    [SerializeField][ColorUsage(true, true)] private Color sequenceColor = Color.cyan;
    [Tooltip("Overlay colour blended on top of the base material for a correct press / puzzle complete.")]
    [SerializeField][ColorUsage(true, true)] private Color correctColor = Color.green;
    [Tooltip("Overlay colour blended on top of the base material on a wrong press.")]
    [SerializeField][ColorUsage(true, true)] private Color wrongColor = Color.red;
    [Tooltip("How strongly the correct/wrong overlay blends over the base material (0 = invisible, 1 = solid).")]
    [SerializeField][Range(0f, 1f)] private float overlayAlpha = 0.4f;

    [Header("Timing")]
    [Tooltip("Seconds to wait after BeginPuzzle() before the sequence starts lighting up.")]
    [SerializeField] private float startDelay = 0f;
    [Tooltip("Seconds each button stays lit during the sequence preview.")]
    [SerializeField] private float sequenceShowDuration = 0.8f;
    [Tooltip("Gap between sequence button flashes.")]
    [SerializeField] private float sequenceGapDuration = 0.3f;
    [Tooltip("Seconds all buttons flash red after a wrong-order press.")]
    [SerializeField] private float wrongFlashDuration = 1.2f;

    [Header("Idle Repeat")]
    [Tooltip("Seconds without a button press before the sequence replays automatically. Set to 0 to disable.")]
    [SerializeField] private float idleRepeatDelay = 10f;

    [Header("Events")]
    public UnityEvent OnPuzzleCompleted;

    // ── State machine ──────────────────────────────────────────────────────────
    private enum PuzzleState { Idle, ShowingSequence, WaitingForInput, WrongInput, Completed }
    private PuzzleState state = PuzzleState.Idle;

    private int currentStep;
    private Coroutine activeCoroutine;
    private float _lastInputTime;

    // Pre-allocated per-button listeners capturing index by value.
    private UnityEngine.Events.UnityAction[] _buttonListeners;

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    void Start()
    {
        // Pre-allocate index-capturing listeners once so the same delegate reference
        // can be reliably added and removed each puzzle cycle.
        _buttonListeners = new UnityEngine.Events.UnityAction[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            int capturedIndex = i;
            _buttonListeners[i] = () => OnButtonPressed(capturedIndex);
        }

        // All buttons start disabled, showing only their base material.
        SetAllButtonsInteractable(false);
        ResetAllButtons();
    }

    void Update()
    {
        if (state != PuzzleState.WaitingForInput || idleRepeatDelay <= 0f) return;
        if (Time.time - _lastInputTime < idleRepeatDelay) return;

        UnsubscribeFromAllButtons();
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        activeCoroutine = StartCoroutine(IdleRepeatCoroutine());
    }

    void OnDestroy()
    {
        UnsubscribeFromAllButtons();
    }

    // ── Public entry point ─────────────────────────────────────────────────────

    /// <summary>
    /// Starts the puzzle. Wire to an Inspector event or call from a trigger.
    /// Safe to call only when state == Idle.
    /// </summary>
    [ContextMenu("Fire Puzzle Start")]
    public void BeginPuzzle()
    {
        if (state != PuzzleState.Idle)
        {
            Debug.LogWarning($"[SimonSaysPuzzle:{name}] BeginPuzzle called in state {state} — ignored.");
            return;
        }

        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        activeCoroutine = StartCoroutine(ShowSequenceCoroutine(startDelay));
    }

    // ── State machine coroutines ───────────────────────────────────────────────

    private IEnumerator ShowSequenceCoroutine(float delay = 0f)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        state = PuzzleState.ShowingSequence;

        SetAllButtonsInteractable(false);
        ResetAllButtons();

        for (int i = 0; i < sequence.Length; i++)
        {
            int idx = sequence[i];
            if (idx < 0 || idx >= buttons.Length || buttons[idx] == null)
            {
                Debug.LogError($"[SimonSaysPuzzle:{name}] sequence[{i}]={idx} is out of range or null.");
                yield break;
            }

            buttons[idx].SetLightState(sequenceColor, true);
            AudioManager.PlaySound(SfxType.SimonSequenceLight, buttons[idx].transform);
            yield return new WaitForSeconds(sequenceShowDuration);
            buttons[idx].SetLightState(Color.black, false); // back to base material

            if (i < sequence.Length - 1)
                yield return new WaitForSeconds(sequenceGapDuration);
        }

        // Enable ALL buttons — any wrong press (not in sequence, or wrong order) triggers a fail.
        SetAllButtonsInteractable(true);

        currentStep = 0;
        SubscribeToAllButtons();
        _lastInputTime = Time.time;
        state = PuzzleState.WaitingForInput;

        activeCoroutine = null;
    }

    private IEnumerator IdleRepeatCoroutine()
    {
        state = PuzzleState.ShowingSequence;
        SetAllButtonsInteractable(false);

        for (int i = 0; i < buttons.Length; i++)
            if (buttons[i] != null) buttons[i].UnlockAndSnapBack();

        yield return null; // grace frame for snap-back coroutines to start

        ResetAllButtons();
        activeCoroutine = StartCoroutine(ShowSequenceCoroutine());
    }

    private IEnumerator WrongInputCoroutine()
    {
        state = PuzzleState.WrongInput;

        // Disable all buttons immediately (synchronous, before any yield).
        SetAllButtonsInteractable(false);

        // Overlay all 9 with wrong colour.
        SetAllButtonsOverlay(wrongColor, overlayAlpha, true);
        yield return new WaitForSeconds(wrongFlashDuration);

        // Extinguish and reset to base material.
        ResetAllButtons();

        // Unlock and snap back any buttons that were locked in a correct-press state.
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
                buttons[i].UnlockAndSnapBack();
        }

        // One grace frame so snap-back coroutines can start cleanly.
        yield return null;

        activeCoroutine = StartCoroutine(ShowSequenceCoroutine());
    }

    // ── Button press handler ───────────────────────────────────────────────────

    private void OnButtonPressed(int buttonIndex)
    {
        if (state != PuzzleState.WaitingForInput) return;
        _lastInputTime = Time.time;
        AudioManager.PlaySound(SfxType.SimonButtonPress, transform);
        int expectedIndex = sequence[currentStep];

        if (buttonIndex == expectedIndex)
        {
            // Correct press — overlay this button with the correct colour.
            buttons[buttonIndex].LockPressed();
            buttons[buttonIndex].SetOverlay(correctColor, overlayAlpha, true);
            currentStep++;

            if (currentStep >= sequence.Length)
            {
                // Puzzle complete — overlay ALL buttons with the correct colour.
                SetAllButtonsOverlay(correctColor, overlayAlpha, true);
                UnsubscribeFromAllButtons();
                state = PuzzleState.Completed;
                AudioManager.PlaySound(SfxType.SimonCorrect, transform);
                Debug.Log($"[SimonSaysPuzzle:{name}] Puzzle completed — firing OnPuzzleCompleted.");
                OnPuzzleCompleted.Invoke();
            }
        }
        else
        {
            // Wrong order.
            UnsubscribeFromAllButtons();
            if (activeCoroutine != null) StopCoroutine(activeCoroutine);
            AudioManager.PlaySound(SfxType.SimonWrong, transform);
            activeCoroutine = StartCoroutine(WrongInputCoroutine());
        }
    }

    // ── Subscription helpers ───────────────────────────────────────────────────

    private void SubscribeToAllButtons()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
                buttons[i].OnButtonPressed.AddListener(_buttonListeners[i]);
        }
    }

    private void UnsubscribeFromAllButtons()
    {
        if (_buttonListeners == null) return;
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
                buttons[i].OnButtonPressed.RemoveListener(_buttonListeners[i]);
        }
    }

    // ── Visual helpers ─────────────────────────────────────────────────────────

    private void SetAllButtonsLight(Color color, bool on)
    {
        foreach (var btn in buttons)
            if (btn != null) btn.SetLightState(color, on);
    }

    private void SetAllButtonsOverlay(Color color, float alpha, bool on)
    {
        foreach (var btn in buttons)
            if (btn != null) btn.SetOverlay(color, alpha, on);
    }

    /// <summary>Clears emission and overlay on all buttons, returning them to their base material.</summary>
    private void ResetAllButtons()
    {
        SetAllButtonsLight(Color.black, false);
        SetAllButtonsOverlay(Color.black, 0f, false);
    }

    private void SetAllButtonsInteractable(bool interactable)
    {
        foreach (var btn in buttons)
            if (btn != null) btn.SetInteractable(interactable);
    }
}
