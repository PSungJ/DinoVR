using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private List<TerrainSpawnData> terrainDataList; // 테레인별 데이터 모음
    private TerrainSpawnData currentData;
    private Transform currentTerrainParent;
    private Transform dinosParent;
    private Transform propsParent;

    [Header("건물 아이템 스폰 제한")]
    public List<ItemSpawnLimit> itemLimits = new List<ItemSpawnLimit>
    {
        new ItemSpawnLimit { key = "MedKit", maxCount = 3 },
        new ItemSpawnLimit { key = "Medicine", maxCount = 5 },
        new ItemSpawnLimit { key = "Lantern", maxCount = 2 },
        new ItemSpawnLimit { key = "Ammo", maxCount = 10 }
    };

    [Header("건물 SpawnPoint")]
    public Transform[] buildingSpawnPoints;

    [Range(0f, 1f)]
    public float itemSpawnProbability = 1f; // 각 SpawnPoint별 스폰 확률

    // 현재 활성화된 아이템 수
    private Dictionary<string, int> spawnedCounts = new Dictionary<string, int>();

    private void Awake()
    {
        // 카운트 초기화
        foreach (var limit in itemLimits)
        {
            spawnedCounts[limit.key] = 0;
        }

        InitializeBuildingSpawnPoints();
    }

    /// <summary>
    /// "Laboratory ~ Laboratory (11)" 오브젝트 아래 SpawnPoint 초기화
    /// </summary>
    public void InitializeBuildingSpawnPoints()
    {
        List<Transform> points = new List<Transform>();

        for (int i = 0; i <= 11; i++)
        {
            string labName = i == 0 ? "Laboratory" : $"Laboratory ({i})";
            GameObject labObj = GameObject.Find(labName);
            if (labObj == null)
            {
                Debug.LogWarning($"'{labName}' 오브젝트를 찾을 수 없습니다.");
                continue;
            }

            // 하위에 있는 모든 SpawnPoint 가져오기
            foreach (Transform child in labObj.transform)
            {
                if (child.name.StartsWith("SpawnPoint"))
                {
                    points.Add(child);
                }
            }
        }

        buildingSpawnPoints = points.ToArray();
        Debug.Log($"총 {buildingSpawnPoints.Length}개의 SpawnPoint가 초기화되었습니다.");
    }

    /// <summary>
    /// 외부에서 현재 테레인을 지정할 때 호출
    /// 예: SetTerrain("Jungle");
    /// </summary>
    public void SetTerrain(string terrainName)
    {
        currentData = terrainDataList.Find(t => t.terrainName == terrainName);
        if (currentData == null)
        {
            Debug.Log($"TerrainSpawnData for '{terrainName}' not found.");
            return;
        }

        // Terrain GameObject 찾기
        GameObject terrainObj = GameObject.Find(terrainName);
        if (terrainObj != null)
        {
            // 공룡 폴더
            dinosParent = EnsureFolder(terrainObj.transform, "Dinos");
            // 환경 오브젝트 폴더
            propsParent = EnsureFolder(terrainObj.transform, "Props");

            currentTerrainParent = terrainObj.transform;
        }
        else
        {
            Debug.LogWarning($"Terrain GameObject '{terrainName}' not found in scene.");
            dinosParent = null;
            propsParent = null;
            currentTerrainParent = null;
        }

        SpawnDinos();
        SpawnProps(); // 환경 오브젝트 스폰 추가
        SpawnBuildingItems(); // 건물 내부 아이템
    }

    /// <summary>
    /// 하위 폴더가 없으면 생성 후 반환
    /// </summary>
    private Transform EnsureFolder(Transform parent, string name)
    {
        Transform folder = parent.Find(name);
        if (folder == null)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.localPosition = Vector3.zero;
            folder = obj.transform;
        }
        return folder;
    }

    /// <summary>
    /// 공룡 스폰 (NavMesh 위에만)
    /// </summary>
    private void SpawnDinos()
    {
        if (currentData == null || dinosParent == null) return;

        foreach (var info in currentData.dinoList)
        {
            for (int i = 0; i < info.count; i++)
            {
                if (Random.value <= info.probability)
                {
                    Vector3 pos;
                    if (TryGetNavMeshPosition(out pos))
                    {
                        string key = info.dinoData.dino.ToString();
                        GameObject dino = PoolingManager.Instance.SpawnFromPool(
                            key, pos, Quaternion.identity, dinosParent
                        );

                        if (dino == null)
                        {
                            Debug.LogWarning($"Failed to spawn dino with key {key}");
                            continue;
                        }

                        // NavMeshAgent가 있는 경우 Warp로 위치 보정
                        NavMeshAgent agent = dino.GetComponent<NavMeshAgent>();
                        if (agent != null)
                        {
                            if (!agent.isOnNavMesh)
                            {
                                if (!NavMesh.SamplePosition(pos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                                {
                                    Debug.LogWarning($"Dino '{dino.name}' spawn failed: no NavMesh near {pos}");
                                    PoolingManager.Instance.ReturnToPool(key, dino);
                                    continue;
                                }
                                agent.Warp(hit.position);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning("No valid NavMesh position found near terrain area.");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Terrain 위의 랜덤 위치 중 NavMesh 위의 유효 위치 찾기
    /// </summary>
    private bool TryGetNavMeshPosition(out Vector3 result)
    {
        Vector3 randomPos = GetRandomPositionOnTerrain();

        // NavMesh.SamplePosition(시작점, out hit정보, 탐색반경, 영역마스크)
        if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }

        result = Vector3.zero;
        return false;
    }

    /// <summary>
    /// 현재 테레인에 맞는 환경 오브젝트(나뭇가지, 돌 등)를 스폰
    /// </summary>
    private void SpawnProps()
    {
        if (currentData == null || propsParent == null) return;

        foreach (var info in currentData.propList)
        {
            for (int i = 0; i < info.count; i++)
            {
                if (Random.value <= info.probability)
                {
                    Vector3 pos = GetRandomPositionOnTerrain();

                    // 풀에서 스폰 시도
                    GameObject prop = PoolingManager.Instance.SpawnFromPool(
                        info.key, pos, Quaternion.identity, propsParent
                    );

                    // 풀에 없을 경우 직접 프리팹 Instantiate
                    if (prop == null && info.prefab != null)
                    {
                        prop = Instantiate(info.prefab, pos, Quaternion.identity, propsParent);
                        Debug.LogWarning($"[SpawnManager] '{info.key}' 풀 없음 → 직접 생성됨");
                    }

                    if (prop == null)
                        Debug.LogWarning($"Failed to spawn prop with key {info.key}");
                }
            }
        }
    }

    /// <summary>
    /// Terrain 위의 랜덤 좌표 반환
    /// </summary>
    private Vector3 GetRandomPositionOnTerrain()
    {
        Terrain t = Terrain.activeTerrain;
        if (t == null)
        {
            Debug.LogWarning("No active Terrain found. Returning Vector3.zero.");
            return Vector3.zero;
        }

        Vector3 terrainPos = t.transform.position;
        float terrainWidth = t.terrainData.size.x;
        float terrainLength = t.terrainData.size.z;

        float x = terrainPos.x + Random.Range(0f, terrainWidth);
        float z = terrainPos.z + Random.Range(0f, terrainLength);
        float y = t.SampleHeight(new Vector3(x, 0f, z)) + terrainPos.y + 0.1f;

        return new Vector3(x, y, z);
    }

    // ==================================
    // 건물 내부 아이템 스폰 (최대 제한 적용)
    // ==================================
    public void SpawnBuildingItems()
    {
        if (buildingSpawnPoints == null || buildingSpawnPoints.Length == 0) return;

        foreach (Transform sp in buildingSpawnPoints)
        {
            if (Random.value > itemSpawnProbability) continue;

            // 랜덤 아이템 선택
            ItemSpawnLimit selectedItem = itemLimits[Random.Range(0, itemLimits.Count)];

            // 최대 스폰 제한 체크
            if (spawnedCounts[selectedItem.key] >= selectedItem.maxCount) continue;

            GameObject obj = PoolingManager.Instance.SpawnFromPool(selectedItem.key, sp.position, sp.rotation, sp);
            if (obj != null)
            {
                spawnedCounts[selectedItem.key]++;
            }
        }
    }

    // 아이템 반환 시 호출
    public void ReturnBuildingItem(string key, GameObject obj)
    {
        PoolingManager.Instance.ReturnToPool(key, obj);
        if (spawnedCounts.ContainsKey(key))
            spawnedCounts[key] = Mathf.Max(0, spawnedCounts[key] - 1);
    }
}
