using UnityEngine;

public class EndMusic : MonoBehaviour
{
    [SerializeField] SfxScriptableObject endMusic;
    public bool isCool;
    [SerializeField] SfxScriptableObject coolEndMusic;
    public void Play()
    {
        if(isCool)
        {
            if(coolEndMusic != null)
                AudioManager.PlayLooping(coolEndMusic.sfxType, transform, false, true);
        }
        else
        {
            if(endMusic != null)
                AudioManager.PlayFadeIn(endMusic.sfxType, 40, true);
        }
    }
}
