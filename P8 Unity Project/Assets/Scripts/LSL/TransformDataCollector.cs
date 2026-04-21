/************************************************************************************
\file       TransformDataCollector.cs
\brief      This script ...
\author     #AUTHOR#
\date       #CREATIONDATE#
\copyright  © #YEAR#, #COMPANY# ApS. All rights reserved.
************************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformDataCollector : DataCollector
{
    //==============================================================================
    // Fields
    //==============================================================================
    public bool collectWhenMoved = true;
    public bool beenMoved = false;
    public bool sendDataOnStart = true;

    private Vector3 lastPosition;

    //==============================================================================
    // MonoBehaviour
    //==============================================================================
    void Start()
    {
        if (sendDataOnStart)
        {
            collectDataTick();
        }
    }

    //==============================================================================
    // Public Methods
    //==============================================================================

    public override void collectDataOnEvent()
    {

    }

    public override void collectDataTick()
    {
        if (collectWhenMoved)
        {
            if (!beenMoved)
            {
                lastPosition = transform.position;
                beenMoved = true;
            }
            else
            {
                if (transform.position != lastPosition)
                {
                    lastPosition = transform.position;
                    DataPackage dp = collectAndPackageData();
                    sendData(dp);
                }
            }
        }
        else
        {
            DataPackage dp = collectAndPackageData();
            sendData(dp);
        }
    }

    //==============================================================================
    // Private Methods
    //==============================================================================
    private DataPackage collectAndPackageData()
    {
        List<string> numericNames = new List<string>(7);
        List<float> numericValues = new List<float>(7);
        List<string> stringNames = new List<string>();
        List<string> stringValues = new List<string>();

        AddVector3Channels(numericNames, numericValues, "position", transform.position);
        AddQuaternionChannels(numericNames, numericValues, "rotation", transform.rotation);

        DataPackage data = CreatePackage("TransformData", numericNames, numericValues, stringNames, stringValues);
        return data;
    }

    private void sendData(DataPackage data)
    {
        SendPackage(data);
    }
}