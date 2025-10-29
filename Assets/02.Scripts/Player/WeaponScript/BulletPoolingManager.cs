using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Inspector에서 등록한 탄환 프리팹을 기반으로 풀을 생성하고 관리합니다.
/// </summary>
public class BulletPoolingManager : MonoBehaviour
{
    [System.Serializable]
    public class BulletPoolEntry
    {
        public string key;
        public GameObject prefab;
        public int poolSize = 10;
    }

    public static BulletPoolingManager Instance { get; private set; }

    [Header("Bullet Pool 설정")]
    [Tooltip("풀링할 탄환 프리팹과 사이즈를 Inspector에서 등록하세요.")]
    [SerializeField] private List<BulletPoolEntry> bulletPoolList = new List<BulletPoolEntry>();

    private Dictionary<string, Queue<GameObject>> bulletPools = new Dictionary<string, Queue<GameObject>>();

    private void Awake()
    {
        // 싱글톤 설정
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        InitializePools();
    }

    /// <summary>
    /// Inspector에서 설정된 프리팹 리스트를 기반으로 풀 생성
    /// </summary>
    private void InitializePools()
    {
        foreach (var entry in bulletPoolList)
        {
            if (entry.prefab == null || string.IsNullOrEmpty(entry.key))
            {
                Debug.LogWarning("[BulletPoolingManager] 유효하지 않은 풀 항목이 있습니다.");
                continue;
            }

            Queue<GameObject> poolQueue = new Queue<GameObject>();

            for (int i = 0; i < entry.poolSize; i++)
            {
                GameObject bullet = Instantiate(entry.prefab);
                bullet.name = entry.key; // key 이름으로 명명
                bullet.SetActive(false);
                poolQueue.Enqueue(bullet);
            }

            bulletPools[entry.key] = poolQueue;
        }
    }

    public GameObject GetBullet(string key)
    {
        if (!bulletPools.ContainsKey(key))
        {
            Debug.LogWarning($"[BulletPoolingManager] 키 '{key}'에 해당하는 풀을 찾을 수 없습니다.");
            return null;
        }

        if (bulletPools[key].Count > 0)
        {
            GameObject bullet = bulletPools[key].Dequeue();
            bullet.SetActive(true);
            return bullet;
        }

        Debug.LogWarning($"[BulletPoolingManager] 풀 '{key}'이 비어 있습니다.");
        return null;
    }

    public void ReturnBullet(GameObject bullet)
    {
        bullet.SetActive(false);
        string key = bullet.name.Replace("(Clone)", "").Trim();

        if (bulletPools.ContainsKey(key))
        {
            bulletPools[key].Enqueue(bullet);
        }
        else
        {
            Debug.LogWarning($"[BulletPoolingManager] '{key}' 풀을 찾을 수 없습니다. 삭제합니다.");
            Destroy(bullet);
        }
    }
}
