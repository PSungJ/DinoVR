using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemSpawnLimit
{
    public string key;    // PoolingManager 키
    public int maxCount;  // 최대 스폰 개수
}

public class LabItemSpawner : MonoBehaviour
{
    [Header("아이템 최대 스폰 제한 설정")]
    public List<ItemSpawnLimit> itemLimits = new List<ItemSpawnLimit>
    {
        new ItemSpawnLimit { key = "MedKit", maxCount = 3 },
        new ItemSpawnLimit { key = "Medicine", maxCount = 5 },
        new ItemSpawnLimit { key = "Lantern", maxCount = 2 },
    };

    [Header("스폰할 오브젝트 키 (PoolingManager 키)")]
    public List<string> itemKeys = new List<string> { "MedKit", "Lantern", "Medicine", "Ammo" };

    [Header("스폰 포인트")]
    public Transform[] spawnPoints;

    [Header("스폰 설정")]
    public int maxItemsPerBuilding = 2;      // 건물당 최대 스폰 개수
    [Range(0f, 1f)]
    public float spawnProbability = 0.7f;    // 각 SpawnPoint별 스폰 확률

    private void Start()
    {
        SpawnItems();
    }

    /// <summary>
    /// 지정된 SpawnPoint에서 아이템 랜덤 스폰
    /// </summary>
    public void SpawnItems()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("No spawn points assigned!");
            return;
        }

        int spawnedCount = 0;

        foreach (Transform sp in spawnPoints)
        {
            if (spawnedCount >= maxItemsPerBuilding)
                break;

            if (Random.value > spawnProbability)
                continue; // 확률 체크

            // 랜덤 아이템 선택
            string key = itemKeys[Random.Range(0, itemKeys.Count)];

            // 풀에서 스폰
            GameObject obj = PoolingManager.Instance.SpawnFromPool(
                key,
                sp.position,
                sp.rotation,
                sp // 부모를 SpawnPoint로 설정하면 정렬 편리
            );

            if (obj != null)
            {
                spawnedCount++;
            }
        }
    }
}
