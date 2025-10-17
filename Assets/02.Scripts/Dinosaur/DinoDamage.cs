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

    public void Damage(Collider col, float damage)
    {
        if (col.tag == "Head")
            damage *= 1.5f;
        status.hpCurrent -= damage;
        dino.Hit();
        Debug.Log($"{col.name} ¿¡ ¸ÂÀ½");

        foreach (Collider c in colliders)
        {
            if (c.GetComponent<DinoAttack>() == null)
                c.enabled = false;
        }
    }
}
