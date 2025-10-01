using UnityEngine;
using System.Collections.Generic;
using System;

public class QuickSlotManager : MonoBehaviour
{
    [Header("Dependencies")]
    // Inventory 참조 필수 (소모품 제거 및 무기 사용 로직 호출)
    [SerializeField] private Inventory inventoryManager;
    // EquipmentManager 참조 (장착 정보 확인용)
    [SerializeField] private EquipmentManager equipmentManager;
    // QuickSlotUI 참조 (UI 갱신용)
    [SerializeField] private QuickSlotUI quickSlotUI;

    [Header("Quick Slot Settings")]
    [SerializeField] private int slotCount = 5;

    // 퀵슬롯에 할당된 아이템 데이터 (5칸)
    private ItemBaseSO[] quickSlots;

    // 현재 선택된 퀵슬롯 인덱스 (0부터 4)
    private int currentSlotIndex = 0;

    private void Awake()
    {
        quickSlots = new ItemBaseSO[slotCount];
    }

    // ----------------------------------------------------
    // [아이템 사용 로직] - VR Action Button과 연결
    // ----------------------------------------------------
    // 현재 선택된 슬롯의 아이템을 사용합니다.
    public void UseCurrentSlotItem()
    {
        ItemBaseSO item = quickSlots[currentSlotIndex];
        if (item == null) return;

        // 1. 무기 장비 (Weapon)
        if (item.itemType == ItemType.Weapon)
        {
            // Inventory에 무기 장착 요청 (Inventory가 EquipManager 호출)
            inventoryManager.UseItemByData(item); 
        }
        // 2. 소모품 (Consumable)
        else if (item.itemType == ItemType.Consumable)
{
    ConsumableItemSO consumable = item as ConsumableItemSO;

    // Inventory에 소모 요청 (Inventory가 개수 체크 및 제거 담당)
    if (inventoryManager.RemoveItemByData(item, 1))
    {
        // TODO: PlayerStats.Restore(consumable.effectType, consumable.restoreAmount) 호출
        Debug.Log($"Used consumable: {item.itemName}.");

        // TODO: 퀵슬롯 아이콘 업데이트 이벤트 발생 (스택 0이 될 경우)
    }
}
    }

    // ----------------------------------------------------
    // [슬롯 선택 로직] - VR Wheel Input 또는 Scroll Input과 연결
    // ----------------------------------------------------
    public void SelectSlot(int index)
    {
        if (index < 0 || index >= slotCount) return;
currentSlotIndex = index;

if (quickSlotUI != null)
{
    quickSlotUI.UpdateSelection(currentSlotIndex);
}
    }

    // ----------------------------------------------------
    // [퀵슬롯 할당 로직] - VRSlotInteraction에서 호출됨
    // ----------------------------------------------------
    // 인벤토리에서 퀵슬롯으로 드롭될 때 호출됩니다.
    public void AssignItemToSlot(ItemBaseSO item, int slotIndex)
    {
    if (slotIndex < 0 || slotIndex >= slotCount) return;

    // 무기 또는 소모품만 할당 가능
    if (item.itemType == ItemType.Weapon || item.itemType == ItemType.Consumable)
    {
        quickSlots[slotIndex] = item;

        if (quickSlotUI != null)
        {
            quickSlotUI.UpdateSlot(slotIndex, item);
        }
    }
}
}