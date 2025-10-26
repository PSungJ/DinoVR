using UnityEngine;
using System.Collections.Generic;
using System;

// 장착 아이템이 들어갈 수 있는 슬롯의 종류를 정의합니다.
// 이 열거형은 EquippableItemSO에서도 참조됩니다.
public enum EquipSlotType
{
    None = 0, // Enum의 첫 번째 값은 0이어야 합니다.
    Weapon = 1,
    Armor = 2
    // 다른 장비 슬롯도 여기에 추가 (예: Head, Body 등)
}

// [가정] InventorySlot 구조체와 EquippableItemSO 클래스는 별도의 파일에 정의되어 있습니다.

/// <summary>
/// 플레이어의 장비 아이템을 관리하고 관련 로직을 처리하는 클래스입니다.
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    // Singleton pattern for easy access
    public static EquipmentManager Instance { get; private set; }

    [Header("Dependencies")]
    [SerializeField] private QuickSlotManager quickSlotManager;
    [SerializeField] private EquipmentSlotUI[] equipmentSlotUIs; // UI 갱신을 위해 UI 컴포넌트 참조

    // 인벤토리 확장 슬롯 데이터를 저장하는 내부 배열 (UI 개수와 일치)
    private InventorySlot[] equipmentSlot;

    // 인벤토리 확장 슬롯의 시작 인덱스
    [SerializeField] private int equipmentSlotStartIndex = 33;
    // 인벤토리 확장 슬롯의 끝 인덱스는 equipmentSlotStartIndex + equipmentSlot.Length - 1 입니다.

    // 현재 장착된 아이템을 EquipSlotType을 키로 저장하는 딕셔너리
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

        // 1. equippedItems 딕셔너리 초기화 (None 제외)
        foreach (EquipSlotType slot in System.Enum.GetValues(typeof(EquipSlotType)))
        {
            if (slot == EquipSlotType.None) continue; // None 타입은 장비 딕셔너리에 추가하지 않음

            if (!equippedItems.ContainsKey(slot))
            {
                equippedItems.Add(slot, null);
            }
        }

        // 2. equipmentSlot 배열 초기화
        if (equipmentSlotUIs != null && equipmentSlotUIs.Length > 0)
        {
            equipmentSlot = new InventorySlot[equipmentSlotUIs.Length];
            for (int i = 0; i < equipmentSlot.Length; i++)
            {
                // 모든 슬롯을 빈 상태로 초기화 (Empty InventorySlot 구조체 사용)
                equipmentSlot[i] = InventorySlot.Empty;
            }
        }
        else
        {
            Debug.LogError("[EquipmentManager] equipmentSlotUIs 배열이 Inspector에 설정되지 않았습니다. 장비 슬롯을 초기화할 수 없습니다. VR 상호작용에 오류가 발생할 수 있습니다.");
            equipmentSlot = new InventorySlot[0];
        }
    }

    // ----------------------------------------------------
    // [인덱스-타입 변환 로직]
    // ----------------------------------------------------

    /// <summary>
    /// 인벤토리 확장 인덱스를 EquipSlotType으로 변환합니다.
    /// </summary>
    private EquipSlotType GetSlotTypeFromIndex(int index)
    {
        // 장비 슬롯 배열의 시작 인덱스에 대한 내부 인덱스 (0, 1, 2...)
        int internalIndex = index - equipmentSlotStartIndex;

        if (internalIndex >= 0 && internalIndex < equipmentSlot.Length)
        {
            // EquipSlotType의 정의에 따라 오프셋을 조정하여 반환합니다.
            // internalIndex 0 -> EquipSlotType 1 (Weapon)
            return (EquipSlotType)(internalIndex + 1);
        }

        throw new IndexOutOfRangeException($"[EquipmentManager] 인덱스 {index}는 유효한 장비 슬롯 범위({equipmentSlotStartIndex}~{equipmentSlotStartIndex + equipmentSlot.Length - 1})를 벗어났습니다.");
    }


    // ----------------------------------------------------
    // [장비 로직]
    // ----------------------------------------------------

    /// <summary>
    /// 새로운 장비 아이템을 장착합니다.
    /// Inventory.cs의 로직을 위해 장착 해제된 이전 아이템을 반환합니다.
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

        // ⭐[FIX] 1. EquipSlotType이 None인 아이템은 장착 불가
        if (targetSlot == EquipSlotType.None)
        {
            // 이 로그는 주로 Katana SO의 equipSlotType이 Weapon으로 설정되지 않았을 때 발생합니다.
            Debug.LogError($"[EquipmentManager] 장착 실패: 아이템 '{itemToEquip.itemName}'의 equipSlotType이 None으로 설정되어 있습니다. 인스펙터를 확인하세요.");
            return itemToEquip; // 거부된 아이템을 Inventory에게 반환하여 원래 슬롯으로 복귀시킵니다.
        }

        // 1. 이미 장착된 아이템이 있는지 확인 및 해제
        if (equippedItems.TryGetValue(targetSlot, out EquippableItemSO currentItem) && currentItem != null)
        {
            oldItem = currentItem;
            equippedItems[targetSlot] = null;
            UpdateEquipmentSlotData(targetSlot, InventorySlot.Empty);
            Debug.Log($"[EquipmentManager] Unequipping {oldItem.itemName} from {targetSlot} before new equip.");
        }

        // 2. 새 아이템 장착
        equippedItems[targetSlot] = itemToEquip;

        // UI 슬롯의 InventorySlot 데이터도 업데이트 (stack: 1)
        UpdateEquipmentSlotData(targetSlot, new InventorySlot(itemToEquip, 1));

        Debug.Log($"[EquipmentManager] Successfully equipped {itemToEquip.itemName} into {targetSlot} slot.");

        // 3. UI 갱신
        UpdateEquipmentUI(targetSlot, itemToEquip);

        // 4. QuickSlotManager 연동 (옵션)

        return oldItem; // 이전 아이템 반환 (Inventory.cs와의 호환성 유지)
    }

    /// <summary>
    /// 특정 슬롯의 장비를 해제하는 로직입니다.
    /// </summary>
    public EquippableItemSO Unequip(EquipSlotType slotType)
    {
        if (equippedItems.TryGetValue(slotType, out EquippableItemSO currentItem) && currentItem != null)
        {
            equippedItems[slotType] = null;
            Debug.Log($"[EquipmentManager] Unequipped {currentItem.itemName} from {slotType}.");

            // UI 슬롯의 InventorySlot 데이터도 업데이트
            UpdateEquipmentSlotData(slotType, InventorySlot.Empty);

            // UI 갱신 (빈 슬롯 상태로 만듭니다)
            UpdateEquipmentUI(slotType, null);

            // QuickSlotManager 연동 (장비 해제 시 퀵슬롯에 알림)

            return currentItem;
        }
        return null;
    }

    /// <summary>
    /// 장비 슬롯에 새 아이템을 장착하고 기존 아이템을 반환합니다.
    /// 이 함수는 Inventory.SwapSlots에서 장비-인벤토리 교환 시 사용됩니다.
    /// </summary>
    public EquippableItemSO SwapEquippedItem(int index, EquippableItemSO newItem)
    {
        if (!IsEquipmentSlotIndex(index))
        {
            Debug.LogError($"[EquipmentManager] SwapEquippedItem: 유효하지 않은 장비 슬롯 인덱스 {index}");
            return newItem;
        }

        try
        {
            EquipSlotType slotType = GetSlotTypeFromIndex(index);

            // 1. 기존 아이템 가져오기
            equippedItems.TryGetValue(slotType, out EquippableItemSO oldItem);

            // 2. 새 아이템 타입 검사 및 장착 가능 여부 확인
            if (newItem != null)
            {
                // 장착하려는 아이템의 EquipSlotType이 현재 슬롯 타입과 일치하지 않는지 확인
                if (newItem.equipSlotType != slotType)
                {
                    // 장비 슬롯에 맞지 않는 아이템이 들어온 경우, 스왑 취소.
                    Debug.LogWarning($"[EquipmentManager] 타입 불일치. {slotType} 슬롯에 {newItem.equipSlotType} 아이템이 들어왔습니다. 스왑 취소. (인벤토리 복귀 아이템: {newItem.itemName})");

                    // 장비 슬롯 UI 복구 (혹시 모를 UI 깜빡임을 방지)
                    UpdateEquipmentUI(slotType, oldItem);

                    // 거부된 아이템 (newItem)을 반환하여 인벤토리가 이를 원래 슬롯에 되돌려 놓도록 합니다.
                    return newItem;
                }
            }

            // 3. 새 아이템 장착 (null이면 해제)
            equippedItems[slotType] = newItem;

            // 4. InventorySlot 데이터 업데이트
            InventorySlot newSlotData = newItem != null ? new InventorySlot(newItem, 1) : InventorySlot.Empty;
            UpdateEquipmentSlotData(slotType, newSlotData);

            // 5. UI 갱신
            UpdateEquipmentUI(slotType, newItem);

            Debug.Log($"[EquipmentManager] Swap successful at index {index}. Old: {(oldItem != null ? oldItem.itemName : "None")}, New: {(newItem != null ? newItem.itemName : "None")}");

            return oldItem; // 성공했으므로 장비 해제된 oldItem을 인벤토리로 보냅니다.
        }
        catch (IndexOutOfRangeException ex)
        {
            Debug.LogError(ex.Message);
            return newItem; // 오류 발생 시 안전하게 newItem이라도 돌려보냅니다.
        }
    }


    // ----------------------------------------------------
    // [데이터 및 상태 확인]
    // ----------------------------------------------------

    /// <summary>
    /// 인덱스를 사용하여 현재 장착된 아이템을 반환합니다.
    /// </summary>
    public EquippableItemSO GetEquippedItemByIndex(int index)
    {
        if (!IsEquipmentSlotIndex(index)) return null;

        try
        {
            EquipSlotType slotType = GetSlotTypeFromIndex(index);
            equippedItems.TryGetValue(slotType, out EquippableItemSO item);
            return item;
        }
        catch (IndexOutOfRangeException ex)
        {
            Debug.LogError(ex.Message);
            return null;
        }
    }

    public bool IsEquipmentSlotIndex(int index)
    {
        // UI 배열의 길이를 사용하여 장비 슬롯 인덱스 범위를 확인합니다.
        return index >= equipmentSlotStartIndex && equipmentSlot != null && index < equipmentSlotStartIndex + equipmentSlot.Length;
    }

    public bool IsEquipmentSlotEmpty(int index)
    {
        if (!IsEquipmentSlotIndex(index)) return true;

        int internalIndex = index - equipmentSlotStartIndex;

        if (equipmentSlot != null && internalIndex >= 0 && internalIndex < equipmentSlot.Length)
        {
            // equipmentSlot은 InventorySlot 구조체이므로 IsEmpty 속성 사용 가능
            return equipmentSlot[internalIndex].IsEmpty;
        }

        return true;
    }

    // ----------------------------------------------------
    // [내부 데이터 및 UI 갱신]
    // ----------------------------------------------------

    /// <summary>
    /// 장비 슬롯의 내부 InventorySlot 데이터를 갱신합니다.
    /// </summary>
    private void UpdateEquipmentSlotData(EquipSlotType slotType, InventorySlot slotData)
    {
        // ⭐[FIX] 2. None 타입은 인덱스 계산을 시도하지 않고 바로 리턴 (인덱스 -1 오류 방지)
        if (slotType == EquipSlotType.None)
        {
            Debug.LogError("[EquipmentManager] InventorySlot 데이터 갱신 오류: EquipSlotType이 None입니다. 장착 가능한 타입이 아닙니다.");
            return;
        }

        // EquipSlotType.Weapon (1) -> internalIndex 0
        int internalIndex = (int)slotType - 1;

        if (equipmentSlot != null && internalIndex >= 0 && internalIndex < equipmentSlot.Length)
        {
            equipmentSlot[internalIndex] = slotData;
        }
        else
        {
            Debug.LogError($"[EquipmentManager] InventorySlot 데이터 갱신 오류: 유효하지 않은 내부 인덱스 {internalIndex} (SlotType: {slotType})");
        }
    }

    private void UpdateEquipmentUI(EquipSlotType slotType, EquippableItemSO item)
    {
        // 해당 EquipSlotType을 가진 UI 컴포넌트를 찾아 갱신
        foreach (var uiSlot in equipmentSlotUIs)
        {
            if (uiSlot != null && uiSlot.SlotType == slotType)
            {
                // itemData는 EquippableItemSO, stackSize는 1로 고정
                uiSlot.UpdateSlotUI(item, item != null ? 1 : 0);
                break;
            }
        }
    }
}
