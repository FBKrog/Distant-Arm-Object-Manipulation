using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Attach to the socket's snap-point Transform.
/// When a Chip-tagged XRGrabInteractable enters snapRadius, it is force-released
/// from all three grab techniques (XRI, HOMER, DAOM) and snapped permanently in
/// place. Fires OnChipInserted once on success.
/// </summary>
public class ChipSocket : MonoBehaviour
{
    [SerializeField] private Transform pos;
    [SerializeField] private XRGrabInteractable chip;
    [SerializeField] private float snapRadius = 0.15f;

    [Header("Snap Offset")]
    [Tooltip("Local-space position offset applied to the chip relative to this socket transform.")]
    [SerializeField] private Vector3 snapPositionOffset = Vector3.zero;
    [Tooltip("Local-space Euler rotation offset applied to the chip relative to this socket transform.")]
    [SerializeField] private Vector3 snapRotationOffset = Vector3.zero;

    [Header("Events")]
    public UnityEvent OnChipInserted;

    private bool _chipInserted = false;
    private bool _snapInProgress = false;

    private void Start()
    {
        if (chip == null) Debug.LogError($"[ChipSocket] '{name}' — chip is not assigned!", this);
        Debug.Log($"[ChipSocket] '{name}' — Start. chip='{(chip != null ? chip.name : "NULL ⚠")}', snapRadius={snapRadius}m.");
    }

    private void Update()
    {
        if (_chipInserted || _snapInProgress || chip == null) return;

        if (Vector3.Distance(chip.transform.position, transform.position) <= snapRadius)
        {
            Debug.Log($"[ChipSocket] '{name}' — chip '{chip.name}' entered snap radius. Starting snap.");
            _snapInProgress = true;
            StartCoroutine(SnapChip(chip));
        }
    }

    private IEnumerator SnapChip(XRGrabInteractable chip)
    {
        // --- 1. Force-release from XRI (also covers DAOM, whose interactor is an XRI DirectInteractor) ---
        if (chip.isSelected)
        {
            Debug.Log($"[ChipSocket] '{name}' — releasing '{chip.name}' from XRI/DAOM.");
            var interactors = new List<IXRSelectInteractor>(chip.interactorsSelecting);
            foreach (var interactor in interactors)
                chip.interactionManager.SelectExit(interactor, chip);

            int safetyFrames = 0;
            while (chip.isSelected && safetyFrames < 10)
            {
                yield return null;
                safetyFrames++;
            }
        }

        // --- 2. Force-release from HOMER ---
        var homers = FindObjectsByType<HOMERArm>(FindObjectsSortMode.None);
        bool homerReleased = false;
        foreach (var h in homers)
        {
            if (h.IsGrabbing && h.GrabbedObject == chip.gameObject)
            {
                Debug.Log($"[ChipSocket] '{name}' — releasing '{chip.name}' from HOMER.");
                h.EndGrab();
                homerReleased = true;
                break;
            }
        }
        if (homerReleased)
            yield return null;

        // --- 3. Apply snap ---
        Rigidbody rb = chip.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        chip.transform.SetParent(transform);
        chip.transform.localPosition = snapPositionOffset;
        chip.transform.localEulerAngles = snapRotationOffset;

        // --- 4. Permanently lock ---
        chip.enabled = false;

        _chipInserted = true;
        _snapInProgress = false;

        Debug.Log($"[ChipSocket] '{name}' — snap complete for '{chip.name}'. Firing OnChipInserted ({OnChipInserted.GetPersistentEventCount()} listener(s)).");
        OnChipInserted.Invoke();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
        Gizmos.DrawSphere(transform.position, snapRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, snapRadius);
    }
#endif
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(ChipSocket))]
public class ChipSocketEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        UnityEditor.EditorGUILayout.Space();
        if (GUILayout.Button("Fire OnChipInserted (Test)"))
            ((ChipSocket)target).OnChipInserted.Invoke();
    }
}
#endif
