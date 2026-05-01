using UnityEngine;

/// <summary>
/// Placed on the ObjectivesManager GameObject.
/// Detects which arm-extension technique is active in the scene and enables
/// the matching TutorialManager child, disabling the other two. Also re-wires
/// the tutorialManager reference on any TutorialObjective and OrbTutorialObjective
/// siblings before their Start() runs.
/// </summary>
public class TutorialModeSelector : MonoBehaviour
{
    [Header("TutorialManager Children (assign all 3, all start disabled)")]
    [SerializeField] private TutorialManager tutorialManagerDAOM;
    [SerializeField] private TutorialManager tutorialManagerGoGo;
    [SerializeField] private TutorialManager tutorialManagerHOMER;

    [Header("Technique Detection (assign the root GameObject that is enabled per technique)")]
    [SerializeField] private GameObject daomTechniqueRoot;
    [SerializeField] private GameObject goGoTechniqueRoot;
    [SerializeField] private GameObject homerTechniqueRoot;

    private void Awake()
    {
        TutorialManager activeTM = SelectActiveTutorialManager();

        if (activeTM == null)
        {
            Debug.LogError("[TutorialModeSelector] Could not determine active technique — no TutorialManager enabled.");
            return;
        }

        GetComponent<OrbTutorialObjective>()?.SetTutorialManager(activeTM);

        foreach (var to in GetComponents<TutorialObjective>())
            to.SetTutorialManager(activeTM);
    }

    private TutorialManager SelectActiveTutorialManager()
    {
        TutorialManager activeTM;
        string techniqueName;

        if (daomTechniqueRoot != null && daomTechniqueRoot.activeInHierarchy)
        {
            activeTM      = tutorialManagerDAOM;
            techniqueName = "DAOM";
        }
        else if (goGoTechniqueRoot != null && goGoTechniqueRoot.activeInHierarchy)
        {
            activeTM      = tutorialManagerGoGo;
            techniqueName = "GoGo";
        }
        else if (homerTechniqueRoot != null && homerTechniqueRoot.activeInHierarchy)
        {
            activeTM      = tutorialManagerHOMER;
            techniqueName = "HOMER";
        }
        else
        {
            Debug.LogWarning("[TutorialModeSelector] No active technique detected. Falling back to HOMER.");
            activeTM      = tutorialManagerHOMER;
            techniqueName = "HOMER (fallback)";
        }

        Debug.Log($"[TutorialModeSelector] Active technique: {techniqueName}. Enabling matching TutorialManager.");

        SetActive(tutorialManagerDAOM,  activeTM == tutorialManagerDAOM);
        SetActive(tutorialManagerGoGo,  activeTM == tutorialManagerGoGo);
        SetActive(tutorialManagerHOMER, activeTM == tutorialManagerHOMER);

        return activeTM;
    }

    private static void SetActive(TutorialManager tm, bool active)
    {
        if (tm != null)
            tm.gameObject.SetActive(active);
    }
}
