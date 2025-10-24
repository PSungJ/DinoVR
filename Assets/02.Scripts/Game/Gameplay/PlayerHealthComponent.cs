using UnityEngine;
using Game.Foundation;

namespace Game.Gameplay
{
    /// <summary>
    /// 플레이어의 체력 및 마나를 관리하는 시스템.
    /// Consumable 아이템 사용, 상태 효과 적용 등의 목표 대상입니다.
    /// </summary>
    public class PlayerHealthComponent : MonoBehaviour, IPlayerHealthService
    {
        [Header("Stats")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth = 100;

        [SerializeField] private int maxMana = 50;
        [SerializeField] private int currentMana = 50;

        [Header("Visuals")]
        [Tooltip("플레이어 자식에 있는 체력 회복용 파티클 시스템")]
        [SerializeField] private ParticleSystem healEffect;

        private void Awake()
        {
            // 서비스 등록 (ServiceLocator)
            ServiceLocator.Register<IPlayerHealthService>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IPlayerHealthService>(this);
        }

        // ------------------------------------------------------------
        // ✅ IPlayerHealthService 구현부
        // ------------------------------------------------------------

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public int CurrentMana => currentMana;
        public int MaxMana => maxMana;

        public void Heal(int amount)
        {
            if (amount <= 0) return;
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            Debug.Log($"[PlayerHealth] +{amount} HP (Current: {currentHealth})");
            PlayHealEffect();
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0) return;
            currentHealth = Mathf.Max(currentHealth - amount, 0);
            Debug.Log($"[PlayerHealth] -{amount} HP (Current: {currentHealth})");
        }

        public void RestoreMana(int amount)
        {
            if (amount <= 0) return;
            currentMana = Mathf.Min(currentMana + amount, maxMana);
            Debug.Log($"[PlayerHealth] +{amount} Mana (Current: {currentMana})");
        }

        public void ConsumeMana(int amount)
        {
            if (amount <= 0) return;
            currentMana = Mathf.Max(currentMana - amount, 0);
            Debug.Log($"[PlayerHealth] -{amount} Mana (Current: {currentMana})");
        }

        // ------------------------------------------------------------
        // 🎇 시각 효과
        // ------------------------------------------------------------

        private void PlayHealEffect()
        {
            if (healEffect == null)
            {
                Debug.LogWarning("[PlayerHealth] Heal particle effect not assigned.");
                return;
            }

            healEffect.Play();
        }
    }
}
