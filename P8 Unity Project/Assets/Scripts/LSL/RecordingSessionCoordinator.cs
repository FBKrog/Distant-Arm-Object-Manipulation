using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Orchestrates the full recording workflow by coordinating DataLoader,
/// LSL publishing, and LabRecorder RCS control.
/// This class should contain policy and sequencing, not low-level transport details.
/// </summary>
public class RecordingSessionCoordinator : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private DataLoader dataLoader;

    [Header("Session Settings")]
    [SerializeField] private string participantId;
    [SerializeField] private string sessionId;
    [SerializeField] private string taskName;

    [Header("LSL Settings")]
    [SerializeField] private string lslDataStreamPrefix = "NeonatalData";
    [SerializeField] private bool includeCollectorNameInStreamName = true;
    [SerializeField] private bool includeScenarioTimeAsChannel = false;
    [SerializeField] private bool includeSequenceIdAsChannel = false;

    [Header("LabRecorder Settings")]
    [SerializeField] private bool autoStartSessionOnPlay = true;
    [SerializeField] private bool useLabRecorderControl = true;
    [SerializeField] private bool allowLslWithoutLabRecorder = true;
    [SerializeField] private string host = "localhost";
    [SerializeField] private int port = 22345;
    [SerializeField] private string studyRoot;
    [SerializeField] private string filenameTemplate = "sub-%p\\ses-%s\\sub-%p_ses-%s_task-%b_run-01_beh.xdf";

    private LabRecordingSessionClient sessionClient;
    private LslDataBridge lslBridge;
    private bool sessionActive;

    /// <summary>
    /// Resolves dependencies and initializes helper classes.
    /// </summary>
    private void Awake()
    {
        if (dataLoader == null)
        {
            dataLoader = FindFirstObjectByType<DataLoader>();
        }

        sessionClient = new LabRecordingSessionClient();
        lslBridge = new LslDataBridge();
    }

    /// <summary>
    /// Registers DataLoader callbacks for package forwarding.
    /// </summary>
    private void OnEnable()
    {
        if (dataLoader == null)
        {
            return;
        }

        dataLoader.PackageReceived += OnPackageReceived;
        dataLoader.PackagesFlushed += OnPackagesFlushed;

        if (autoStartSessionOnPlay)
        {
            _ = BeginSessionAsync();
        }
    }

    /// <summary>
    /// Unregisters callbacks and performs best-effort teardown.
    /// </summary>
    private void OnDisable()
    {
        if (dataLoader != null)
        {
            dataLoader.PackageReceived -= OnPackageReceived;
            dataLoader.PackagesFlushed -= OnPackagesFlushed;
        }

        if (sessionActive)
        {
            _ = EndSessionAsync();
        }
        else
        {
            lslBridge?.Dispose();
            sessionClient?.Disconnect();
        }
    }

    /// <summary>
    /// Starts a coordinated recording session:
    /// initialize LSL, connect/configure recorder, emit start marker, then start recording.
    /// </summary>
    /// <returns>True when session start sequence succeeds.</returns>
    public Task<bool> BeginSessionAsync()
    {
        return BeginSessionInternalAsync();
    }

    /// <summary>
    /// Ends a coordinated recording session:
    /// emit stop marker, stop recorder, flush bridge, and disconnect.
    /// </summary>
    /// <returns>True when session stop sequence succeeds.</returns>
    public Task<bool> EndSessionAsync()
    {
        return EndSessionInternalAsync();
    }

    /// <summary>
    /// Handles each package produced by DataLoader and forwards it to the LSL bridge.
    /// </summary>
    /// <param name="package">Package to publish.</param>
    private void OnPackageReceived(DataPackage package)
    {
        if (!sessionActive)
        {
            return;
        }

        lslBridge?.PublishPackage(package);
    }

    /// <summary>
    /// Handles flush boundaries from DataLoader.
    /// Useful for optional batching markers or diagnostics.
    /// </summary>
    /// <param name="batch">Packages included in this flush.</param>
    private void OnPackagesFlushed(DataPackageBatch batch)
    {
        if (!sessionActive)
        {
            return;
        }

        lslBridge?.Flush();
    }

    private async Task<bool> BeginSessionInternalAsync()
    {
        if (sessionActive)
        {
            return true;
        }

        if (sessionClient == null)
        {
            sessionClient = new LabRecordingSessionClient();
        }

        if (lslBridge == null)
        {
            lslBridge = new LslDataBridge();
        }

        lslBridge.Configure(
            lslDataStreamPrefix,
            includeCollectorNameInStreamName,
            includeScenarioTimeAsChannel,
            includeSequenceIdAsChannel);

        lslBridge.InitializeStreams(sessionId, participantId, taskName);
        sessionActive = true;
        lslBridge.PublishMarker("SessionStart", sessionId);

        if (!useLabRecorderControl)
        {
            Debug.Log("RecordingSessionCoordinator: LSL started without LabRecorder control.");
            return true;
        }

        bool connected = await sessionClient.ConnectAsync(host, port);
        if (!connected)
        {
            if (allowLslWithoutLabRecorder)
            {
                Debug.LogWarning("RecordingSessionCoordinator: LabRecorder connect failed, continuing with LSL-only mode.");
                return true;
            }

            sessionActive = false;
            lslBridge.Dispose();
            return false;
        }

        bool configured = await sessionClient.ConfigureRecordingAsync(
            studyRoot,
            filenameTemplate,
            participantId,
            sessionId,
            taskName);
        if (!configured)
        {
            if (allowLslWithoutLabRecorder)
            {
                Debug.LogWarning("RecordingSessionCoordinator: LabRecorder configure failed, continuing with LSL-only mode.");
                return true;
            }

            sessionActive = false;
            lslBridge.Dispose();
            sessionClient.Disconnect();
            return false;
        }

        bool started = await sessionClient.StartRecordingAsync();
        if (!started)
        {
            if (allowLslWithoutLabRecorder)
            {
                Debug.LogWarning("RecordingSessionCoordinator: LabRecorder start failed, continuing with LSL-only mode.");
                return true;
            }

            sessionActive = false;
            lslBridge.Dispose();
            sessionClient.Disconnect();
            return false;
        }

        return true;
    }

    private async Task<bool> EndSessionInternalAsync()
    {
        if (!sessionActive)
        {
            lslBridge?.Dispose();
            sessionClient?.Disconnect();
            return true;
        }

        lslBridge?.PublishMarker("SessionStop", sessionId);

        bool stopped = await sessionClient.StopRecordingAsync();
        lslBridge?.Flush();
        lslBridge?.Dispose();
        sessionClient?.Disconnect();
        sessionActive = false;
        return stopped;
    }
}