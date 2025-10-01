using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolingManager : MonoBehaviour
{
    // 싱글톤으로 전역에서 접근
    public static PoolingManager Instance { get; private set; }

    [System.Serializable]
    public class DinoPool
    {
        public string dinokey;          // 풀 식별자, 예: "Raptor"
        public GameObject dinoPrefab;   // 풀링할 프리팹
        public int size;                // 초기 생성 개수
    }
    [SerializeField] private List<DinoPool> pools;  // 인스펙터에서 풀들을 설정
    private Dictionary<string, Queue<GameObject>> poolDictionary;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        InitializeDino();
    }

    //=================== 공룡 Pooling ===================
    private void InitializeDino()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        // 모든 풀을 초기화: size만큼 비활성화된 오브젝트를 생성하여 큐에 넣음
        foreach (var pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();
            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.dinoPrefab);
                obj.SetActive(false);

                // 하이라키 정리용으로 PoolingManager를 부모로 할당
                obj.transform.SetParent(this.transform);
                objectPool.Enqueue(obj);
            }
            // key 중복시 경고
            if (poolDictionary.ContainsKey(pool.dinokey))
                Debug.LogWarning($"Key 중복 : {pool.dinokey}");
            else
                poolDictionary.Add(pool.dinokey, objectPool);
        }
    }

    // Enqueue 후 바로 다시 넣는(원형) 방식
    // 주의: 이 방식은 "라운드로빈"처럼 동작하지만, 풀 크기가 작으면
    // 이미 활성화된 오브젝트를 다시 꺼내게 되어 문제(겹침)가 생길 수 있음.
    public GameObject SpawnFromPool(string dinokey, Vector3 pos, Quaternion rot)
    {
        if (!poolDictionary.ContainsKey(dinokey))
        {
            Debug.LogWarning($"{dinokey}가 없습니다.");
            return null;
        }
        GameObject objectToSpawn = poolDictionary[dinokey].Dequeue();

        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = pos;
        objectToSpawn.transform.rotation = rot;

        // 즉시 다시 큐에 넣음: 사용 후 반환을 별도로 호출하지 않는 간단한 패턴
        poolDictionary[dinokey].Enqueue(objectToSpawn);

        return objectToSpawn;
    }

    // 명시적으로 반환하는 방식의 메서드
    // 예: PoolingManager.Instance.ReturnToPool("Raptor", gameObject);
    public void ReturnToPool(string key, GameObject obj)
    {
        if (!poolDictionary.ContainsKey(key))
        {
            Debug.LogWarning($"Pool with key {key} doesn't exist. Destroying object.");
            Destroy(obj);
            return;
        }

        // 비활성화 및 큐에 재삽입
        obj.SetActive(false);
        obj.transform.SetParent(this.transform);
        poolDictionary[key].Enqueue(obj);
    }

    //=================== 아이템 Pooling ===================
}
