using UnityEngine;
using LSL;

public class BPVTest : MonoBehaviour
{
    private StreamInlet inlet;
    private float[] sample = new float[1];

    void Start()
    {
        // Resolve BVP stream
        var results = LSL.LSL.resolve_stream("name", "OpenSignals", 1, 5.0);

        if (results.Length > 0)
        {
            inlet = new StreamInlet(results[0]);
            Debug.Log("BVP stream connected!");
        }
        else
        {
            Debug.LogError("No BVP stream found.");
        }
        int channels = inlet.info().channel_count();
        sample = new float[channels];
    }

    void Update()
    {
        if (inlet == null) return;

        double timestamp = inlet.pull_sample(sample, 0.0);

        if (timestamp != 0.0)
        {
            int[] bvpValue;
            bvpValue = new int[inlet.info().channel_count()];
            timestamp = inlet.pull_sample(bvpValue, 0.0);
            Debug.Log("Time: " + timestamp + " | BVP: " + bvpValue[0]);

            Debug.Log("Time: " + timestamp + " | BVP: " + bvpValue);
        }
    }
}