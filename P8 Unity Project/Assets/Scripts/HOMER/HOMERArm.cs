using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// HOMER technique — unified script (input, raycast, extension, grab, movement).
///
/// Setup:
///   • Add a LineRenderer component to the same GameObject.
///   • Add an aimOffset child GO — ray fires from here.
///   • Add a launchPoint child GO — marks the forearm socket (extend origin / retract target).
///   • Assign homerHandPrefab — instantiated on extend; must contain XRDirectInteractor.
///   • Assign physicalHandRenderers — hidden while the arm is extended.
///   • Assign armXRTarget (IK end-effector — driven to the extended position so the avatar arm stretches).
///   • Assign armRoot (shoulder/root of the arm hierarchy).
///   • Assign physicalInteractor (the XRDirectInteractor on the physical hand).
///   • Set hitableLayer (surfaces the arm can land on).
///   • Set unhitableLayer (surfaces that block the ray).
/// </summary>
[DefaultExecutionOrder(100)]
public class HOMERArm : MonoBehaviour
{
    [Header("Input")]
    public InputActionProperty aimAction;
    public InputActionProperty triggerAction;
    public InputActionProperty selectAction;

    [Header("Raycast")]
    [SerializeField] float rayLength = 25f;
    [SerializeField] LayerMask hitableLayer;
    [Tooltip("Layers the raycast skips entirely — use for the player's own body colliders, " +
             "invisible volumes, etc. Everything else acts as a physical blocker.")]
    [SerializeField] LayerMask unhitableLayer;

    [Header("Aim")]
    [SerializeField] GameObject aimOffset;

    [Header("HOMER Hand Prefab")]
    [Tooltip("Instantiated when the arm extends. Must contain a forearm+hand mesh and an XRDirectInteractor.")]
    public GameObject homerHandPrefab;

    [Header("Physical Hand")]
    [Tooltip("Renderers on the physical forearm/hand mesh. Hidden while the arm is extended.")]
    public Renderer[] physicalHandRenderers;

    [Header("Firing Arm")]
    [SerializeField] GameObject armRoot;
    [SerializeField] GameObject armXRTarget;

    [Header("Physical Arm")]
    [Tooltip("XRDirectInteractor on the physical hand. Disabled while the arm is extended.")]
    [SerializeField] XRDirectInteractor physicalInteractor;
    [Tooltip("Child GO that marks the forearm socket — extend origin and retract target.")]
    [SerializeField] GameObject launchPoint;

    [Header("Extension")]
    [SerializeField] float extendSpeed = 30f;
    [SerializeField] float retractSpeed = 30f;

    [Header("Line Renderer")]
    [SerializeField][ColorUsage(true, true)] Color validColor = Color.green;
    [SerializeField][ColorUsage(true, true)] Color invalidColor = Color.red;
    [SerializeField][ColorUsage(true, true)] Color invalidPressColor = Color.yellow;

    [Header("Torso Estimation")]
    [SerializeField] float torsoHeadOffset = 0.15f;

    [Header("Velocity Scaling")]
    [SerializeField] float minVelocity = 0.05f;
    [SerializeField] float maxVelocity = 1.5f;
    [SerializeField][Range(0f, 1f)] float minSpeedScale = 0.1f;

    [Header("Edge Cases")]
    [SerializeField] float minHandDistance = 0.05f;

    // ── Public API ────────────────────────────────────────────────────────
    public bool IsAiming => _state == State.Aiming;
    public bool IsGrabbing => _state == State.Grabbed;
    public bool IsHandExtended => _state == State.Extended || _state == State.Grabbed;
    public Transform VirtualHand { get; private set; }
    /// <summary>Intended grab/attach point — interactor's attachTransform (Attach Point child).
    /// Falls back to VirtualHand root if interactor has no attachTransform.</summary>
    public Transform HandTip => (_virtualInteractor != null && _virtualInteractor.attachTransform != null)
        ? _virtualInteractor.attachTransform
        : VirtualHand;
    public Transform PhysicalHand => transform;
    public GameObject GrabbedObject { get; private set; }

    public event Action ExtendStarted;
    public event Action<GameObject> GrabStarted;
    public event Action GrabEnded;
    public event Action RetractStarted;

    // ── State machine ─────────────────────────────────────────────────────
    private enum State { Idle, Aiming, Extending, Extended, Grabbed, Retracting }
    private State _state = State.Idle;

    // ── Aim ───────────────────────────────────────────────────────────────
    private Vector3 _targetWorldPos;
    private XRGrabInteractable _grabbableAtTarget;

    // ── Grab ──────────────────────────────────────────────────────────────
    private Rigidbody _grabbedRb;
    private bool _rbWasKinematic;

