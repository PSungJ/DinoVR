using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DinoDamage : MonoBehaviour
{
    public List<Collider> colliders;

    DinoStatus status;

    void Start()
    {
        status = GetComponent<DinoStatus>();
        GetComponentsInChildren<Collider>(colliders);
        colliders.RemoveAt(0);
        foreach (Collider col in colliders)
        {
            col.isTrigger = false;
        }
    }

    public void Damage(Collider col, float damage)
    {
        Debug.Log($"{col.name} ¿¡ ¸ÂÀ½");
    }
}
