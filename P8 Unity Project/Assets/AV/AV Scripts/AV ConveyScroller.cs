using UnityEngine;

public class AVConveyScroller : MonoBehaviour
{
    [SerializeField] private float conveyorBeltSpeed;
    [SerializeField] private Material beltMaterial;

    void Update()
    {
        beltMaterial.mainTextureOffset += new Vector2(-conveyorBeltSpeed * Time.deltaTime, 0);
    }
}
