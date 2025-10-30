using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// 기본,  배회,    먹기,     잠,      도망,     공격,       찾기,    포효,   추적,   죽음,   부르기, 은밀
public enum DinoState { IDLE, ROAMING, EATING, SLEEPING, FLEEING, ATTACKING, SEARCHING, ROAR, CHASING, DEATH , CALL, SNEAK };

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(DinoStatus))]
[RequireComponent(typeof(Animator))]
public class DinoBase : MonoBehaviour
{
    [Header("컴포넌트 및 시스템 속성")]
    public Animator animator;
    public DinoStatus status;
    public NavMeshAgent agent;
    public DinoSound sound;

    public DinoState currentState;

    protected float currentIdleTime = 0f;    // 현재 멈춰있는 시간
    protected float toRoamTime = 5f;  // 배회하기 까지 멈춰있는 시간
    protected float lastRoarTime = 0f;  // 마지막 포효 시간
    protected float currentAttackTime = 0f;
    protected float toAttackTime = 3f; // 공격하기 까지 기다리는 시간

    protected float breathSoundTime = 0f; // 숨소리 재생 시간


    [SerializeField]
    protected float chaseTime = 0f;

    protected float callTime = 4f;

    protected bool isSearching = false;
    public float searchingTime = 0f;

    public bool isAnimating = false;
    public string currentStateName;

    protected readonly string _aniWalk = "IsWalk";
    protected readonly string _aniRun = "IsRun";
    protected readonly string _aniEat = "Eat";
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

    private void Awake()
    {
        sound = GetComponent<DinoSound>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        status = GetComponent<DinoStatus>();
    }

    private void OnEnable()
    {
        DinoInit();
    }

