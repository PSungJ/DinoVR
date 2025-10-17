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

        if (status.fearCurrent == 0 && status.target != null) // 공포가 0 이라면 == 사냥
        {
            float dis = (status.target.position - transform.position).magnitude;
            if (dis > status.detactRange / 2f)  // 멀리서 접근하는 걸 발견했다면 바라보기
            {
                RotateSmoothly(status.target.position - transform.position);
                currentAttackTime += Time.deltaTime;
                if (currentAttackTime >= toAttackTime)
                {
                    DinoStatus targetStat = status.target.GetComponent<DinoStatus>();
                    if (targetStat != null)
                    {
                        if(targetStat.fearCurrent == 0)
                        {
                            currentAttackTime = 0f;
                            ChangeState(DinoState.SNEAK);
                        }
                        else
                        {
                            currentAttackTime = 0f;
                            ChangeState(DinoState.CHASING);
                        }
                    }
                }
            }
        }
        else if (status.fearOrigin != null)
        {
            float dis = (status.fearOrigin.position - transform.position).magnitude;
            if (dis > status.detactRange / 2f)  // 멀리서 접근하는 걸 발견했다면 바라보기
            {
                RotateSmoothly(status.fearOrigin.position - transform.position);
                if (status.IsAfraid())
                    StartFleeing();
            }
            else if (dis > status.attackRange)  // 거리가 가깝지만 공격사거리 밖이라면 포효로 경고하기
            {
                ChangeState(DinoState.ROAR);
            }
        }
        else
        {
            ChangeState(DinoState.IDLE);
        }
    }

    public override void Sneak()
    {
        agent.isStopped = false;
        if (status.target == null)
        {
            ChangeState(DinoState.IDLE);
            return;
        }
        agent.speed = status.walkSpeed / 2;
        agent.destination = status.target.position;
        DinoStatus targetStat = status.target.GetComponent<DinoStatus>();
        if (targetStat != null)
        {
            if (targetStat.fearCurrent > 0)
            {
                ChangeState(DinoState.CHASING);
            }
        }
    }

    public override void Attack()
    {
        base.Attack();
        if (Vector3.Distance(transform.position, status.target.position) > status.attackRange * 0.8f)
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