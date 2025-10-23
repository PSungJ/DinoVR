using UnityEngine;
using System.Collections.Generic;
using Game.Foundation;
using Game.Gameplay;

namespace Game.Gameplay
{
    /// <summary>
    /// 플레이어의 체력, 스태미나, 공격력 및 상태 이상을 관리하는 클래스.
    /// 장비 효과나 소비 아이템 사용 시 수치가 갱신됩니다.
    /// </summary>
    public class PlayerStats : MonoBehaviour, IPlayerStatsService
    {
        [Header("Base Stats")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private int baseAttack = 10;

        private float currentHealth;
        private float currentStamina;
        private int equipmentAttackBonus = 0;

        [Header("UI Reference")]
        [SerializeField] private StatusUI statusUI;

        // 상태 이상 관리
        private readonly HashSet<StatusEffectType> activeStatusEffects = new();
        private readonly Dictionary<StatusEffectType, float> effectDurations = new();

        public float CurrentHealth => currentHealth;
        public float CurrentStamina => currentStamina;
        public int FinalAttack => baseAttack + equipmentAttackBonus;

        private void Awake()
        {
            ServiceLocator.Register<IPlayerStatsService>(this);
            currentHealth = maxHealth;
            currentStamina = maxStamina;
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IPlayerStatsService>(this);
        }

        private void Update()
        {
            CheckStatusEffectDuration();
        }

        // ----------------------------------------------------
        // [기본 수치 계산]
        // ----------------------------------------------------
        public float GetHealthPercentage() => currentHealth / maxHealth;
        public float GetStaminaPercentage() => currentStamina / maxStamina;

        // ----------------------------------------------------
        // [장비 효과 적용/해제]
        // ----------------------------------------------------
        public void ApplyEquipmentModifiers(EquippableItemSO item)
        {
            equipmentAttackBonus += item.attackModifier;
            statusUI?.UpdateUI();
        }

        public void RemoveEquipmentModifiers(EquippableItemSO item)
        {
            equipmentAttackBonus -= item.attackModifier;
            statusUI?.UpdateUI();
        }

        // ----------------------------------------------------
        // [회복 및 데미지]
        // ----------------------------------------------------
        public void Restore(string effectType, float amount)
        {
            if (effectType == "Health")
                currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            else if (effectType == "Stamina")
                currentStamina = Mathf.Min(currentStamina + amount, maxStamina);

            statusUI?.UpdateUI();
        }

        public void TakeDamage(float amount)
        {
            currentHealth = Mathf.Max(0, currentHealth - amount);
            statusUI?.UpdateUI();
        }

        // ----------------------------------------------------
        // [상태 이상 처리]
        // ----------------------------------------------------
        public void ApplyStatus(StatusEffectType type, float duration)
        {
            if (!activeStatusEffects.Contains(type))
            {
                activeStatusEffects.Add(type);
                statusUI?.UpdateUI();
            }

            effectDurations[type] = Time.time + duration;
        }

        public void RemoveStatus(StatusEffectType type)
        {
            if (activeStatusEffects.Remove(type))
                statusUI?.UpdateUI();

            effectDurations.Remove(type);
        }

        public bool IsEffectActive(StatusEffectType type)
        {
            return activeStatusEffects.Contains(type);
        }

        private void CheckStatusEffectDuration()
        {
            List<StatusEffectType> expired = new();

            foreach (var pair in effectDurations)
            {
                if (Time.time > pair.Value)
                    expired.Add(pair.Key);
            }

            foreach (var type in expired)
                RemoveStatus(type);
        }
    }
}
