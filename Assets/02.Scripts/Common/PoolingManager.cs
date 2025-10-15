using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolingManager : MonoBehaviour
{
    public static PoolingManager Instance { get; private set; }

    [System.Serializable]
    public class DinoPool
    {
        public string dinokey;          // 풀 식별자, 예: "Raptor"
        public GameObject dinoPrefab;   // 풀링할 프리팹
        public int size;                // 초기 생성 개수
    }

    [SerializeField] private List<DinoPool> pools;  // 인스펙터에서 설정
    private Dictionary<string, Queue<GameObject>> poolDictionary;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeDino();
    }

    // =================== 초기화 ===================
    private void InitializeDino()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (var pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.dinoPrefab);
                obj.SetActive(false);
                obj.transform.SetParent(this.transform);
                objectPool.Enqueue(obj);
            }

            if (poolDictionary.ContainsKey(pool.dinokey))
                Debug.LogWarning($"Key 중복 : {pool.dinokey}");
            else
                poolDictionary.Add(pool.dinokey, objectPool);
        }
    }

    // =================== 스폰 ===================
    public GameObject SpawnFromPool(string dinokey, Vector3 pos, Quaternion rot, Transform parent = null)
    {
        if (!poolDictionary.ContainsKey(dinokey))
        {
            Debug.LogWarning($"[PoolingManager] '{dinokey}' 풀을 찾을 수 없습니다.");
            return null;
        }

        var queue = poolDictionary[dinokey];
        GameObject objectToSpawn = null;

        if (queue.Count > 0)
        {
            objectToSpawn = queue.Dequeue();
        }
        else
        {
            // 풀에 남은 오브젝트가 없다면 새로 생성
            DinoPool foundPool = pools.Find(p => p.dinokey == dinokey);
            if (foundPool != null)
            {
                objectToSpawn = Instantiate(foundPool.dinoPrefab);
                Debug.LogWarning($"[PoolingManager] '{dinokey}' 풀 부족 → 새 인스턴스 생성");
            }
            else
            {
                Debug.LogError($"[PoolingManager] '{dinokey}' 풀 데이터를 찾지 못했습니다.");
                return null;
            }
        }

        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = pos;
        objectToSpawn.transform.rotation = rot;

        // 부모 설정: Terrain 하위 or PoolManager 하위
        if (parent != null)
            objectToSpawn.transform.SetParent(parent);
        else if (Terrain.activeTerrain != null)
            objectToSpawn.transform.SetParent(Terrain.activeTerrain.transform);
        else
            objectToSpawn.transform.SetParent(this.transform);

        return objectToSpawn;
    }

    // =================== 반환 ===================
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

    //=================== 아이템 Pooling ===================
}
