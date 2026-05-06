using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Go-Go interaction technique (Poupyrev et al. 1996).
/// Attach to a child GameObject under the Right Hand controller.
///
/// Instantiates a floating virtual hand prefab (goGoHandPrefab) at Start and positions it
/// each LateUpdate using the Go-Go mapping:
///
///   R_v = R_r               if R_r &lt; D  (linear zone, 1:1)
///   R_v = R_r + k(R_r-D)²  otherwise   (non-linear zone)
///
/// where D = 2/3 × armLength. The physical hand renderers are hidden when the virtual hand
/// enters the non-linear zone (i.e. when they start to diverge spatially).
/// </summary>
[DefaultExecutionOrder(100)]
public class GoGoExtend : MonoBehaviour
{
    [Header("GoGo Hand Prefab")]
    [Tooltip("Prefab instantiated at Start as the floating virtual hand. Must have an XRDirectInteractor.")]
    public GameObject goGoHandPrefab;

    [Header("Physical Hand")]
    [Tooltip("Renderers on the physical right-controller hand mesh. Hidden when the virtual hand enters the non-linear zone.")]
    public Renderer[] physicalHandRenderers;

    [Header("References")]
    [Tooltip("Transform at the chest/torso — the user-centred origin for Go-Go vectors. " +
             "If null, estimated as main camera position offset down by chestHeadOffset.")]
    public Transform chestTransform;

    [Tooltip("The IK end-effector target that FollowXROrigin drives to the physical hand each frame. " +
             "GoGoExtend overrides it (at execution order 100) to the virtual GoGo position so TwoBoneIK extends the whole arm.")]
    [SerializeField] private GameObject armXRTarget;

    [Header("Go-Go Parameters")]
    [Tooltip("Physical arm length in metres. The linear-zone threshold D = 2/3 × armLength.")]
    public float armLength = 0.6f;

    [Tooltip("Maximum virtual reach in metres when the physical hand is fully extended (R_r = armLength).")]
    public float maxReachDistance = 5f;

    [Header("Rotation")]
    [Tooltip("Euler offset applied on top of the controller rotation.")]
    public Vector3 rotationOffset = new Vector3(0f, 0f, 120f);

    [Tooltip("Slerp speed for mirroring the controller rotation onto the virtual hand.")]
    public float rotationSmoothing = 12f;

    [Header("Position Offset")]
    [Tooltip("Controller-local XYZ offset applied to the virtual hand position. X = right, Y = up, Z = forward relative to the controller. Adjust Z to push forward/back, X for side offset.")]
    public Vector3 handPositionOffset = Vector3.zero;

    [Header("Chest Estimation")]
    [Tooltip("Vertical offset below the HMD used to estimate chest position when chestTransform is null.")]
    public float chestHeadOffset = 0.45f;

    [Header("Audio")]
    [SerializeField] bool loopSfxWhileExtended;

    private AudioSource _extendedLoopSource;
    private Transform _virtualHand;
    private XRDirectInteractor _interactor;
    private Rigidbody _virtualHandRb;
    public bool disablePhysicsWhileGrabbing;

    /// <summary>The instantiated floating hand Transform. LeverGrab/ValveGrab/SimonButton read and write its position for arc-locking.</summary>
    public Transform VirtualHand => _virtualHand;
    /// <summary>The XRDirectInteractor on the virtual hand. Puzzle scripts compare against this to detect GoGo grabs.</summary>
    public XRDirectInteractor Interactor => _interactor;
    /// <summary>The intended grab/attach point on the virtual hand — the interactor's attachTransform (Attach Point child).
    /// Falls back to VirtualHand root if the interactor has no attachTransform assigned.</summary>
    public Transform HandTip => (_interactor != null && _interactor.attachTransform != null)
        ? _interactor.attachTransform
        : _virtualHand;
    /// <summary>Current physical arm length R_r (chest to controller).</summary>
    public float CurrentRr { get; private set; }
    /// <summary>Current virtual arm length R_v after Go-Go mapping.</summary>
    public float CurrentRv { get; private set; }

    void Start()
    {
        if (goGoHandPrefab == null)
        {
            Debug.LogWarning("[GoGoExtend] goGoHandPrefab is not assigned. Assign a prefab with an XRDirectInteractor in the Inspector.", this);
            return;
        }

        GameObject hand = Instantiate(goGoHandPrefab, transform.position, Quaternion.identity, null);
        _virtualHand = hand.transform;
        _interactor = hand.GetComponentInChildren<XRDirectInteractor>();
        _virtualHandRb = hand.GetComponent<Rigidbody>();

        if (_interactor == null)
            Debug.LogWarning("[GoGoExtend] Instantiated GoGo hand prefab has no XRDirectInteractor component.", this);
    }

