using UnityEngine;
using UnityEngine.VFX;
using UnityEditor;

[CustomEditor(typeof(VFXPathManager))]
public class VFXPathManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        VFXPathManager vFXPathManager = (VFXPathManager)target;
        if (GUILayout.Button("Play")){
            vFXPathManager.StartVFX();
        }
        if (GUILayout.Button("Stop"))
        {
            vFXPathManager.StopVFX();
        }
    }

}

public class VFXPathManager : MonoBehaviour
{
    private VisualEffect effect;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        effect = GetComponentInChildren<VisualEffect>();
        effect.Stop();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartVFX()
    {
        effect = GetComponentInChildren<VisualEffect>();
        effect.Play();
    }

    public void StopVFX() {
        effect = GetComponentInChildren<VisualEffect>();
        effect.Stop();
    }
}
