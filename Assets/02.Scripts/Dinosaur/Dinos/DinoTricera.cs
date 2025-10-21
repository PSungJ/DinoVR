using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DinoTricera : DinoBase
{
    public override void Attack()
    {
        base.Attack();
        animator.SetTrigger(_aniAttack);
        if (status.fearOrigin != null)
            RotateSmoothly(status.fearOrigin.position - transform.position);
    }
}
