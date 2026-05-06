using UnityEngine;
using UnityEngine.VFX;
using PathCreation.Examples;

#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(VFXPathManager))]
public class VFXPathManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        VFXPathManager vFXPathManager = (VFXPathManager)target;
        if (GUILayout.Button("Play"))
        {
            vFXPathManager.StartVFX();
        }
        if (GUILayout.Button("Stop"))
        {
            vFXPathManager.StopVFX();
        }
    }
}
#endif

public class VFXPathManager : MonoBehaviour
{
    private VisualEffect effect;
    private PathFollower pathFollower;
    AudioSource audioSource;

    void Start()
    {
        effect = GetComponentInChildren<VisualEffect>();
        effect.Stop();
    }

    public void StartVFX()
    {
        if (effect != null)
        {
            effect.Play();
        }
        else
        {
            effect = GetComponentInChildren<VisualEffect>();
            effect.Play();
        }

        if (pathFollower != null)
        {
            pathFollower.isActive = true;
        }
        else
        {
            pathFollower = GetComponentInChildren<PathFollower>();
            pathFollower.isActive = true;
        }
        if(audioSource == null)
        {
            audioSource = AudioManager.PlayLooping(SfxType.Spark, effect.transform, true);
        }
    }

    public void StopVFX() 
    {
        if(effect != null)
        {
            effect.Stop();
        }
        else
        {
            effect = GetComponentInChildren<VisualEffect>();
            effect.Stop();
        }

        if (pathFollower != null)
        {
            pathFollower.isActive = false;
        }
        else
        {
            pathFollower = GetComponentInChildren<PathFollower>();
            pathFollower.isActive = false;
        }
        if(audioSource != null) 
        {
            AudioManager.StopLooping(audioSource);
        }
    }
}
