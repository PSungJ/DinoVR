using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DinoAttack : MonoBehaviour
{
    DinoStatus status;
    public float damage;
    Rigidbody rb;
    DinoBase dino;

    List<DinoDamage> hitList = new List<DinoDamage>();

    void Start()
    {
        rb = gameObject.AddComponent<Rigidbody>();  // 충돌에 필요한 RigidBody 생성
        rb.isKinematic = true;
        status = GetComponentInParent<DinoStatus>();
        dino = GetComponentInParent<DinoBase>();
        damage = status.attackDamage;
    }

    private void OnTriggerEnter(Collider other)
    {
        var enemy = other.GetComponentInParent<DinoDamage>();
        if (enemy == null)  // 상대가 공룡이 아니라면 리턴
            return;
        else 
        {
            var enemyStat = enemy.GetComponent<DinoStatus>();
            if (enemyStat.threat == status.threat)  // 상태가 공룡인데 같은 종이면 리턴
                return;
        }
        if (!hitList.Contains(enemy))
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
