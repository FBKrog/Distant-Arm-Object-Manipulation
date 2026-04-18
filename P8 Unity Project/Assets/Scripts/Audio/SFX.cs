using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SFX : MonoBehaviour
{
    [SerializeField] SfxType sfxType;
    [SerializeField] bool sfxOnImpact = false;

    /// <summary>
    /// To be called from an animation event or something if needed.
    /// </summary>
    public void PlaySound()
    {
        AudioManager.PlaySound(sfxType, transform);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (sfxOnImpact)
        {
            AudioManager.PlaySound(sfxType, transform);
        }
    }
}
