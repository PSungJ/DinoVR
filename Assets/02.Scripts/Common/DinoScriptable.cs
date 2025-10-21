using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Dino Data", menuName = "GameData/DinoData", order = 0)]

public class DinoScriptable : ScriptableObject
{
    // 공룡 식별용 enum — Inspector에서 드롭다운으로 선택 가능
    public enum DinoName { Phacy, Stego, Brachio, Croco, Raptor, Tyranno }
    public DinoName dino;

    [Header("스테이터스")]
    public float hpMax = 100;           // 체력
    public float hungerMax = 100;       // 최대 배고픔
    public float walkSpeed = 2;         // 걷는 속도
    public float runSpeed = 4;         // 뛰는 속도
    public float rotationSpeed = 1f;    // 회전 속도
    [Tooltip("밀림 우선순위 (PositionPriority) 낮을 수록 높은 개체에게 밀리지 않음")]
    public int pp = 10;               // 밀림 우선순위    DinoBase의 NavMeshAgent 의 Obstacle Avoidance Priority 값 결정
    [Tooltip("도망 거리")]
    public float fleeDistance = 30f;    // 도망 거리
    public float attackDamage = 50;      // 공격력
    public float attackRange = 4;       // 공격 시작 사거리
    [Tooltip("스폰 지점에서 부터 몇 미터 까지 돌아다닐 지 (서식 영역)")]
    public float territoryRange = 50;    // 서식지 범위 ( 스폰 위치를 기준으로 최대 몇 미터까지 돌아다닐 지 )
    [Tooltip("발자국 생성 주기")]
    public float footStepInterval = 180;  // 발자국 생성 주기 (초)

    [Header("스테이터스 2")]
    [Tooltip("최대 공포수치")]
    public float fearThreshold = 100;   // 최대 공포
    [Tooltip("공포 감소 주기")]
    public float fearReduceInterval = 1f; // 공포 감소 주기
    [Tooltip("공포 지속시간")]
    public float fearDuration = 5f;     // 공포 지속시간
    [Tooltip("위협 생성 수치")]
    public float threat = 0;            // 위협 생성
    [Tooltip("시야밖 감지 예민성")]
    public float awareness = 10;         // 시야밖 감지 예민성
    public float detactRange = 20;      // 감지 거리

    [Tooltip("육식여부")]
    public bool isFoodMeat = false; // 육식 여부
}
