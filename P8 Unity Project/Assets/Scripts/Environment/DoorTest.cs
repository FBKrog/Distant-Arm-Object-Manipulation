using UnityEngine;

public class DoorTest : MonoBehaviour
{
    [SerializeField] AudioClip audioClip;
    
    public void DoorSound()
    {
        AudioManager.Play(SfxType.DoorOpen, transform);
    }
}
