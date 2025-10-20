using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Terrain Data", menuName = "GameData/TerrainSpawnData", order = 1)]
public class TerrainSpawnData : ScriptableObject
{
    [Header("Terrain 기본 정보")]
    public string terrainName;

    // ============================
    // 공룡 스폰 데이터
    // ============================
    [System.Serializable]
    public class DinoSpawnInfo
    {
        public DinoScriptable dinoData; // ScriptableObject 참조: 공룡의 데이터 + Prefab
        public int count;               // 스폰 시도 횟수 (또는 최대 개수)
        [Range(0f, 1f)] public float probability = 1f; // 각 시도별 스폰 확률 (0~1)
    }

    [Header("공룡 스폰 리스트")]
    public List<DinoSpawnInfo> dinoList = new List<DinoSpawnInfo>();


    // ============================
    // 환경 오브젝트 스폰 데이터
    // ============================
    [System.Serializable]
    public class PropSpawnInfo
    {
        public string key;               // PoolingManager에서 사용할 키 (예: "Branch", "Rock")
        public GameObject prefab;        // (선택) 직접 프리팹 지정 가능 (풀에 없을 경우 대비)
        public int count;                // 스폰 시도 횟수
        [Range(0f, 1f)] public float probability = 1f; // 스폰 확률
    }

    [Header("환경 오브젝트 스폰 리스트 (나뭇가지, 돌 등)")]
    public List<PropSpawnInfo> propList = new List<PropSpawnInfo>();
}
