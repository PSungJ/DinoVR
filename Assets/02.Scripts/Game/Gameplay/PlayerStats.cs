using System.Collections.Generic;
using UnityEngine;
using Game.Foundation;
using Game.InventorySystem;
using Game.UI.Interface;

namespace Game.Gameplay
{
    /// <summary>
    /// 플레이어의 능력치, 상태 이상, 장비 보너스 등을 관리하는 서비스입니다.
    /// </summary>
    public class PlayerStats : MonoBehaviour, IPlayerStatsService
    {
        [Header("Base Stats")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private int baseAttack = 10;

        [Header("Runtime Stats")]
        private float currentHealth;
        private float currentStamina;
        private int equipmentAttackBonus = 0;

        [Header("Status Effect Management")]
        private readonly HashSet<StatusEffectType> activeStatusEffects = new();
        private readonly Dictionary<StatusEffectType, float> effectDuration = new();

        [Header("Dependencies")]
        [SerializeField] private StatusUI statusUI;

        private void Awake()
        {
            currentHealth = maxHealth;
            currentStamina = maxStamina;
            ServiceLocator.Register<IPlayerStatsService>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IPlayerStatsService>(this);
        }

        // ------------------------------------------------------------
        // ✅ IPlayerStatsService 구현
        // ------------------------------------------------------------

        public float GetHealthPercentage() => currentHealth / maxHealth;
        public float GetStaminaPercentage() => currentStamina / maxStamina;
        public int GetFinalAttack() => baseAttack + equipmentAttackBonus;

        public bool IsEffectActive(StatusEffectType type) => activeStatusEffects.Contains(type);

        public void ApplyEquipmentModifiers(EquippableItemSO item)
        {
            equipmentAttackBonus += item.attackModifier;
            UpdateUI();
        }

        public void RemoveEquipmentModifiers(EquippableItemSO item)
        {
            equipmentAttackBonus -= item.attackModifier;
            UpdateUI();
        }

        public void Restore(string effectType, float amount)
        {
            switch (effectType)
            {
                case "Health":
                    currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
                    break;
                case "Stamina":
                    currentStamina = Mathf.Min(currentStamina + amount, maxStamina);
                    break;
            }
            UpdateUI();
        }

        public void ApplyStatus(StatusEffectType type, float duration)
        {
            activeStatusEffects.Add(type);
            effectDuration[type] = Time.time + duration;
            UpdateUI();
        }

        public void RemoveStatus(StatusEffectType type)
        {
            if (activeStatusEffects.Remove(type))
            {
                effectDuration.Remove(type);
                UpdateUI();
            }
        }

        // ------------------------------------------------------------
        // 🔄 상태 이상 지속 시간 업데이트
        // ------------------------------------------------------------

        private void Update()
        {
            if (effectDuration.Count == 0) return;

            var expired = new List<StatusEffectType>();

            foreach (var kvp in effectDuration)
            {
                if (Time.time > kvp.Value)
                    expired.Add(kvp.Key);
            }

            foreach (var type in expired)
                RemoveStatus(type);
        }

        // ------------------------------------------------------------
        // 🖥️ UI 업데이트
        // ------------------------------------------------------------

        private void UpdateUI()
        {
            if (statusUI != null)
                statusUI.UpdateUI();
        }
    }
}
