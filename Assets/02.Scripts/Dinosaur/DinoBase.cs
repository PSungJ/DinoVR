using System.Collections;

using UnityEngine;
using UnityEngine.AI;

                      // 기본,  배회,    먹기,    마시기,    잠,      도망,     공격,       찾기,    포효,   추적,   죽음,   부르기, 은밀
public enum DinoState { IDLE, ROAMING, EATING, DRINKING, SLEEPING, FLEEING, ATTACKING, SEARCHING, ROAR, CHASING, DEATH , CALL, SNEAK};

public class DinoBase : MonoBehaviour
{
    [Header("컴포넌트 및 시스템 속성")]
    public Animator animator;
    public DinoSound sound;
    public DinoStatus status;
    public NavMeshAgent agent;

    public DinoState currentState;

    protected float currentIdleTime = 0f;    // 현재 멈춰있는 시간
    protected float toRoamTime = 5f;  // 배회하기 까지 멈춰있는 시간
    protected float currentAttackTime = 0f;
    protected float toAttackTime = 3f; // 공격하기 까지 기다리는 시간

    protected float callTime = 4f;

    protected bool isSearching = false;

    public bool isAnimating = false;
    public string currentStateName;

    protected readonly string _aniWalk = "IsWalk";
    protected readonly string _aniRun = "IsRun";
    protected readonly string _aniEat = "IsEat";
    protected readonly string _aniDrink = "IsDrink";
    protected readonly string _aniSleep = "IsSleep";
    protected readonly string _aniSearch = "IsSearch";
    protected readonly string _aniDeath = "IsDeath";
    protected readonly string _aniSneak = "IsSneak";
    protected readonly string _aniRoar = "Roar";
    protected readonly string _aniHurt = "Hurt";
    protected readonly string _aniAttack = "Attack";
    protected readonly string _aniAttack1 = "Attack1";
    protected readonly string _aniCall = "Call";

    void Start()
    {
        TryGetComponent<DinoSound>(out sound);
        TryGetComponent<DinoStatus>(out status);
        TryGetComponent<NavMeshAgent>(out agent);
        TryGetComponent<Animator>(out animator);
        agent.updateRotation = false;
        agent.isStopped = true;
    }

