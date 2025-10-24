using UnityEngine;
using System.Collections.Generic;
using static UnityEditor.Progress;

// StatusEffectType Enum 정의
public enum StatusEffectType
{
    Fracture,      // 골절 (이동 속도 저하)
    FoodPoisoning, // 식중 (지속 피해)
    Fatigue        // 피로 (스테이터스 회복 속도 저하)
}

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    // [Header("Dependencies")]
    // StatusUI 참조 필수 (스탯 변화 시 UI 업데이트용)
    [SerializeField] private StatusUI statusUI;

    [Header("Base Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private int baseAttack = 10;

    private float currentHealth;
    private float currentStamina;

    // 장비로 인한 공격력 보너스 관리
    private int equipmentAttackBonus = 0;

    // 상태 이상 관리
    private HashSet<StatusEffectType> activeStatusEffects = new HashSet<StatusEffectType>();
    private Dictionary<StatusEffectType, float> statusDuration = new Dictionary<StatusEffectType, float>();

    public float CurrentHealth => currentHealth;

    // 추가: 장비 공격력 보너스를 읽기 위한 속성
    public int EquipmentAttackBonus => equipmentAttackBonus;

    private void Awake()
    {
        // 🔥 수정: 싱글톤 초기화 로직 추가
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            // 이미 인스턴스가 존재하면 새로운 오브젝트 파괴
            Destroy(gameObject);
            return;
        }

        currentHealth = maxHealth;
        currentStamina = maxStamina;
    }

    // ----------------------------------------------------
    // [UI 및 최종 스탯 확인 함수]
    // ----------------------------------------------------
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }

    // TODO: GetStaminaPercentage() 함수 구현 (UI 바 표시용)
    /*
    public float GetStaminaPercentage()
    {
        return currentStamina / maxStamina;
    }
    */

    // 최종 공격력 반환 (기본 스탯 + 장비 보너스)
    public int GetFinalAttack()
    {
        return baseAttack + equipmentAttackBonus;
    }

// 상태 이상 활성화 여부 확인 (StatusUI 및 기타 로직 사용)
public bool IsEffectActive(StatusEffectType type)
{
    return activeStatusEffects.Contains(type);
}

    // ----------------------------------------------------
    // [장비 스탯 반영 로직] (EquipmentManager에서 호출됨)
    // ----------------------------------------------------
    public void AddEquipmentModifiers(EquippableItemSO item)
    {
        equipmentAttackBonus += item.attackModifier;
        if (statusUI != null) statusUI.UpdateUI(); // UI 업데이트
    }

    public void RemoveEquipmentModifiers(EquippableItemSO item)
    {
        equipmentAttackBonus -= item.attackModifier;
        if (statusUI != null) statusUI.UpdateUI(); // UI 업데이트
    }
    // ----------------------------------------------------
    // [상태 이상 및 회복 로직] (ConsumableItem 사용 시 호출됨)
    // ----------------------------------------------------
    public void Restore(string effectType, float amount)
{
    if (effectType == "Health")
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }
    else if (effectType == "Stamina")
    {
        currentStamina = Mathf.Min(currentStamina + amount, maxStamina);
    }
    if (statusUI != null) statusUI.UpdateUI(); // UI 업데이트
}

// 상태 이상 적용 (Duration 포함)
public void ApplyStatus(StatusEffectType type, float duration)
{
    if (!activeStatusEffects.Contains(type))
    {
        activeStatusEffects.Add(type);
        if (statusUI != null) statusUI.UpdateUI(); // UI 업데이트
    }
    statusDuration[type] = Time.time + duration;
}

// 상태 이상 제거
public void RemoveStatus(StatusEffectType type)
{
    if (activeStatusEffects.Remove(type))
    {
        if (statusUI != null) statusUI.UpdateUI(); // UI 업데이트
    }
    statusDuration.Remove(type);
}

    // TODO: Update 함수에서 Damage Over Time 및 Duration 체크 로직 구현 필요
}