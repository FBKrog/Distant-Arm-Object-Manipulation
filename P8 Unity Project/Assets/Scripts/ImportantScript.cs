using UnityEngine;
using System.Collections;

public class ImportantScript : MonoBehaviour
{
    new Light light;
    [SerializeField] float interval = 0.475f;
    void Awake()
    {
        light = GetComponent<Light>();
        light.enabled = false;
    }

    void OnDisable()
    {
        light.enabled = false;
    }

    public void PlayEnding()
    {
        StartCoroutine(ChangeLightColor());
        AudioManager.PlayLoopSound(SfxType.Griddy, transform, false, true);
    }

    IEnumerator ChangeLightColor()
    {
        light.enabled = true;
        while(light.enabled)
        {
            yield return new WaitForSeconds(interval);
            var randomColorChannel = Random.Range(0, 3);

            int r = Random.Range(0, 256);
            int g = Random.Range(0, 256);
            int b = Random.Range(0, 256);


            if (randomColorChannel == 0)
                r = 255;
            if (randomColorChannel == 1)
                g = 255;
            if (randomColorChannel == 2)
                b = 255;
            
            light.color = new(r / 255f, g / 255f, b / 255f);
        }
    }
}
