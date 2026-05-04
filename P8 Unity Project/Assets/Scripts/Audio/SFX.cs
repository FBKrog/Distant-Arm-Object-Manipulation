using UnityEngine;

public class SFX : MonoBehaviour
{
    [SerializeField] SfxScriptableObject sfxSO;
    [SerializeField] bool sfxOnImpact = false;
    [SerializeField] bool twoD = false;
    [SerializeField] [Tooltip("Transform to play the sound at. If left empty, the sound will play at this GameObject's position.")] Transform targetTransform;

    void Awake()
    {
        if(targetTransform == null)
            targetTransform = transform;
    }

    /// <summary>
    /// To be called from an animation event or something if needed.
    /// </summary>
    public void PlaySound()
    {
        AudioManager.Play(sfxSO.sfxType, transform);
    }

    public void PlayFadeInSound(float fadeTime)
    {
        AudioManager.PlayFadeIn(sfxSO.sfxType, fadeTime, twoD);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (sfxOnImpact)
        {
            AudioManager.Play(sfxSO.sfxType, transform);
        }
    }
}
