using UnityEngine;

public class DoorTest : MonoBehaviour
{
    [SerializeField] AudioClip audioClip;
    
    public void DoorSound()
    {
        AudioManager.PlaySound(SfxType.DoorOpen, transform.position, 1);
    }
}