    // ── Carried object (during retract-with-object) ───────────────────────
    private GameObject _carriedObject;
    private Rigidbody _carriedRb;
    private bool _carriedRbWasKinematic;

    // ── Arm extension (prefab-based) ──────────────────────────────────────
    private Vector3 _extendedPos;
    private GameObject _handInstance;
    private XRDirectInteractor _virtualInteractor;
    private bool _didDisablePhysicalInteractor;

    // ── Line renderer ─────────────────────────────────────────────────────
    private LineRenderer _line;
    private bool _lastValid;
    private bool _invalidFlashRunning;
    private static readonly WaitForSeconds _flashWait = new(0.15f);

    // ── Manipulator state ─────────────────────────────────────────────────
    private float _scaleFactor;
    private Quaternion _rotationOffset;
    private Quaternion _handViewRotOffset;
    private Vector3 _prevHandPos;

    // ── Unity lifecycle ───────────────────────────────────────────────────

    void Awake()
    {
        _line = GetComponent<LineRenderer>();
        if (_line != null)
        {
            _line.positionCount = 2;
            _line.useWorldSpace = true;
            _line.enabled = false;
        }

        if (aimOffset == null) Debug.LogWarning("[HOMERArm] aimOffset not assigned.", this);
        if (armXRTarget == null) Debug.LogWarning("[HOMERArm] armXRTarget not assigned.", this);
        if (homerHandPrefab == null) Debug.LogWarning("[HOMERArm] homerHandPrefab not assigned.", this);
    }

    void OnEnable()
    {
        aimAction.action?.Enable();
        triggerAction.action?.Enable();
        selectAction.action?.Enable();
    }

    void OnDisable()
    {
        aimAction.action?.Disable();
        triggerAction.action?.Disable();
        selectAction.action?.Disable();
    }

    void Update()
    {
        bool aimPressed = aimAction.action.WasPressedThisFrame();
        bool aimReleased = aimAction.action.WasReleasedThisFrame();
        bool triggerPressed = triggerAction.action.WasPressedThisFrame();
        bool selectPressed = selectAction.action.WasPressedThisFrame();
        bool selectReleased = selectAction.action.WasReleasedThisFrame();

        switch (_state)
        {
            case State.Idle:
                if (aimPressed)
                {
                    _state = State.Aiming;
                    if (_line != null) _line.enabled = true;
                }
                break;

            case State.Aiming:
                if (aimReleased)
                {
                    CancelAim();
                    break;
                }
                if (triggerPressed)
                {
                    if (TryGetTarget(out Vector3 hitPt, out XRGrabInteractable grabbable))
                    {
                        _targetWorldPos = hitPt;
                        _grabbableAtTarget = grabbable;
                        StartExtend();
                        if (_line != null) _line.enabled = false;
                        _state = State.Extending;
                    }
                    else if (!_invalidFlashRunning)
                        StartCoroutine(FlashInvalidLine());
                }
                break;

            case State.Extending:
                if (triggerPressed)
                {
                    BeginRetract();
                    break;
                }
                MoveHandToward(_targetWorldPos, extendSpeed);
                if (VirtualHand != null && Vector3.Distance(_extendedPos, _targetWorldPos) < 0.01f)
                {
                    _extendedPos = _targetWorldPos;
                    OnExtendBegin();
                    ExtendStarted?.Invoke();
                    if (_grabbableAtTarget != null && _grabbableAtTarget.enabled)
                        BeginGrab(_grabbableAtTarget.gameObject);
                    else
                        BeginRetract();
                }
                break;

            case State.Extended:
                BeginRetract();
                break;

            case State.Grabbed:
                if (selectReleased)
                {
                    EndGrab();
                }
                else if (triggerPressed)
                {
                    _carriedObject = GrabbedObject;
                    _carriedRb = _grabbedRb;
                    _carriedRbWasKinematic = _rbWasKinematic;
                    GrabbedObject = null;
                    _grabbedRb = null;
                    GrabEnded?.Invoke();
                    BeginRetract();
                }
                break;

            case State.Retracting:
                if (VirtualHand == null) { _state = State.Idle; break; }
                if (selectReleased && _carriedObject != null)
                    DropCarriedObject();
                // Retract toward the forearm socket — launchPoint moves with the player.
                Vector3 armTip = launchPoint != null ? launchPoint.transform.position : transform.position;
                MoveHandToward(armTip, retractSpeed);
                if (Vector3.Distance(_extendedPos, armTip) < 0.01f)
                {
                    EndExtend();
                    _state = State.Idle;
                }
                break;
        }
    }

