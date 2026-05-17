using UnityEngine;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[DefaultExecutionOrder(100)] 
[RequireComponent(typeof(LimbStretch))]
public class DAOMArm : MonoBehaviour
{
    public static DAOMArm ActiveInstance { get; private set; }
    public DynamicXRDirectInteractorAnimator Interactor => interactor;
    public Transform DaomIKTarget => daomIKTarget.transform;

    [HideInInspector] GameObject playerRoot;
    [HideInInspector] GameObject playerXRTarget; // Called player hand in comments for readability

    [Header("DAOM")]
    [SerializeField] Animator animator;
    [SerializeField] GameObject daomRoot;
    [SerializeField] GameObject upperArm;
    [SerializeField] GameObject lowerArm;
    [SerializeField] GameObject tip;
    [SerializeField] Rigidbody daomIKTarget; // Called daom hand in comments for readability

    [Header("Travel")]
    [SerializeField] GameObject thruster;
    [SerializeField][Tooltip("The time it takes the arm to travel from the body to the surface and vice-versa.")] float travelSpeed = 7f;
    [SerializeField] Vector3 lowerArmExtention;
    [SerializeField] Vector3 lowerArmRetraction;
    [SerializeField] float retractionTime = 0.4f;

    [Header("Motion and Rotation")]
    [SerializeField] [Tooltip("Adjust the strength of how much the DAOM arm follows the directional movement of the player's arm. 0.6 seems so be fitting for the new robot model.")] [Range(0,2)] float followStrength = 0.6f;
    [SerializeField] [Tooltip("The point in travel time the arm will rotate, normalized from percentage of traveled distance")] [Range(0,1)] float rotationStartTime = 0.8f;
    [SerializeField] [Tooltip("The time it takes the arm to rotate into place relative to the surface's normal.")] float rotationDuration = 0.5f;
    [SerializeField] Vector3 handRotationOffset = new(90,0,0);

    [Header("Wall Offset")]
    [SerializeField] GameObject wallExtention;
    [SerializeField] GameObject wallAttachPoint;
    [SerializeField] float wallDistanceOffset = 0.3f;

    [Header("Interactor")]
    [SerializeField] DynamicXRDirectInteractorAnimator interactor;

    AudioSource fireAudioSource;

    Vector3 hitPoint;
    bool surfaceIsGround;
    Quaternion targetRot;
    GameObject playerCamera;

    Quaternion upperArmStartRot;
    Quaternion lowerArmStartRot;
    Quaternion tipStartRot;

    IXRSelectInteractable selectedInteractable;
    IXRSelectInteractable hitInteractable;
    [Header("Debug")]
    [SerializeField]bool isTraveling = false;
    [SerializeField]bool isAttachedToSurface = false;
    public bool IsAttachedToSurface => isAttachedToSurface;
    bool recalling = false;
    bool isLanding = false;
    bool mayAttach = false;

    public bool Recalling => recalling;

    void Awake()
    {
        ActiveInstance = this;
    }

    void OnDestroy()
    {
        if (ActiveInstance == this) ActiveInstance = null;
        if(fireAudioSource != null)
            AudioManager.StopLooping(fireAudioSource);
    }

    void OnEnable()
    {
        LaunchArm.SetInteractorHandedness += GetHandedness;
        interactor.selectEntered.AddListener(OnGrab);
        interactor.selectExited.AddListener(OnRelease);
    }

    void OnDisable()
    {
        LaunchArm.SetInteractorHandedness -= GetHandedness;
        interactor.selectEntered.RemoveListener(OnGrab);
        interactor.selectExited.RemoveListener(OnRelease);
    }

    /// <summary>
    /// The handedness of the interactor and input is set relative to the hand the player used to launch the arm.
    /// </summary>
    void GetHandedness(XRDirectInteractor interactor)
    {
        this.interactor.handedness = interactor.handedness;
        this.interactor.selectInput = interactor.selectInput;
        this.interactor.activateInput = interactor.activateInput;
    }

    /// <summary>
    /// Handles the grab event by updating the currently selected interactable object.
    /// </summary>
    void OnGrab(SelectEnterEventArgs args)
    {
        selectedInteractable = args.interactableObject;
    }

    /// <summary>
    /// Handles the release event for the interactable object.
    /// </summary>
    void OnRelease(SelectExitEventArgs args)
    {
        selectedInteractable = null;
    }

