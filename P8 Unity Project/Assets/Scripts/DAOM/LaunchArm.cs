using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class LaunchArm : MonoBehaviour
{
    [Header("Player Rotation")]
    [SerializeField] new GameObject camera;
    
    [Header("Raycast")]
    [SerializeField] float rayLength = 50f;
    [SerializeField] LayerMask hitableLayer;
    [SerializeField] LayerMask unhitableLayer;
    RaycastHit hit;

    [Header("Firing Arm")]
    [SerializeField] GameObject boomEffect;
    [SerializeField] GameObject armRoot;
    [SerializeField] GameObject armXRTarget;
    [SerializeField] GameObject armGameObject;

    [Header("Launched Arm")]
    [SerializeField] GameObject daomArmPrefab;
    [SerializeField] GameObject launchPoint;
    GameObject daomArm;

    [Header("Interactor")]
    [SerializeField] XRDirectInteractor interactor;

    [Header("Input")]
    [SerializeField] InputActionReference launchInput;
    [SerializeField] InputActionReference aimInput;

    [Header("State")]
    [SerializeField] bool aiming = false;
    [SerializeField] bool canLaunch = true;

    [Header("Line Renderer")]
    [SerializeField] GameObject aimOffset;
    [SerializeField] [ColorUsage(true, true)] Color validColor;
    [SerializeField] [ColorUsage(true, true)] Color invalidColor;
    LineRenderer lineRenderer;

    IXRSelectInteractable selectedInteractable;
    IXRSelectInteractable hitInteractable;
    public bool IsAiming => aiming;

    public static Action<XRDirectInteractor> SetInteractorHandedness;
    public static void OnSetInteractorHandedness(XRDirectInteractor interactor) => SetInteractorHandedness?.Invoke(interactor);

    public static Action ArmLaunched;
    public static void OnArmLaunched() => ArmLaunched?.Invoke();

    public static Action<IXRSelectInteractable> ArmRecalled;
    public static void OnArmRecalled(IXRSelectInteractable interactable) => ArmRecalled?.Invoke(interactable);

    public static Action<IXRSelectInteractable> GrabbedGameObject;
    public static void OnGrabbedGameObject(IXRSelectInteractable interactable) => GrabbedGameObject?.Invoke(interactable);

    public static Action EarlyRecall;
    public static void OnEarlyRecall() => EarlyRecall?.Invoke();

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (camera == null)
        {
            camera = Camera.main.gameObject;
        }
        aiming = false;
    }

    void OnEnable()
    {
        ArmRecalled += RemoveDAOMArm;
        EarlyRecall += canLaunch ? Launch : null;

        // <Input>
        interactor.selectEntered.AddListener(OnGrab);
        interactor.selectExited.AddListener(OnRelease);
        
        launchInput.action.performed += LaunchState;
        
        aimInput.action.performed += AimState;
        aimInput.action.canceled += AimState;
        // </Input>
    }


    void OnDisable()
    {
        ArmRecalled -= RemoveDAOMArm;
        EarlyRecall -= canLaunch ? Launch : null;

        // <Input>
        interactor.selectEntered.RemoveListener(OnGrab);
        interactor.selectExited.RemoveListener(OnRelease);

        launchInput.action.performed -= LaunchState;

        aimInput.action.performed -= AimState;
        aimInput.action.canceled -= AimState;
        // </Input>
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

    void LaunchState(InputAction.CallbackContext ctx)
    {
        if (ctx.ReadValue<float>() >= 0.99f)
        {
            Launch();
        }
    }

    /// <summary>
    /// Handles the aiming state based on the input action context.
    /// </summary>
    void AimState(InputAction.CallbackContext ctx)
    {
        if(!canLaunch) return;
        if (ctx.ReadValue<float>() > 0)
        {
            //interactor.keepSelectedTargetValid = false;
            aiming = true;
        }
        else
        {
            //interactor.keepSelectedTargetValid = true;
            aiming = false;
        }
    }

    /// <summary>
    /// Removes the currently active DAOM arm from the scene, if one exists. And resets the state to allow for launching again as well as interaction.
    /// </summary>
    void RemoveDAOMArm(IXRSelectInteractable interactable)
    {
        if (daomArm != null)
        {
            armGameObject.SetActive(true);
            Destroy(daomArm);
            daomArm = null;
            ForceGrabInteractable(interactable);
            canLaunch = true;
        }
    }

    /// <summary>
    /// Forces the interactor to grab the interactable object from the recalled DAOM.
    /// </summary>
    void ForceGrabInteractable(IXRSelectInteractable interactable)
    {
        interactor.allowSelect = true;
        if (interactable != null)
        {
            //interactor.keepSelectedTargetValid = true;
            interactable.transform.position = interactor.attachTransform.position;
            interactor.interactionManager.SelectEnter(interactor, interactable);
        }
    }

    void Update()
    {
        DrawLineRenderer();
        SetLineMaterial(ValidLayer());
    }

    /// <summary>
    /// Shoot a ray forward to check for valid surfaces
    /// </summary>
    bool ValidLayer()
    {
        if (!aiming) return false;

        if(Physics.Raycast(aimOffset.transform.position, aimOffset.transform.forward, out hit, rayLength, unhitableLayer))
        {
            return false;
        }

        RaycastHit[] hits = Physics.RaycastAll(aimOffset.transform.position, aimOffset.transform.forward, rayLength, hitableLayer);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        if (hits.Length == 0) return false;
        foreach (var h in hits)
        {
            if (selectedInteractable != null && !h.collider.transform.IsChildOf(selectedInteractable.transform) &&
                h.collider.transform.parent.TryGetComponent(out XRGrabInteractable hitInteractable))
                return false;

            if (selectedInteractable != null && h.collider.transform.IsChildOf(selectedInteractable.transform))
                continue;

            hit = h;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Initiates the launch sequence for the arm if all preconditions are met.
    /// </summary>
    public void Launch()
    {
        if (!canLaunch)
        {
            aiming = false;
            RecallArm();
        }
        if(canLaunch && ValidLayer())
        {
            if (daomArm != null)
            {
                if(!daomArm.GetComponent<DAOMArm>().Recalling)
                {
                    // Arm is still flying towards the target, recalling, or attached to the surface.
                }
                return;
            }
            // Preconditions met, launch the arm and set canLaunch to false until the arm is recalled.
            canLaunch = false;
            aiming = false;

            if (hit.collider.gameObject.transform.TryGetComponent(out XRGrabInteractable hitInteractable) && selectedInteractable == null && hitInteractable.gameObject.tag != "Unrecallable" && hitInteractable.enabled)
            {
                this.hitInteractable = hitInteractable;
            }
            else
            {
                this.hitInteractable = null;
            }

            armGameObject.SetActive(false);

            // Calculate the rotation for the arm to be launched at based on the hit point and the launch point and multiplying with an offset.
            var direction = (hit.point - launchPoint.transform.position).normalized;
            var rotation = Quaternion.LookRotation(direction);

            var boomRotation = Quaternion.LookRotation(-camera.transform.position, Vector3.up);
            Instantiate(boomEffect, launchPoint.transform.position, rotation);
            daomArm = Instantiate(daomArmPrefab, launchPoint.transform.position, rotation);
            bool surfaceIsGround = hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground"); // This is used to determine extra rotation for the arm when it hits the ground to make it look better.
            daomArm.GetComponent<DAOMArm>().Initialize(armRoot, armXRTarget, hit.point, this.hitInteractable, selectedInteractable, surfaceIsGround);
            OnSetInteractorHandedness(interactor);
            interactor.allowSelect = false;
            OnArmLaunched();
        }
    }

    /// <summary>
    /// Recalls the arm to the launch point if it is currently attached to a surface.
    /// </summary>
    void RecallArm()
    {
        if (daomArm != null)
        {
            if (!daomArm.GetComponent<DAOMArm>().IsAttachedToSurface)
            {
                // Arm is still flying towards the target or is being recalled.
                return;
            }
            daomArm.GetComponent<DAOMArm>().RecallArm(launchPoint);
        }
    }

    /// <summary>
    /// Renders a line indicating the aiming direction when the player is aiming.
    /// </summary>
    void DrawLineRenderer()
    {
        if (lineRenderer)
        {
            if(aiming && daomArm == null)
            {
                lineRenderer.enabled = true;
                var startPos = aimOffset.transform.position;
                lineRenderer.SetPosition(0, startPos);
                if(ValidLayer())
                {
                    lineRenderer.SetPosition(1, hit.point);
                    return;
                }
                lineRenderer.SetPosition(1, startPos + aimOffset.transform.forward * rayLength);
                return;
            }
            lineRenderer.enabled = false;
        }
    }

    bool lasttValid = false;
    /// <summary>
    /// Sets the line renderer material based on whether the raycast is hitting a valid surface or not, to give the player feedback on whether they can launch the arm or not. lastValid is used to prevent unnecessary material changes for better performance.
    /// </summary>
    /// <param name="valid"></param>
    void SetLineMaterial(bool valid)
    {
        if(valid == lasttValid && lineRenderer) return;
        lasttValid = valid;
        lineRenderer.material.color = valid ? validColor : invalidColor;
        lineRenderer.material.SetColor("_EmissionColor", valid ? validColor : invalidColor);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if(ValidLayer())
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(aimOffset.transform.position, aimOffset.transform.forward * rayLength);
            return;
        }
        Gizmos.color = Color.red;
        Gizmos.DrawRay(aimOffset.transform.position, aimOffset.transform.forward * rayLength);
    }
#endif
}
