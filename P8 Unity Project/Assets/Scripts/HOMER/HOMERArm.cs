using System;
using System.Collections.Generic;
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
    [Tooltip("Euler rotation applied to the virtual hand during retraction. (0,180,0) flips the palm toward the player as the arm returns.")]
    [SerializeField] Vector3 retractHandRotationOffset = new(0f, 180f, 0f);

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
    [Tooltip("When enabled, pressing the trigger during extend or while grabbing will retract the arm. " +
             "Disable to match original HOMER behaviour where only releasing the object triggers retraction.")]
    [SerializeField] bool retractOnTriggerPress = true;

    [Header("Audio")]
    [SerializeField] bool loopSfxWhileExtended;
    [SerializeField] SfxType extendedLoopSfx;

    [Header("Line Renderer")]
    [SerializeField][ColorUsage(true, true)] Color validColor;
    [SerializeField][ColorUsage(true, true)] Color invalidColor;
    [SerializeField][ColorUsage(true, true)] Color invalidPressColor;

    [Header("Torso Estimation")]
    [SerializeField] float torsoHeadOffset = 0.15f;

    [Header("Velocity Scaling")]
    [SerializeField] float minVelocity = 0.05f;
    [SerializeField] float maxVelocity = 1.5f;
    [SerializeField][Range(0f, 1f)] float minSpeedScale = 0.1f;
    [Tooltip("Maximum speed multiplier applied at and above maxVelocity. " +
             "Reduce below 1 to cap how fast the virtual hand moves at high physical velocity. " +
             "Effective max movement per frame = maxSpeedScale × scaleFactor × physicalHandDelta.")]
    [SerializeField][Range(0f, 100f)] float maxSpeedScale = 100f;

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
    /// <summary>
    /// The internal position accumulator that drives VirtualHand each LateUpdate.
    /// Arc-locking scripts (LeverGrab, ValveGrab) must write back here after overriding
    /// VirtualHand.position so HOMER's next-frame delta starts from the correct base.
    /// </summary>
    public Vector3 ExtendedPos { get => _extendedPos; set => _extendedPos = value; }

    public event Action ExtendStarted;
    public event Action<GameObject> GrabStarted;
    public event Action GrabEnded;
    public event Action RetractStarted;

    // ── HOMER Physics ─────────────────────────────────────────────────────
    private Rigidbody _virtualHandRb;

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

    // ── Audio ─────────────────────────────────────────────────────────────
    private AudioSource _extendedLoopSource;

    // ── Line renderer ─────────────────────────────────────────────────────
    private LineRenderer _line;
    private bool _lastValid;
    private bool _invalidFlashRunning;
    private static readonly WaitForSeconds _flashWait = new(0.15f);
    private static readonly RaycastHit[] _rayBuffer = new RaycastHit[16];

    // ── Extend-carry (object taken from physical hand at extend start) ────
    private GameObject _extendCarryObject;
    private Rigidbody _extendCarryRb;
    private bool _extendCarryRbWasKinematic;

    // ── Manipulator state ─────────────────────────────────────────────────
    private float _scaleFactor;
    private Quaternion _rotationOffset;
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
                if (aimPressed && (physicalInteractor == null || !physicalInteractor.hasSelection))
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
                if (triggerPressed && retractOnTriggerPress)
                {
                    if (_extendCarryObject != null)
                    {
                        _extendCarryObject.transform.SetParent(null, worldPositionStays: true);
                        if (_extendCarryRb != null) _extendCarryRb.isKinematic = _extendCarryRbWasKinematic;
                        _extendCarryObject = null;
                        _extendCarryRb = null;
                    }
                    BeginRetract();
                    break;
                }
                MoveHandToward(_targetWorldPos, extendSpeed);
                if (VirtualHand != null && Vector3.Distance(_extendedPos, _targetWorldPos) < 0.01f)
                {
                    _extendedPos = _targetWorldPos;
                    OnExtendBegin();
                    ExtendStarted?.Invoke();
                    if (_extendCarryObject != null)
                    {
                        var obj = _extendCarryObject;
                        obj.transform.SetParent(null, worldPositionStays: true);
                        GrabbedObject = obj;
                        _grabbedRb = _extendCarryRb;
                        _rbWasKinematic = _extendCarryRbWasKinematic;
                        _extendCarryObject = null;
                        _extendCarryRb = null;
                        if (obj.GetComponent<IRotaryGrabbable>() == null)
                            obj.transform.position = HandTip != null ? HandTip.position : _extendedPos;
                        VirtualHand.rotation = transform.rotation;
                        _rotationOffset = obj.transform.rotation * Quaternion.Inverse(transform.rotation);
                        _state = State.Grabbed;
                        var pa = _virtualInteractor as DynamicXRDirectInteractorAnimator;
                        if (pa != null) { var gp = obj.GetComponent<GrabPose>(); if (gp != null && gp.data != null) pa.BendPhalanges(gp.data.handData); }
                        GrabStarted?.Invoke(obj);
                    }
                    else if (_grabbableAtTarget != null && _grabbableAtTarget.enabled)
                        BeginGrab(_grabbableAtTarget.gameObject);
                    else
                        _state = State.Extended;
                }
                break;

            case State.Extended:
                if (triggerPressed && retractOnTriggerPress)
                    BeginRetract();
                if (selectReleased)
                    BeginRetract();
                break;

            case State.Grabbed:
                if (selectReleased)
                {
                    EndGrab();
                }
                else if (triggerPressed && retractOnTriggerPress)
                {
                    _carriedObject = GrabbedObject;
                    _carriedRb = _grabbedRb;
                    _carriedRbWasKinematic = _rbWasKinematic;
                    GrabbedObject = null;
                    _grabbedRb = null;
                    // Parent to VirtualHand so the object cannot be repositioned by
                    // XRI or event listeners that fire inside GrabEnded.
                    if (_carriedObject != null && VirtualHand != null)
                        _carriedObject.transform.SetParent(VirtualHand, worldPositionStays: true);
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

        if (VirtualHand == null) return;

        // Detect external snaps (e.g. PlugController disabling XRGrabInteractable).
        // Must run before any position overrides so the released object keeps its snapped position.
        if (_state == State.Grabbed && GrabbedObject != null)
        {
            var g = GrabbedObject.GetComponent<XRGrabInteractable>();
            if (g != null && !g.enabled)
            {
                _grabbedRb = null; // leave kinematic — PlugController already set it
                var paSnap = _virtualInteractor as DynamicXRDirectInteractorAnimator;
                if (paSnap != null) paSnap.ResetToDefaultPose();
                GrabbedObject = null;
                GrabEnded?.Invoke();
                BeginRetract();
            }
        }
        if (_state == State.Retracting && _carriedObject != null)
        {
            var g = _carriedObject.GetComponent<XRGrabInteractable>();
            if (g != null && !g.enabled)
            {
                _carriedObject.transform.SetParent(null, worldPositionStays: true);
                _carriedRb = null;
                _carriedObject = null;
            }
        }
        if (_state == State.Extending && _extendCarryObject != null)
        {
            var g = _extendCarryObject.GetComponent<XRGrabInteractable>();
            if (g != null && !g.enabled)
            {
                _extendCarryObject.transform.SetParent(null, worldPositionStays: true);
                if (_extendCarryRb != null) _extendCarryRb.isKinematic = true;
                _extendCarryRb = null;
                _extendCarryObject = null;
                BeginRetract();
            }
        }

        Quaternion extendedRot = _state == State.Retracting
            ? transform.rotation * Quaternion.Euler(retractHandRotationOffset)
            : transform.rotation;

        // Velocity-scaled manipulation — while grabbing or freely extended.
        if (_state == State.Grabbed || _state == State.Extended)
        {
            Vector3 handPos = transform.position;
            Vector3 handDelta = handPos - _prevHandPos;
            _prevHandPos = handPos;

            float velocity = handDelta.magnitude / Time.deltaTime;
            float t = Mathf.Clamp01(Mathf.InverseLerp(minVelocity, maxVelocity, velocity));
            float speedScale = Mathf.Lerp(minSpeedScale, maxSpeedScale, t);

            // Rotary objects (lever, valve) supply their own scale multiplier so the
            // virtual hand moves closer to 1:1 with the physical hand instead of
            // amplifying by the full HOMER distance ratio.
            var rotary = (_state == State.Grabbed && GrabbedObject != null)
                ? GrabbedObject.GetComponent<IRotaryGrabbable>()
                : null;
            float rotaryMult = rotary != null ? rotary.HomerScaleMultiplier : 1f;

            _extendedPos += handDelta * (_scaleFactor * speedScale * rotaryMult);
        }

        // Reposition virtual hand and IK target every frame they exist — covers Extending, Grabbed, and Retracting.
        VirtualHand.SetPositionAndRotation(_extendedPos, extendedRot);
        if (armXRTarget != null)
            armXRTarget.transform.SetPositionAndRotation(_extendedPos, extendedRot);

        //// Physics-based movement.
        //_virtualHandRb.linearVelocity = _extendedPos - _virtualHandRb.position / Time.fixedDeltaTime;

        //// Rotate the virtual hand toward the target rotation.
        //var rotationDifference = extendedRot * Quaternion.Inverse(_virtualHandRb.rotation);
        //rotationDifference.ToAngleAxis(out float angleInDegrees, out Vector3 rotationAxis);
        //_virtualHandRb.angularVelocity = rotationAxis * angleInDegrees * Mathf.Deg2Rad / Time.fixedDeltaTime;


        if (IsGrabbing && GrabbedObject != null
            && GrabbedObject.GetComponent<IRotaryGrabbable>() == null)
        {
            GrabbedObject.transform.SetPositionAndRotation(
                HandTip != null ? HandTip.position : _extendedPos,
                transform.rotation * _rotationOffset);
        }
    }


    void OnDestroy()
    {
        AudioManager.StopLooping(_extendedLoopSource);

        if (_extendCarryObject != null)
        {
            _extendCarryObject.transform.SetParent(null, worldPositionStays: true);
            if (_extendCarryRb != null) _extendCarryRb.isKinematic = _extendCarryRbWasKinematic;
            _extendCarryObject = null;
            _extendCarryRb = null;
        }
        if (_carriedObject != null) DropCarriedObject();
        if (_handInstance != null) { Destroy(_handInstance); _handInstance = null; }
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
        _virtualHandRb = VirtualHand.GetComponent<Rigidbody>();

        SetPhysicalHandVisible(false);

        // If physical hand is already holding something, take it along for the extend.
        _extendCarryObject = null;
        _extendCarryRb = null;
        if (physicalInteractor != null && physicalInteractor.hasSelection
            && physicalInteractor.interactablesSelected.Count > 0)
        {
            var mb = physicalInteractor.interactablesSelected[0] as MonoBehaviour;
            if (mb != null)
            {
                var obj = mb.gameObject;
                ForceReleaseFromAllInteractors(obj);
                var rb = obj.GetComponent<Rigidbody>();
                _extendCarryRbWasKinematic = rb != null && rb.isKinematic;
                if (rb != null) rb.isKinematic = true;
                _extendCarryRb = rb;
                _extendCarryObject = obj;
                obj.transform.SetParent(VirtualHand, worldPositionStays: true);
            }
        }

        _didDisablePhysicalInteractor = physicalInteractor != null && !physicalInteractor.hasSelection;
        if (_didDisablePhysicalInteractor) physicalInteractor.allowSelect = false;

        // Disable all XRDirectInteractors in the avatar arm hierarchy (e.g. IK hand interactor)
        // so none of them auto-select the target via XRI while HOMER owns the grab.
        if (armRoot != null)
            foreach (var inter in armRoot.GetComponentsInChildren<XRDirectInteractor>())
                if (!inter.hasSelection) inter.allowSelect = false;

        if (_virtualInteractor != null) _virtualInteractor.allowSelect = false;

        if (loopSfxWhileExtended)
            _extendedLoopSource = AudioManager.PlayLooping(extendedLoopSfx, transform, true);
    }

    private void EndExtend()
    {
        AudioManager.StopLooping(_extendedLoopSource);
        _extendedLoopSource = null;

        var poseAnimatorEnd = _virtualInteractor as DynamicXRDirectInteractorAnimator;
        if (poseAnimatorEnd != null) poseAnimatorEnd.ResetToDefaultPose();

        // Un-parent BEFORE destroying the hand prefab — Destroy() propagates to all
        // children at end-of-frame, so the carried object must leave the hierarchy first.
        if (_carriedObject != null)
            _carriedObject.transform.SetParent(null, worldPositionStays: true);

        if (_handInstance != null) { Destroy(_handInstance); _handInstance = null; }
        _virtualInteractor = null;
        VirtualHand = null;

        SetPhysicalHandVisible(true);

        // Restore physical interactor before delivery so SelectEnter can succeed.
        if (_didDisablePhysicalInteractor && physicalInteractor != null)
        {
            physicalInteractor.allowSelect = true;
            _didDisablePhysicalInteractor = false;
        }

        // Deliver BEFORE restoring arm hierarchy — prevents IK arm interactor
        // from auto-selecting the carried object before the physical hand gets it.
        if (_carriedObject != null)
        {
            DeliverToPhysicalHand(_carriedObject);
            _carriedObject = null;
            _carriedRb = null;
        }

        if (armRoot != null)
            foreach (var inter in armRoot.GetComponentsInChildren<XRDirectInteractor>())
                if (inter != null) inter.allowSelect = true;
    }

    private void RestorePhysicalArm()
    {
        if (_didDisablePhysicalInteractor && physicalInteractor != null)
        {
            physicalInteractor.allowSelect = true;
            _didDisablePhysicalInteractor = false;
        }
        if (armRoot != null)
            foreach (var inter in armRoot.GetComponentsInChildren<XRDirectInteractor>())
                if (inter != null) inter.allowSelect = true;
    }

    private void ForceReleaseFromAllInteractors(GameObject obj)
    {
        var xrGrabbable = obj.GetComponent<XRGrabInteractable>();
        if (xrGrabbable == null || !xrGrabbable.isSelected) return;
        var interactors = new List<IXRSelectInteractor>(xrGrabbable.interactorsSelecting);
        foreach (var interactor in interactors)
            xrGrabbable.interactionManager.SelectExit(interactor, xrGrabbable);
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
        ForceReleaseFromAllInteractors(obj);
        GrabbedObject = obj;

        _grabbedRb = obj.GetComponent<Rigidbody>();
        if (_grabbedRb != null)
        {
            _rbWasKinematic = _grabbedRb.isKinematic;
            _grabbedRb.isKinematic = true;
        }

        if (obj.GetComponent<IRotaryGrabbable>() == null)
            obj.transform.position = HandTip != null ? HandTip.position : _extendedPos;
        VirtualHand.rotation = transform.rotation;

        _rotationOffset = obj.transform.rotation * Quaternion.Inverse(transform.rotation);
        _state = State.Grabbed;

        var poseAnimator = _virtualInteractor as DynamicXRDirectInteractorAnimator;
        if (poseAnimator != null)
        {
            var grabPose = obj.GetComponent<GrabPose>();
            if (grabPose != null && grabPose.data != null)
            {
                poseAnimator.BendPhalanges(grabPose.data.handData);
                // Apply the authored attach-point offsets so HandTip position and the
                // object's held rotation both match the pose regardless of grab angle.
                // homerRotationOffset / homerPositionOffset on GrabPose allow per-object
                // fine-tuning in the Inspector without editing the shared ScriptableObject.
                Vector3 rot = grabPose.data.rotationOffset + grabPose.homerRotationOffset;
                Vector3 pos = grabPose.data.positionOffset + grabPose.homerPositionOffset;
                if (_virtualInteractor.attachTransform != null)
                    _virtualInteractor.attachTransform.SetLocalPositionAndRotation(
                        pos, Quaternion.Euler(rot));
                _rotationOffset = Quaternion.Euler(rot);
            }
        }

        GrabStarted?.Invoke(obj);

        var wireEnd = obj.GetComponent<WireEndGrabbable>();
        if (wireEnd != null) wireEnd.OnHOMERGrab();
    }

    public void EndGrab()
    {
        if (_grabbedRb != null)
        {
            _grabbedRb.isKinematic = _rbWasKinematic;
            _grabbedRb = null;
        }

        var poseAnimatorEnd = _virtualInteractor as DynamicXRDirectInteractorAnimator;
        if (poseAnimatorEnd != null) poseAnimatorEnd.ResetToDefaultPose();

        if (GrabbedObject != null)
        {
            var wireEnd = GrabbedObject.GetComponent<WireEndGrabbable>();
            if (wireEnd != null) wireEnd.OnHOMERRelease();
        }

        GrabbedObject = null;
        GrabEnded?.Invoke();
        BeginRetract();
    }

    private void DropCarriedObject()
    {
        if (_carriedObject != null) _carriedObject.transform.SetParent(null, worldPositionStays: true);

        var poseAnimator = _virtualInteractor as DynamicXRDirectInteractorAnimator;
        if (poseAnimator != null) poseAnimator.ResetToDefaultPose();

        if (_carriedRb != null)
        {
            _carriedRb.isKinematic = _carriedRbWasKinematic;
            _carriedRb = null;
        }

        if (_carriedObject != null)
        {
            var wireEnd = _carriedObject.GetComponent<WireEndGrabbable>();
            if (wireEnd != null) wireEnd.OnHOMERRelease();
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
        AudioManager.Play(SfxType.HomerInvalidTarget, transform);
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
    }

    private Vector3 GetTorsoPosition()
    {
        Camera cam = Camera.main;
        return cam != null
            ? cam.transform.position + Vector3.down * torsoHeadOffset
            : transform.position;
    }

    // ── Raycast ───────────────────────────────────────────────────────────

    // Processes a pre-filled _rayBuffer (hitCount entries) sorted by distance.
    // Rules: trigger+grabbable → grab target; trigger+no-grab → transparent;
    //        solid+grabbable → grab target; solid+no-grab → blocker (blockerLayer set).
    // Returns true if a grab target was found. blockerLayer = -1 when no solid blocker.
    private bool ProcessRayHits(int hitCount, out Vector3 hitPoint, out XRGrabInteractable grabbable, out int blockerLayer)
    {
        Array.Sort(_rayBuffer, 0, hitCount, Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance)));
        hitPoint = Vector3.zero;
        grabbable = null;
        blockerLayer = -1;
        for (int i = 0; i < hitCount; i++)
        {
            var hit = _rayBuffer[i];
            var found = hit.collider.GetComponentInParent<XRGrabInteractable>();
            if (found != null && found.enabled)
            {
                hitPoint = hit.point;
                grabbable = found;
                return true;
            }
            if (!hit.collider.isTrigger)
            {
                hitPoint = hit.point;
                blockerLayer = hit.collider.gameObject.layer;
                return false;
            }
            // non-grabbable trigger: transparent, keep searching
        }
        return false;
    }

    private bool TryGetTarget(out Vector3 hitPoint, out XRGrabInteractable grabbable)
    {
        Vector3 origin = aimOffset != null ? aimOffset.transform.position : transform.position;
        Vector3 dir = aimOffset != null ? aimOffset.transform.forward : transform.forward;
        LayerMask scanMask = ~unhitableLayer;

        int count = Physics.RaycastNonAlloc(origin, dir, _rayBuffer, rayLength, scanMask, QueryTriggerInteraction.Collide);
        if (count == 0) { hitPoint = Vector3.zero; grabbable = null; return false; }

        bool found = ProcessRayHits(count, out hitPoint, out grabbable, out int blockerLayer);
        if (!found && blockerLayer >= 0 && (hitableLayer.value & (1 << blockerLayer)) != 0)
        {
            // Hitable surface with no grabbable: arm extends here and stays (State.Extended).
            // grabbable remains null — player moves the empty hand, trigger retracts.
            return true;
        }
        return found;
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
        int count = Physics.RaycastNonAlloc(origin, dir, _rayBuffer, rayLength, scanMask, QueryTriggerInteraction.Collide);
        if (count > 0)
        {
            bool hasTarget = ProcessRayHits(count, out Vector3 hp, out _, out int bl);
            if (hp != Vector3.zero) endpoint = hp;
            valid = hasTarget || (bl >= 0 && (hitableLayer.value & (1 << bl)) != 0);
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
