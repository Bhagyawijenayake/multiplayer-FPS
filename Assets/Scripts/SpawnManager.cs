using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager instance;
    private void Awake()
    {
        instance = this;
    }
    public Transform[] spawnPoints;
    // Start is called before the first frame update
    void Start()
    {
        foreach (Transform spawnPoint in spawnPoints)
        {
            spawnPoint.gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Transform getSpawnPoint()
    {
        
        int randomSpawnPoint = Random.Range(0, spawnPoints.Length);
        return spawnPoints[randomSpawnPoint];
    }
}
