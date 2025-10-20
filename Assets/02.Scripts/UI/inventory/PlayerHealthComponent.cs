using UnityEngine;

// 아이템 사용 효과 구현을 위해 가정된 플레이어 체력 관리 컴포넌트입니다.
public class PlayerHealthComponent : MonoBehaviour
{
    public static PlayerHealthComponent Instance { get; private set; }

    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        currentHealth = maxHealth;
        Debug.Log("[PlayerHealthComponent] Initialized. Current Health: " + currentHealth);
    }

    /// <summary>
    /// 체력을 회복합니다. ConsumableItemSO에서 호출됩니다.
    /// </summary>
    public void Heal(int amount)
    {
        if (amount <= 0) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"[PlayerHealthComponent] Healed by {amount}. Current Health: {currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// 체력을 감소시킵니다.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        currentHealth -= amount;
        Debug.Log($"[PlayerHealthComponent] Took {amount} damage. Remaining Health: {currentHealth}/{maxHealth}");
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("[PlayerHealthComponent] Player Died.");
    }
}
