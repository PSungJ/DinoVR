using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic; // Dictionary 사용을 위해 추가될 수 있음

// InventorySlot, ItemBaseSO, EquippableItemSO, ConsumableItemSO, ItemType 등 외부 정의는 생략합니다.
// PlayerHealthComponent, SlotUIUpdater 등 외부 정의는 생략합니다.

public class Inventory : MonoBehaviour
{
    // --- [싱글톤 인스턴스] ---
    public static Inventory Instance { get; private set; }

    [Header("Dependencies")]
    [SerializeField] private int capacity = 30;

    // 인벤토리 인덱스 범위: 0 ~ 29 (총 30개)
    public int Capacity => capacity;

    [SerializeField] private EquipmentManager equipmentManager;
    [SerializeField] private QuickSlotManager quickSlotManager;

    // 인벤토리의 실제 아이템 데이터를 저장하는 배열
    public InventorySlot[] slots; // InventorySlot은 외부에서 정의됨

    [Header("Initial Setup (Test)")]
    [SerializeField] private EquippableItemSO initialWeapon;
    [SerializeField] private ConsumableItemSO initialConsumable;
    [SerializeField] private int initialConsumableAmount = 5;

    [Header("UI References")]
    [SerializeField] private SlotUIUpdater[] inventorySlotUIs;


    private void Awake()
    {
        // --- [싱글톤 초기화 로직] ---
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 인벤토리 슬롯 배열 초기화
        slots = new InventorySlot[capacity];
        for (int i = 0; i < capacity; i++)
        {
            slots[i] = InventorySlot.Empty;
        }
    }

    private void Start()
    {
        // 1. 초기 아이템 할당 로직
        if (initialWeapon != null)
        {
            AddItem(initialWeapon, 1);
        }

        if (initialConsumable != null)
        {
            AddItem(initialConsumable, initialConsumableAmount);
        }

        // 2. 초기화 완료 후 UI 전체 갱신
        RefreshAllInventoryUI();
    }

    /// <summary>
    /// 특정 인덱스의 슬롯에 아이템이 없는지 확인합니다. 모든 슬롯 인덱스를 처리합니다.
    /// </summary>
    public bool IsSlotEmpty(int index)
    {
        // 1. 순수 인벤토리 슬롯 범위 체크 (0 <= index < capacity)
        if (index >= 0 && index < capacity)
        {
            return slots[index].IsEmpty;
        }

        // 2. 퀵슬롯 범위 체크 및 처리 (30 ~ 32)
        if (quickSlotManager != null && quickSlotManager.IsQuickSlotIndex(index))
        {
            return quickSlotManager.IsQuickSlotEmpty(index);
        }

        // 3. 장비슬롯 범위 채크 및 처리 (33 ~ 34)
        if (equipmentManager != null && equipmentManager.IsEquipmentSlotIndex(index))
        {
            return equipmentManager.IsEquipmentSlotEmpty(index);
        }

        // 4. 범위를 벗어난 인덱스인 경우
        return true;
    }

    // ----------------------------------------------------
    // [UI 갱신 로직]
    // ----------------------------------------------------
    public void RefreshAllInventoryUI()
    {
        for (int i = 0; i < capacity; i++)
        {
            if (inventorySlotUIs == null || i >= inventorySlotUIs.Length || inventorySlotUIs[i] == null)
            {
                Debug.LogWarning($"UI 연결 오류: Inventory Slot UIs 배열의 Element {i}가 연결되지 않았습니다. Inspector를 확인하세요.");
                continue;
            }

            inventorySlotUIs[i].UpdateSlotUI(slots[i].itemData, slots[i].stackSize);
        }
    }

    // ----------------------------------------------------
    // [아이템 재고 관리 로직]
    // ----------------------------------------------------
    public bool AddItem(ItemBaseSO itemToAdd, int amount)
    {
        if (itemToAdd == null || amount <= 0)
        {
            Debug.LogWarning("[Inventory] AddItem: 추가할 아이템 또는 수량이 유효하지 않습니다.");
            return false;
        }

        // 1. 스택 가능한 아이템이라면, 기존 슬롯에 스택을 쌓습니다.
        if (itemToAdd.maxStackSize > 1)
        {
            for (int i = 0; i < capacity; i++)
            {
                if (slots[i].itemData == itemToAdd && slots[i].stackSize < itemToAdd.maxStackSize)
                {
                    slots[i].stackSize += amount;
                    slots[i].stackSize = Mathf.Min(slots[i].stackSize, itemToAdd.maxStackSize);

                    Debug.Log($"[Inventory] Item Stacked in Slot {i}: {itemToAdd.itemName}, New Amount: {slots[i].stackSize}");
                    RefreshAllInventoryUI();
                    return true;
                }
            }
        }

        // 2. 빈 슬롯을 찾아서 추가합니다.
        for (int i = 0; i < capacity; i++)
        {
            if (slots[i].IsEmpty)
            {
                slots[i] = new InventorySlot(itemToAdd, amount);

                Debug.Log($"[Inventory] Item Added to Slot {i}: {itemToAdd.itemName}, Amount: {amount}");
                RefreshAllInventoryUI();
                return true;
            }
        }

        Debug.LogWarning($"[Inventory] AddItem failed for {itemToAdd.itemName}. Inventory is full.");
        return false;
    }

