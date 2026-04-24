using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HeadCollisionFader : MonoBehaviour
{
    [SerializeField] CanvasGroup fadeCanvas;
    [SerializeField] float fadeDuration = 0.2f;

    Coroutine currentFade;
    HashSet<Collider> currentCollisions = new();
    bool clearScreen;

    void Awake()
    {
        if (fadeCanvas == null)
        {
            Debug.LogError("Fade Canvas is not assigned in the Inspector.");
            enabled = false;
            return;
        }
        fadeCanvas.alpha = 0f;
        clearScreen = true;
    }

    void Update()
    {
        if (currentCollisions.Count > 0)
            CleanupCollisions();
    }

    void CleanupCollisions()
    {
        currentCollisions.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
        if (currentCollisions.Count == 0 && !clearScreen)
            StartFade(0f);
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Environment") || other.CompareTag("Immovable"))
        {
            currentCollisions.Add(other);
            StartFade(1f);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Environment") || other.CompareTag("Immovable"))
        {
            if(currentCollisions.Contains(other))
                currentCollisions.Remove(other);
            StartFade(0f);
        }
    }

    void StartFade(float targetAlpha)
    {
        if (currentFade != null)
            StopCoroutine(currentFade);

        currentFade = StartCoroutine(FadeRoutine(targetAlpha));
    }

    IEnumerator FadeRoutine(float target)
    {
        float start = fadeCanvas.alpha;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeCanvas.alpha = Mathf.Lerp(start, target, time / fadeDuration);
            yield return null;
        }
        if(target == 0f)
            clearScreen = true;
        else
            clearScreen = false;
        fadeCanvas.alpha = target;
    }
}
