using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DinoAttack : MonoBehaviour
{
    DinoStatus status;
    public float damage;
    Rigidbody rb;

    List<DinoDamage> hitList = new List<DinoDamage>();

    void Start()
    {
        rb = gameObject.AddComponent<Rigidbody>();  // 충돌에 필요한 RigidBody 생성
        rb.isKinematic = true;
        status = GetComponentInParent<DinoStatus>();
        damage = status.attackDamage;
    }

    private void OnTriggerEnter(Collider other)
    {
        var enemy = other.GetComponentInParent<DinoDamage>();
        if (enemy == null)
            return;
        else if (!hitList.Contains(enemy))
        {
            CancelInvoke("ResetAttack");
            Invoke("ResetAttack", 1f);
            enemy.Damage(other, damage);
            hitList.Add(enemy);
        }
    }

    void ResetAttack()
    {
        hitList.Clear();
    }
}