    /// <summary>
    /// Initializes a bunch of variables sent by the player, setting the root, IK and more. It then begins movement toward the specified point.
    /// </summary>
    public void Initialize(GameObject root, GameObject IKTarget, Vector3 point, IXRSelectInteractable hitInteractable = null, IXRSelectInteractable interactable = null, bool surfaceIsGround = false)
    {
        animator.enabled = false;
        GetComponent<LimbStretch>().enabled = false; // Disable limb stretch during recall to prevent weird positioning of the arm.

        thruster.SetActive(true);
        wallExtention.SetActive(false);
        daomIKTarget.isKinematic = true;

        playerCamera = Camera.main.gameObject;

        playerRoot = root;
        playerXRTarget = IKTarget;
        hitPoint = point;
        this.surfaceIsGround = surfaceIsGround;

        upperArmStartRot = upperArm.transform.localRotation;
        lowerArmStartRot = lowerArm.transform.localRotation;
        tipStartRot = tip.transform.localRotation;

        if (interactor != null && interactable != null) // If we already hold an interactable, keep holding it.
        {
            selectedInteractable = interactable;
            selectedInteractable.transform.position = interactor.attachTransform.position;
            interactor.interactionManager.SelectEnter(interactor, selectedInteractable);
            interactor.selectActionTrigger = XRBaseInputInteractor.InputTriggerType.Sticky;
        }
        if (hitInteractable != null) // If the arm hit is an interactable, store it so we can recall the arm holding the interactable after hitting it.
        {
            this.hitInteractable = hitInteractable;
        }

        StartCoroutine(TravelToPoint(transform, point));
        lowerArm.transform.localPosition = lowerArmRetraction;

        AudioManager.Play(SfxType.Explosion, transform); 
        fireAudioSource = AudioManager.PlayLooping(SfxType.Fire, transform); 
    }

    /// <summary>
    /// Initiates the process of recalling the arm to the specified point and orientation.
    /// </summary>
    public void RecallArm(GameObject goPoint)
    {
        if (isTraveling) return;
        if (!isAttachedToSurface) return;
        if (recalling) return;

        isAttachedToSurface = false;
        recalling = true;

        AudioManager.Play(SfxType.Explosion, transform); 
        fireAudioSource = AudioManager.PlayLooping(SfxType.Fire, transform); 

        thruster.SetActive(true);
        wallExtention.SetActive(false);
        GetComponent<LimbStretch>().enabled = false; // Disable limb stretch during recall to prevent weird positioning of the arm.
        
        animator.enabled = false;

        interactor.selectActionTrigger = XRBaseInputInteractor.InputTriggerType.Sticky;
        
        if(hitInteractable != null)
        {
            selectedInteractable = hitInteractable;
        }
        LaunchArm.OnGrabbedGameObject(selectedInteractable);

        targetRot = LookDirection(goPoint.transform.position);
        
        StartCoroutine(RotateToTargetRotation(transform, targetRot, rotationDuration));
        StartCoroutine(RotateToTargetRotation(upperArm.transform, upperArmStartRot, rotationDuration, true));
        StartCoroutine(RotateToTargetRotation(lowerArm.transform, lowerArmStartRot, rotationDuration, true));
        StartCoroutine(RotateToTargetRotation(tip.transform, tipStartRot, rotationDuration, true));
        StartCoroutine(TravelToGameObject(goPoint));
    }

    /// <summary>
    /// Moves the GameObject smoothly to the specified POSITION. The local parameter is used ONLY for the lower arm extension.
    /// </summary>
    IEnumerator TravelToPoint(Transform transform, Vector3 point, bool local = false)
    {
        var startRot = local ? transform.localPosition : transform.position;
        if (local)
        {
            var t = 0f;
            var elapsedTime = 0f;
            while (true)
            {
                elapsedTime += Time.deltaTime;
                if (recalling)
                {
                    t = Mathf.Clamp01(elapsedTime / retractionTime); // Cool little retraction for the arm when recalling
                    transform.localPosition = Vector3.Lerp(startRot, point, t);
                }
                else
                {
                    transform.localPosition = point;
                }

                if (t >= 1 || !recalling)
                {
                    break;
                }
                yield return null;
            }
        }
        else
        {
            if (isTraveling) yield break;
            isTraveling = true;

            var totalDistance = Vector3.Distance(startRot, point) - wallDistanceOffset;
            var newPoint = point - wallDistanceOffset * (point - startRot).normalized; // Move the target point back a bit so the arm doesn't clip into the wall.

            //var totalDistance = Vector3.Distance(startRot, point);
            //var newPoint = point - (point - startRot).normalized; // Move the target point back a bit so the arm doesn't clip into the wall.

            var traveledDistance = 0f;

            while (true)
            {
                var distanceDelta = travelSpeed * Time.deltaTime;
                traveledDistance += distanceDelta;
                var d = Mathf.Clamp01(traveledDistance / totalDistance);

                transform.position = Vector3.Lerp(startRot, newPoint, d);

                if (d >= rotationStartTime)
                {
                    StartCoroutine(PrepareSurfaceLanding());
                }
                if (d >= 1 && mayAttach)
                {
                    wallExtention.SetActive(true);
                    ArmAttaching();
                    break;
                }
                yield return null;
            }
        }
    }

