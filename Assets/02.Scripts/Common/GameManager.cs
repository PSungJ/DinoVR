using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Manager References")]
    [SerializeField] private SpawnManager spawnManager;

    [Header("Environment Settings")]
    [SerializeField] private string currentTerrainName = "Terrain_2_2"; // 현재 테레인 이름

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private IEnumerator Start()
    {
        // PoolingManager가 Awake에서 초기화되도록 1프레임 대기
        yield return null;

        Debug.Log($"[GameManager] Initializing terrain: {currentTerrainName}");

        // 현재 테레인에 맞는 공룡 스폰
        if (spawnManager != null)
        {
            spawnManager.SetTerrain(currentTerrainName);
        }
        else
        {
            Debug.LogWarning("[GameManager] SpawnManager not assigned!");
        }
    }

    // 나중에 맵 전환 시 쉽게 호출 가능
    public void ChangeTerrain(string nextTerrain)
    {
        if (currentTerrainName == nextTerrain)
            return;

        currentTerrainName = nextTerrain;
        Debug.Log($"[GameManager] Terrain changed to {nextTerrain}");
        spawnManager.SetTerrain(nextTerrain);
    }
}