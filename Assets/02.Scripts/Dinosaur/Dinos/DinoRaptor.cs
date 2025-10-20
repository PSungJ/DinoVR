using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class DinoRaptor : DinoBase
{

    public override void Searching()
    {
        if (agent.hasPath)
            agent.ResetPath();

        if(status.hungerCurrent > status.hungerMax / 2f)
        {
            if (status.target != null)
            {
                status.AddFear(1f, status.target);
            }
        }

        if (status.fearCurrent <= 0 && status.target != null) // 공포가 0 이고, 타겟이 존재하면
        {
            if (!IsLive(status.target)) // 타겟이 죽어있다면 리턴
            {
                ResetTarget();
                return;
            }
            float dis = (status.target.position - transform.position).magnitude;
            if (dis > status.detactRange / 2f)  // 멀리서 접근하는 걸 발견했다면 바라보기
            {
                RotateSmoothly(status.target.position - transform.position);
                currentAttackTime += Time.deltaTime;
                if (currentAttackTime >= toAttackTime)
                {
                    currentAttackTime = 0f;
                    ChangeState(DinoState.CALL);
                    isAnimating = true;
                }
            }
        }
        else if (status.fearOrigin != null && status.fearCurrent > 0)
        {
            float dis = (status.fearOrigin.position - transform.position).magnitude;
            if (dis > status.detactRange * 0.9f)  // 멀리서 접근하는 걸 발견했다면 바라보기
            {
                RotateSmoothly(status.fearOrigin.position - transform.position);
                if (status.IsAfraid())
                    StartFleeing();
                else if (status.fearCurrent >= status.fearThreshold * 0.2f)
                    ChangeState(DinoState.ROAR);
            }
            else if (dis > status.attackRange)  // 거리가 가깝지만 공격사거리 밖이라면 포효로 경고하기
            {
                ChangeState(DinoState.ROAR);
            }
        }
        else if (status.fearCurrent <= 0)
        {
            ResetTarget();
        }
    }

    float lastAttackTime = 0f;

    public override void Chasing()
    {
        agent.speed = status.runSpeed;

        if (status.IsAfraid())
        {
            StartFleeing();
            return;
        }

        if (status.target == null)
        {
            ChangeState(DinoState.IDLE);
            return;
        }
        if (Time.time - lastAttackTime > 5f)        // 마지막 공격으로 부터 5초가 넘었다면 추격 후 공격
        {
            if (Vector3.Distance(status.target.position, transform.position) <= status.attackRange)
            {
                agent.SetDestination(transform.position);
                ChangeState(DinoState.ATTACKING);
            }
            else
                agent.SetDestination(status.target.position);
        }
        else if (!agent.hasPath)                    // 공격 후 5초간 랜덤 좌표 배회
        {
            agent.destination = GetRandomPoint(transform.position,20f);
        }
    }


    public override void Attack()    // 공격
    {
        if (status.target == null)
        {
            ChangeState(DinoState.IDLE);
            return;
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

        animator.SetTrigger(_aniAttack);        // 공격 애니메이션 재생

        if (!isAnimating)
        {
            if (status.fearOrigin != null && status.IsAfraid())   // 공포원인이 있고 공포에 도달했다면 도망
                StartFleeing();
            else if (status.target != null)                       // 그런거 없고 공격중인 타겟이 있다면
            {
                lastAttackTime = Time.time;
                ChangeState(DinoState.CHASING);
            }
        }
    }

    public override void Call()
    {
        animator.SetTrigger(_aniCall);
        if (status.target == null)
        {
            ChangeState(DinoState.IDLE);
            animator.ResetTrigger(_aniCall);
        }
        else if (isAnimating == false)
        {
            ChangeState(DinoState.CHASING);
            animator.ResetTrigger(_aniCall);
        }
    }

    public void Calling()
    {
        Collider[] raptors = Physics.OverlapSphere(transform.position, 80f, LayerMask.GetMask("Dinosaur"));
        foreach (Collider col in raptors)
        {
            if (col.gameObject == gameObject) continue; // 자기 자신 제외
            if (col.TryGetComponent<DinoStatus>(out DinoStatus stat))
            {
                if (stat.threat == status.threat)
                {
                    col.TryGetComponent<DinoBase>(out DinoBase raptor);
                    if (raptor.currentState != DinoState.CALL && raptor.currentState != DinoState.CHASING && raptor.currentState != DinoState.ATTACKING)
                    {
                        stat.target = status.target;
                        raptor.ChangeState(DinoState.CALL);
                        raptor.isAnimating = true;
                    }
                }
            }
        }
    }
}