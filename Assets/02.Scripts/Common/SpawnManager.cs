using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private List<TerrainSpawnData> terrainDataList; // 테레인별 데이터 모음
    private TerrainSpawnData currentData;
    private Transform currentTerrainParent;

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

        // Terrain GameObject 찾기 (Hierarchy에서 이름으로)
        GameObject terrainObj = GameObject.Find(terrainName);
        if (terrainObj != null)
        {
            // Terrain 하위에 "Dinos" 폴더가 없다면 생성
            Transform dinosFolder = terrainObj.transform.Find("Dinos");
            if (dinosFolder == null)
            {
                GameObject folder = new GameObject("Dinos");
                folder.transform.SetParent(terrainObj.transform);
                folder.transform.localPosition = Vector3.zero;
                dinosFolder = folder.transform;
            }
            currentTerrainParent = dinosFolder;
        }
        else
        {
            Debug.LogWarning($"Terrain GameObject '{terrainName}' not found in scene.");
            currentTerrainParent = null;
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
                if (Random.value <= info.probability)
                {
                    Vector3 pos = GetRandomPositionOnTerrain();
                    string key = info.dinoData.dino.ToString();

                    GameObject dino = PoolingManager.Instance.SpawnFromPool(
                        key, pos, Quaternion.identity, currentTerrainParent // 부모 전달
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
        float y = t.SampleHeight(new Vector3(x, 0f, z)) + terrainPos.y + 0.1f;

        return new Vector3(x, y, z);
    }
}