    void OnEnable()
    {
        if (_virtualHand != null) _virtualHand.gameObject.SetActive(true);
        SetPhysicalHandVisible(false);
        if (loopSfxWhileExtended)
            _extendedLoopSource = AudioManager.PlayLooping(SfxType.Hover, transform, true);
    }

    void OnDisable()
    {
        if (_virtualHand != null) _virtualHand.gameObject.SetActive(false);
        SetPhysicalHandVisible(true);
        AudioManager.StopLooping(_extendedLoopSource);
        _extendedLoopSource = null;
    }

    void OnDestroy()
    {
        AudioManager.StopLooping(_extendedLoopSource);
        if (_virtualHand != null) Destroy(_virtualHand.gameObject);
    }

    void FixedUpdate()
    {
        if (_virtualHand == null && armXRTarget == null) return;

        Vector3 chestPos = GetChestPosition();
        Vector3 controllerPos = transform.position;
        Vector3 toController = controllerPos - chestPos;

        float R_r = toController.magnitude;
        if (R_r < 1e-5f) return;

        Vector3 dir = toController / R_r;
        float D = 2f / 3f * armLength;

        float armOverThree = armLength / 3f;
        float k = armOverThree > 1e-5f
            ? Mathf.Max(0f, maxReachDistance - armLength) / (armOverThree * armOverThree)
            : 0f;

        float R_v = R_r < D
            ? R_r
            : R_r + k * (R_r - D) * (R_r - D);

        R_v = Mathf.Min(R_v, maxReachDistance);

        CurrentRr = R_r;
        CurrentRv = R_v;

        // Single consistent formula — no branch, no snap at the zone boundary.
        // In the linear zone R_v == R_r, so targetPos == controllerPos exactly.
        Vector3 targetPos = chestPos + dir * R_v;
        // Controller-local offset so it stays aligned regardless of hand orientation.
        Vector3 worldOffset = transform.TransformDirection(handPositionOffset);

        if (armXRTarget != null)
            armXRTarget.transform.SetPositionAndRotation(targetPos, transform.rotation);

        if (_virtualHand != null && !disablePhysicsWhileGrabbing)
        {
            _virtualHandRb.isKinematic = false;
            // Set position based on velocity for contrained physics.
            _virtualHandRb.linearVelocity = (targetPos + worldOffset - _virtualHandRb.position) / Time.fixedDeltaTime;

            // Set rotation based on angular velocity for contrained physics.
            var rotationDifference = (transform.rotation * Quaternion.Euler(rotationOffset)) * Quaternion.Inverse(_virtualHandRb.rotation);
            rotationDifference.ToAngleAxis(out float angleInDegrees, out Vector3 rotationAxis);
            if (angleInDegrees > 180f)
                angleInDegrees -= 360f;
            if (Mathf.Abs(angleInDegrees) < 0.01f || rotationAxis == Vector3.zero)
                _virtualHandRb.angularVelocity = Vector3.zero;
            else
                _virtualHandRb.angularVelocity = rotationAxis * angleInDegrees * Mathf.Deg2Rad / Time.fixedDeltaTime;
        }
        if(_virtualHand != null && disablePhysicsWhileGrabbing)
        {
            _virtualHandRb.isKinematic = true;
            _virtualHandRb.linearVelocity = Vector3.zero;
            _virtualHandRb.angularVelocity = Vector3.zero;
            Quaternion handRot = transform.rotation * Quaternion.Euler(rotationOffset);
            Quaternion smoothedRot = Quaternion.Slerp(_virtualHand.rotation, handRot, rotationSmoothing * Time.deltaTime);
            _virtualHand.SetPositionAndRotation(targetPos + worldOffset, smoothedRot);
        }
    }

    private void SetPhysicalHandVisible(bool visible)
    {
        if (physicalHandRenderers == null) return;
        foreach (var r in physicalHandRenderers)
            if (r != null) r.enabled = visible;
    }

    private Vector3 GetChestPosition()
    {
        if (chestTransform != null)
            return chestTransform.position;

        Camera cam = Camera.main;
        if (cam != null)
            return cam.transform.position + Vector3.down * chestHeadOffset;

        return transform.position;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 chest = Application.isPlaying
            ? GetChestPosition()
            : (chestTransform != null
                ? chestTransform.position
                : transform.position + Vector3.down * chestHeadOffset);

        float D = 2f / 3f * armLength;

        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.10f);
        Gizmos.DrawSphere(chest, D);
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.80f);
        Gizmos.DrawWireSphere(chest, D);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.05f);
        Gizmos.DrawSphere(chest, armLength);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.60f);
        Gizmos.DrawWireSphere(chest, armLength);

        if (!Application.isPlaying || _virtualHand == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(chest, transform.position);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(chest, _virtualHand.position);
    }
#endif
}