    protected virtual void Update()
    {
        if (currentState == DinoState.DEATH)    // 죽었으면 다 무시
            return;

        // 공포 원인이 나타났거나, 배고픈데 타겟을 발견했다면
        if (status.fearOrigin != null)
        {
            if (status.fearCurrent > 0 && isSearching == false)
            {
                isSearching = true;
                ChangeState(DinoState.SEARCHING);   // 경계 태세 진입
            }
        }
        else if (status.target != null && isSearching == false)
        {
            isSearching = true;
            ChangeState(DinoState.SEARCHING);
        }

        if (agent.hasPath)
        {
            MoveWithSteering();
        }

        HandleState();
        UpdateAnimator();
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
            case DinoState.CALL:
                Call();
                break;
            case DinoState.SNEAK:
                Sneak();
                break;
            case DinoState.CHASING:
                Chasing();
                break;
            case DinoState.DEATH:
                Death();
                break;
        }
    }

    protected virtual void UpdateAnimator()
    {
        animator.SetBool(_aniWalk, currentState == DinoState.ROAMING);
        animator.SetBool(_aniRun, currentState == DinoState.FLEEING || currentState == DinoState.CHASING);
        animator.SetBool(_aniEat, currentState == DinoState.EATING);
        animator.SetBool(_aniDrink, currentState == DinoState.DRINKING);
        animator.SetBool(_aniSleep, currentState == DinoState.SLEEPING);
        animator.SetBool(_aniSearch, currentState == DinoState.SEARCHING);
        animator.SetBool(_aniDeath, currentState == DinoState.DEATH);
    }

    public virtual void Idle()  // 기본 상태
    {
        isSearching = false;
        
        if (currentIdleTime < toRoamTime)           // toRoamTime 만큼 대기 후 떠돌기 위한 체크
        {
            currentIdleTime += Time.deltaTime;
        }
        else if (currentIdleTime >= toRoamTime)     // 랜덤한 20 거리 지점으로 이동
        {
            currentIdleTime = 0f;
            agent.SetDestination(GetRandomPoint(transform.position, 20f));
            ChangeState(DinoState.ROAMING);
        }
    }

    protected void StartFleeing()       // 도망치라고 말했습니다.
    {
        Vector3 fleeDir = (transform.position - status.fearOrigin.position).normalized;

        if (currentState != DinoState.FLEEING)  // 처음 도망갈 때
        {
            agent.destination = transform.position + fleeDir * status.fleeDistance;
            ChangeState(DinoState.FLEEING);
        }
        else                                    // 도망가다 다른 적을 만났을 때 서로의 위치의 중간값으로 도망가도록
        {
            agent.destination = agent.destination + fleeDir * status.fleeDistance;
            ChangeState(DinoState.FLEEING);
        }
    }

    public virtual void Roam()  // 떠도는 중
    {
        agent.speed = status.walkSpeed;
        //RotateSmoothly((agent.destination - transform.position).normalized);
        if (agent.destination == transform.position)     // 도착하면 기본상태로 전환
        {
            ChangeState(DinoState.IDLE);
        }
    }

    public virtual void Eating()
    {
        
    }

    public virtual void Drink()
    {
        
    }

    public virtual void Sleeping()
    {
        
    }

    public virtual void Fleeing()   // 도망
    {
        //agent.isStopped = false;
        agent.speed = status.runSpeed;
        //RotateSmoothly((agent.destination - transform.position).normalized);

        if (agent.destination == transform.position)     // 도망 지점에 도착하면 경계 상태로 전환
        {
            status.fearOrigin = null;
            status.fearCurrent = 50f;
            ChangeState(DinoState.SEARCHING);
        }
        else if (status.fearOrigin != null && !status.IsAfraid())                         // 도망중에 적이 사거리에 오면
        {
            float dis = (status.fearOrigin.position - transform.position).magnitude;
            if (dis <= status.attackRange)
            {
                ChangeState(DinoState.ATTACKING);
            }
        }
    }

    public virtual void Searching() // 경계 태세
    {
        agent.destination = transform.position;

        if (status.isFoodMeat == false) // 초식이면
        {
            if (status.fearCurrent <= 0)    // 공포 수치가 0이 되면 경계 풀기
            {
                ChangeState(DinoState.IDLE);
                return;
            }
            else if (status.fearOrigin != null)
            {
                float dis = (status.fearOrigin.position - transform.position).magnitude;
                if (dis > status.detactRange * 0.9f)  // 멀리서 접근하는 걸 발견했다면 바라보기
                {
                    RotateSmoothly(status.fearOrigin.position - transform.position);
                    if (status.IsAfraid())
                        StartFleeing();
                    else if (status.fearCurrent >= status.fearThreshold*0.2f)
                        StartFleeing();
                }
                else if (dis > status.attackRange)  // 거리가 가깝지만 공격사거리 밖이라면
                {
                    ChangeState(DinoState.ROAR);
                }
                else if (dis <= status.attackRange) // 공격사거리 안이라면
                {
                    if (!status.IsAfraid())
                        ChangeState(DinoState.ATTACKING);
                    else
                        StartFleeing();
                }
            }
        }
        else    // 육식이면
        {
            if (status.fearCurrent == 0 && status.target != null) // 공포가 0 이라면 == 사냥
            {
                float dis = (status.target.position - transform.position).magnitude;
                if (dis > status.detactRange / 2f)  // 멀리서 접근하는 걸 발견했다면 바라보기
                {
                    RotateSmoothly(status.target.position - transform.position);
                    currentAttackTime += Time.deltaTime;
                    if (currentAttackTime >= toAttackTime)
                    {
                        currentAttackTime = 0f;
                        ChangeState(DinoState.CHASING);
                        isAnimating = true;
                    }
                }
            }
        }
    }

    public virtual void Call()
    {

    }
    public virtual void Sneak()
    {
        
    }

    public virtual void Chasing()
    {
        if(status.target == null)
        {
            ChangeState(DinoState.IDLE);
            return;
        }
        agent.destination = status.target.position;
        agent.speed = status.runSpeed;
        if(Vector3.Distance(status.target.position,transform.position) <= status.attackRange)
        {
            agent.destination = transform.position;
            ChangeState(DinoState.ATTACKING);
        }
    }

    public virtual void Roar()  // 포효
    {
        animator.SetTrigger(_aniRoar);
                         
        if (status.IsAfraid())                      // 공포상태라면 도망
            StartFleeing();
                                                    // 포효 상태에서 적이 공격사거리에 들어오면 공격하기
        else if (Vector3.Distance(transform.position, status.fearOrigin.position) <= status.attackRange)
        {
            ChangeState(DinoState.ATTACKING);
        }
        else                                    // 그것도 다 아니라면 기본상태로 전환
            ChangeState(DinoState.IDLE);
    }

    public virtual void Attack()    // 공격
    {
        if (status.target == null)
        {
            if (status.isFoodMeat)
            {
                ChangeState(DinoState.IDLE);
                return;
            }
        }
        else
        {
            RotateSmoothly(status.target.position - transform.position);
        }

        if (!isAnimating)
        {
            if (status.isFoodMeat == false) // 초식
            {
                if (status.fearOrigin != null)
                {
                    if (status.IsAfraid())
                        StartFleeing();
                }
            }
            else                            // 육식
            {
                if (status.fearOrigin != null && status.IsAfraid())   // 공포원인이 있고 공포에 도달했다면 도망
                    StartFleeing();
                else if (status.target != null)                       // 그런거 없고 공격중인 타겟이 있다면
                {
                    isAnimating = true;
                    if (Vector3.Distance(transform.position, status.target.position) <= status.attackRange)
                        ChangeState(DinoState.ATTACKING);
                    else
                        ChangeState(DinoState.CHASING);
                }
            }
        }
    }

    public virtual void Death()
    {
        
    }

    public void ChangeState(DinoState newState)
    {
        ResetAnimationTrigger();
        currentState = newState;
    }

    public void MoveToward(Vector3 pos, float moveSpeed)
    {
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 20f, NavMesh.AllAreas))
        {
            agent.destination = pos;
        }
        agent.speed = moveSpeed;
    }

    public void ResetAnimationTrigger()
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Trigger)
            {
                animator.ResetTrigger(param.name);
            }
        }
    }
    public void SetAnimate(bool value, string stateName)
    {
        isAnimating = value;
        currentStateName = stateName;
        Debug.Log($"isAnimating : {isAnimating}, currentState : {stateName}");
    }

    protected Vector3 GetRandomPoint(Vector3 center, float range)     // 주변에 걸을 수 있는 랜덤 지점을 반환
    {
        Vector3 randomPos = center + new Vector3(Random.Range(-range, range), 0, Random.Range(-range, range));
        if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, range, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return GetRandomPoint(center, range);
    }

    protected void RotateSmoothly(Vector3 dir, bool slowTurn = false)   // 방향회전
    {
        if (dir.sqrMagnitude < 0.01f) return;
        float rotSpeed = slowTurn ? status.rotationSpeed * 0.5f : status.rotationSpeed;
        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotSpeed * Time.deltaTime);
    }

    void MoveWithSteering()
    {
        if (agent.pathPending || !agent.hasPath) return;

        Vector3 target = agent.steeringTarget;
        Vector3 dir = (target - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.1f)
        {
            agent.ResetPath();
            return;
        }

        Vector3 moveDir = dir.normalized;

        // 회전
        Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 10f * status.rotationSpeed * Time.deltaTime);

        // 현재 정면과 목표 방향의 각도 차이
        float angle = Vector3.Angle(transform.forward, moveDir);

        // 각도가 작을수록 빠르게, 클수록 느리게 전진
        float alignmentFactor = Mathf.Clamp01(1f - (angle / 90f)); // 0~90도 기준으로 보정
        float currentSpeed = agent.speed * alignmentFactor;

        // 이동
        transform.position += transform.forward * currentSpeed * Time.deltaTime;
    }



}
