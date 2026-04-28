using UnityEngine;
using UnityEngine.VFX;

namespace Cutscene
{
    public class VFXPropertyAlterer : MonoBehaviour
    {
        [SerializeField, ColorUsage(true, true)] private Color firstColor;
        [SerializeField, ColorUsage(true, true)] private Color secondColor;
        [SerializeField] private VisualEffect theVisualEffect;

        public bool updateValues = false;

        private void Update()
        {
            if (!updateValues)
                return;

            Gradient targetGradient = new Gradient();

            GradientColorKey[] colors = new GradientColorKey[2];
            colors[0] = new GradientColorKey(firstColor, 0.1f);
            colors[1] = new GradientColorKey(secondColor, 0.9f);

            GradientAlphaKey[] alphas = new GradientAlphaKey[2];
            alphas[0] = new GradientAlphaKey(1f, 0f);
            alphas[1] = new GradientAlphaKey(1f, 1f);

            targetGradient.SetKeys(colors, alphas);

            theVisualEffect.SetGradient("ColorOverLife", targetGradient);
        }
    }
}
