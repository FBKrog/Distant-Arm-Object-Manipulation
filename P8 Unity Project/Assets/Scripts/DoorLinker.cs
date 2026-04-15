using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Opens a door when every DoorTrigger in the Triggers list has been activated.
/// Fires the Animator "DoorOpen" trigger and plays the door sound via AudioManager.
///
/// Add a DoorTrigger component to each source object (Plug, Battery Socket, …)
/// and wire the source's UnityEvent to DoorTrigger.Activate() in the Inspector,
/// then add each DoorTrigger to this component's Triggers list.
/// </summary>
public class DoorLinker : MonoBehaviour
{
    [Header("Triggers — ALL must be activated to open the door")]
    [SerializeField] private List<DoorTrigger> triggers = new();

    [Header("Animation & Audio")]
    [Tooltip("Animator on the door. Will receive the 'DoorOpen' trigger when all conditions are met.")]
    [SerializeField] private Animator _animator;
    [Tooltip("Audio clip to play when the door opens.")]
    [SerializeField] private AudioClip _doorOpenClip;
    [SerializeField] private float _audioVolume = 1f;

    [Header("Events")]
    public UnityEvent OnDoorOpened;

    private bool _isOpen = false;

    // -------------------------------------------------------------------------

    private void Start()
    {
        foreach (var trigger in triggers)
        {
            if (trigger != null)
                trigger.Activated += CheckAndOpen;
        }
    }

    private void OnDestroy()
    {
        foreach (var trigger in triggers)
        {
            if (trigger != null)
                trigger.Activated -= CheckAndOpen;
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
        if (!_isOpen)
        {
            OpenDoor();
            OnDoorOpened.Invoke();
        }
    }

    private void CheckAndOpen()
    {
        if (_isOpen) return;

        foreach (var trigger in triggers)
        {
            // Any null entry or unactivated trigger blocks the door
            if (trigger == null || !trigger.IsActivated)
                return;
        }

        OpenDoor();
        OnDoorOpened.Invoke();
    }

    private void OpenDoor()
    {
        _isOpen = true;

        if (_animator != null)
            _animator.SetTrigger("DoorOpen");

        if (_doorOpenClip != null)
            AudioManager.PlaySound(_doorOpenClip, transform, _audioVolume);
    }
}
