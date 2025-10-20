using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DinoDamage : MonoBehaviour
{
    public List<Collider> colliders;

    DinoStatus status;
    DinoBase dino;

    void Start()
    {
        status = GetComponent<DinoStatus>();
        dino = GetComponent<DinoBase>();
        GetComponentsInChildren<Collider>(colliders);
        colliders.RemoveAt(0);
        foreach (Collider col in colliders)
        {
            if(col.GetComponent<DinoAttack>() == null)
                col.isTrigger = false;
        }
    }

    public void Damage(Collider col, float damage, bool isPlayer = false)
    {
        if (col.tag == "Head")
            damage *= 1.5f;
        status.hpCurrent -= damage;
        dino.Hit();
        Debug.Log($"{col.name} 에 맞음");

        foreach (Collider c in colliders)
        {
            if (c.GetComponent<DinoAttack>() == null)
                c.enabled = false;
        }
                        // 상대가 플레이어 라면
        if (isPlayer)
        {
            //status.fearOrigin = player
        }
    }
}
