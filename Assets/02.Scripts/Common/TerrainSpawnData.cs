using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Terrain Data", menuName = "GameData/TerrainSpawnData", order = 1)]

public class TerrainSpawnData : ScriptableObject
{
    public string terrainName;

    [System.Serializable]
    public class DinoSpawnInfo
    {
        public DinoScriptable dinoData; // ScriptableObject 참조: 해당 공룡의 데이터+prefab
        public int count;               // 시도할 스폰 횟수 (또는 최대 개수)
        public float probability = 1f;  // 각 시도별 스폰 확률 (0~1);
    }

    public List<DinoSpawnInfo> dinoList; // 이 테레인에 스폰될 공룡 리스트
}
