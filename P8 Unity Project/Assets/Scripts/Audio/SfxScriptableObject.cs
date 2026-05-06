using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "_AudioData", menuName = "Audio Scriptable Object", order = 1)]
public class SfxScriptableObject : ScriptableObject
{
    public SfxType sfxType;
    public Sfx data;
}

#if UNITY_EDITOR
[CustomEditor(typeof(SfxScriptableObject))]
public class ScriptableObjectEditor : Editor
{
    AudioSource audioSource;
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        SfxScriptableObject sfxScriptableObject = (SfxScriptableObject)target;
        if (GUILayout.Button("Rename"))
        {
            AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(sfxScriptableObject), sfxScriptableObject.sfxType.ToString());
        }
        if (GUILayout.Button("Play"))
        {
            AudioManager.EditorTestPlay(sfxScriptableObject.sfxType);
        }
        if (GUILayout.Button("Play Looping"))
        {
            if(audioSource == null)
                audioSource = AudioManager.EditorTestPlayLooping(sfxScriptableObject.sfxType);
        }
        if (GUILayout.Button("Stop Looping"))
        {
            if (audioSource != null)
            {
                AudioManager.EditorTestStopLooping(audioSource);
                audioSource = null;
            }
        }
    }
}
#endif