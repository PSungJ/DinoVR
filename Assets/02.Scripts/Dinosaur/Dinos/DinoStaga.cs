using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DinoStaga : DinoBase
{

    public override void Idle()
    {
        base.Idle();
    }

    public override void Roam()
    {
        base.Roam();
    }

    public override void Eating()
    {
        base.Eating();
    }

    public override void Drink()
    {
        base.Drink();
    }

    public override void Sleeping()
    {
        base.Sleeping();
    }

    public override void Fleeing()
    {
        base.Fleeing();
    }

    public override void Searching()
    {
        base.Searching();
    }

    public override void Attack()
    {
        agent.isStopped = true;
        Vector3 toTarget = status.fearOrigin.position - transform.position;
        float dot = Vector3.Dot(transform.forward, toTarget.normalized);
        if (dot > 0f)   // 포식자가 내 앞에 있음
        {
            RotateSmoothly((status.fearOrigin.position - transform.position).normalized);
            animator.ResetTrigger(_aniAttack1);
            animator.SetTrigger(_aniAttack);        // 공격 애니메이션 재생
        }
        else
        {
            RotateSmoothly((transform.position - status.fearOrigin.position).normalized);
            animator.ResetTrigger(_aniAttack);
            animator.SetTrigger(_aniAttack1);        // 공격 애니메이션 재생
        }

        if (!isAnimating)
        {
            if (status.fearOrigin != null)
            {
                if (status.IsAfraid())
                    StartFleeing();
            }
        }
    }

    public override void Roar()
    {
        base.Roar();
    }

    public override void Death()
    {
        base.Death();
    }
}
