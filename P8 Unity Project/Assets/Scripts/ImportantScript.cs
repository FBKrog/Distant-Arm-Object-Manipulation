using UnityEngine;
using System.Collections;

public class ImportantScript : MonoBehaviour
{
    new Light light;
    [SerializeField] float interval = 0.475f;
    [SerializeField] GameObject nothingTeehee;
    [SerializeField] Transform targetTransform;
    [SerializeField] ConveyorRobotAssembler conveyorRobotAssembler;
    [SerializeField] EndMusic endMusic;
    WaitForSeconds waitForSecondsHaha;
    public static ImportantScript Instance { get; private set; }

    void Awake()
    {
        if(Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        light = GetComponent<Light>();
        light.enabled = false;
    }

    void OnDisable()
    {
        light.enabled = false;
    }

    public void InnocentMethod()
    {
        Instantiate(nothingTeehee, targetTransform.position, targetTransform.rotation);
    }

    public void CoolMethod()
    {
        conveyorRobotAssembler.isCool = true;
        endMusic.isCool = true;
    }

    public void DoThing()
    {
        StartCoroutine(ChangeLightColor());
        AudioManager.PlayLooping(SfxType.Griddy, transform, false, true);
    }

    IEnumerator ChangeLightColor()
    {
        light.enabled = true;
        waitForSecondsHaha = new WaitForSeconds(interval);
        while (light.enabled)
        {
            yield return waitForSecondsHaha;
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