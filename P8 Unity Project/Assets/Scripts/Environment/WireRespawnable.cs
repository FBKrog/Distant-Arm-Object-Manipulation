using UnityEngine;

public class WireRespawnable : MonoBehaviour
{
    //[SerializeField] GameObject wireParent;
    //[SerializeField] GameObject wirePrefab;
    //[SerializeField] Vector3 spawnPosition;
    //[SerializeField] Transform spawnParent;

    [SerializeField] Rigidbody[] rbs;
    Vector3[] rbInitialPositions;
    Quaternion[] rbInitialRotations;
    bool[] wasKinematic;
    public void Awake()
    {
        wasKinematic = new bool[rbs.Length];
        rbInitialPositions = new Vector3[rbs.Length];
        rbInitialRotations = new Quaternion[rbs.Length];

        for(int i = 0; i < rbs.Length; i++)
        {
            wasKinematic[i] = rbs[i].isKinematic ? true : false;
            rbInitialPositions[i] = rbs[i].transform.localPosition;
            rbInitialRotations[i] = rbs[i].transform.localRotation;
        }
    }

    public void Respawn()
    {
        for(int i = 0; i < rbs.Length; i++)
        {
            rbs[i].isKinematic = true;
            rbs[i].linearVelocity = Vector3.zero;
            rbs[i].angularVelocity = Vector3.zero;
            rbs[i].transform.localPosition = rbInitialPositions[i];
            rbs[i].transform.localRotation = rbInitialRotations[i];
            rbs[i].isKinematic = wasKinematic[i];
        }
        //var wire = Instantiate(wirePrefab, new(0,100,0), Quaternion.identity);
        //wire.transform.parent = spawnParent;
        //wire.transform.localPosition = spawnPosition;
        //wire.transform.localRotation = Quaternion.Euler(Vector3.zero);
        //Destroy(wireParent);
    }

    //IEnumerator BeginRepawn()
    //{
    //    var wire = Instantiate(wirePrefab);
    //    wire.transform.parent = spawnParent;
    //    wire.transform.localPosition = spawnPosition;
    //    wire.transform.localRotation = Quaternion.Euler(Vector3.zero);
    //    Destroy(wireParent, 0.01f);
    //}
}