    public void RemoveItem(int slotIndex, int amount)
    {
        if (slotIndex >= 0 && slotIndex < capacity && !slots[slotIndex].IsEmpty)
        {
            slots[slotIndex].stackSize -= amount;
            if (slots[slotIndex].stackSize <= 0)
            {
                slots[slotIndex] = InventorySlot.Empty;
            }
            RefreshAllInventoryUI();
        }
    }

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

    public bool UseItem(int slotIndex)
    {
        // 1. 퀵슬롯 위임 로직
        if (quickSlotManager != null && quickSlotManager.IsQuickSlotIndex(slotIndex))
        {
            Debug.Log($"[Inventory] UseItem Delegated to QuickSlotManager for index: {slotIndex}");
            return quickSlotManager.HandleQuickSlotUse(slotIndex);
        }

        // 2. 메인 인벤토리 로직
        if (slotIndex < 0 || slotIndex >= capacity || slots[slotIndex].IsEmpty)
        {
            Debug.LogWarning($"[Inventory] UseItem failed: Index {slotIndex} is outside main inventory bounds or is empty.");
            return false;
        }

        ItemBaseSO item = slots[slotIndex].itemData;

        if (item is EquippableItemSO equipItem)
        {
            return TryEquipItem(slotIndex, equipItem);
        }
        else if (item.itemType == ItemType.Consumable)
        {
            return ConsumeItem(slotIndex);
        }

        return false;
    }

    public bool UseItemByData(ItemBaseSO itemToUse)
    {
        for (int i = 0; i < capacity; i++)
        {
            if (slots[i].itemData == itemToUse)
            {
                return UseItem(i);
            }
        }
        return false;
    }

    /// <summary>
    /// 두 인벤토리 슬롯의 아이템 위치를 서로 교환합니다.
    /// QuickSlotManager 또는 EquipmentManager가 연결되어 있으면 교환을 위임합니다.
    /// </summary>
    public void SwapSlots(int indexA, int indexB)
    {
        if (indexA == indexB) return;

        // 1. 위임 가능성 확인
        bool isAQuickSlot = quickSlotManager != null && quickSlotManager.IsQuickSlotIndex(indexA);
        bool isBQuickSlot = quickSlotManager != null && quickSlotManager.IsQuickSlotIndex(indexB);
        bool isAEquipmentSlot = equipmentManager != null && equipmentManager.IsEquipmentSlotIndex(indexA);
        bool isBEquipmentSlot = equipmentManager != null && equipmentManager.IsEquipmentSlotIndex(indexB);

        // 2. 위임 처리

        // 2-A. 퀵슬롯이 관련되어 있다면 QuickSlotManager에 위임
        if (isAQuickSlot || isBQuickSlot)
        {
            if (quickSlotManager != null)
            {
                quickSlotManager.HandleInventorySwap(indexA, indexB);
                Debug.Log($"[Inventory] Swap Delegated to QuickSlotManager: Indices ({indexA}, {indexB})");
                return;
            }
        }

        // 2-B. 퀵슬롯이 아니며, 장비 슬롯이 관련되어 있다면 장비 슬롯 처리 로직으로 이동
        if (isAEquipmentSlot || isBEquipmentSlot)
        {
            if (equipmentManager != null)
            {
                // 장비 슬롯과의 스왑 로직 수행
                HandleEquipmentSlotSwap(indexA, indexB, isAEquipmentSlot, isBEquipmentSlot);
                Debug.Log($"[Inventory] Swap Handled by Equipment Logic: Indices ({indexA}, {indexB})");
                return;
            }
        }

        // 3. 순수 인벤토리 슬롯 간의 교환 (위임 실패 또는 퀵/장비 슬롯이 아닌 경우)
        if (indexA < 0 || indexA >= capacity || indexB < 0 || indexB >= capacity)
        {
            // 이 로직은 이제 순수 인벤토리 인덱스가 유효하지 않을 때만 실행됩니다.
            Debug.LogWarning($"[Inventory] SwapSlots Error: 유효하지 않은 인덱스 ({indexA}, {indexB}). 위임 처리 실패.");
            return;
        }

        // 4. 실제 순수 인벤토리 스왑 실행
        InventorySlot temp = slots[indexA];
        slots[indexA] = slots[indexB];
        slots[indexB] = temp;

        Debug.Log($"[Inventory] Swap Success: Slot {indexA} <-> Slot {indexB} (Internal Inventory Swap)");
        RefreshAllInventoryUI();
    }

