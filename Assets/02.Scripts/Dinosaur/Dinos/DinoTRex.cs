using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class DinoTrex : DinoBase
{

    protected override void UpdateAnimator()
    {
        base.UpdateAnimator();
        animator.SetBool(_aniSneak, currentState == DinoState.SNEAK);
    }

    public override void Searching()
    {
        if(agent.hasPath)
            agent.ResetPath();

        searchingTime += Time.deltaTime;

        if (status.hungerCurrent > status.stats.hungerMax / 2f || searchingTime >= 10f)
        {
            if (status.target != null)
            {
                status.AddFear(1f, status.target);
            }
        }

        if (status.fearCurrent == 0 && status.target != null) // 공포가 0 이라면 == 사냥
        {
            if (!IsLive(status.target)) // 타겟이 죽어있다면 리턴
            {
                ResetTarget();
                return;
            }
            RotateSmoothly(status.target.position - transform.position);
            currentAttackTime += Time.deltaTime;
            if (currentAttackTime >= toAttackTime)
            {
                currentAttackTime = 0f;
                float dis = (status.target.position - transform.position).magnitude;
                if (status.meat != null && dis > status.stats.detactRange / 2f)    // 먹을게 있는데 멀리서 다가온다면
                {
                    if(Time.time - lastRoarTime >= 10f)
                        ChangeState(DinoState.ROAR);
                }
                else
                {
                    DinoStatus targetStat = status.target.GetComponent<DinoStatus>();
                    if (targetStat != null)
                    {
                        if (targetStat.fearCurrent == 0)
                            ChangeState(DinoState.SNEAK);
                        else
                            ChangeState(DinoState.CHASING);
                    }
                    else    // 목표가 사람일 경우
                    {
                        ChangeState(DinoState.SNEAK);
                    }
                }
            }
            
        }
        else if (status.fearOrigin != null && status.fearCurrent > 0)
        {
            float dis = Vector3.Distance(status.fearOrigin.position, transform.position);
            if (dis > status.stats.detactRange * 0.8f)  // 멀리서 접근하는 걸 발견했다면 바라보기
            {
                RotateSmoothly(status.fearOrigin.position - transform.position);
                if (status.IsAfraid())
                    StartFleeing();
                else if (status.fearCurrent >= (status.stats.fearThreshold * 0.05f) && Time.time - lastRoarTime >= 10f)
                {
                    ChangeState(DinoState.ROAR);
                    DinoStatus fearStat = status.fearOrigin.GetComponent<DinoStatus>();
                    if (fearStat != null)
                    {
                        fearStat.AddFear(status.stats.threat * 100f, transform);
                    }
                }
                //Debug.Log($"{status.fearCurrent}, {status.fearThreshold * 0.05f}, {Time.time - lastRoarTime}");
            }
            else if (dis > status.stats.attackRange * 2f)  // 거리가 가깝지만 공격사거리 밖이라면 포효로 경고하기
            {
                if (status.IsAfraid())
                    StartFleeing();
                else if (Time.time - lastRoarTime >= 10f)
                    ChangeState(DinoState.ROAR);
                else
                {
                    RotateSmoothly(status.target.position - transform.position);
                    currentAttackTime += Time.deltaTime;
                    if (currentAttackTime >= toAttackTime)
                    {
                        currentAttackTime = 0f;
                        status.target = status.fearOrigin;
                        ChangeState(DinoState.CHASING);
                    }
                }
            }
            else
            {
                ChangeState(DinoState.CHASING);
            }
        }
        else if (status.fearCurrent <= 0)
        {
            ResetTarget();
        }
    }

    public override void Roar()  // 포효
    {
        lastRoarTime = Time.time;
        animator.SetTrigger(_aniRoar);

        Collider[] dinos = Physics.OverlapSphere(transform.position, 80f, LayerMask.GetMask("Dinosaur"));
        foreach (Collider col in dinos)
        {
            if (col.gameObject == gameObject) continue; // 자기 자신 제외
            DinoStatus stat = col.GetComponent<DinoStatus>();
            if (stat != null)
            {
                if (stat.stats.threat < status.stats.threat)
                {
                    stat.AddFear(status.stats.threat , transform);
                }
            }
        }

        if (!isAnimating)
        {
            if (status.IsAfraid())                      // 공포상태라면 도망
                StartFleeing();
            // 포효 상태에서 적이 공격사거리에 들어오면 공격하기
            else if (status.fearOrigin != null && Vector3.Distance(transform.position, status.fearOrigin.position) <= status.stats.attackRange)
            {
                ChangeState(DinoState.ATTACKING);
            }
            else if (status.target != null && Vector3.Distance(transform.position, status.target.position) <= status.stats.attackRange)
            {
                ChangeState(DinoState.ATTACKING);
            }
            else                                    // 그것도 다 아니라면 기본으로 돌아가기
            {
                ResetTarget();
            }
        }
    }

    public override void Sneak()
    {
        if (status.target == null)
        {
            ChangeState(DinoState.IDLE);
            return;
        }
        else if (Vector3.Distance(status.target.position, transform.position) > status.stats.awareness)   // 너무 멀어지면 포기
        {
            chaseTime += ((status.hungerCurrent) / status.stats.hungerMax) * Time.deltaTime;
            chaseTime += status.meat != null ? 2f : 0f;
            if (chaseTime > 20f)
            {
                chaseTime = 0f;
                ResetTarget();
                return;
            }
        }
        agent.speed = status.stats.walkSpeed / 2;
        agent.destination = status.target.position;
        DinoStatus targetStat = status.target.GetComponent<DinoStatus>();
        if (targetStat != null)
        {
            if (targetStat.fearCurrent > 0)
            {
                ChangeState(DinoState.CHASING);
            }
        }
        else        // 사람이라면
        {

        }
    }

    public override void Attack()
    {
        base.Attack();
        if (Vector3.Distance(transform.position, status.target.position) > status.stats.attackRange * 0.8f)
        {
            animator.SetTrigger(_aniAttack);        // 공격 애니메이션 재생
            if (!agent.hasPath)
                agent.destination = status.target.position;
        }
        else
        {
            animator.SetTrigger(_aniAttack1);
            if (agent.hasPath)
                agent.ResetPath();
        }
    }
}