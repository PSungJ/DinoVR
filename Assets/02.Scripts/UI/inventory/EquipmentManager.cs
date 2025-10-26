using UnityEngine;
using System.Collections.Generic;
using System;

// 장착 아이템이 들어갈 수 있는 슬롯의 종류를 정의합니다.
// 이 열거형은 EquippableItemSO에서도 참조됩니다.
public enum EquipSlotType
{
    Weapon,
    Helmet,
    Armor,
    Boots
}

/// <summary>
/// 플레이어의 장비 아이템을 관리하고 관련 로직을 처리하는 클래스입니다.
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    // Singleton pattern for easy access
    public static EquipmentManager Instance { get; private set; }

    [Header("Dependencies")] // 기존 Canvas 필드 유지
    [SerializeField] private QuickSlotManager quickSlotManager;
    [SerializeField] private EquipmentSlotUI[] equipmentSlotUIs; // UI 갱신을 위해 UI 컴포넌트 참조
    [SerializeField] private InventorySlot[] equipmentSlot;
    private int equipmentSlotStartIndex = 32;
    private int equipmentSlotEndIndex = 33;


    // 현재 장착된 아이템을 저장하는 딕셔너리 (사용자 요청 반영)
    private Dictionary<EquipSlotType, EquippableItemSO> equippedItems = new Dictionary<EquipSlotType, EquippableItemSO>();

    /// <summary>
    /// 현재 장착된 아이템 목록을 읽기 전용으로 반환합니다.
    /// </summary>
    public IReadOnlyDictionary<EquipSlotType, EquippableItemSO> CurrentEquipment => equippedItems;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 초기화: 모든 슬롯을 null로 설정
        foreach (EquipSlotType slot in System.Enum.GetValues(typeof(EquipSlotType)))
        {
            // 중복 추가를 피하기 위해 이미 존재하는지 확인합니다.
            if (!equippedItems.ContainsKey(slot))
            {
                equippedItems.Add(slot, null);
            }
        }
    }

    // ----------------------------------------------------
    // [장비 로직]
    // ----------------------------------------------------

    /// <summary>
    /// 새로운 장비 아이템을 장착합니다.
    /// 🔥 Inventory.cs의 로직을 위해 장착 해제된 이전 아이템을 반환합니다.
    /// </summary>
    /// <param name="itemToEquip">장착할 아이템 데이터</param>
    /// <param name="inventorySlotIndex">아이템이 온 Inventory 슬롯의 인덱스 (-1은 Inventory 외부에서 온 경우)</param>
    /// <returns>장착 해제된 기존 아이템. 없으면 null.</returns>
    public EquippableItemSO Equip(EquippableItemSO itemToEquip, int inventorySlotIndex)
    {
        if (itemToEquip == null)
        {
            Debug.LogError("[EquipmentManager] Attempted to equip a null item.");
            return null;
        }

        EquipSlotType targetSlot = itemToEquip.equipSlotType;
        EquippableItemSO oldItem = null;

        // 1. 이미 장착된 아이템이 있는지 확인
        if (equippedItems.TryGetValue(targetSlot, out EquippableItemSO currentItem) && currentItem != null)
        {
            oldItem = currentItem;
            // 2. 이미 아이템이 있다면, 현재 아이템을 해제
            equippedItems[targetSlot] = null;
            Debug.Log($"[EquipmentManager] Unequipping {oldItem.itemName} from {targetSlot} before new equip. (Old item will be returned to inventory.)");
        }

        // 3. 새 아이템 장착
        equippedItems[targetSlot] = itemToEquip;
        Debug.Log($"[EquipmentManager] Successfully equipped {itemToEquip.itemName} into {targetSlot} slot.");

        // 4. UI 갱신
        UpdateEquipmentUI(targetSlot, itemToEquip);

        // 5. QuickSlotManager 연동 (옵션)
        // 여기에 QuickSlotManager 연동 로직이 들어갈 수 있습니다.

        return oldItem; // 이전 아이템 반환 (Inventory.cs와의 호환성 유지)
    }

    /// <summary>
    /// 특정 슬롯의 장비를 해제하는 로직입니다.
    /// </summary>
    /// <param name="slotType">해제할 장착 위치</param>
    /// <returns>해제된 아이템. 없으면 null.</returns>
    public EquippableItemSO Unequip(EquipSlotType slotType)
    {
        if (equippedItems.TryGetValue(slotType, out EquippableItemSO currentItem) && currentItem != null)
        {
            equippedItems[slotType] = null;
            Debug.Log($"[EquipmentManager] Unequipped {currentItem.itemName} from {slotType}.");

            // UI 갱신 (빈 슬롯 상태로 만듭니다)
            UpdateEquipmentUI(slotType, null);

            // QuickSlotManager 연동 (장비 해제 시 퀵슬롯에 알림)

            return currentItem;
        }
        return null;
    }

    // ----------------------------------------------------
    // [UI 갱신]
    // ----------------------------------------------------

    private void UpdateEquipmentUI(EquipSlotType slotType, EquippableItemSO item)
    {
        // 해당 EquipSlotType을 가진 UI 컴포넌트를 찾아 갱신
        foreach (var uiSlot in equipmentSlotUIs)
        {
            if (uiSlot != null && uiSlot.SlotType == slotType)
            {
                uiSlot.UpdateSlotUI(item);
                break;
            }
        }
    }

    public bool IsEquipmentSlotIndex(int index)
    {
        return index >= equipmentSlotStartIndex && index < equipmentSlotStartIndex + equipmentSlotUIs.Length;
    }

    public bool IsEquipmentSlotEmpty(int index)
    {
        if (!IsEquipmentSlotIndex(index)) return true;

        int internalIndex = index - equipmentSlotStartIndex;

        if (internalIndex >= 0 && internalIndex < equipmentSlotUIs.Length)
        {
            // struct이므로 복사본에 접근하지만, IsEmpty는 안전합니다.
            return equipmentSlot[internalIndex].IsEmpty;
        }

        return true;
    }
}
