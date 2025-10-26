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

    [Header("Dependencies")]
    [SerializeField] private QuickSlotManager quickSlotManager;
    [SerializeField] private EquipmentSlotUI[] equipmentSlotUIs; // UI 갱신을 위해 UI 컴포넌트 참조

    // 🔥 수정: SerializeField 제거. 코드로 초기화하고 외부에서 접근하지 않으므로 private 유지.
    private InventorySlot[] equipmentSlot;

    [SerializeField] private int equipmentSlotStartIndex = 32;
    [SerializeField] private int equipmentSlotEndIndex = 35; // UI 개수에 따라 32 + length - 1로 사용될 수 있음

    // 현재 장착된 아이템을 저장하는 딕셔너리
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

        // 🔥 [FIX] 1단계: equipmentSlot 배열 초기화
        // equipmentSlotUIs의 길이와 일치하도록 InventorySlot 배열을 초기화합니다.
        if (equipmentSlotUIs != null && equipmentSlotUIs.Length > 0)
        {
            equipmentSlot = new InventorySlot[equipmentSlotUIs.Length];
            for (int i = 0; i < equipmentSlot.Length; i++)
            {
                // 모든 슬롯을 빈 상태로 초기화 (Empty InventorySlot 구조체 사용)
                equipmentSlot[i] = InventorySlot.Empty;
            }
            // 디버깅을 위해 endIndex를 UI 개수에 맞게 조정할 수 있습니다.
            // equipmentSlotEndIndex = equipmentSlotStartIndex + equipmentSlotUIs.Length - 1; 
        }
        else
        {
            Debug.LogError("[EquipmentManager] equipmentSlotUIs 배열이 Inspector에 설정되지 않았습니다. 장비 슬롯을 초기화할 수 없습니다. VR 상호작용에 오류가 발생할 수 있습니다.");
            // UI가 없더라도 배열 접근 오류를 피하기 위해 최소한의 배열을 설정합니다.
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
        // 장비 슬롯 배열의 시작 인덱스에 대한 내부 인덱스
        int internalIndex = index - equipmentSlotStartIndex;

        if (internalIndex >= 0 && internalIndex < equipmentSlot.Length)
        {
            // Enum 값을 int로 캐스팅하여 인덱스와 매핑
            // UI 배열과 EquipSlotType 열거형의 순서가 일치한다고 가정합니다.
            return (EquipSlotType)internalIndex;
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

        // 1. 이미 장착된 아이템이 있는지 확인
        if (equippedItems.TryGetValue(targetSlot, out EquippableItemSO currentItem) && currentItem != null)
        {
            oldItem = currentItem;
            // 2. 이미 아이템이 있다면, 현재 아이템을 해제
            equippedItems[targetSlot] = null;

            // UI 슬롯의 InventorySlot 데이터도 업데이트
            // 장비 슬롯의 InventorySlot은 항상 스택 1을 가집니다.
            UpdateEquipmentSlotData(targetSlot, InventorySlot.Empty);

            Debug.Log($"[EquipmentManager] Unequipping {oldItem.itemName} from {targetSlot} before new equip. (Old item will be returned to inventory.)");
        }

        // 3. 새 아이템 장착
        equippedItems[targetSlot] = itemToEquip;

        // UI 슬롯의 InventorySlot 데이터도 업데이트
        UpdateEquipmentSlotData(targetSlot, new InventorySlot(itemToEquip, 1));

        Debug.Log($"[EquipmentManager] Successfully equipped {itemToEquip.itemName} into {targetSlot} slot.");

        // 4. UI 갱신
        UpdateEquipmentUI(targetSlot, itemToEquip);

        // 5. QuickSlotManager 연동 (옵션)

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

            // 2. 새 아이템 타입 검사 및 장착 가능 여부 확인 (강제 스왑이 아니라면 여기에 타입 체크가 필요)
            if (newItem != null && newItem.equipSlotType != slotType)
            {
                // 장비 슬롯에 맞지 않는 아이템이 들어온 경우, oldItem을 반환하고 newItem을 그대로 유지 (스왑 취소 효과)
                Debug.LogWarning($"[EquipmentManager] 타입 불일치. {slotType} 슬롯에 {newItem.equipSlotType} 아이템이 들어왔습니다. 스왑 취소.");
                return oldItem;
            }

            // 3. 새 아이템 장착 (null이면 해제)
            equippedItems[slotType] = newItem;

            // 4. InventorySlot 데이터 업데이트
            InventorySlot newSlotData = newItem != null ? new InventorySlot(newItem, 1) : InventorySlot.Empty;
            UpdateEquipmentSlotData(slotType, newSlotData);

            // 5. UI 갱신
            UpdateEquipmentUI(slotType, newItem);

            Debug.Log($"[EquipmentManager] Swap successful at index {index}. Old: {(oldItem != null ? oldItem.itemName : "None")}, New: {(newItem != null ? newItem.itemName : "None")}");

            return oldItem;
        }
        catch (IndexOutOfRangeException ex)
        {
            Debug.LogError(ex.Message);
            return newItem;
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

        // 🔥 [FIX] 2단계: equipmentSlot 배열의 길이로 경계를 확인하고 안전하게 접근
        if (equipmentSlot != null && internalIndex >= 0 && internalIndex < equipmentSlot.Length)
        {
            // equipmentSlot은 InventorySlot 구조체이므로 IsEmpty 속성 사용 가능
            return equipmentSlot[internalIndex].IsEmpty;
        }

        // 초기화 오류 또는 유효하지 않은 인덱스인 경우
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
        int internalIndex = (int)slotType;
        if (equipmentSlot != null && internalIndex >= 0 && internalIndex < equipmentSlot.Length)
        {
            equipmentSlot[internalIndex] = slotData;
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