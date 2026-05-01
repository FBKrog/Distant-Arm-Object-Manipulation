using UnityEngine;
using System.Collections;

public class ConveyorProduction : MonoBehaviour
{
    [SerializeField] GameObject spawnPoint;
    public int productionID = 0;
    [HideInInspector] public bool isActive = false;
    [SerializeField] float spawnInterval = 2f;
    [SerializeField] GameObject[] robotPartPrefabs;

    AudioSource productionAudioSource;

    void Awake()
    {
        isActive = false;
        if (spawnPoint == null)
            spawnPoint = gameObject;
    }

    void OnEnable()
    {
        ConveyorProductionManager.OnProductionStateChanged += HandleProductionStateChange;
        ConveyorProductionManager.OnAllProductionIntervalChanged += (newInterval) => spawnInterval += newInterval;
    }

    void OnDisable()
    {
        ConveyorProductionManager.OnProductionStateChanged -= HandleProductionStateChange;
        ConveyorProductionManager.OnAllProductionIntervalChanged -= (newInterval) => spawnInterval += newInterval;
    }

    void HandleProductionStateChange(int id, bool state)
    {
        if (id != productionID) return;
        isActive = state;
        if (isActive)
        {
            StartProduction();

            productionAudioSource = AudioManager.PlayLoopSound(SfxType.ProductionAmbience, transform);
        }
        else
        {
            StopAllCoroutines();

            AudioManager.StopLoopSound(productionAudioSource);
        }
    }

    public void StartProduction()
    {
        if (isActive)
        {
            StartCoroutine(SpawnPart());
        }
    }

    IEnumerator SpawnPart()
    {
        while(isActive)
        {
            foreach (var part in robotPartPrefabs)
            {
                var newPart = Instantiate(part, spawnPoint.transform.position, Quaternion.identity, transform);
                yield return new WaitForSeconds(spawnInterval);
            }
            yield return null;
        }
    }
}
