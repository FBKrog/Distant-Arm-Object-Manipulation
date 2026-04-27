using System;
using System.Collections;
using UnityEngine;

public class ConveyorProductionManager : MonoBehaviour
{
    [Header("Conveyor Belt")]
    [SerializeField] Material beltMaterial;
    [SerializeField] [Tooltip("Speed of the conveyor belts (make sure it matches the speed assigned to the belts)")] float conveyorBeltSpeed = 1f;
    [SerializeField] [Tooltip("Multiplier for the change in belt material offset")] float speedMultiplier = 1f;
    [SerializeField] bool productionStartsActive = false;
    bool beltsActive = false;

    public static event Action<int, bool> OnProductionStateChanged;
    public static event Action<bool> OnAllConveyorBeltsStateChanged;
    public static event Action<float> OnAllProductionIntervalChanged;
    public static event Action<float> OnAllConveyorBeltsSpeedChanged;
    public static event Action OnBeginIncreaseProductionRate;

    public static void ProductionStateChange(int productionID, bool isActive) => OnProductionStateChanged?.Invoke(productionID, isActive);
    public static void AllConveyorBeltsStateChange(bool isActive) => OnAllConveyorBeltsStateChanged?.Invoke(isActive);
    public static void AllProductionIntervalChange(float newInterval) => OnAllProductionIntervalChanged?.Invoke(newInterval);
    public static void AllConveyorBeltsSpeedChange(float newSpeed) => OnAllConveyorBeltsSpeedChanged?.Invoke(newSpeed);
    public static void BeginIncreaseProductionRate() => OnBeginIncreaseProductionRate?.Invoke();

    void Start()
    {
        AllConveyorBeltsStateChange(productionStartsActive);
        ProductionStateChange(1, productionStartsActive);
        ProductionStateChange(2, productionStartsActive);
        ProductionStateChange(3, productionStartsActive);
    }

    void OnEnable()
    {
        OnBeginIncreaseProductionRate += HandleBeginIncreaseProductionRate;
        OnAllConveyorBeltsStateChanged += (state) => beltsActive = state;
    }

    void OnDisable()
    {
        OnBeginIncreaseProductionRate -= HandleBeginIncreaseProductionRate;
        OnAllConveyorBeltsStateChanged -= (state) => beltsActive = state;
    }

    void HandleBeginIncreaseProductionRate()
    {
        StartCoroutine(IncreaseProductionRate());
    }

    IEnumerator IncreaseProductionRate()
    {
        float newSpeed = 0.2f;
        float newProductionInterval = -0.1f;
        while (true)
        {
            AllConveyorBeltsSpeedChange(newSpeed);
            AllProductionIntervalChange(newProductionInterval);
            conveyorBeltSpeed += newSpeed;
            yield return new WaitForSeconds(1f);
        }
    }

    public void EnableEverything(float delay)
    {
        AllConveyorBeltsStateChange(true);
        ProductionStateChange(1, true);
        ProductionStateChange(2, true);
        ProductionStateChange(3, true);
        Invoke("HandleBeginIncreaseProductionRate", delay);
    }

    public void EnableProduction(int productionID)
    {
        ProductionStateChange(productionID, true);
    }

    public void DisableProduction(int productionID)
    {
        ProductionStateChange(productionID, false);
    }

    void Update()
    {
        if(beltsActive)
        {
            beltMaterial.mainTextureOffset += new Vector2(-conveyorBeltSpeed * speedMultiplier * Time.deltaTime, 0);
        }
    }
}
