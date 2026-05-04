using UnityEngine;

/// <summary>
/// Shows a brief subtitle hint when <see cref="ShowHint"/> is called.
/// Wire any UnityEvent (OrbPedestal.OrbPlaced, OnLeverActivated, etc.) to ShowHint()
/// in the Inspector. Picks the hint text for whichever TutorialManager's GameObject
/// is currently active in the hierarchy. Reuses TutorialManager's world-space subtitle
/// panel and auto-dismisses after <see cref="displayDuration"/> seconds.
/// </summary>
public class PuzzleHintDisplay : MonoBehaviour
{
    [Header("DAOM")]
    [SerializeField] private TutorialManager daomTutorialManager;
    [TextArea(2, 5)]
    [SerializeField] private string daomHintText = "";

    [Header("HOMER")]
    [SerializeField] private TutorialManager homerTutorialManager;
    [TextArea(2, 5)]
    [SerializeField] private string homerHintText = "";

    [Header("Go-Go")]
    [SerializeField] private TutorialManager goGoTutorialManager;
    [TextArea(2, 5)]
    [SerializeField] private string goGoHintText = "";

    [SerializeField] private float displayDuration = 5f;

    [Tooltip("If true, the hint only shows the first time ShowHint() is called.")]
    [SerializeField] private bool showOnce = true;

    private bool _shown;

    public void ShowHint()
    {
        if (showOnce && _shown) return;

        (TutorialManager mgr, string text) = ResolveActive();
        if (mgr == null) return;

        _shown = true;
        mgr.ShowTemporaryHint(text, displayDuration);
    }

    private (TutorialManager, string) ResolveActive()
    {
        if (daomTutorialManager != null && daomTutorialManager.gameObject.activeInHierarchy)
            return (daomTutorialManager, daomHintText);
        if (homerTutorialManager != null && homerTutorialManager.gameObject.activeInHierarchy)
            return (homerTutorialManager, homerHintText);
        if (goGoTutorialManager != null && goGoTutorialManager.gameObject.activeInHierarchy)
            return (goGoTutorialManager, goGoHintText);

        // Fallback: use whichever manager was assigned, in priority order
        if (daomTutorialManager != null) return (daomTutorialManager, daomHintText);
        if (homerTutorialManager != null) return (homerTutorialManager, homerHintText);
        if (goGoTutorialManager != null) return (goGoTutorialManager, goGoHintText);

        return (null, string.Empty);
    }
}
