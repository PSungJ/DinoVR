using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// 기본,  배회,    먹기,    마시기,    잠,      도망,     공격,       찾기,    포효,   추적,   죽음
public enum DinoState { IDLE, ROAMING, EATING, DRINKING, SLEEPING, FLEEING, ATTACKING, SEARCHING, ROAR, CHASING, DEATH };

public class DinoBase : MonoBehaviour
{
    [Header("컴포넌트 및 시스템 속성")]
    public Animator animator;
    public DinoSound sound;
    public DinoStatus status;
    public NavMeshAgent agent;

    public DinoState currentState;
    public Transform target;
    public Transform fearOrigin;
    public Vector3 roamDestination;

    float currentIdleTime = 0f;    // 현재 멈춰있는 시간
    float toRoamTime = 5f;  // 배회하기 까지 멈춰있는 시간

    void Start()
    {
        TryGetComponent<DinoSound>(out sound);
        TryGetComponent<DinoStatus>(out status);
        TryGetComponent<NavMeshAgent>(out agent);
    }

    private void Update()
    {
        if(fearOrigin != null)
        {
            if (Vector3.Distance(transform.position, fearOrigin.position) > status.detactRange) // 감지 범위 밖까지 도망가면 공포 원인 초기화
            {
                fearOrigin = null;
            }
        }
        HandleState();
    }

    public void HandleState()
    {
        switch (currentState)
        {
            case DinoState.IDLE:
                Idle();
                break;
            case DinoState.ROAMING:
                Roam();
                break;
            case DinoState.EATING:
                Eating();
                break;
            case DinoState.DRINKING:
                Drink();
                break;
            case DinoState.SLEEPING:
                Sleeping();
                break;
            case DinoState.FLEEING:
                Fleeing();
                break;
            case DinoState.ATTACKING:
                Attack();
                break;
            case DinoState.SEARCHING:
                Searching();
                break;
            case DinoState.ROAR:
                Roar();
                break;
            case DinoState.DEATH:
                Death();
                break;
        }
    }

    public void ChangeState(DinoState newState)
    {
        currentState = newState;
    }

    #region 행동
    public void Idle()
    {
        agent.isStopped = true;
        if(currentIdleTime < toRoamTime)
        {
            currentIdleTime += Time.deltaTime;
        }

        if (status.IsAfraid())      // 공포가 최대수치를 넘었다면 도망
        {
            ChangeState(DinoState.FLEEING);
        }
        else if (status.fearCurrent >= (float)status.fearThreshold / 2 && fearOrigin != null) // 공포가 최대 수치의 50% 이상이면 경고
        {
            ChangeState(DinoState.ROAR);
        }
        else if (currentIdleTime >= toRoamTime)
        {
            currentIdleTime = 0f;
            agent.SetDestination(GetRandomPoint(transform.position,20f));
            ChangeState(DinoState.ROAMING);
        }
    }

    public void Roam()
    {
        agent.isStopped = false;
        if (!agent.pathPending && agent.remainingDistance < 1f)
        {
            ChangeState(DinoState.IDLE);
        }
    }

    public void Eating()
    {
        agent.isStopped = true;
    }

    public void Drink()
    {
        agent.isStopped = true;
    }

    public void Sleeping()
    {
        agent.isStopped = true;
    }

    public void Fleeing()
    {
        agent.isStopped = false;
    }

    public void Searching()
    {
        agent.isStopped = true;
    }

    public void Attack()
    {
        agent.isStopped = true;
    }

    public void Roar()
    {
        agent.isStopped = true;
    }

    public void Death()
    {
        agent.isStopped = true;
    }
    #endregion

    Vector3 GetRandomPoint(Vector3 center, float range)     // 주변에 걸을 수 있는 랜덤 지점을 반환
    {
        Vector3 randomPos = center + new Vector3(Random.Range(-range, range), 0, Random.Range(-range, range));
        if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, range, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return GetRandomPoint(center, range);
    }
}
