using System.Collections;
using UnityEngine;

/// <summary>
/// Renders a waypoint polyline on the floor and animates a looping glowing fill
/// from the first waypoint to the last when Activate() is called.
///
/// Setup:
///   1. Assign GuideLine.mat directly to the LineRenderer component's Material slot.
///   2. Create child empty GameObjects as waypoints and assign them in order.
///   3. Call Activate() from a puzzle completion UnityEvent, or tick Activate On Start.
///
/// Flow: Activate() → fill loops 0→1 repeatedly for activeDuration seconds → stops at 0.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class GuideLine : MonoBehaviour
{
    [Header("Path")]
    [Tooltip("Ordered Transforms defining the polyline. Place ~0.02 m above the floor to avoid Z-fighting.")]
    [SerializeField] private Transform[] waypoints;
    [Tooltip("Width of the line in metres.")]
    [SerializeField] private float lineWidth = 0.05f;

    [Header("Timing")]
    [Tooltip("Seconds for one fill pass from start to end.")]
    [SerializeField] private float fillDuration = 1.5f;
    [Tooltip("Total seconds the line stays active before stopping.")]
    [SerializeField] private float activeDuration = 6f;

    [Header("Activation")]
    [Tooltip("Activate automatically when the scene starts.")]
    [SerializeField] private bool activateOnStart = false;

    private LineRenderer _line;
    private Coroutine    _activeCoroutine;

    private static readonly int FillProgressId = Shader.PropertyToID("_FillProgress");

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
        _line.useWorldSpace = true;
        _line.textureMode   = LineTextureMode.Stretch; // UV.x = 0→1 along full length
        _line.alignment     = LineAlignment.View;
        RefreshWidth();
        SetFillProgress(0f);
    }

    private void Start()
    {
        RefreshPath();
        if (activateOnStart) Activate();
    }

    private void OnValidate()
    {
        if (_line == null) _line = GetComponent<LineRenderer>();
        if (_line == null) return;
        RefreshPath();
        RefreshWidth();
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>Starts the looping fill animation, running for activeDuration then stopping.</summary>
    [ContextMenu("Test Activate")]
    public void Activate()
    {
        if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
        _activeCoroutine = StartCoroutine(FillRoutine());
    }

    /// <summary>Stops the animation immediately and resets the line.</summary>
    [ContextMenu("Test Deactivate")]
    public void Deactivate()
    {
        if (_activeCoroutine != null)
        {
            StopCoroutine(_activeCoroutine);
            _activeCoroutine = null;
        }
        SetFillProgress(0f);
    }

    // ── Coroutine ──────────────────────────────────────────────────────────────

    private IEnumerator FillRoutine()
    {
        float elapsed = 0f;
        while (elapsed < activeDuration)
        {
            float cycleProgress = elapsed % fillDuration / fillDuration;
            SetFillProgress(cycleProgress);
            elapsed += Time.deltaTime;
            yield return null;
        }
        SetFillProgress(0f);
        _activeCoroutine = null;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private void RefreshPath()
    {
        if (waypoints == null || waypoints.Length < 2) return;
        _line.positionCount = waypoints.Length;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] != null)
                _line.SetPosition(i, waypoints[i].position);
        }
    }

    private void RefreshWidth()
    {
        _line.startWidth = lineWidth;
        _line.endWidth   = lineWidth;
    }

    private void SetFillProgress(float value)
    {
        if (_line != null && _line.sharedMaterial != null)
            _line.material.SetFloat(FillProgressId, value);
    }
}
