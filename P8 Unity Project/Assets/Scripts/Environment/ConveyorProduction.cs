using UnityEngine;
using System.Collections;

public class ConveyorProduction : MonoBehaviour
{
    [SerializeField] GameObject spawnPoint;
    public int productionID = 0;
    public bool isActive = false;
    [SerializeField] float spawnInterval = 2f;
    [SerializeField] GameObject[] robotPartPrefabs;
    
    [Header("Audio")]
    [SerializeField] AudioClip productionSound;
    AudioSource currentLoopSound;

    void Awake()
    {
        isActive = false;
    }

    void OnEnable()
    {
        ConveyorProductionManager.OnProductionStateChanged += HandleProductionStateChange;
        ConveyorProductionManager.OnAllProductionRateChanged += (newRate) => spawnInterval = newRate;
    }

    void OnDisable()
    {
        ConveyorProductionManager.OnProductionStateChanged -= HandleProductionStateChange;
        ConveyorProductionManager.OnAllProductionRateChanged -= (newRate) => spawnInterval = newRate;
    }

    void HandleProductionStateChange(int id, bool state)
    {
        if (id != productionID) return;
        isActive = state;
        if (isActive)
        {
            StartProduction();

            currentLoopSound = AudioManager.PlayLoopSound(productionSound, transform, 1);
            print($"Production #{productionID} enabled.");
        }
        else
        {
            StopAllCoroutines();

            AudioManager.StopSound(currentLoopSound);
            print($"Production #{productionID} disabled.");
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