    public void DinoInit()
    {
        status.StatusInit();
        agent.updateRotation = false;

        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.avoidancePriority = status.stats.pp;
        }
        else
        {
            Debug.LogWarning($"[DinoInit] {gameObject.name} is not on NavMesh yet. Delaying agent setup.");
            StartCoroutine(WaitAndInitAgent());
        }
    }

    protected virtual void Update()
    {
        HandleState();
        UpdateAnimator();
        PlayBreathSound();

        if (currentState == DinoState.DEATH)    // 죽었으면 다 무시
            return;
        ThreatCheck();

        if (agent.hasPath)
        {
            MoveWithSteering();
        }
    }

    private void PlayBreathSound()      // 5 ~ 10 초 마다 숨소리 재생
    {
        if (currentState == DinoState.IDLE || currentState == DinoState.ROAMING)
        {
            if (breathSoundTime <= 0)
            {
                breathSoundTime = Random.Range(5f, 20f);
                sound.PlayBreath();
            }
            else
                breathSoundTime -= Time.deltaTime;
        }
    }

    private void ThreatCheck()
    {
        // 공포 원인이 나타났다면
        if (status.fearOrigin != null)
        {
            if (status.fearCurrent > 0 && isSearching == false)
            {
                isSearching = true;
                ChangeState(DinoState.SEARCHING);   // 경계 태세 진입
            }
        } // 공포 원인이 없고 육식공룡의 타겟후보가 있다면
        else if (status.target != null && isSearching == false)
        {
            if (status.meat == null || (status.meat != null && Vector3.Distance(status.target.position, transform.position) < status.stats.detactRange * 0.75f))
            {
                isSearching = true;
                ChangeState(DinoState.SEARCHING);
            }
        }
    }

    public void HandleState()
    {
        switch (currentState)
        {
            case DinoState.IDLE:
                agent.avoidancePriority = status.stats.pp;
                Idle();
                break;
            case DinoState.ROAMING:
                agent.avoidancePriority = status.stats.pp + 1;    // 이동시에는 멈춘 유닛을 밀어낼 수 없음
                Roam();
                break;
            case DinoState.EATING:
                Eating();
                break;
            case DinoState.SLEEPING:
                Sleeping();
                break;
            case DinoState.FLEEING:
                agent.avoidancePriority = status.stats.pp + 1;    // 이동시에는 멈춘 유닛을 밀어낼 수 없음
                Fleeing();
                break;
            case DinoState.ATTACKING:
                agent.avoidancePriority = status.stats.pp + 2;    // 공격시에는 다른 유닛을 밀어낼 수 없음
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
                agent.avoidancePriority = status.stats.pp + 1;    // 이동시에는 멈춘 유닛을 밀어낼 수 없음
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
        animator.SetBool(_aniSleep, currentState == DinoState.SLEEPING);
        animator.SetBool(_aniSearch, currentState == DinoState.SEARCHING);
        animator.SetBool(_aniDeath, currentState == DinoState.DEATH);
    }

    public virtual void Idle()  // 기본 상태
    {
        isSearching = false;
        searchingTime = 0f;

        if (currentIdleTime < toRoamTime)           // toRoamTime 만큼 대기 후 떠돌기 위한 체크
        {
            currentIdleTime += Time.deltaTime;
        }
        else if (currentIdleTime >= toRoamTime)     // 일정시간 대기 후
        {
            currentIdleTime = 0f;
            if (status.meat != null && status.hungerCurrent <= status.stats.hungerMax / 2f)   // 배고픈데 고기 있으면 고기로 이동
            {
                if (Vector3.Distance(transform.position, status.meat.position) <= status.stats.attackRange / 1.5f)  // 고기가 근처면 그냥 먹음
                {
                    ChangeState(DinoState.EATING);
                    isAnimating = true;
                }
                else
                {
                    Vector3 dir = (status.meat.position - transform.position).normalized;
                    agent.SetDestination(status.meat.position - dir * (status.stats.attackRange / 2f));
                    ChangeState(DinoState.ROAMING);
                }
            }
            else if (!status.stats.isFoodMeat && status.hungerCurrent <= status.stats.hungerMax / 2f)
            {
                ChangeState(DinoState.EATING);
                isAnimating = true;
            }
            else
            {
                agent.SetDestination(GetRandomPoint(transform.position, 20f));  // 랜덤한 20 거리 지점으로 이동
                ChangeState(DinoState.ROAMING);
            }
        }
    }

    public virtual void Roam()  // 떠도는 중
    {
        agent.speed = status.stats.walkSpeed;

        if (!agent.hasPath)     // 도착하면 기본상태로 전환
        {
            ChangeState(DinoState.IDLE);
        }
    }

    public virtual void Eating()
    {
        animator.SetTrigger(_aniEat);

        if (!isAnimating)
        {
            if (status.meat != null)
            {
                RotateSmoothly(status.meat.position - transform.position);
                DinoStatus meat = status.meat.GetComponent<DinoStatus>();
                meat.hpCurrent -= status.stats.hungerMax / 4f;
                if (meat.hpCurrent <= 0f)
                {
                    meat.gameObject.SetActive(false);
                }
            }
            if (!status.stats.isFoodMeat)
                status.hungerCurrent += status.stats.hungerMax / 8f;
            else
                status.hungerCurrent += status.stats.hungerMax / 2f;

            ChangeState(DinoState.IDLE);
        }
    }

    public virtual void Sleeping()
    {

    }

    public virtual void Fleeing()   // 도망
    {
        agent.speed = status.stats.runSpeed;

        if (!agent.hasPath)     // 도망 지점에 도착하면 경계 상태로 전환
        {
            ResetTarget();
            status.fearCurrent = 10f;
            ChangeState(DinoState.SEARCHING);
        }
        else if (status.fearOrigin != null && !status.IsAfraid())                         // 도망중에 적이 사거리에 오면
        {
            float dis = Vector3.Distance(status.fearOrigin.position, transform.position);
            if (dis <= status.stats.attackRange)
            {
                agent.ResetPath();
                ChangeState(DinoState.ATTACKING);
            }
        }
    }

    public virtual void Searching() // 경계 태세
    {
        agent.destination = transform.position;
        agent.ResetPath();

        if (status.stats.isFoodMeat == false) // 초식이면
        {
            if (status.fearCurrent <= 0)    // 공포 수치가 0이 되면 경계 풀기
            {
                ChangeState(DinoState.IDLE);
                return;
            }
            else if (status.fearOrigin != null)
            {
                float dis = (status.fearOrigin.position - transform.position).magnitude;
                if (dis > status.stats.detactRange * 0.9f)  // 멀리서 접근하는 걸 발견했다면 바라보기
                {
                    RotateSmoothly(status.fearOrigin.position - transform.position, true);
                    if (status.IsAfraid())
                        StartFleeing();
                    else if (status.fearCurrent >= status.stats.fearThreshold * 0.1f)
                        StartFleeing();
                }
                else if (dis > status.stats.attackRange && Time.time - lastRoarTime >= 15f)  // 거리가 가깝지만 공격사거리 밖이라면
                {
                    ChangeState(DinoState.ROAR);
                }
                else if (dis <= status.stats.attackRange) // 공격사거리 안이라면
                {
                    if (!status.IsAfraid())
                        ChangeState(DinoState.ATTACKING);
                    else
                        StartFleeing();
                }
                else
                {
                    StartFleeing();
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
        if (!IsLive(status.target))
        {
            ResetTarget();
            chaseTime = 0f;
            return;
        }

        agent.destination = status.target.position;
        agent.speed = status.stats.runSpeed;

        if (Vector3.Distance(status.target.position, transform.position) <= status.stats.attackRange)
        {
            agent.destination = transform.position;
            ChangeState(DinoState.ATTACKING);
            chaseTime = 0f;
        }
        else
        {
            chaseTime += 5f * ((status.hungerCurrent) / status.stats.hungerMax) * Time.deltaTime;
            chaseTime += status.meat != null ? 2f : 0f;
            if (chaseTime > 20f)
            {
                chaseTime = 0f;
                ResetTarget();
                return;
            }
        }
    }

    public virtual void Roar()  // 포효
    {
        lastRoarTime = Time.time;
        animator.SetTrigger(_aniRoar);

        if (!isAnimating)
        {
            if (status.IsAfraid())                      // 공포상태라면 도망
                StartFleeing();
            // 포효 상태에서 적이 공격사거리에 들어오면 공격하기
            else if (status.fearOrigin != null && Vector3.Distance(transform.position, status.fearOrigin.position) <= status.stats.attackRange)
            {
                ChangeState(DinoState.ATTACKING);
            }
            else if (status.fearCurrent > 0)                                    // 그것도 다 아니라면 자리에서 벗어나기
                StartFleeing();
        }
    }

    public virtual void Attack()    // 공격
    {
        if (status.target == null)
        {
            if (status.stats.isFoodMeat)
            {
                ResetTarget();
                return;
            }
        }
        else
        {
            if (!IsLive(status.target))
            {
                ResetTarget();
                return;
            }
            RotateSmoothly(status.target.position - transform.position);
        }

        if (!isAnimating)
        {
            if (status.stats.isFoodMeat == false) // 초식
            {
                if (status.fearOrigin != null)
                {
                    if (status.IsAfraid())
                        StartFleeing();
                    else if (Vector3.Distance(transform.position, status.fearOrigin.position) <= status.stats.attackRange)
                        ChangeState(DinoState.ATTACKING);
                    else
                        ChangeState(DinoState.SEARCHING);
                }
                else if (!IsLive(status.fearOrigin))
                {
                    ResetTarget();
                    return;
                }
            }
            else                            // 육식
            {
                if (status.fearOrigin != null && status.IsAfraid())   // 공포원인이 있고 공포에 도달했다면 도망
                    StartFleeing();
                else if (status.target != null)                       // 그런거 없고 공격중인 타겟이 있다면
                {
                    if (Vector3.Distance(transform.position, status.target.position) <= status.stats.attackRange)
                        ChangeState(DinoState.ATTACKING);
                    else
                        ChangeState(DinoState.CHASING);
                }
            }
        }
    }

    public virtual void Death()
    {
        if (!status.isDie)
        {
            isAnimating = true;
            status.isDie = true;
            status.hpCurrent = status.stats.hpMax;
            agent.ResetPath();
            agent.avoidancePriority = 1;    // 죽은 시체는 밀리지 않게
            animator.SetTrigger(_aniHurt);
            animator.SetTrigger(_aniDeath);
        }
    }
    protected void StartFleeing()       // 도망치라고 명령
    {
        Vector3 fleeDir = (transform.position - status.fearOrigin.position).normalized;

        if (currentState != DinoState.FLEEING)  // 처음 도망갈 때
        {
            // 도망 시작시 주변에 공포 전파
            Collider[] dinos = Physics.OverlapSphere(transform.position, 30f, LayerMask.GetMask("Dinosaur"));
            foreach (Collider col in dinos)
            {
                if (col.gameObject == gameObject) continue; // 자기 자신 제외
                if (col.TryGetComponent<DinoStatus>(out DinoStatus stat))
                {
                    if (stat != null && stat.stats.threat == status.stats.threat && stat.fearCurrent == 0)
                    {
                        stat.AddFear(10, status.fearOrigin);
                    }
                }
            }

            agent.SetDestination(transform.position + fleeDir * status.stats.fleeDistance);
            ChangeState(DinoState.FLEEING);
        }
        else                                    // 도망가다 다른 적을 만났을 때 서로의 위치의 중간값으로 도망가도록
        {
            agent.SetDestination(agent.destination + fleeDir * status.stats.fleeDistance);
            ChangeState(DinoState.FLEEING);
        }
    }

    public virtual void Hit()
    {
        if (!isAnimating)
        {
            isAnimating = true;
            animator.SetTrigger(_aniHurt);
        }

        if (!status.stats.isFoodMeat)  // 초식이면 공포 원인으로 부터 공포 받음
            status.AddFear(status.stats.hpMax - status.hpCurrent, status.fearOrigin);
        else if (status.fearOrigin != null) // 육식인데 공포 원인이 있으면 공포 원인으로 부터 공포 받음
            status.AddFear(status.stats.hpMax - status.hpCurrent, status.fearOrigin);
        else                                // 육식인데 공포 원인이 없으면 타겟한테 공포 받음
            status.AddFear(status.stats.hpMax - status.hpCurrent, status.target);

        if (status.hpCurrent <= 0)
        {
            ChangeState(DinoState.DEATH);
        }
    }

    public void ChangeState(DinoState newState)
    {
        ResetAnimationTrigger();
        if (newState == DinoState.ATTACKING || newState == DinoState.ROAR)
            isAnimating = true;
        currentState = newState;

        print($"{gameObject.name} : {newState.ToString()} . . . ({Time.time})");
    }

    protected bool IsLive(Transform dinoTr)
    {
        if (dinoTr == null)
        {
            return false;
        }
        DinoStatus stat = dinoTr.GetComponent<DinoStatus>();
        if (stat != null && stat.isDie)
        {
            return false;
        }
        return true;
    }

    protected void ResetTarget()
    {
        if (currentState == DinoState.SEARCHING)
            ChangeState(DinoState.IDLE);
        else
            ChangeState(DinoState.SEARCHING);

        agent.ResetPath();
        status.target = null;
        status.fearOrigin = null;
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
        Debug.Log($"{gameObject.name} isAnimating : {isAnimating}, currentState : {stateName}");
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
        float rotSpeed = slowTurn ? status.stats.rotationSpeed * 0.5f : status.stats.rotationSpeed;
        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 10f * status.stats.rotationSpeed * Time.deltaTime);
    }


    void MoveWithSteering()
    {
        if (agent.pathPending || !agent.hasPath) return;

        agent.nextPosition = transform.position;

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
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 10f * status.stats.rotationSpeed * Time.deltaTime);

        // 현재 정면과 목표 방향의 각도 차이
        float angle = Vector3.Angle(transform.forward, moveDir);

        // 각도가 작을수록 빠르게, 클수록 느리게 전진
        float alignmentFactor = Mathf.Clamp01(1f - (angle / 90f)); // 0~90도 기준으로 보정
        float currentSpeed = agent.speed * alignmentFactor;

        // 이동
        transform.position += transform.forward * currentSpeed * Time.deltaTime;
    }

     IEnumerator WaitAndInitAgent()
    {
        // 최대 0.5초까지 NavMesh에 올라올 때까지 대기
        float timeout = 0.5f;
        while (!agent.isOnNavMesh && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.avoidancePriority = status.stats.pp;
            Debug.Log($"[DinoInit] {gameObject.name} NavMeshAgent initialized after wait.");
        }
        else
        {
            Debug.LogError($"[DinoInit] {gameObject.name} failed to initialize NavMeshAgent: not on NavMesh.");
        }
    }


}
