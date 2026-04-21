/************************************************************************************
\file       DataPackages.cs
\brief      Data transfer objects used by collectors and packer.
\author     #AUTHOR#
\date       #CREATIONDATE#
\copyright  (c) #YEAR#, #COMPANY# ApS. All rights reserved.
************************************************************************************/

using System;

[Serializable]
public class DataPackage
{
    // Name/id of the collector that produced this package.
    public string CollectorId;

    // Logical category of the payload, for example QuizAnswer or SessionEnd.
    public string DataType;

    // Serialized payload body (typically JSON).
    public string PayloadJson;

    // Optional named numeric channels for this package.
    public string[] NumericChannelNames;
    public float[] NumericChannelValues;

    // Optional named string channels for this package.
    public string[] StringChannelNames;
    public string[] StringChannelValues;

    // Scenario-local timestamp from DataPacker.GetTime() using format hh:mm:ss.ff.
    public string ScenarioTime;

    // Monotonic id assigned by DataPacker when the package is received.
    public int SequenceId;
}