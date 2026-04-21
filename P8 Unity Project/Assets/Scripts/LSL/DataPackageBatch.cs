using System;
using System.Collections.Generic;

/// <summary>
/// Group of packages processed together (flush, send, or debug print).
/// </summary>
[Serializable]
public class DataPackageBatch
{
    public List<DataPackage> Packages = new List<DataPackage>();
}