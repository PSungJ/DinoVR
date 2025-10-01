using UnityEngine;
using System.Linq;

public class Inventory : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private int capacity = 30;
    // EquipmentManager 참조 필수 (장착 로직 호출)
    [SerializeField] private EquipmentManager equipmentManager; 
    // QuickSlotManager 참조 필수 (AddItem 등의 이벤트 처리용)
    [SerializeField] private QuickSlotManager quickSlotManager; 

    public InventorySlot[] slots; 

    private void Awake()
    {
        slots = new InventorySlot[capacity];
    }
    
    // ----------------------------------------------------
    // [아이템 재고 관리 로직]
    // ----------------------------------------------------
    
    // 아이템 추가 로직
    public bool AddItem(ItemBaseSO itemToAdd, int amount)
    {
        // 1. 스택 가능한 슬롯을 찾아서 추가
        if (itemToAdd.maxStackSize > 1)
        {
            for (int i = 0; i < capacity; i++)
            {
                if (slots[i].itemData == itemToAdd && slots[i].stackSize < itemToAdd.maxStackSize)
                {
                    slots[i].stackSize += amount;
                    slots[i].stackSize = Mathf.Min(slots[i].stackSize, itemToAdd.maxStackSize);
                    // TODO: UI 업데이트 이벤트 발생
                    return true;
                }
            }
        }

        // 2. 빈 슬롯을 찾아서 추가
        for (int i = 0; i < capacity; i++)
            {
            if (slots[i].IsEmpty)
            {
                slots[i] = new InventorySlot(itemToAdd, amount);
                // TODO: UI 업데이트 이벤트 발생
                return true;
            }
        }
        return false; // 인벤토리 가득 참
    }

    // 인덱스와 수량을 기반으로 아이템 제거
    public void RemoveItem(int slotIndex, int amount)
    {
        if (slotIndex >= 0 && slotIndex < capacity && !slots[slotIndex].IsEmpty)
        {
            slots[slotIndex].stackSize -= amount;
            if (slots[slotIndex].stackSize <= 0)
            {
                slots[slotIndex] = InventorySlot.Empty; // 💡 수정됨
            }
            // TODO: UI 업데이트 이벤트 발생 (해당 슬롯)
        }
    }

    // 아이템 데이터로 아이템을 찾아 제거합니다. (소모품 사용 시)
    public bool RemoveItemByData(ItemBaseSO itemToRemove, int amount)
    {
        for (int i = 0; i < capacity; i++)
        {
            if (slots[i].itemData == itemToRemove && slots[i].stackSize >= amount)
            {
                RemoveItem(i, amount);
                return true;
            }
        }
        return false;
    }
    
    // ----------------------------------------------------
    // [VR 상호작용 및 사용 로직]
    // ----------------------------------------------------
    
    // 인벤토리 슬롯에서 아이템을 사용 (장착 또는 소모)
    public bool UseItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= capacity || slots[slotIndex].IsEmpty) return false;
        
        ItemBaseSO item = slots[slotIndex].itemData;
        
        // EquippableItemSO (장비)
        if (item is EquippableItemSO equipItem) return TryEquipItem(slotIndex, equipItem);
        // ConsumableItemSO (소모품)
        else if (item.itemType == ItemType.Consumable) return ConsumeItem(slotIndex);
        
        return false;
    }

    // 퀵슬롯 무기 장착을 위한 헬퍼 함수
    public bool UseItemByData(ItemBaseSO itemToUse)
    {
        for (int i = 0; i < capacity; i++)
        {
            if (slots[i].itemData == itemToUse)
            {
                return UseItem(i); // 찾은 아이템의 인덱스로 UseItem 로직 재활용
            }
        }
        return false;
    }
    
    // 슬롯 위치 교환 (VR Grab 버튼과 연결)
    public void SwapSlots(int indexA, int indexB)
    {
    if (indexA < 0 || indexA >= capacity || indexB < 0 || indexB >= capacity) return;
        
        InventorySlot temp = slots[indexA];
        slots[indexA] = slots[indexB];
        slots[indexB] = temp;
        // TODO: UI 업데이트 이벤트 발생
    }

    // ----------------------------------------------------
    // [내부 호출 함수]
    // ----------------------------------------------------

    // 장비 장착 시도 로직 (아이템을 EquipmentManager로 보내고, 되돌아온 아이템은 슬롯에 재배치)
    private bool TryEquipItem(int slotIndex, EquippableItemSO equipItem)
    {
        slots[slotIndex] = InventorySlot.Empty; //  수정됨: 인벤토리 슬롯에서 제거
        
        EquippableItemSO oldItem = equipmentManager.Equip(equipItem); // 장착 요청

        // 반환된 기존 아이템이 있다면 인벤토리로 되돌림 (Swap)
        if (oldItem != null)
        {
            slots[slotIndex] = new InventorySlot(oldItem, 1);
        }
        
        // TODO: UI 전체 업데이트 이벤트 발생
        return true;
    }

    // 소모품 사용 로직
    private bool ConsumeItem(int slotIndex)
    {
        ConsumableItemSO consumable = slots[slotIndex].itemData as ConsumableItemSO;
        if (consumable == null) return false;

        // TODO: PlayerStats.Restore(...) 호출 (스탯 회복 로직)
        
        RemoveItem(slotIndex, 1); // 소모품 1개 제거
        return true;
    }
}