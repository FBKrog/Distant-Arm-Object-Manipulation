using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SFX : MonoBehaviour
{
    [SerializeField] SfxType sfx;
    [SerializeField] bool sfxOnImpact = false;

    /// <summary>
    /// To be called from an animation event or something if needed.
    /// </summary>
    public void PlaySound()
    {
        AudioManager.PlaySound(sfx, transform);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (sfxOnImpact)
        {
            AudioManager.PlaySound(sfx, transform);
        }
    }
}
