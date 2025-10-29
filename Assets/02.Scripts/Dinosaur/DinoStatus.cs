using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;

public class DinoStatus : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("공룡 기본 스테이터스 파일 (Scriptable Object)")]
    public DinoScriptable stats;

    float lastFearTime;

    DinoBase dino;

    [Header("확인용")]
    public float hpCurrent;
    public float fearCurrent;
    public float hungerCurrent;
    public bool isDie = false;
    public List<Transform> targetList = new();   // 사냥감 후보 리스트
    public List<Transform> meatList = new();   // 사냥감 후보 리스트
    public Transform target;            // 가장 가까운 사냥감
    public Transform meat;              // 가장 가까운 고기
    public Transform fearOrigin;        // 공포 원인

    private const float statusUpdateInterval = 1.0f;    // 배고픔 , 체력회복 갱신 주기
    private const float targetingInterval = 0.5f;       // 타겟 감지 갱신 주기

    private void Start()
    {
        dino = GetComponent<DinoBase>();
        StatusInit();
        StartCoroutine(StatusUpdate());
        StartCoroutine(Targeting());
    }

    IEnumerator StatusUpdate()
    {
        while (!isDie)
        {
            yield return new WaitForSeconds(statusUpdateInterval);
            if (hungerCurrent > 0)
                hungerCurrent = Mathf.Clamp(hungerCurrent - 1f, 0, stats.hungerMax);
            // 배고프지 않을때 체력 회복
            if (hpCurrent < stats.hpMax && hungerCurrent > 0f)
            {
                hpCurrent = Mathf.Clamp(hpCurrent + 1f, 0, stats.hpMax);
            }
        }
    }

    public void StatusInit()
    {
        isDie = false;
        hpCurrent = stats.hpMax;
        fearCurrent = 0;
        hungerCurrent = stats.hungerMax;
        lastFearTime = Time.time;
    }

    private void OnDrawGizmos()
    {
                            // 스탯 할당 전에는 Gizmo 그리지 않음
        if (stats == null) return;
        Gizmos.color = Color.white.WithAlpha(0.1f);
        Gizmos.DrawWireSphere(transform.position, stats.awareness);
        Gizmos.DrawWireSphere(transform.position+ transform.forward * stats.detactRange, stats.awareness);
    }

    IEnumerator Targeting()        // 타겟 감지 및 주변 공포 획득
    {
        while (!isDie)   // 죽지 않았다면
        {
            yield return new WaitForSeconds(targetingInterval);

            if (fearCurrent > 0 && Time.time - lastFearTime >= stats.fearReduceInterval)
            {
                fearCurrent = Mathf.Clamp(fearCurrent - stats.fearThreshold * 0.01f, 0, stats.fearThreshold);
                if (fearCurrent == 0)
                    fearOrigin = null;
            }

            Collider[] dinos = Physics.OverlapCapsule(transform.position, transform.position + transform.forward * stats.detactRange, stats.awareness);
            
            foreach (Collider col in dinos)
            {
                if (col.gameObject == gameObject) continue; // 자기 자신 제외
                //if (col.gameObject.layer != LayerMask.GetMask("Dinosaur")) // 공룡이나 사람 아니면 스킵
                    //continue;
                
                DinoStatus dino = col.GetComponent<DinoStatus>();
                if (dino != null)
                {
                    // 자신보다 위협수치가 이하이거나 죽었다면
                    if (dino.stats.threat <= stats.threat || isDie)
                    {
                        // 자신이 육식일때 상대가 살아있고 나보다 위협수치가 낮다면
                        if (stats.isFoodMeat && dino.isDie == false && dino.stats.threat < stats.threat)
                        {
                            if (RayCheck(dino.transform))
                            {
                                targetList.Add(dino.transform);
                            }
                        }
                        continue; // 자신보다 위협수치가 작은 개체면 무시
                    }
                    AddFear(dino.stats.threat, col.transform);
                }


                // 플레이어 인식해서 타겟에 추가하기

                if (col.tag == "Player")
                    targetList.Add(col.transform);

            }
            if (stats.isFoodMeat) // 육식공룡 이라면
            {
                if (target != null)     // 기존 타겟도 후보에 포함
                    targetList.Add(target);
                target = FindNearest(targetList, transform);    // 가장 가까운 타겟 확정
                targetList.Clear();

                // 고기 감지
                Collider[] meats = Physics.OverlapCapsule(transform.position, transform.position + transform.forward * stats.detactRange, stats.awareness * 2, LayerMask.GetMask("Dinosaur"));
                foreach (Collider col in meats)
                {
                    if (col.gameObject == gameObject) continue; // 자기 자신 제외
                    DinoStatus stat = col.GetComponent<DinoStatus>();
                    if (stat != null && stat.isDie)
                    {
                        meatList.Add(col.transform);
                    }
                }
                meat = FindNearest(meatList, transform);        // 가장 가까운 고기 지정
                meatList.Clear();
            }
        }
    }

    public bool RayCheck(Transform target)
    {
        float maxDistance = stats.detactRange + stats.awareness / 2f;
        RaycastHit hit;
        Vector3 pos = transform.position + Vector3.up * 2f;
        Debug.DrawRay(pos, (target.transform.position - pos) * maxDistance, Color.blue);
        if (Physics.Raycast(pos, target.transform.position - pos, out hit, maxDistance, LayerMask.GetMask("Building") | LayerMask.GetMask("Object")))
        {
            if (Vector3.Distance(target.transform.position, pos) < stats.awareness / 2f) // 장애물이 있더라도 감지범위의 50% 이내라면
            {
                Debug.DrawRay(pos, (target.transform.position - pos) * maxDistance, Color.yellow);
                return true;
            }
        }
        else if (hit.transform == null)    // 닿은게 공룡이라면
        {
            Debug.DrawRay(pos, (target.transform.position - pos) * maxDistance, Color.red);
            return true;
        }
        return false;
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
        float hpPercent = Mathf.Max(0.1f, hpCurrent / stats.hpMax);
        float healthFactor =  1f / hpPercent;

        float finalFear = amount * disFactor * healthFactor * angleFactor;

        fearCurrent += finalFear;
        if (fearCurrent > stats.fearThreshold)
        {
            fearCurrent = stats.fearThreshold;
        }
        fearOrigin = fearOriginTr;
        lastFearTime = Time.time;

        //Debug.Log($"현재 공포:{fearCurrent} 공포 {finalFear} 증가 = 거리 보정:{disFactor} | 시야보정:{disFactor} | 체력 보정:{healthFactor}");
    }

    public bool IsAfraid()  // (공포 수치가 임계점을 넘었는지) 확인
    {
        return fearCurrent >= stats.fearThreshold;
    }

    private Transform FindNearest(List<Transform> targets, Transform self)
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
        return nearest;
    }
}
