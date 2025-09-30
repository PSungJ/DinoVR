using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DinoRaptor : DinoBase
{

    public override void Searching()
    {
        agent.isStopped = true;
        
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
                    ChangeState(DinoState.CALL);
                    isAnimating = true;
                }
            }
        }
    }

    public override void Attack()
    {
        base.Attack();
        animator.SetTrigger(_aniAttack);        // 공격 애니메이션 재생
    }

    public override void Call()
    {
        animator.SetTrigger(_aniCall);
        agent.isStopped = true;
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
        Collider[] raptors = Physics.OverlapSphere(transform.position, 30f, LayerMask.GetMask("Dinosaur"));
        foreach (Collider col in raptors)
        {
            if (col.gameObject == gameObject) continue; // 자기 자신 제외
            if (col.TryGetComponent<DinoStatus>(out DinoStatus stat))
            {
                if (stat.threat == status.threat)
                {
                    col.TryGetComponent<DinoBase>(out DinoBase raptor);
                    if (raptor.currentState != DinoState.CALL && raptor.currentState != DinoState.CHASING)
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