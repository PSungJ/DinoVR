using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private List<TerrainSpawnData> terrainDataList; // 테레인별 데이터 모음
    private TerrainSpawnData currentData;

    /// <summary>
    /// 외부에서 현재 테레인을 지정할 때 호출
    /// 예: SetTerrain("Jungle");
    /// </summary>
    public void SetTerrain(string terrainName)
    {
        currentData = terrainDataList.Find(t => t.terrainName == terrainName);
        if (currentData == null)
        {
            Debug.LogWarning($"TerrainSpawnData for '{terrainName}' not found.");
            return;
        }
        SpawnDinos();
    }

    /// <summary>
    /// 현재 테레인에 맞는 공룡들을 스폰
    /// </summary>
    private void SpawnDinos()
    {
        if (currentData == null) return;

        foreach (var info in currentData.dinoList)
        {
            for (int i = 0; i < info.count; i++)
            {
                // 확률 체크 (0~1)
                if (Random.value <= info.probability)
                {
                    Vector3 pos = GetRandomPositionOnTerrain();

                    // 풀링에서 해당 공룡 prefab 꺼내기
                    string key = info.dinoData.dino.ToString();
                    GameObject dino = PoolingManager.Instance.SpawnFromPool(
                        key, pos, Quaternion.identity
                    );

                    if (dino == null)
                    {
                        Debug.LogWarning($"Failed to spawn dino with key {key}");
                    }
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
        float y = t.SampleHeight(new Vector3(x, 0f, z)) + terrainPos.y;

        return new Vector3(x, y, z);
    }
}
