using UnityEngine;
using Game.Foundation;
using Game.Gameplay;

namespace Game.Gameplay
{
    /// <summary>
    /// 플레이어의 체력 및 마나를 관리하는 컴포넌트.
    /// ServiceLocator 기반으로 아이템/스킬 시스템에서 접근됩니다.
    /// </summary>
    public class PlayerHealthComponent : MonoBehaviour, IPlayerHealthService
    {
        [Header("Current Stats")]
        [SerializeField] private int currentHealth = 100;
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentMana = 50;
        [SerializeField] private int maxMana = 50;

        [Header("Effects")]
        [Tooltip("씬에 존재하는 Heal 파티클 시스템 (프리팹 아님)")]
        [SerializeField] private ParticleSystem healParticle;

        private void Awake()
        {
            ServiceLocator.Register<IPlayerHealthService>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IPlayerHealthService>(this);
        }

        // ----------------------------------------------------
        // [체력 관련 메서드]
        // ----------------------------------------------------
        public void Heal(int amount)
        {
            if (amount <= 0) return;

            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            Debug.Log($"[PlayerHealth] 체력 +{amount}, 현재 체력: {currentHealth}");

            PlayHealEffect();
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0) return;

            currentHealth = Mathf.Max(0, currentHealth - amount);
            Debug.Log($"[PlayerHealth] 피해 -{amount}, 현재 체력: {currentHealth}");
        }

        // ----------------------------------------------------
        // [마나 관련 메서드]
        // ----------------------------------------------------
        public void RestoreMana(int amount)
        {
            if (amount <= 0) return;

            currentMana = Mathf.Min(currentMana + amount, maxMana);
            Debug.Log($"[PlayerHealth] 마나 +{amount}, 현재 마나: {currentMana}");
        }

        // ----------------------------------------------------
        // [파티클 재생]
        // ----------------------------------------------------
        private void PlayHealEffect()
        {
            if (healParticle != null)
            {
                healParticle.Play();
            }
            else
            {
                Debug.LogWarning("[PlayerHealth] Heal 파티클이 할당되지 않았습니다.");
            }
        }
    }
}