    /// <summary>
    /// Moves the GameObject smoothly to the specified GAMEOBJECT'S POSITION.
    /// </summary>
    IEnumerator TravelToGameObject(GameObject goPoint)
    {
        if (isTraveling) yield break;
        isTraveling = true;
        var startPos = transform.position;
        var totalDistance = Vector3.Distance(startPos, goPoint.transform.position);
        var traveledDistance = 0f;

        while (true)
        {
            var distanceDelta = travelSpeed * Time.deltaTime;
            traveledDistance += distanceDelta;
            var d = Mathf.Clamp01(traveledDistance / totalDistance);

            transform.position = Vector3.Lerp(startPos, goPoint.transform.position, d);

            if (d >= rotationStartTime)
            {
                StartCoroutine(PrepareSurfaceLanding());
            }
            if (d >= 1 && mayAttach)
            {
                ArmAttaching();
                break;
            }
            yield return null;
        }
    }

    /// <summary>
    /// Prepares the arm for landing on a surface by initiating its rotation to the appropriate orientation.
    /// </summary>
    IEnumerator PrepareSurfaceLanding()
    {
        if (!isLanding)
        {
            isLanding = true;
            if(hitInteractable != null && !recalling)
            {
                mayAttach = true;
                yield return null;
            }
            
            thruster.SetActive(false);
            targetRot = Quaternion.LookRotation(-transform.forward);
            StartCoroutine(RotateToTargetRotation(transform, targetRot, rotationDuration));
            StartCoroutine(TravelToPoint(lowerArm.transform, recalling == true ? lowerArmRetraction : lowerArmExtention, true));
            
            yield return new WaitForSeconds(rotationDuration);
            if (Physics.Raycast(transform.position, -transform.forward, out var hit, 2f)) // Allign the arm to the surface normal if we hit the surface, otherwise just look in the direction of the player camera.
            {
                targetRot = Quaternion.LookRotation(hit.normal);
                var mainRotation = Quaternion.Euler(new Vector3(0, targetRot.eulerAngles.y, 0) - playerCamera.transform.forward);

                if (surfaceIsGround)
                {
                    targetRot = Quaternion.LookRotation(hit.normal - playerCamera.transform.forward);
                }

                var roundedRot = new Vector3(0,
                                             Mathf.Round(targetRot.eulerAngles.y / 90) * 90, // Make sure the Y rotation is only in 90° increments.
                                             0);
                var groundTargetRotY = Quaternion.Euler(-90, roundedRot.y, 0);
                var wallTargetRotY = Quaternion.Euler(0, roundedRot.y, 0);
                StartCoroutine(RotateToTargetRotation(transform, mainRotation, rotationDuration));
                if (surfaceIsGround)
                {
                    StartCoroutine(RotateToTargetRotation(wallExtention.transform, groundTargetRotY, rotationDuration));
                }
                else
                {
                    StartCoroutine(RotateToTargetRotation(wallExtention.transform, wallTargetRotY, rotationDuration));
                }

                yield return new WaitForSeconds(rotationDuration);
                mayAttach = true;
            }
            else
            {
                LaunchArm.OnEarlyRecall();
            }
        }
        yield return null;
    }

    /// <summary>
    /// Attaches the arm to a surface and rotates it to the appropriate orientation.
    /// </summary>
    void ArmAttaching()
    {
        mayAttach = false;
        isLanding = false;
        isTraveling = false;
        isAttachedToSurface = true;
        animator.enabled = true;
        GetComponent<LimbStretch>().enabled = true;
        
        AudioManager.StopLooping(fireAudioSource);
        AudioManager.Play(SfxType.ArmAttach, transform);
        
        // If the arm hit an interactable, recall the arm WITH the interactable so the player holds it after recall.
        if (hitInteractable != null && !recalling)
        {
            LaunchArm.OnEarlyRecall();
            selectedInteractable.transform.position = interactor.attachTransform.position;
            interactor.interactionManager.SelectEnter(interactor, selectedInteractable);
            interactor.selectActionTrigger = XRBaseInputInteractor.InputTriggerType.Sticky;
            return;
        }

        interactor.selectActionTrigger = XRBaseInputInteractor.InputTriggerType.StateChange;

        if (recalling)
        {
            LaunchArm.OnArmRecalled(selectedInteractable);
            return;
        }

        // The wall extension offset makes the raycast stop a bit before the actual wall and since we round to the nearest 90 degrees for rotation, it looks like the arm didn't hit the right spot
        // This needs some adjusment if the surface is the ground.
        if (surfaceIsGround)
        {
            var newPoint = hitPoint + transform.up * wallDistanceOffset;
            transform.position = newPoint;
        }
        else
        {
            var newPoint = hitPoint + transform.forward * wallDistanceOffset;
            transform.position = newPoint;
        }
        daomIKTarget.isKinematic = false;
    }

