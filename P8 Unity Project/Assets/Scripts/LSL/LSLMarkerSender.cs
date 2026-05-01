using UnityEngine;
using LSL;

public class LSLMarkerSender : MonoBehaviour
{
    private StreamOutlet bvpOutlet;
    private StreamOutlet markerOutlet;

    [Header("BVP Settings")]
    public float samplingRate = 100f; // Hz
    private float nextSampleTime = 0f;

    void Start()
    {
        var markerInfo = new StreamInfo(
            "Markers",              // stream name
            "Markers",              // type
            1,                      // one channel
            0,                      // irregular sampling (events)
            channel_format_t.cf_string, // STRING markers
            "unity_marker_stream"
        );

        markerOutlet = new StreamOutlet(markerInfo);
        Debug.Log("LSL Marker stream initialized.");
    }

    void Update()
    {
        // --- TEST INPUTS ---
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SendMarker("Task1_Start");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SendMarker("Task1_End");
        }
    }

    public void SendMarker(string label)
    {
        markerOutlet.push_sample(new string[] { label });
        Debug.Log("Marker sent: " + label);
    }
}