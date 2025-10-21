using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DinoPachy : DinoBase
{
    public override void Attack()
    {
        base.Attack();
        agent.ResetPath();
        animator.SetTrigger(_aniAttack);
        if (status.fearOrigin != null)
            RotateSmoothly(status.fearOrigin.position - transform.position);
    }
}
