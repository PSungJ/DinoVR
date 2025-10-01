using UnityEngine;
using System.Collections.Generic;

// EquipSlotType Enum 정의 (PlayerStats.cs에서 정의된 Enum과 동일하게 사용)
public enum EquipSlotType
{
    Weapon,
    Cyberware
}

public class EquipmentManager : MonoBehaviour
{
    [Header("Dependencies")]
    // PlayerStats 참조 필수 (스탯 반영/제거용)
    [SerializeField] private PlayerStats playerStats;
    
    [Header("UI Dependencies")]
    // EquipmentSlotUI 컴포넌트 참조 (UI 업데이트용)
    [SerializeField] private EquipmentSlotUI weaponSlotUI; 
    [SerializeField] private EquipmentSlotUI cyberwareSlotUI;
    
    // 현재 장착된 장비들을 Dictionary로 관리합니다.
    private Dictionary<EquipSlotType, EquippableItemSO> currentEquipment;
    
    // 외부에서 장착 정보를 읽을 수 있도록 public으로 선언
    public Dictionary<EquipSlotType, EquippableItemSO> CurrentEquipment => currentEquipment;

    private void Awake()
    {
        currentEquipment = new Dictionary<EquipSlotType, EquippableItemSO>();
        // 모든 EquipSlotType을 자동으로 null로 초기화합니다.
        foreach (EquipSlotType type in System.Enum.GetValues(typeof(EquipSlotType)))
        {
            currentEquipment.Add(type, null);
        }
    }

    // ----------------------------------------------------
    // [장비 장착 로직] (Inventory.cs에서 호출됨)
    // ----------------------------------------------------
    // 새 장비를 장착하고, 기존 장비(있을 경우)를 반환합니다.
    public EquippableItemSO Equip(EquippableItemSO item)
    {
        EquipSlotType slot = item.equipSlotType;
        EquippableItemSO oldItem = null;

        // 1. 기존 장비 해제
        if (currentEquipment[slot] != null)
        {
            oldItem = currentEquipment[slot];
            
            // PlayerStats에서 이전 장비의 스탯 해제
            if (playerStats != null)
            {
                playerStats.RemoveEquipmentModifiers(oldItem);
            }
        }

        // 2. 새 아이템 장착 및 스탯 적용
        currentEquipment[slot] = item;

// PlayerStats에 새 장비의 스탯 적용 (공격력만)
        if (playerStats != null)
        {
            playerStats.ApplyEquipmentModifiers(item);
}

// 3. UI 업데이트
        UpdateEquipmentUI(slot, item);

        return oldItem; // 인벤토리로 반환할 아이템 (스왑된 아이템)
    }

    // ----------------------------------------------------
    // [장비 해제 로직] (EquipmentSlotUI.cs에서 호출됨)
    // ----------------------------------------------------
    // 장비를 해제하고, 해제된 아이템을 반환합니다.
    public EquippableItemSO Unequip(EquipSlotType slot)
    {
        if (currentEquipment[slot] == null) return null;

        EquippableItemSO itemToReturn = currentEquipment[slot];
        
        // PlayerStats에서 스탯 해제 (공격력만)
        if (playerStats != null)
        {
            playerStats.RemoveEquipmentModifiers(itemToReturn);
        }

        currentEquipment[slot] = null;
        
        // UI 업데이트
        UpdateEquipmentUI(slot, null);
        
        return itemToReturn; // 인벤토리로 돌려보낼 아이템
    }
    
    // ----------------------------------------------------
    // [내부 UI 업데이트 헬퍼]
    // ----------------------------------------------------
    private void UpdateEquipmentUI(EquipSlotType slot, EquippableItemSO item)
    {
        // UI 슬롯 타입에 맞게 해당 UI 컴포넌트의 업데이트 함수를 호출합니다.
        if (slot == EquipSlotType.Weapon && weaponSlotUI != null)
        {
            weaponSlotUI.UpdateSlotUI(item);
        }
        else if (slot == EquipSlotType.Cyberware && cyberwareSlotUI != null)
        {
            cyberwareSlotUI.UpdateSlotUI(item);
        }
    }
}