    // ----------------------------------------------------
    // [내부 호출 함수]
    // ----------------------------------------------------

    private bool TryEquipItem(int slotIndex, EquippableItemSO equipItem)
    {
        // 1. 인벤토리 슬롯 비우기
        slots[slotIndex] = InventorySlot.Empty;

        // 2. 장비 장착 요청
        EquippableItemSO oldItem = equipmentManager.Equip(equipItem, slotIndex);

        if (oldItem != null)
        {
            // 3. 이전 장비 아이템을 다시 인벤토리의 해당 슬롯에 되돌려 놓습니다.
            slots[slotIndex] = new InventorySlot(oldItem, 1);
        }

        RefreshAllInventoryUI();
        return true;
    }

    private bool ConsumeItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= capacity || slots[slotIndex].IsEmpty) return false;

        ConsumableItemSO consumable = slots[slotIndex].itemData as ConsumableItemSO;
        if (consumable == null) return false;

        Debug.Log($"[Inventory] Consuming {consumable.itemName} from slot {slotIndex}");

        PlayerHealthComponent playerHealth = PlayerHealthComponent.Instance;
        EquipmentManager equipmentManager = EquipmentManager.Instance;

        if (playerHealth == null || equipmentManager == null)
        {
            Debug.LogError("[Inventory] PlayerHealthComponent 또는 EquipmentManager 인스턴스를 찾을 수 없습니다. 아이템 효과가 적용되지 않았습니다.");
        }
        else
        {
            consumable.Use(slotIndex, playerHealth, equipmentManager);
        }

        RemoveItem(slotIndex, 1);
        return true;
    }

    // ----------------------------------------------------
    // [장비 슬롯 처리 함수]
    // ----------------------------------------------------

    private void HandleEquipmentSlotSwap(int indexA, int indexB, bool isAEquipmentSlot, bool isBEquipmentSlot)
    {
        // 1. A와 B 모두 장비 슬롯인 경우 (장비 슬롯 간 스왑)
        if (isAEquipmentSlot && isBEquipmentSlot)
        {
            EquippableItemSO itemA = equipmentManager.GetEquippedItemByIndex(indexA);
            EquippableItemSO itemB = equipmentManager.GetEquippedItemByIndex(indexB);

            // 서로 아이템 교환
            equipmentManager.SwapEquippedItem(indexA, itemB);
            equipmentManager.SwapEquippedItem(indexB, itemA);

            return;
        }

        // 2. 인벤토리 슬롯 <-> 장비 슬롯 스왑 (A=Inventory, B=Equipment이거나 그 반대)
        int inventoryIndex = isAEquipmentSlot ? indexB : indexA;
        int equipmentIndex = isAEquipmentSlot ? indexA : indexB;

        // 인벤토리 인덱스가 순수 인벤토리 범위 내인지 최종 확인
        if (inventoryIndex < 0 || inventoryIndex >= capacity)
        {
            Debug.LogError($"[Inventory] 장비 스왑 오류: 인벤토리 인덱스 {inventoryIndex}가 범위를 벗어났습니다.");
            return;
        }

        InventorySlot slotDataFromInventory = slots[inventoryIndex];
        EquippableItemSO itemFromEquipment = equipmentManager.GetEquippedItemByIndex(equipmentIndex);

        EquippableItemSO itemToEquip = slotDataFromInventory.itemData as EquippableItemSO;

        if (itemToEquip != null)
        {
            // 3. 인벤토리 아이템을 장비 슬롯에 장착/교체 요청
            EquippableItemSO oldItemFromEquipment = equipmentManager.SwapEquippedItem(equipmentIndex, itemToEquip);

            // 4. 장비 슬롯에서 해제된 아이템을 인벤토리 슬롯에 넣기
            slots[inventoryIndex] = oldItemFromEquipment != null ? new InventorySlot(oldItemFromEquipment, 1) : InventorySlot.Empty;
        }
        else if (slotDataFromInventory.IsEmpty)
        {
            // 인벤토리 슬롯이 비어있고, 장비 슬롯에 아이템이 있는 경우 (장비 해제 요청)
            if (itemFromEquipment != null)
            {
                // 장비 슬롯 비우기 및 아이템 반환 (SwapEquippedItem에 null 전달)
                EquippableItemSO unequippedItem = equipmentManager.SwapEquippedItem(equipmentIndex, null);

                // 인벤토리 슬롯에 해제된 아이템 넣기
                slots[inventoryIndex] = new InventorySlot(unequippedItem, 1);
            }
        }
        // 인벤토리 아이템이 장비 불가능한 아이템인 경우 -> 스왑 불가.
        else
        {
            Debug.LogWarning($"[Inventory] 장비 슬롯 스왑 실패: 인벤토리 아이템({slotDataFromInventory.itemData.itemName})은 장비 가능한 아이템이 아닙니다.");
            return;
        }

        RefreshAllInventoryUI();
    }
}