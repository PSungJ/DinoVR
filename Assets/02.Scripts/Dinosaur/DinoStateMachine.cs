using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DinoStateMachine : StateMachineBehaviour
{
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        //animator.gameObject.GetComponent<DinoBase>().isAnimating = true;
        var dino = animator.GetComponent<DinoBase>();
        if (dino != null)
        {
            dino.SetAnimate(true, stateInfo.shortNameHash.ToString()); // 현재 상태 이름을 반환
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        //animator.gameObject.GetComponent<DinoBase>().isAnimating = false;
        var dino = animator.GetComponent<DinoBase>();
        if (dino != null)
        {
           dino.SetAnimate(false, stateInfo.shortNameHash.ToString());
        }
    }
}