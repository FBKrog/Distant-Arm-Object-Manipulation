using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Attach to the Battery Insert Box. Every frame it checks whether a Battery-tagged
/// object's Plus pole is within snapRadius of the socket's Plus pole. When it is,
/// the battery is force-released from the player's hand and snapped in place so its
/// Plus/Minus poles align exactly with the socket's. Fires OnBatteryPlaced on success.
/// The battery stays grabbable so the player can pull it back out.
///
/// No trigger collider required — the snap zone is driven entirely by snapRadius and
/// is visualised as green/red spheres in the Scene view when this object is selected.
/// </summary>
public class BatterySocket : MonoBehaviour
{
    [Header("Socket Poles")]
    [Tooltip("Plus pole transform on this socket (child of the insert box).")]
    [SerializeField] private Transform socketPlus;
    [Tooltip("Minus pole transform on this socket (child of the insert box).")]
    [SerializeField] private Transform socketMinus;

    [Header("Battery Detection")]
    [Tooltip("Tag on the battery root GameObject.")]
    [SerializeField] private string batteryTag = "Battery";
    [Tooltip("Name of the Plus child on the battery.")]
    [SerializeField] private string plusChildName = "Plus";
    [Tooltip("Name of the Minus child on the battery.")]
    [SerializeField] private string minusChildName = "Minus";

    [Header("Snap Settings")]
    [Tooltip("How close the battery's Plus pole must get to the socket's Plus pole to trigger a snap (metres).")]
    [SerializeField] private float snapRadius = 0.15f;

    [Header("Events")]
    public UnityEvent OnBatteryPlaced;

    private bool _batteryInserted  = false;
    private bool _snapInProgress   = false;
    private GameObject _currentBattery;


    // -------------------------------------------------------------------------

    private void OnDisable()
    {
        Debug.LogWarning($"[BatterySocket] '{name}' — OnDisable fired! snapInProgress={_snapInProgress}, batteryInserted={_batteryInserted}.\n" +
                         new System.Diagnostics.StackTrace().ToString());
    }

    private void OnDestroy()
    {
        Debug.LogWarning($"[BatterySocket] '{name}' — OnDestroy fired! snapInProgress={_snapInProgress}.\n" +
                         new System.Diagnostics.StackTrace().ToString());
    }

    private void Start()
    {
        // ── Configuration validation ──────────────────────────────────────────
        Debug.Log($"[BatterySocket] '{name}' — Start. " +
                  $"socketPlus={(socketPlus != null ? socketPlus.name : "NULL ⚠")}, " +
                  $"socketMinus={(socketMinus != null ? socketMinus.name : "NULL ⚠")}, " +
                  $"batteryTag='{batteryTag}', plusChildName='{plusChildName}', " +
                  $"minusChildName='{minusChildName}', snapRadius={snapRadius}m");

        if (socketPlus  == null) Debug.LogError($"[BatterySocket] '{name}' — socketPlus is not assigned!", this);
        if (socketMinus == null) Debug.LogWarning($"[BatterySocket] '{name}' — socketMinus is not assigned (needed for snap rotation).", this);
    }

    private void Update()
    {
        // ── Early-exit guards ─────────────────────────────────────────────────
        if (_batteryInserted || _snapInProgress || socketPlus == null) return;

        Collider[] hits = Physics.OverlapSphere(socketPlus.position, snapRadius * 2f);

        foreach (Collider hit in hits)
        {
            GameObject battery = GetBatteryRoot(hit.gameObject);
            if (battery == null) continue;

            Transform batteryPlus = battery.transform.Find(plusChildName);
            if (batteryPlus == null) continue;

            float dist = Vector3.Distance(batteryPlus.position, socketPlus.position);
            if (dist > snapRadius) continue;

            Debug.Log($"[BatterySocket] '{name}' — snapping '{battery.name}'.");
            _snapInProgress = true;
            StartCoroutine(SnapBattery(battery));
            return;
        }
    }

    // -------------------------------------------------------------------------

