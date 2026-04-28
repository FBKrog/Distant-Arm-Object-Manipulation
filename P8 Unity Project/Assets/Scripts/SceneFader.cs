using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class SceneFader : MonoBehaviour
{
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField][Tooltip("Color of the cutscene fade")] Color screenColor;
    [SerializeField][Tooltip("Used for the initial fade from the cutscene")] float initialFadeDuration = 1f;
    [SerializeField][Tooltip("Used for the final fade to black")] float finalFadeDuration = 15f;
    [SerializeField][Tooltip("Used for the delay before the final fade to black")] float finalFadeDelay = 10f;
    [SerializeField][Tooltip("Text for final fade")] string finalFadeText = "Game Over";

    Image fadeImage;

    public static event Action<float, float, string> StartFade;
    public static event Action FinalFade;
    public static void OnStartFade(float alpha, float duration, string text) => StartFade?.Invoke(alpha, duration, text);
    public static void OnFinalFade() => FinalFade?.Invoke();

    void Awake()
    {
        canvasGroup.alpha = 1f;
        fadeImage = canvasGroup.GetComponentInChildren<Image>();
        fadeImage.color = screenColor;
    }

    void Start()
    {
        OnStartFade(0f, initialFadeDuration, "");
        Invoke(nameof(SetColorBlack), initialFadeDuration);
    }

    void SetColorBlack()
    {
        fadeImage.color = Color.black;
    }

    public void CallFade()
    {
        OnFinalFade();
        Invoke(nameof(BeginFinalFade), finalFadeDelay);
    }

    void BeginFinalFade()
    {
        OnStartFade(1f, finalFadeDuration, finalFadeText);
        StartCoroutine(BeginEndGame());
    }

    IEnumerator BeginEndGame()
    {
        yield return new WaitForSeconds(finalFadeDuration);
        Invoke(nameof(EndGame), 6f);
    }

    void EndGame()
    {
        Application.Quit();
    }
}
