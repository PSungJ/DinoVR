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
    [Tooltip("플레이어 자식 오브젝트에 이미 부착된 체력 회복 파티클 시스템")]
    // ⭐ 프리팹이 아닌, 씬에 존재하는 ParticleSystem 컴포넌트 자체를 할당해야 합니다.
    [SerializeField] private ParticleSystem healParticlePrefab;
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

        // ⭐ 파티클 시스템 실행 로직 호출
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
    /// 할당된 파티클 시스템을 재생합니다. (인스턴스화 대신)
    /// </summary>
    private void PlayHealEffect()
    {
        if (healParticlePrefab != null)
        {
            // 씬에 이미 존재하는 파티클 시스템 컴포넌트를 바로 재생합니다.
            // (이미 플레이어의 자식으로 위치가 고정되어 있다고 가정합니다.)
            healParticlePrefab.Play();
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] healParticlePrefab이 할당되지 않았습니다. Inspector를 확인하세요. 씬의 Player 오브젝트 자식에 있는 ParticleSystem을 할당해야 합니다.");
        }
    }
}
