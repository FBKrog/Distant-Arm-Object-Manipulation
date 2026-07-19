using UnityEngine;
using UnityEngine.UI;

public class CamSnapping : MonoBehaviour
{
    [SerializeField] private Camera mainCam;
    private Camera myCam;

    private void Awake()
    {
        myCam = GetComponent<Camera>();
    }

    public void TakeSnap(RawImage whereToDisplay)
    {
        whereToDisplay.texture = TakeSnap();
    }

    public Texture2D TakeSnap()
    {
        if (myCam == null) myCam = GetComponent<Camera>();

        // Remember what is currently rendering
        RenderTexture currentActiveRT = RenderTexture.active;

        // Make the active rendertexture the one to display the snap on
        RenderTexture.active = myCam.targetTexture;

        myCam.CopyFrom(mainCam);
        myCam.Render();
        Debug.Log("Took picture");

        Texture2D image = new Texture2D(myCam.targetTexture.width, myCam.targetTexture.height);
        image.ReadPixels(new Rect(0, 0, myCam.targetTexture.width, myCam.targetTexture.height), 0, 0);
        image.Apply();

        // Return to what was currently rendering
        RenderTexture.active = currentActiveRT;

        return image;
    }
}
