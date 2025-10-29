using UnityEngine;

public class BulletProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifeTime = 2f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Fire(Vector3 direction)
    {
        gameObject.SetActive(true);
        rb.velocity = direction * speed;
        Invoke(nameof(Deactivate), lifeTime); // 일정 시간 후 비활성화
    }

    private void Deactivate()
    {
        rb.velocity = Vector3.zero;
        gameObject.SetActive(false);
        BulletPoolingManager.Instance.ReturnBullet(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 피격 처리 로직 등
        Deactivate();
    }
}
