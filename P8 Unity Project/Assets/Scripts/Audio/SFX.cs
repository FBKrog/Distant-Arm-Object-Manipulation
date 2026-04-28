using UnityEngine;

public class SFX : MonoBehaviour
{
    [SerializeField] SfxType sfx;
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
        AudioManager.PlaySound(sfx, transform);
    }

    public void PlayFadeInSound(float fadeTime)
    {
        AudioManager.PlayFadeInSound(sfx, fadeTime, twoD);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (sfxOnImpact)
        {
            AudioManager.PlaySound(sfx, transform);
        }
    }
}
