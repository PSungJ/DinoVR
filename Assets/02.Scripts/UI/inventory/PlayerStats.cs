using UnityEngine;
using System.Collections.Generic;

// ----------------------------------------------------
// [상태 이상 타입 Enum]
// ----------------------------------------------------
public enum StatusEffectType
{
    Fracture,      // 골절 (이동 속도 저하)
    FoodPoisoning, // 식중 (지속 피해)
    Fatigue        // 피로 (스테이터스 회복 속도 저하)
}

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private StatusUI statusUI;

    [Header("Base Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private int baseAttack = 10;

    [SerializeField] private float currentHealth;
    private float currentStamina;

    private int equipmentAttackBonus = 0;

    private HashSet<StatusEffectType> activeStatusEffects = new HashSet<StatusEffectType>();
    private Dictionary<StatusEffectType, float> statusDuration = new Dictionary<StatusEffectType, float>();

    public float CurrentHealth => currentHealth;

    // ----------------------------------------------------
    // 🔹 Shader Graph 머티리얼 연동용
    // ----------------------------------------------------
    [Header("Shader Graph Health Bar Material")]
    [Tooltip("Shader Graph로 만든 체력바 머티리얼을 연결합니다.")]
    [SerializeField] private Material healthBarMaterial; // ✅ 머티리얼 직접 참조

    private void Awake()
    {
        currentHealth = maxHealth;
        currentStamina = maxStamina;

        // 시작 시 머티리얼 초기화
        UpdateHealthBarMaterial();
    }

    // ----------------------------------------------------
    // [UI 및 최종 스탯 확인 함수]
    // ----------------------------------------------------
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }

    public int GetFinalAttack()
    {
        return baseAttack + equipmentAttackBonus;
    }

    public bool IsEffectActive(StatusEffectType type)
    {
        return activeStatusEffects.Contains(type);
    }

    // ----------------------------------------------------
    // [장비 스탯 반영 로직]
    // ----------------------------------------------------
    public void ApplyEquipmentModifiers(EquippableItemSO item)
    {
        equipmentAttackBonus += item.attackModifier;
        if (statusUI != null) statusUI.UpdateUI();
    }

    public void RemoveEquipmentModifiers(EquippableItemSO item)
    {
        equipmentAttackBonus -= item.attackModifier;
        if (statusUI != null) statusUI.UpdateUI();
    }

    // ----------------------------------------------------
    // [스탯 회복 / 상태 이상 로직]
    // ----------------------------------------------------
    public void Restore(string effectType, float amount)
    {
        if (effectType == "Health")
        {
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            UpdateHealthBarMaterial(); // ✅ 체력 회복 시 머티리얼 갱신
        }
        else if (effectType == "Stamina")
        {
            currentStamina = Mathf.Min(currentStamina + amount, maxStamina);
        }

        if (statusUI != null) statusUI.UpdateUI();
    }

    public void ApplyStatus(StatusEffectType type, float duration)
    {
        if (!activeStatusEffects.Contains(type))
        {
            activeStatusEffects.Add(type);
            if (statusUI != null) statusUI.UpdateUI();
        }
        statusDuration[type] = Time.time + duration;
    }

    public void RemoveStatus(StatusEffectType type)
    {
        if (activeStatusEffects.Remove(type))
        {
            if (statusUI != null) statusUI.UpdateUI();
        }
        statusDuration.Remove(type);
    }

    // ----------------------------------------------------
    // ✅ Shader Graph 머티리얼의 _FillAmount 업데이트
    // ----------------------------------------------------
    private void UpdateHealthBarMaterial()
    {
        if (healthBarMaterial != null)
        {
            float fillRatio = currentHealth / maxHealth;
            healthBarMaterial.SetFloat("_FillAmount", fillRatio);
        }
    }

    // 데미지 감소 효과

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        UpdateHealthBarMaterial();

    }

    private WeaponComponent GetEquippedWeapon()
    {
        if (EquipmentManager.Instance != null)
        {
            return EquipmentManager.Instance.GetEquippedWeapon();
        }
        return null;
    }

    public int GetCurrentAmmo()
    {
        WeaponComponent weapon = GetEquippedWeapon();
        return weapon != null ? weapon.currentAmmo : 0;
    }


}
