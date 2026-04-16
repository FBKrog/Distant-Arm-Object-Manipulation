using System;
using UnityEngine;
using UnityEngine.UI;

public class ConveyorProductionManager : MonoBehaviour
{
    [Header("Production Settings")]
    [SerializeField] Material beltMaterial;
    [SerializeField] Slider productionRateSlider;
    [SerializeField] Slider conveyorBeltsSpeedSlider;
    [SerializeField] float speedDivider = 1f;
    bool beltsAreActive = true;

    public static event Action<int, bool> OnProductionStateChanged;
    public static event Action<bool> OnAllConveyorBeltsStateChanged;
    public static event Action<float> OnAllProductionRateChanged;
    public static event Action<float> OnAllConveyorBeltsSpeedChanged;

    public static void ProductionStateChange(int productionID, bool isActive) => OnProductionStateChanged?.Invoke(productionID, isActive);
    public static void AllConveyorBeltsStateChange(bool isActive) => OnAllConveyorBeltsStateChanged?.Invoke(isActive);
    public static void AllProductionRateChange(float newSpeed) => OnAllProductionRateChanged?.Invoke(newSpeed);
    public static void AllConveyorBeltsSpeedChange(float newSpeed) => OnAllConveyorBeltsSpeedChanged?.Invoke(newSpeed);

    void Awake()
    {
        beltsAreActive = true;
        AllConveyorBeltsStateChange(beltsAreActive);
        print($"Conveyor belts running:{beltsAreActive}");
    }

    public void EnableProduction(int productionID)
    {
        ProductionStateChange(productionID, true);
    }

    public void DisableProduction(int productionID)
    {
        ProductionStateChange(productionID, false);
    }

    public void ChangeAllProductionRate()
    {
        AllProductionRateChange(productionRateSlider.value);
    }

    public void ChangeAllConveyorBeltsSpeed()
    {
        AllConveyorBeltsSpeedChange(conveyorBeltsSpeedSlider.value);
    }

    void Update()
    {
        if(beltsAreActive)
            beltMaterial.mainTextureOffset += new Vector2(-conveyorBeltsSpeedSlider.value / speedDivider * Time.deltaTime, 0);
    }
}
