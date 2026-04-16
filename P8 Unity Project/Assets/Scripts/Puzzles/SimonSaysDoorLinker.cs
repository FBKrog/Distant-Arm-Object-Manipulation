using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Slides a single panel (e.g. the Simon Says backboard) when every DoorTrigger
/// in the list has been activated. Wire SimonSaysPuzzle.OnPuzzleCompleted →
/// DoorTrigger.Activate() in the Inspector, then add that DoorTrigger here.
/// </summary>
public class SimonSaysDoorLinker : MonoBehaviour
{
    [Header("Triggers — ALL must be activated to slide the panel")]
    [SerializeField] private List<DoorTrigger> triggers = new();

    [Header("Panel")]
    [Tooltip("The backboard transform to slide (buttons parented to it will move with it).")]
    [SerializeField] private Transform panel;

    [Header("Slide Settings")]
    [Tooltip("Local-space axis along which the panel slides. Default is down.")]
    [SerializeField] private Vector3 slideAxis = Vector3.down;
    [Tooltip("How far the panel slides (local units).")]
    [SerializeField] private float slideDistance = 1.5f;
    [Tooltip("Duration of the slide animation in seconds.")]
    [SerializeField] private float slideDuration = 0.8f;

    [Header("Audio")]
    [SerializeField] private AudioClip _slideClip;
    [SerializeField] private float _audioVolume = 1f;

    [Header("Events")]
    public UnityEvent OnPanelSlid;

    private bool _hasSlid = false;

    // -------------------------------------------------------------------------

    private void Start()
    {
        if (panel == null)
            panel = transform;

        foreach (var trigger in triggers)
        {
            if (trigger != null)
                trigger.Activated += CheckAndSlide;
        }
    }

    private void OnDestroy()
    {
        foreach (var trigger in triggers)
        {
            if (trigger != null)
                trigger.Activated -= CheckAndSlide;
        }
    }

    // -------------------------------------------------------------------------

    [ContextMenu("Test Slide Panel")]
    private void TestSlidePanel()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[SimonSaysDoorLinker] Test Slide only works in Play mode.");
            return;
        }
        if (!_hasSlid)
        {
            SlidePanel();
            OnPanelSlid.Invoke();
        }
    }

    private void CheckAndSlide()
    {
        if (_hasSlid) return;

        foreach (var trigger in triggers)
        {
            if (trigger == null || !trigger.IsActivated)
                return;
        }

        SlidePanel();
        OnPanelSlid.Invoke();
    }

    private void SlidePanel()
    {
        _hasSlid = true;

        if (_slideClip != null)
            AudioManager.PlaySound(_slideClip, transform, _audioVolume);

        StartCoroutine(SlidePanelCoroutine());
    }

    private IEnumerator SlidePanelCoroutine()
    {
        Vector3 axis = slideAxis.normalized;
        Vector3 startPos = panel.localPosition;
        Vector3 endPos = startPos + axis * slideDistance;

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
            panel.localPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        panel.localPosition = endPos;
    }
}
