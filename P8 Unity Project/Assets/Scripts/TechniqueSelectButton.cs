using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Place on each of the 3 technique-selector buttons (DAOM / GoGo / HOMER).
/// Requires an XRGrabInteractable and a kinematic Rigidbody on the same GameObject.
///
/// Grab and push the button down pressDistance metres to activate it.
/// Activating one button deactivates the others via a static broadcast event.
/// The active button stays depressed and green; inactive buttons are red and at rest.
/// </summary>
[DefaultExecutionOrder(200)]
public class TechniqueSelectButton : MonoBehaviour
{
    // ── Static radio-button coordination ──────────────────────────────────────
    public static event Action<TechniqueSelectButton> OnButtonActivated;

    // ── Inspector ──────────────────────────────────────────────────────────────
    [Tooltip("The technique root GameObject this button enables (e.g. DAOM_Technique).")]
    [SerializeField] private GameObject techniqueRoot;

    [Header("Press Settings")]
    [SerializeField] private Vector3 pressDirectionLocal = Vector3.down;
    [SerializeField] private float   pressDistance       = 0.02f;

    [Header("Visuals")]
    [Tooltip("Auto-found in children if left empty.")]
    [SerializeField] private Renderer buttonRenderer;

    [ColorUsage(true, true)]
    [SerializeField] private Color activeColor   = Color.green * 2f;
    [ColorUsage(true, true)]
    [SerializeField] private Color inactiveColor = Color.red   * 2f;

    // ── Private state ──────────────────────────────────────────────────────────
    private bool  _isActive;
    private bool  _isGrabbed;
    private float _currentTravel;
    private float _travelAtGrabStart;

    private Vector3 _restLocalPos;
    private Vector3 _pressWorldDir;
    private Vector3 _buttonFixedWorldPos;
    private Vector3 _grabAnchor;

    private XRGrabInteractable _grabbable;
    private Rigidbody          _rb;
    private Material           _material;

    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rb        = GetComponent<Rigidbody>();
        _grabbable = GetComponent<XRGrabInteractable>();

        _restLocalPos        = transform.localPosition;
        _buttonFixedWorldPos = transform.position;
        _pressWorldDir       = (transform.rotation * pressDirectionLocal.normalized).normalized;

        if (_rb != null)
            _rb.isKinematic = true;

        if (_grabbable != null)
        {
            _grabbable.trackPosition = false;
            _grabbable.trackRotation = false;
            _grabbable.throwOnDetach = false;
            _grabbable.movementType  = XRBaseInteractable.MovementType.Instantaneous;
            _grabbable.selectEntered.AddListener(OnSelectEntered);
            _grabbable.selectExited.AddListener(OnSelectExited);
        }

        if (buttonRenderer == null)
            buttonRenderer = GetComponentInChildren<Renderer>();

        if (buttonRenderer != null)
        {
            _material = buttonRenderer.material;
            _material.DisableKeyword("_USEEMISSION_OFF");
        }

        // Initialize from scene state — whichever technique root is already active
        // starts as the selected button.
        _isActive = techniqueRoot != null && techniqueRoot.activeInHierarchy;
        if (_isActive)
            _currentTravel = pressDistance;

        ApplyColor(_isActive ? activeColor : inactiveColor);
    }

    private void OnEnable()  => OnButtonActivated += OnOtherButtonActivated;
    private void OnDisable() => OnButtonActivated -= OnOtherButtonActivated;

    private void OnDestroy()
    {
        if (_grabbable != null)
        {
            _grabbable.selectEntered.RemoveListener(OnSelectEntered);
            _grabbable.selectExited.RemoveListener(OnSelectExited);
        }

        if (_material != null)
            Destroy(_material);
    }

    // ── LateUpdate (order 200) ─────────────────────────────────────────────────

    private void LateUpdate()
    {
        // Keep the fixed world position in sync with any parent movement.
        if (!_isGrabbed)
            _buttonFixedWorldPos = transform.parent != null
                ? transform.parent.TransformPoint(_restLocalPos)
                : transform.position;

        // Unconditional position lock — always enforces current travel depth.
        transform.position = _buttonFixedWorldPos + _pressWorldDir * _currentTravel;

        if (!_isGrabbed || _isActive) return;

        // Compute travel from physical hand movement along the press axis.
        var interactor = _grabbable?.firstInteractorSelecting as XRBaseInteractor;
        if (interactor == null) return;

        float proj = Vector3.Dot(interactor.transform.position - _grabAnchor, _pressWorldDir)
                     + _travelAtGrabStart;
        _currentTravel = Mathf.Clamp(proj, 0f, pressDistance);

        if (_currentTravel >= pressDistance)
            Activate();
    }

    // ── XRI event handlers ─────────────────────────────────────────────────────

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (_isActive) return; // already active — allow grab but don't track press

        var interactor = args.interactorObject as XRBaseInteractor;
        if (interactor == null) return;

        _travelAtGrabStart = _currentTravel;
        // Anchor = hand position offset so travel continues from current depth.
        _grabAnchor = interactor.transform.position - _pressWorldDir * _currentTravel;
        _isGrabbed  = true;
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        _isGrabbed = false;
        if (!_isActive)
            _currentTravel = 0f; // snap back to rest if not activated
    }

    // ── Activation ─────────────────────────────────────────────────────────────
    [ContextMenu("Fire Activation")]
    private void Activate()
    {
        _isActive      = true;
        _isGrabbed     = false;
        _currentTravel = pressDistance; // stay depressed

        if (techniqueRoot != null)
            techniqueRoot.SetActive(true);

        ApplyColor(activeColor);

        // Release the player's hand.
        var interactor = _grabbable?.firstInteractorSelecting;
        if (interactor != null && _grabbable != null)
            _grabbable.interactionManager.SelectExit(interactor, _grabbable);

        OnButtonActivated?.Invoke(this);
    }

    private void OnOtherButtonActivated(TechniqueSelectButton activated)
    {
        if (activated == this || !_isActive) return;

        _isActive      = false;
        _currentTravel = 0f;
        transform.localPosition = _restLocalPos;

        if (techniqueRoot != null)
            techniqueRoot.SetActive(false);

        ApplyColor(inactiveColor);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private void ApplyColor(Color color)
    {
        _material?.SetColor(EmissionColorID, color);
    }
}
