using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DinoStatus : MonoBehaviour
{
    [Header("스테이터스")]
    public float hpMax = 100;           // 체력
    public float hungerMax = 100;       // 최대 배고픔
    public float thirstMax = 100;       // 최대 갈증
    public float walkSpeed = 2;         // 걷는 속도
    public float runSpeed = 4;         // 뛰는 속도
    public float rotationSpeed = 1f;    // 회전 속도
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

    DinoBase dino;

    float lastFearTime;

    [Tooltip("육식여부")]
    public bool isFoodMeat = false; // 육식 여부

    [Header("확인용")]
    public float hpCurrent;
    public float moveSpeedCurrent;
    public float fearCurrent;
    public float hungerCurrent;
    public float thirstCurrent;
    public bool isDie = false;
    public List<Transform> targetList;   // 사냥감 후보 리스트
    public List<Transform> meatList;   // 사냥감 후보 리스트
    public Transform target;            // 가장 가까운 사냥감
    public Transform meat;              // 가장 가까운 고기
    public Transform fearOrigin;        // 공포 원인

    private void Start()
    {
        hpCurrent = hpMax;
        moveSpeedCurrent = walkSpeed;
        fearCurrent = 0;
        hungerCurrent = hungerMax;
        thirstCurrent = thirstMax;

        lastFearTime = Time.time;

        dino = GetComponent<DinoBase>();
        StartCoroutine(FearUpdate());
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white.WithAlpha(0.1f);
        Gizmos.DrawWireSphere(transform.position, awareness);
        Gizmos.DrawWireSphere(transform.position+ transform.forward * detactRange, awareness);
    }

    IEnumerator FearUpdate()        // 공포 감지
    {
        while (!isDie)   // 죽지 않았다면
        {
            yield return new WaitForSeconds(0.1f);
            if (hungerCurrent > 0)
                hungerCurrent -= 0.1f;
            if (fearCurrent > 0 && Time.time - lastFearTime >= fearReduceInterval)
            {
                fearCurrent -= 1f;
            }

            Collider[] dinos = Physics.OverlapCapsule(transform.position, transform.position + transform.forward * detactRange, awareness, LayerMask.GetMask("Dinosaur"));
            foreach (Collider col in dinos)
            {
                if (col.gameObject == gameObject) continue; // 자기 자신 제외
                if (col.TryGetComponent<DinoStatus>(out DinoStatus stat))
                {
                    if (stat.threat <= threat || stat.isDie)
                    {
                        if(isFoodMeat && stat.threat < threat)
                        {
                            if(stat.isDie)
                                meatList.Add(col.transform);
                            else
                                targetList.Add(col.transform);
                        }

                        continue; // 자신보다 위협수치가 작은 개체면 무시
                    }
                    AddFear(stat.threat, col.transform);
                }
            }
            if (isFoodMeat) // 육식공룡 이라면
            {
                target = FindNearest(targetList, transform);    // 가장 가까운 적 타겟 지정
                targetList.Clear();

                meat = FindNearest(meatList, transform);        // 가장 가까운 고기 지정
                meatList.Clear();
            }
        }
    }

    public void AddFear(float amount, Transform fearOriginTr)
    {
        // 거리 보정    // 가까울 수록 공포 수치 증가
        float sqrDist = Vector3.Magnitude(transform.position - fearOriginTr.position);
        float disFactor = Mathf.Clamp01(1f / (sqrDist * 0.1f));

        // 시야 보정    // 정면 120도 밖이면 공포 0.5 배
        Vector3 dirToSource = (fearOriginTr.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dirToSource);
        float angleFactor = (angle <= 120f) ? 1f : 0.5f;    

        // 체력 보정   // 체력이 낮으면 더 민감하게 반응
        float healthFactor = 1f;
        float hpPercent = hpCurrent / hpMax;
        healthFactor =  1f / hpPercent;

        float finalFear = amount * disFactor * healthFactor;

        fearCurrent += finalFear;
        if (fearCurrent > fearThreshold)
        {
            fearCurrent = fearThreshold;
        }
        fearOrigin = fearOriginTr;
        lastFearTime = Time.time;
        IsAfraid();

        //Debug.Log($"현재 공포:{fearCurrent} 공포 {finalFear} 증가 = 거리 보정:{disFactor} | 시야보정:{disFactor} | 체력 보정:{healthFactor}");
    }

    public bool IsAfraid()  // (공포 수치가 임계점을 넘었는지) 확인
    {
        bool terrified = fearCurrent >= fearThreshold;
        return terrified;
    }

    public Transform FindNearest(List<Transform> targets, Transform self)
    {
        Transform nearest = null;
        float nearestDistSqr = Mathf.Infinity;
        Vector3 currentPos = self.position;

        foreach (Transform t in targets)
        {
            if (t == null) continue; // 비어있는 경우 무시
            float distSqr = (t.position - currentPos).sqrMagnitude; // 제곱 거리 계산
            if (distSqr < nearestDistSqr)
            {
                nearest = t;
                nearestDistSqr = distSqr;
            }
        }
        if (nearest == null && target != null)
            nearest = target;        
        return nearest;
    }
}
