using UnityEngine;
using System.Collections;

public class HeadCollisionFader : MonoBehaviour
{
    [SerializeField] CanvasGroup fadeCanvas;
    [SerializeField] float fadeDuration = 0.2f;

    Coroutine currentFade;

    void Awake()
    {
        if (fadeCanvas == null)
        {
            Debug.LogError("Fade Canvas is not assigned in the Inspector.");
            enabled = false;
            return;
        }
        fadeCanvas.alpha = 0f;
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Environment") || other.CompareTag("Immovable"))
            StartFade(1f);
    }

    void OnTriggerStay(Collider other)
    {
        if (fadeCanvas.alpha == 0f) return;
        if (other == null)
            fadeCanvas.alpha = 0f;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Environment") || other.CompareTag("Immovable"))
            StartFade(0f);
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

        fadeCanvas.alpha = target;
    }
}
