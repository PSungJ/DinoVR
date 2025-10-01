using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Dino Data", menuName = "GameData/DinoData", order = 0)]

public class DinoScriptable : ScriptableObject
{
    // 공룡 식별용 enum — Inspector에서 드롭다운으로 선택 가능
    public enum DinoName { Phacy, Stego, Brachio, Ptera, Croco, Raptor, Tyranno }
    public DinoName dino;

    public float health = 0;        // 최대 체력
    public float speed = 0;         // 이동속도
    public float damage = 0;        // 공격력
    public string skill = "";       // 특수공격 이름

    public GameObject dinoPrefab;   // 이 ScriptableObject에 연결된 실제 프리팹
                                    // PoolingManager에 등록할 프리팹으로 사용
}
