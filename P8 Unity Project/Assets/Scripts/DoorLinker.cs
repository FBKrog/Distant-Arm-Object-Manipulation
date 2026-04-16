using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Unlocks a door when every DoorTrigger in the Triggers list has been activated.
/// The door only opens when the player enters the linked CameraPresencePlate zone
/// (wire CameraPresencePlate.OnPlateActivated → TryOpen and OnPlateDeactivated → Close
/// in the Inspector). The door closes and fires OnDoorClosed when the player exits.
///
/// Add a DoorTrigger component to each source object (Plug, Battery Socket, …)
/// and wire the source's UnityEvent to DoorTrigger.Activate() in the Inspector,
/// then add each DoorTrigger to this component's Triggers list.
/// </summary>
public class DoorLinker : MonoBehaviour
{
    [Header("Triggers — ALL must be activated to unlock the door")]
    [SerializeField] private List<DoorTrigger> triggers = new();

    [Header("Animation & Audio")]
    [Tooltip("Animator on the door. Will receive the 'DoorOpen' / 'DoorClose' triggers.")]
    [SerializeField] private Animator _animator;
    [Tooltip("Audio clip to play when the door opens.")]
    [SerializeField] private AudioClip _doorOpenClip;
    [Tooltip("Audio clip to play when the door closes.")]
    [SerializeField] private AudioClip _doorCloseClip;
    [SerializeField] private float _audioVolume = 1f;

    [Header("Events")]
    public UnityEvent OnDoorOpened;
    public UnityEvent OnDoorClosed;

    private bool _isUnlocked = false;
    private bool _isOpen = false;

    // -------------------------------------------------------------------------

    private void Start()
    {
        foreach (var trigger in triggers)
        {
            if (trigger != null)
                trigger.Activated += CheckAndUnlock;
        }
    }

    private void OnDestroy()
    {
        foreach (var trigger in triggers)
        {
            if (trigger != null)
                trigger.Activated -= CheckAndUnlock;
        }
    }

    // -------------------------------------------------------------------------

    [ContextMenu("Fire Door Trigger")]
    private void TestFireDoor()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[DoorLinker] Fire Door Trigger only works in Play mode.");
            return;
        }
        CheckAndUnlock();
        TryOpen();
    }

    private void CheckAndUnlock()
    {
        foreach (var trigger in triggers)
        {
            // Any null entry or unactivated trigger blocks the unlock
            if (trigger == null || !trigger.IsActivated)
                return;
        }

        _isUnlocked = true;
    }

    /// <summary>
    /// Call this when the player enters the door sensor area (wire to
    /// CameraPresencePlate.OnPlateActivated). No-op if triggers are not yet satisfied
    /// or the door is already open.
    /// </summary>
    public void TryOpen()
    {
        if (!_isUnlocked || _isOpen) return;

        _isOpen = true;

        if (_animator != null)
            _animator.SetTrigger("DoorOpen");

        if (_doorOpenClip != null)
            AudioManager.PlaySound(_doorOpenClip, transform, _audioVolume);

        OnDoorOpened.Invoke();
    }

    /// <summary>
    /// Call this when the player exits the door sensor area (wire to
    /// CameraPresencePlate.OnPlateDeactivated). No-op if the door is already closed.
    /// </summary>
    public void Close()
    {
        if (!_isOpen) return;

        _isOpen = false;

        if (_animator != null)
            _animator.SetTrigger("DoorClose");

        if (_doorCloseClip != null)
            AudioManager.PlaySound(_doorCloseClip, transform, _audioVolume);

        OnDoorClosed.Invoke();
    }
}
