using UnityEngine;

/// <summary>
/// Shows a brief subtitle hint when <see cref="ShowHint"/> is called.
/// Wire any UnityEvent (OrbPedestal.OrbPlaced, OnLeverActivated, etc.) to ShowHint()
/// in the Inspector. Reuses TutorialManager's world-space subtitle panel and
/// auto-dismisses after <see cref="displayDuration"/> seconds.
/// </summary>
public class PuzzleHintDisplay : MonoBehaviour
{
    [Tooltip("Auto-found in scene if not assigned.")]
    [SerializeField] private TutorialManager tutorialManager;

    [TextArea(2, 5)]
    [SerializeField] private string hintText = "Complete the puzzles ahead using your arm techniques.";

    [SerializeField] private float displayDuration = 5f;

    [Tooltip("If true, the hint only shows the first time ShowHint() is called.")]
    [SerializeField] private bool showOnce = true;

    private bool _shown;

    private void Awake()
    {
        if (tutorialManager == null)
            tutorialManager = FindFirstObjectByType<TutorialManager>();
    }

    public void ShowHint()
    {
        if (showOnce && _shown) return;
        if (tutorialManager == null) return;
        _shown = true;
        tutorialManager.ShowTemporaryHint(hintText, displayDuration);
    }
}
