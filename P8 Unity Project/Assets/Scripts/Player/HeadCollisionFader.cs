using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class HeadCollisionFader : MonoBehaviour
{
    WaitForSeconds waitForSeconds = new(0.04f);
    
    [SerializeField][TextArea(2, 5)] string errorString;
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] float fadeDuration = 0.2f;

    Image fadeImage;
    TextMeshProUGUI tmp;
    Coroutine currentFadeCoroutine;

    HashSet<Collider> currentCollisions = new();
    int textIndex = 0;
    bool clearScreen = true;
    bool clearText;
    bool canCollide = true;

    void Awake()
    {
        if (canvasGroup == null)
        {
            Debug.LogError("Fade Canvas is not assigned in the Inspector.");
            enabled = false;
            return;
        }
        fadeImage = canvasGroup.GetComponentInChildren<Image>();
        tmp = canvasGroup.GetComponentInChildren<TextMeshProUGUI>();
        tmp.text = "";
    }

    void OnEnable()
    {
        SceneFader.StartFade += StartFade;
        SceneFader.FinalFade += FinalFade;
    }

    void OnDisable()
    {
        SceneFader.StartFade -= StartFade;
        SceneFader.FinalFade -= FinalFade;
    }

    void FinalFade()
    {
        clearScreen = false;
        clearText = true;
        canCollide = false;
        tmp.fontSize = 30;
        tmp.transform.position = new(-0.25f, tmp.transform.position.y, tmp.transform.position.z);
    }

    void Update()
    {
        if (currentCollisions.Count > 0 && canCollide)
            CleanupCollisions();
    }

    void CleanupCollisions()
    {
        currentCollisions.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
        if (currentCollisions.Count == 0 && !clearScreen)
            StartFade(0f, fadeDuration, errorString);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!canCollide) return;
        if (other.CompareTag("Environment") || other.CompareTag("Immovable"))
        {
            currentCollisions.Add(other);
            StartFade(1f, fadeDuration, errorString);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!canCollide) return;
        if (other.CompareTag("Environment") || other.CompareTag("Immovable"))
        {
            if(currentCollisions.Contains(other))
                currentCollisions.Remove(other);
            StartFade(0f, fadeDuration, errorString);
        }
    }

    void StartFade(float targetAlpha, float duration, string text)
    {
        if (currentFadeCoroutine != null)
            StopCoroutine(currentFadeCoroutine);
        currentFadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha, duration, text));
    }

    IEnumerator FadeRoutine(float target, float duration, string text)
    {
        if(tmp == null || canvasGroup == null)
        {
            Debug.LogError("Error Text UI or Fade Canvas is not assigned in the Inspector.");
            yield break;
        }
        
        float start = canvasGroup.alpha;
        float time = 0f;
        clearText = target == 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, time / duration);
            yield return null;
        }
        canvasGroup.alpha = target;

        if (target == 0f)
        {
            clearScreen = true;
            tmp.text = "";
            textIndex = 0;
        }
        else
        {
            clearScreen = false;
            StartCoroutine(ShowErrorText(text));
        }
    }

    IEnumerator ShowErrorText(string text)
    {
        for (int i = textIndex; i < text.Length; i++)
        {
            if(clearText)
                yield break;
            tmp.text = text.Substring(0, i + 1) + "<color=#FFFFFF>|</color>";
            textIndex = i + 1;
            yield return waitForSeconds;
        }
        tmp.text = text;
        textIndex = 0;
    }
}