    void LateUpdate()
    {
        UpdateLine();

        if (_state == State.Retracting && _carriedObject != null && VirtualHand != null)
            _carriedObject.transform.position = _extendedPos;

        if (!IsHandExtended || VirtualHand == null) return;

        Vector3 handPos = transform.position;
        Vector3 handDelta = handPos - _prevHandPos;
        _prevHandPos = handPos;

        float velocity = handDelta.magnitude / Time.deltaTime;
        float t = Mathf.Clamp01(Mathf.InverseLerp(minVelocity, maxVelocity, velocity));
        float speedScale = Mathf.Lerp(minSpeedScale, 1f, t);
        Vector3 scaledDelta = handDelta * _scaleFactor * speedScale;

        _extendedPos += scaledDelta;

        Quaternion extendedRot = transform.rotation * _handViewRotOffset;
        VirtualHand.SetPositionAndRotation(_extendedPos, extendedRot);

        // Drive IK target to extended position so the avatar arm stretches toward the target.
        if (armXRTarget != null)
            armXRTarget.transform.SetPositionAndRotation(_extendedPos, extendedRot);

        if (IsGrabbing && GrabbedObject != null
            && GrabbedObject.GetComponent<IRotaryGrabbable>() == null)
        {
            GrabbedObject.transform.position += scaledDelta;
            GrabbedObject.transform.rotation = transform.rotation * _rotationOffset;
        }
    }

    void OnDestroy()
    {
        if (_handInstance != null) { Destroy(_handInstance); _handInstance = null; }
        if (_carriedObject != null) DropCarriedObject();
        RestorePhysicalArm();
    }

    // ── Arm extend / end ──────────────────────────────────────────────────

    private void StartExtend()
    {
        if (homerHandPrefab == null)
        {
            Debug.LogWarning("[HOMERArm] homerHandPrefab not assigned — cannot extend.", this);
            return;
        }

        Vector3 startPos = launchPoint != null ? launchPoint.transform.position : transform.position;
        _handInstance = Instantiate(homerHandPrefab, startPos, transform.rotation);
        _handInstance.transform.SetParent(null);
        VirtualHand = _handInstance.transform;
        _virtualInteractor = _handInstance.GetComponentInChildren<XRDirectInteractor>();
        _extendedPos = startPos;

        SetPhysicalHandVisible(false);

        _didDisablePhysicalInteractor = physicalInteractor != null && !physicalInteractor.hasSelection;
        if (_didDisablePhysicalInteractor) physicalInteractor.allowSelect = false;
    }

    private void EndExtend()
    {
        if (_handInstance != null) { Destroy(_handInstance); _handInstance = null; }
        _virtualInteractor = null;
        VirtualHand = null;

        SetPhysicalHandVisible(true);
        RestorePhysicalArm();

        if (_carriedObject != null)
        {
            DeliverToPhysicalHand(_carriedObject);
            _carriedObject = null;
            _carriedRb = null;
        }
    }

    private void RestorePhysicalArm()
    {
        if (_didDisablePhysicalInteractor && physicalInteractor != null)
        {
            physicalInteractor.allowSelect = true;
            _didDisablePhysicalInteractor = false;
        }
    }

    private void SetPhysicalHandVisible(bool visible)
    {
        if (physicalHandRenderers == null) return;
        foreach (var r in physicalHandRenderers)
            if (r != null) r.enabled = visible;
    }

    // ── Grab / Release ────────────────────────────────────────────────────

    private void BeginGrab(GameObject obj)
    {
        GrabbedObject = obj;

        _grabbedRb = obj.GetComponent<Rigidbody>();
        if (_grabbedRb != null)
        {
            _rbWasKinematic = _grabbedRb.isKinematic;
            _grabbedRb.isKinematic = true;
        }

        if (obj.GetComponent<IRotaryGrabbable>() == null)
            obj.transform.position = _extendedPos;
        VirtualHand.rotation = transform.rotation;

        _rotationOffset = obj.transform.rotation * Quaternion.Inverse(transform.rotation);
        _state = State.Grabbed;

        GrabStarted?.Invoke(obj);
    }

    public void EndGrab()
    {
        if (_grabbedRb != null)
        {
            _grabbedRb.isKinematic = _rbWasKinematic;
            _grabbedRb = null;
        }

        GrabbedObject = null;
        _state = State.Extended;

        GrabEnded?.Invoke();
    }

    private void DropCarriedObject()
    {
        if (_carriedRb != null)
        {
            _carriedRb.isKinematic = _carriedRbWasKinematic;
            _carriedRb = null;
        }
        _carriedObject = null;
    }

