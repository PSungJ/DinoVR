using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 공룡, 나뭇가지, 돌 등 모든 오브젝트를 풀링으로 관리하는 매니저.
/// 씬에 단 하나만 존재해야 하며, PoolingManager.Instance 로 접근.
/// </summary>
public class PoolingManager : MonoBehaviour
{
    public static PoolingManager Instance { get; private set; }

    [System.Serializable]
    public class ObjectPool
    {
        public string key;         // 풀의 식별자 (예: "Raptor", "Branch", "Rock")
        public GameObject prefab;  // 해당 오브젝트의 프리팹
        public int size;           // 초기 생성 개수
    }

    [Header("풀 목록 설정 (Inspector에서 설정)")]
    [SerializeField] private List<ObjectPool> pools = new List<ObjectPool>();

    // key별로 오브젝트 큐를 저장하는 딕셔너리
    private Dictionary<string, Queue<GameObject>> poolDictionary;

    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePools();
    }

    /// <summary>
    /// 인스펙터에서 등록된 풀들을 초기화
    /// </summary>
    private void InitializePools()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (var pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            // 초기 오브젝트 생성
            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                obj.transform.SetParent(this.transform);
                objectPool.Enqueue(obj);
            }

            // 키 중복 체크
            if (poolDictionary.ContainsKey(pool.key))
                Debug.LogWarning($"Key 중복 : {pool.key}");
            else
                poolDictionary.Add(pool.key, objectPool);
        }
    }

    /// <summary>
    /// 풀에서 오브젝트를 꺼내서 지정 위치에 배치
    /// </summary>
    public GameObject SpawnFromPool(string key, Vector3 pos, Quaternion rot, Transform parent = null)
    {
        if (!poolDictionary.ContainsKey(key))
        {
            Debug.LogWarning($"[PoolingManager] '{key}' 풀을 찾을 수 없습니다.");
            return null;
        }

        var queue = poolDictionary[key];
        GameObject objectToSpawn = queue.Count > 0 ? queue.Dequeue() : InstantiateFallback(key);

        if (objectToSpawn == null) return null;

        objectToSpawn.SetActive(true);
        objectToSpawn.transform.SetPositionAndRotation(pos, rot);
        objectToSpawn.transform.SetParent(parent ?? this.transform);
        return objectToSpawn;
    }

    /// <summary>
    /// 풀에 남은 오브젝트가 없을 경우 새로 생성
    /// </summary>
    private GameObject InstantiateFallback(string key)
    {
        var foundPool = pools.Find(p => p.key == key);
        if (foundPool != null)
        {
            Debug.LogWarning($"[PoolingManager] '{key}' 풀 부족 → 새 인스턴스 생성");
            return Instantiate(foundPool.prefab);
        }

        Debug.LogError($"[PoolingManager] '{key}' 풀 데이터를 찾지 못했습니다.");
        return null;
    }

    /// <summary>
    /// 사용한 오브젝트를 다시 풀로 반환
    /// </summary>
    public void ReturnToPool(string key, GameObject obj)
    {
        if (!poolDictionary.ContainsKey(key))
        {
            Debug.LogWarning($"[PoolingManager] '{key}' 풀 없음 → 오브젝트 제거");
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        obj.transform.SetParent(this.transform);
        poolDictionary[key].Enqueue(obj);
    }
}