    private IEnumerator SnapBattery(GameObject battery)
    {
        var grab = battery.GetComponent<XRGrabInteractable>();

        // --- 1. Force-release from XRI hand — only yields if the battery is actually held ---
        if (grab != null && grab.isSelected)
        {
            Debug.Log($"[BatterySocket] '{name}' — releasing '{battery.name}' from XRI hand.");
            var interactors = new List<UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor>(grab.interactorsSelecting);
            foreach (var interactor in interactors)
                grab.interactionManager.SelectExit(interactor, grab);

            int safetyFrames = 0;
            while (grab.isSelected && safetyFrames < 10)
            {
                yield return null;
                safetyFrames++;
            }
        }

        // --- 1b. Force-release from HOMER — only yields if HOMER was holding it ---
        var homers = FindObjectsByType<HOMERRaycast>(FindObjectsSortMode.None);
        bool homerReleased = false;
        foreach (var h in homers)
        {
            if (h.IsGrabbing && h.GrabbedObject == battery)
            {
                h.EndGrab();
                homerReleased = true;
                break;
            }
        }
        if (homerReleased)
            yield return null;  // Give HOMERManipulator one cycle to stop tracking

        // --- 2. Locate battery poles ---
        Transform batteryPlus  = battery.transform.Find(plusChildName);
        Transform batteryMinus = battery.transform.Find(minusChildName);

        if (batteryPlus == null || batteryMinus == null)
        {
            Debug.LogWarning($"[BatterySocket] '{name}' — '{battery.name}' missing '{plusChildName}' or '{minusChildName}' child.\n" +
                             $"Direct children: {GetChildNames(battery.transform)}");
            _snapInProgress = false;
            yield break;
        }

        // --- 3. Compute snap rotation ---
        Vector3 batteryAxisWorld = battery.transform.TransformDirection(
            (batteryMinus.localPosition - batteryPlus.localPosition).normalized);
        Vector3 socketAxisWorld  = (socketMinus.position - socketPlus.position).normalized;
        Quaternion axisAlignment   = Quaternion.FromToRotation(batteryAxisWorld, socketAxisWorld);
        Quaternion snappedRotation = axisAlignment * battery.transform.rotation;

        // --- 4. Compute snap position ---
        Vector3 plusOffsetWorld = batteryPlus.position - battery.transform.position;
        Vector3 snappedPosition = socketPlus.position - (axisAlignment * plusOffsetWorld);

        // --- 5. Apply snap ---
        var rb = battery.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic     = false;  // must be non-kinematic to zero velocity
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic     = true;
        }

        battery.transform.SetPositionAndRotation(snappedPosition, snappedRotation);
        battery.transform.SetParent(transform, worldPositionStays: true);

        _batteryInserted = true;
        _snapInProgress  = false;
        _currentBattery  = battery;

        if (grab != null)
            grab.enabled = false;

        Debug.Log($"[BatterySocket] '{name}' — snap complete for '{battery.name}'. Firing OnBatteryPlaced ({OnBatteryPlaced.GetPersistentEventCount()} listener(s)).");
        OnBatteryPlaced.Invoke();
    }

    // -------------------------------------------------------------------------

    private GameObject GetBatteryRoot(GameObject obj)
    {
        Transform t = obj.transform;
        while (t != null)
        {
            if (t.CompareTag(batteryTag))
                return t.gameObject;
            t = t.parent;
        }
        return null;
    }

    private static string GetChildNames(Transform parent)
    {
        if (parent.childCount == 0) return "(no children)";
        var names = new System.Text.StringBuilder();
        for (int i = 0; i < parent.childCount; i++)
        {
            if (i > 0) names.Append(", ");
            names.Append($"'{parent.GetChild(i).name}'");
        }
        return names.ToString();
    }

    // -------------------------------------------------------------------------

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (socketPlus != null)
        {
            // Green sphere = Plus snap zone
            Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
            Gizmos.DrawSphere(socketPlus.position, snapRadius);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(socketPlus.position, snapRadius);
        }

        if (socketMinus != null)
        {
            // Red sphere = Minus reference position
            Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
            Gizmos.DrawSphere(socketMinus.position, snapRadius * 0.5f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(socketMinus.position, snapRadius * 0.5f);
        }

        // Line connecting the two poles
        if (socketPlus != null && socketMinus != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(socketPlus.position, socketMinus.position);
        }
    }
#endif
}