    private void DeliverToPhysicalHand(GameObject obj)
    {
        if (_carriedRb != null)
        {
            _carriedRb.isKinematic = _carriedRbWasKinematic;
            _carriedRb = null;
        }

        var xrGrabbable = obj.GetComponent<XRGrabInteractable>();
        if (xrGrabbable != null && physicalInteractor != null)
        {
            Transform attachPt = physicalInteractor.attachTransform != null
                ? physicalInteractor.attachTransform
                : physicalInteractor.transform;
            obj.transform.position = attachPt.position;
            physicalInteractor.interactionManager.SelectEnter(
                (IXRSelectInteractor)physicalInteractor,
                (IXRSelectInteractable)xrGrabbable);
        }
    }

    private void BeginRetract()
    {
        RetractStarted?.Invoke();
        _state = State.Retracting;
    }

    private void CancelAim()
    {
        if (_line != null) _line.enabled = false;
        _state = State.Idle;
    }

    private System.Collections.IEnumerator FlashInvalidLine()
    {
        _invalidFlashRunning = true;
        AudioManager.PlaySound(SfxType.HomerInvalidTarget, transform);
        if (_line != null)
        {
            _line.material.color = invalidPressColor;
            _line.material.SetColor("_EmissionColor", invalidPressColor);
        }
        yield return _flashWait;
        if (_line != null)
        {
            Color restore = _lastValid ? validColor : invalidColor;
            _line.material.color = restore;
            _line.material.SetColor("_EmissionColor", restore);
        }
        _invalidFlashRunning = false;
    }

    // ── Manipulator helpers ───────────────────────────────────────────────

    private void OnExtendBegin()
    {
        Vector3 torsoPos = GetTorsoPosition();
        Vector3 handPos = transform.position;
        float handDist = Mathf.Max((handPos - torsoPos).magnitude, minHandDistance);
        float virtualDist = (_extendedPos - torsoPos).magnitude;

        _scaleFactor = virtualDist / handDist;
        _prevHandPos = handPos;
        _handViewRotOffset = Quaternion.Inverse(transform.rotation) * VirtualHand.rotation;
    }

    private Vector3 GetTorsoPosition()
    {
        Camera cam = Camera.main;
        return cam != null
            ? cam.transform.position + Vector3.down * torsoHeadOffset
            : transform.position;
    }

    // ── Raycast ───────────────────────────────────────────────────────────

    private bool TryGetTarget(out Vector3 hitPoint, out XRGrabInteractable grabbable)
    {
        Vector3 origin = aimOffset != null ? aimOffset.transform.position : transform.position;
        Vector3 dir = aimOffset != null ? aimOffset.transform.forward : transform.forward;

        // Single scan against all layers except explicitly ignored ones (player body, etc.).
        // Triggers are excluded — only solid colliders stop the ray.
        LayerMask scanMask = ~unhitableLayer;
        if (!Physics.Raycast(origin, dir, out RaycastHit hit, rayLength, scanMask, QueryTriggerInteraction.Ignore))
        { hitPoint = Vector3.zero; grabbable = null; return false; }

        hitPoint = hit.point;

        var found = hit.collider.GetComponentInParent<XRGrabInteractable>();
        bool hasGrabbable = found != null && found.enabled;

        if (!hasGrabbable)
        { grabbable = null; return false; }

        grabbable = found;
        return true;
    }

    // ── Line renderer ─────────────────────────────────────────────────────

    private void UpdateLine()
    {
        if (_line == null || !_line.enabled) return;

        Vector3 origin = aimOffset != null ? aimOffset.transform.position : transform.position;
        Vector3 dir = aimOffset != null ? aimOffset.transform.forward : transform.forward;
        Vector3 endpoint = origin + dir * rayLength;
        bool valid = false;

        LayerMask scanMask = ~unhitableLayer;
        if (Physics.Raycast(origin, dir, out RaycastHit hit, rayLength, scanMask, QueryTriggerInteraction.Ignore))
        {
            endpoint = hit.point;
            var found = hit.collider.GetComponentInParent<XRGrabInteractable>();
            bool hasGrabbable = found != null && found.enabled;
            valid = hasGrabbable;
        }

        _line.SetPosition(0, origin);
        _line.SetPosition(1, endpoint);

        if (valid == _lastValid) return;
        _lastValid = valid;
        Color c = valid ? validColor : invalidColor;
        _line.material.color = c;
        _line.material.SetColor("_EmissionColor", c);
    }

    private void MoveHandToward(Vector3 target, float speed)
    {
        if (VirtualHand == null) return;
        _extendedPos = Vector3.MoveTowards(_extendedPos, target, speed * Time.deltaTime);
    }
}
