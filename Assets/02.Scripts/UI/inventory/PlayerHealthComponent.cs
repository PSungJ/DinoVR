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

    [Header("Effects")]
    [Tooltip("플레이어 자식 오브젝트에 이미 부착된 체력 회복 파티클 시스템")]
    [SerializeField] private ParticleSystem healParticlePrefab;

    // -----------------------------------------------------------------
    [Header("Shader Control")]
    [Tooltip("쉐이더 그래프에서 _FillAmount 속성이 있는 머티리얼")]
    [SerializeField] private Material healthMaterial;

    [Tooltip("쉐이더 속성 이름 (기본값: _FillAmount)")]
    [SerializeField] private string fillAmountProperty = "_FillAmount";
    // -----------------------------------------------------------------

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 필요 시 활성화
        }
        else
        {
            Destroy(gameObject);
        }

        UpdateHealthShader(); // 시작 시 값 반영
    }

    private void Update()
    {
        UpdateHealthShader();
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"[PlayerHealth] 체력 회복 (+{amount}). 현재 체력: {currentHealth}");

        UpdateHealthShader();
        PlayHealEffect();
    }

    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0);
        Debug.Log($"[PlayerHealth] 피해 (-{amount}). 현재 체력: {currentHealth}");

        UpdateHealthShader();
    }

    public void RestoreMana(int amount)
    {
        currentMana = Mathf.Min(currentMana + amount, maxMana);
        Debug.Log($"[PlayerHealth] 마나 회복 (+{amount}). 현재 마나: {currentMana}");
    }

    /// <summary>
    /// 할당된 파티클 시스템을 재생합니다.
    /// </summary>
    private void PlayHealEffect()
    {
        if (healParticlePrefab != null)
        {
            healParticlePrefab.Play();
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] healParticlePrefab이 할당되지 않았습니다.");
        }
    }

    /// <summary>
    /// 체력 비율을 쉐이더 머티리얼의 _FillAmount 속성에 반영합니다.
    /// </summary>
    private void UpdateHealthShader()
    {
        if (healthMaterial != null && healthMaterial.HasProperty(fillAmountProperty))
        {
            float ratio = (float)currentHealth / maxHealth;
            healthMaterial.SetFloat(fillAmountProperty, ratio);
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] healthMaterial이 없거나 _FillAmount 속성이 없습니다.");
        }
    }
}
