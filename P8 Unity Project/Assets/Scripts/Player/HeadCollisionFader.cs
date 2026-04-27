using TMPro;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HeadCollisionFader : MonoBehaviour
{
    WaitForSeconds waitForSeconds = new(0.04f);
    
    [SerializeField][TextArea(2, 5)] string errorString;
    [SerializeField] TextMeshProUGUI errorTextUI;
    [SerializeField] CanvasGroup fadeCanvas;
    [SerializeField] float fadeDuration = 0.2f;

    Coroutine currentFadeCoroutine;
    Coroutine currentErrorTextCoroutine;
    HashSet<Collider> currentCollisions = new();
    int textIndex = 0;
    bool clearScreen;
    bool clearText;
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
        if (currentFadeCoroutine != null)
            StopCoroutine(currentFadeCoroutine);

        currentFadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    IEnumerator FadeRoutine(float target)
    {
        if(errorTextUI == null || fadeCanvas == null)
        {
            Debug.LogError("Error Text UI or Fade Canvas is not assigned in the Inspector.");
            yield break;
        }
        if (currentFadeCoroutine != null)
            StopCoroutine(currentFadeCoroutine);
        
        float start = fadeCanvas.alpha;
        float time = 0f;
        clearText = target == 0;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeCanvas.alpha = Mathf.Lerp(start, target, time / fadeDuration);
            yield return null;
        }
        fadeCanvas.alpha = target;

        if (target == 0f)
        {
            clearScreen = true;
            errorTextUI.text = "";
            textIndex = 0;
        }
        else
        {
            clearScreen = false;
            currentErrorTextCoroutine = StartCoroutine(ShowErrorText());
        }
    }

    IEnumerator ShowErrorText()
    {
        for (int i = textIndex; i < errorString.Length; i++)
        {
            if(clearText)
                yield break;
            errorTextUI.text = errorString.Substring(0, i + 1) + "<color=#FFFFFF>|</color>";
            textIndex = i + 1;
            yield return waitForSeconds;
        }
        errorTextUI.text = errorString;
        textIndex = 0;
    }
}
