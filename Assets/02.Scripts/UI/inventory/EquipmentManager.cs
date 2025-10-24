using UnityEngine;
using System.Collections.Generic;
using System; // Action 델리게이트를 사용하기 위해 추가

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

    // 🔥 추가: EquipmentSlotUI.cs 오류 해결 (CS1061)
    /// <summary>장비 아이템이 장착되거나 해제될 때 발생합니다.</summary>
    public event Action<EquipSlotType, EquippableItemSO> OnEquipmentChanged;

    [Header("Dependencies")] // 기존 Canvas 필드 유지
    [SerializeField] private QuickSlotManager quickSlotManager;
    [SerializeField] private EquipmentSlotUI[] equipmentSlotUIs; // UI 갱신을 위해 UI 컴포넌트 참조

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

        // 초기 딕셔너리 설정 (null로 초기화)
        foreach (EquipSlotType type in Enum.GetValues(typeof(EquipSlotType)))
        {
            equippedItems[type] = null;
        }
    }

    /// <summary>
    /// 장비를 장착하는 로직입니다.
    /// </summary>
    /// <param name="itemToEquip">장착할 아이템</param>
    /// <param name="inventorySlotIndex">아이템이 인벤토리의 몇 번 슬롯에 있었는지 (없다면 -1)</param>
    /// <returns>이전에 장착되어 있던 아이템. 없으면 null.</returns>
    public EquippableItemSO Equip(EquippableItemSO itemToEquip, int inventorySlotIndex)
    {
        if (itemToEquip == null) return null;

        EquipSlotType targetSlot = itemToEquip.equipSlotType;

        // 1. 기존 아이템 가져오기
        EquippableItemSO oldItem = null;
        if (equippedItems.TryGetValue(targetSlot, out oldItem))
        {
            // 교체될 아이템이 있으면 장비 해제 로직을 수행합니다.
            // 이 시점에 아이템이 인벤토리로 돌아가거나 드롭되어야 합니다.
            // 여기서는 교체를 위해 oldItem을 임시로 저장만 하고,
            // 인벤토리 갱신 로직은 EquippableItemSO.Use에서 Inventory.UpdateSlotWithNewEquippedItem을 통해 처리합니다.
        }

        // 2. 새로운 아이템 장착
        equippedItems[targetSlot] = itemToEquip;
        Debug.Log($"[EquipmentManager] Equipped {itemToEquip.itemName} to {targetSlot} slot.");

        // 3. UI 갱신 및 이벤트 발생
        // UpdateEquipmentUI(targetSlot, itemToEquip); // 이 코드는 EquipmentSlotUI.cs가 이벤트를 구독하여 처리하게 됩니다.

        // 🔥 추가: OnEquipmentChanged 이벤트 발생
        OnEquipmentChanged?.Invoke(targetSlot, itemToEquip);

        // 4. QuickSlotManager 연동 (옵션)
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
            // UpdateEquipmentUI(slotType, null); // 이 코드는 EquipmentSlotUI.cs가 이벤트를 구독하여 처리하게 됩니다.

            // 🔥 추가: OnEquipmentChanged 이벤트 발생 (아이템은 null)
            OnEquipmentChanged?.Invoke(slotType, null);

            // QuickSlotManager 연동 (장비 해제 시 퀵슬롯에 알림)

            return currentItem;
        }
        return null;
    }

    // ----------------------------------------------------
    // [UI 갱신] (Event 기반으로 변경됨)
    // ----------------------------------------------------

    // 이전에 UI를 직접 갱신하던 private 메서드는 더 이상 필요하지 않을 수 있습니다. 
    // EquipmentSlotUI.cs에서 OnEquipmentChanged 이벤트를 구독하도록 수정합니다.
}