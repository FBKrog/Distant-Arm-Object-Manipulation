using UnityEngine;

/// <summary>
/// General-purpose emission colour indicator. Wire Activate() / Deactivate() to any
/// UnityEvent in the Inspector. Colour switches instantly on the same call frame as
/// the event — no coroutines or Update loop, so nothing can interrupt the change.
/// </summary>
public class EmissionStateIndicator : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;

    [ColorUsage(true, true)]
    [SerializeField] private Color inactiveColor = Color.red * 2f;

    [ColorUsage(true, true)]
    [SerializeField] private Color activeColor = Color.green * 2f;

    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    private Material _material;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (targetRenderer == null)
        {
            Debug.LogError($"[EmissionStateIndicator] '{name}' — no Renderer found. Assign targetRenderer in the Inspector.", this);
            return;
        }

        _material = targetRenderer.material; // creates a per-instance clone
        _material.DisableKeyword("_USEEMISSION_OFF");
        _material.SetColor(EmissionColorID, inactiveColor);
    }

    private void OnDestroy()
    {
        if (_material != null)
            Destroy(_material);
    }

    public void Activate()
    {
        if (_material == null) return;
        _material.SetColor(EmissionColorID, activeColor);
    }

    public void Deactivate()
    {
        if (_material == null) return;
        _material.SetColor(EmissionColorID, inactiveColor);
    }
}
