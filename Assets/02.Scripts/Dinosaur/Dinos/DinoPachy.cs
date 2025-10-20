using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DinoPachy : DinoBase
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
        base.Attack();
        agent.ResetPath();
        animator.SetTrigger(_aniAttack);
        if (status.fearOrigin != null)
            RotateSmoothly(status.fearOrigin.position - transform.position);
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
