using UnityEngine;
using LSL;

public class LSLMarkerSender : MonoBehaviour
{
    private StreamOutlet markerOutlet;

    [Header("BVP Settings")]
    [SerializeField] string streamName = "OpenSignals";
    [SerializeField] float samplingRate = 1000f; // Hz
    public static LSLMarkerSender Instance;
    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        var markerInfo = new StreamInfo(
            streamName,              // stream name
            "Markers",              // type
            1,                      // one channel
            0,                      // irregular sampling (events)
            channel_format_t.cf_string, // STRING markers
            "unity_marker_stream"
        );

        markerOutlet = new StreamOutlet(markerInfo);
        SendMarker("Session Started");
    }

    public void SendMarker(string label)
    {
        markerOutlet.push_sample(new string[] { label });
        Debug.Log("Marker sent: " + label);
    }

    void OnApplicationQuit()
    {
        SendMarker("Session Ended");
    }
}