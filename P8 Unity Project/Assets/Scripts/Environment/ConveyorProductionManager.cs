using System;
using UnityEngine;

public class ConveyorProductionManager : MonoBehaviour
{
    public static event Action<int, bool> OnProductionStateChanged;
    public static event Action<bool> OnAllConveyorBeltsStateChanged;

    public static void ProductionStateChange(int productionID, bool isActive) => OnProductionStateChanged?.Invoke(productionID, isActive);
    public static void AllConveyorBeltsStateChange(bool isActive) => OnAllConveyorBeltsStateChanged?.Invoke(isActive);

    public void EnableProduction(int productionID)
    {
        ProductionStateChange(productionID, true);
    }

    public void DisableProduction(int productionID)
    {
        ProductionStateChange(productionID, false);
    }
}