    /// <summary>
    /// Calculates the rotation required to face the specified target position from this transform's position.
    /// </summary>
    Quaternion LookDirection(Vector3 target)
    {
        var direction = (target - transform.position).normalized;
        var rotation = Quaternion.LookRotation(direction, Vector3.up);
        return rotation;
    }


    /// <summary>
    /// Rotates the object from its current orientation to the target rotation over a specified duration.
    /// </summary>
    IEnumerator RotateToTargetRotation(Transform transform, Quaternion targetRot, float duration, bool local = false)
    {
        var elapsedTime = 0f;
        var startRot = local ? transform.localRotation : transform.rotation;
        while (true)
        {
            elapsedTime += Time.deltaTime;
            var t = Mathf.Clamp01(elapsedTime / duration);

            if(local)
                transform.localRotation = Quaternion.Slerp(startRot, targetRot, t);
            else
                transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            if (elapsedTime >= duration)
            {
                break;
            }
            yield return null;
        }
        yield break;
    }

    public bool disablePhysicsWhileGrabbing;
    void LateUpdate()
    {
        if(!ActiveInstance.disablePhysicsWhileGrabbing) return;
        if (!isAttachedToSurface || recalling) return;
        print("Grabbing");
        TransformToPlayerHand();
        RotateToPlayerHand();
    }

    void FixedUpdate()
    {
        if (ActiveInstance.disablePhysicsWhileGrabbing) return;
        if (!isAttachedToSurface || recalling) return;
        TransformToPlayerHand();
        RotateToPlayerHand();
    }

    /// <summary>
    /// Translates the position of the DAOM hand target to match the relative position of the player hand.
    /// </summary>
    void TransformToPlayerHand()
    {
        // Get the position of the player hand relative to the player root, and apply that same relative position to the daom root to find the position for the daom hand.
        var playerHandOffset = playerRoot.transform.InverseTransformPoint(playerXRTarget.transform.position);

        playerHandOffset.x *= -1;
        playerHandOffset.y *= -1;

        playerHandOffset *= followStrength;
        
        if(ActiveInstance.disablePhysicsWhileGrabbing)
        {
            if(daomIKTarget.isKinematic == false)
            {
                daomIKTarget.linearVelocity = Vector3.zero;
                daomIKTarget.isKinematic = true;
            }

            // Apply the relative position to the daom root to find the target position for the daom hand.
            daomIKTarget.transform.position = daomRoot.transform.TransformPoint(playerHandOffset);
        }
        else
        {
            if(daomIKTarget.isKinematic == true)
                daomIKTarget.isKinematic = false;
            daomIKTarget.linearVelocity = (daomRoot.transform.TransformPoint(playerHandOffset) - daomIKTarget.position) / Time.fixedDeltaTime;
        }
    }

    /// <summary>
    /// Rotates the DAOM hand to match the orientation of the player hand relative to their respective root transforms.
    /// </summary>
    void RotateToPlayerHand()
    {
        // Get the rotation of the player hand relative to the player root, and apply that same relative rotation to the daom root to find the rotation for the daom hand.
        var relativeRot = playerXRTarget.transform.localRotation * Quaternion.Euler(handRotationOffset);

        relativeRot.x *= -1;
        relativeRot.z *= -1;
        relativeRot *= playerRoot.transform.localRotation;

        if (ActiveInstance.disablePhysicsWhileGrabbing)
        {
            if(daomIKTarget.isKinematic == false)
            {
                daomIKTarget.angularVelocity = Vector3.zero;
                daomIKTarget.isKinematic = true;
            }
            
            // Apply the relative rotation to the daom root to find the target rotation for the daom hand.
            daomIKTarget.transform.rotation = relativeRot;
        }
        else
        {
            if(daomIKTarget.isKinematic == true)
                daomIKTarget.isKinematic = false;
            var difference = relativeRot * Quaternion.Inverse(daomIKTarget.rotation);
            difference.ToAngleAxis(out float angleInDegrees, out Vector3 rotationAxis);
            if (angleInDegrees > 180f)
                angleInDegrees -= 360f;
            if (Mathf.Abs(angleInDegrees) < 0.01f || rotationAxis == Vector3.zero)
                daomIKTarget.angularVelocity = Vector3.zero;
            else
                daomIKTarget.angularVelocity = rotationAxis * angleInDegrees * Mathf.Deg2Rad / Time.fixedDeltaTime;
        }
    }
}
