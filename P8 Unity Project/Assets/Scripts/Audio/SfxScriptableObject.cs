using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "_AudioData", menuName = "Audio Scriptable Object", order = 1)]
public class SfxScriptableObject : ScriptableObject
{
    public SfxType sfxType;
    public Sfx data;
}

[CustomEditor(typeof(SfxScriptableObject))]
public class ScriptableObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        SfxScriptableObject sfxScriptableObject = (SfxScriptableObject)target;
        if (GUILayout.Button("Rename"))
        {
            AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(sfxScriptableObject), sfxScriptableObject.sfxType.ToString());
        }
    }
}
