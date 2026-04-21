/************************************************************************************
\file       DataCollector.cs
\brief      Abstract base class for all data collectors.
\author     #AUTHOR#
\date       #CREATIONDATE#
\copyright  (c) #YEAR#, #COMPANY# ApS. All rights reserved.
************************************************************************************/

using UnityEngine;
using System.Collections.Generic;

public abstract class DataCollector : MonoBehaviour
{
    //==============================================================================
    // Fields
    //==============================================================================

    [SerializeField] protected DataLoader assignedDataLoader;
    [SerializeField, Min(0f)] private float tickInterval = 0f;

    [SerializeField, Tooltip("Used for adding a delay to collection. If it is zero collection happends every FixedUpdate")]
    internal float nextTickAt = 0;

    //==============================================================================
    // MonoBehaviour
    //==============================================================================

    protected virtual void Awake()
    {
        DataPackerSetup();
    }

    protected virtual void OnEnable()
    {
        ScheduleNextTick();
    }

    protected virtual void OnDisable()
    {
        if (assignedDataLoader != null)
        {
            assignedDataLoader.UnregisterCollector(this);
        }
    }

    protected virtual void Update()
    {
        if (tickInterval <= 0f)
        {
            return;
        }

        if (Time.time >= nextTickAt)
        {
            collectDataTick();
            ScheduleNextTick();
        }
    }

    public void OnDestroy()
    {
        if (assignedDataLoader != null)
        {
            assignedDataLoader.UnregisterCollector(this);
        }
    }

    //==============================================================================
    // Public Methods
    //==============================================================================

    /// <summary>
    /// Called at a regular interval when tickInterval is greater than 0.
    /// </summary>
    public abstract void collectDataTick();

    /// <summary>
    /// Called when an external event should trigger a data sample.
    /// </summary>
    public abstract void collectDataOnEvent();

    public float TickInterval
    {
        get { return tickInterval; }
    }

    //==============================================================================
    // Protected Methods
    //==============================================================================

    protected void SendPackage(DataPackage package)
    {
        if (assignedDataLoader == null)
        {
            Debug.LogWarning("No DataPacker assigned for " + name + ".");
            return;
        }

        assignedDataLoader.ReceivePackage(package);
    }

    protected DataPackage CreatePackage(string dataType, string payloadJson)
    {
        return new DataPackage
        {
            CollectorId = name,
            DataType = dataType,
            PayloadJson = payloadJson
        };
    }

    protected DataPackage CreatePackage(
        string dataType,
        List<string> numericNames,
        List<float> numericValues,
        List<string> stringNames,
        List<string> stringValues,
        string payloadJson = null)
    {
        return new DataPackage
        {
            CollectorId = name,
            DataType = dataType,
            PayloadJson = payloadJson,
            NumericChannelNames = numericNames != null ? numericNames.ToArray() : null,
            NumericChannelValues = numericValues != null ? numericValues.ToArray() : null,
            StringChannelNames = stringNames != null ? stringNames.ToArray() : null,
            StringChannelValues = stringValues != null ? stringValues.ToArray() : null
        };
    }

    protected static void AddNumericChannel(List<string> names, List<float> values, string channelName, float value)
    {
        if (names == null || values == null || string.IsNullOrWhiteSpace(channelName))
        {
            return;
        }

        names.Add(channelName);
        values.Add(value);
    }

    protected static void AddNumericChannel(List<string> names, List<float> values, string channelName, int value)
    {
        AddNumericChannel(names, values, channelName, (float)value);
    }

    protected static void AddStringChannel(List<string> names, List<string> values, string channelName, string value)
    {
        if (names == null || values == null || string.IsNullOrWhiteSpace(channelName))
        {
            return;
        }

        names.Add(channelName);
        values.Add(value ?? string.Empty);
    }

    protected static void AddVector3Channels(List<string> names, List<float> values, string prefix, Vector3 vector)
    {
        AddNumericChannel(names, values, prefix + "_x", vector.x);
        AddNumericChannel(names, values, prefix + "_y", vector.y);
        AddNumericChannel(names, values, prefix + "_z", vector.z);
    }

    protected static void AddQuaternionChannels(List<string> names, List<float> values, string prefix, Quaternion quaternion)
    {
        AddNumericChannel(names, values, prefix + "_x", quaternion.x);
        AddNumericChannel(names, values, prefix + "_y", quaternion.y);
        AddNumericChannel(names, values, prefix + "_z", quaternion.z);
        AddNumericChannel(names, values, prefix + "_w", quaternion.w);
    }

    protected static void AddIntArrayChannels(List<string> names, List<float> values, string prefix, int[] array)
    {
        if (array == null)
        {
            return;
        }

        for (int i = 0; i < array.Length; i++)
        {
            AddNumericChannel(names, values, prefix + "_" + (i + 1), array[i]);
        }
    }

    //==============================================================================
    // Internal Methods
    //==============================================================================

    internal virtual void DataPackerSetup()
    {
        if (assignedDataLoader == null)
        {
            assignedDataLoader = GetComponentInParent<DataLoader>();
        }

        if (assignedDataLoader == null)
        {
            assignedDataLoader = FindFirstObjectByType<DataLoader>();
        }

        if (assignedDataLoader != null)
        {
            assignedDataLoader.RegisterCollector(this);
        }
        else
        {
            Debug.LogWarning("No DataPacker found for collector " + name + ".");
        }
    }

    //==============================================================================
    // Private Methods
    //==============================================================================

    private void ScheduleNextTick()
    {
        nextTickAt = Time.time + tickInterval;
    }
}