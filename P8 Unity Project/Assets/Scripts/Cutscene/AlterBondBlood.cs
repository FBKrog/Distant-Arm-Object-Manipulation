using UnityEngine;

public class AlterBondBlood : MonoBehaviour
{
    [SerializeField] private Material theBondBlooder;
    public float bloodAmount = 0f;
    public bool updateBlood = false;
    [SerializeField] private string floatToChange = "_Current_Fill";

    public Color targetColor = Color.white;
    public bool updateColor = false;
    [SerializeField] private string colorToChange = "_ShadowColor";

    private void Update()
    {
        if (updateBlood)
            SetBondBlood();

        if (updateColor)
            SetMatColor();
    }


    public void SetBondBlood()
    {
        theBondBlooder.SetFloat(floatToChange, bloodAmount);
    }

    public void SetMatColor()
    {
        theBondBlooder.SetColor(colorToChange, targetColor);
    }
}
