using UnityEngine;

/// <summary>
/// 플레이어의 체력 및 마나를 관리하는 컴포넌트입니다.
/// 아이템 사용 로직의 목표가 됩니다.
/// </summary>
public class PlayerHealthComponent : MonoBehaviour
{
    public static PlayerHealthComponent Instance { get; private set; }

    [Header("Current Stats")]
    [SerializeField] private int currentHealth = 100;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentMana = 50;
    [SerializeField] private int maxMana = 50;

    // -----------------------------------------------------------------
    [Header("Effects")]
    [Tooltip("체력 회복 시 재생할 파티클 시스템 프리팹")]
    [SerializeField] private GameObject healParticlePrefab; // ⭐ 추가된 파티클 필드
    // -----------------------------------------------------------------

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 필요에 따라 추가
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"[PlayerHealth] 체력 회복 (+{amount}). 현재 체력: {currentHealth}");

        // ⭐ 파티클 시스템 실행 로직 추가
        PlayHealEffect();
    }

    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0);
        Debug.Log($"[PlayerHealth] 피해 (-{amount}). 현재 체력: {currentHealth}");
    }

    public void RestoreMana(int amount)
    {
        currentMana = Mathf.Min(currentMana + amount, maxMana);
        Debug.Log($"[PlayerHealth] 마나 회복 (+{amount}). 현재 마나: {currentMana}");
    }

    /// <summary>
    /// 힐링 파티클 프리팹을 인스턴스화하고 재생합니다.
    /// </summary>
    private void PlayHealEffect()
    {
        if (healParticlePrefab != null)
        {
            // 1. 플레이어 위치에 파티클 프리팹을 생성합니다.
            GameObject effectInstance = Instantiate(
                healParticlePrefab,
                transform.position,
                Quaternion.identity,
                transform // 플레이어 오브젝트의 자식으로 설정하여 함께 움직이도록 할 수 있습니다. (옵션)
            );

            // 2. 파티클 시스템 컴포넌트를 가져옵니다.
            ParticleSystem ps = effectInstance.GetComponent<ParticleSystem>();

            if (ps != null)
            {
                // 파티클이 끝나면 자동으로 제거되도록 설정 (옵션)
                Destroy(effectInstance, ps.main.duration + ps.main.startLifetimeMultiplier);
            }
            else
            {
                // 파티클 컴포넌트가 없다면, 5초 후 수동으로 제거
                Destroy(effectInstance, 5f);
            }
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] healParticlePrefab이 할당되지 않았습니다. Inspector를 확인하세요.");
        }
    }
}
