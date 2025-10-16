using UnityEngine;
using System.Linq;

// InventorySlot, ItemBaseSO 등 외부 정의는 생략합니다.

public class Inventory : MonoBehaviour
{
    // --- [싱글톤 인스턴스] ---
    public static Inventory Instance { get; private set; }

    [Header("Dependencies")]
    [SerializeField] private int capacity = 30;
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
            // 🔥 초기 아이템 로딩 문제 해결: 슬롯을 Empty 상태로 초기화합니다.
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
    /// 🔥 [핵심 수정] 특정 인덱스의 슬롯에 아이템이 없는지 확인합니다. 퀵슬롯 인덱스도 처리합니다.
    /// </summary>
    public bool IsSlotEmpty(int index)
    {
        // 1. 순수 인벤토리 슬롯 범위 체크 (0 <= index < capacity)
        if (index >= 0 && index < capacity)
        {
            return slots[index].IsEmpty;
        }

        // 2. 퀵슬롯 범위 체크 및 처리
        if (quickSlotManager != null && quickSlotManager.IsQuickSlotIndex(index))
        {
            // QuickSlotManager.IsQuickSlotEmpty를 호출하여 실제 퀵슬롯 데이터 확인
            return quickSlotManager.IsQuickSlotEmpty(index);
        }

        // 3. 범위를 벗어난 인덱스인 경우
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
                Debug.LogError($"UI 연결 오류: Inventory Slot UIs 배열의 Element {i}가 연결되지 않았습니다. Inspector를 확인하세요.");
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
                // 🔥 초기 아이템 로딩 문제 해결: 새로운 아이템 슬롯을 할당합니다.
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
        if (slotIndex < 0 || slotIndex >= capacity || slots[slotIndex].IsEmpty) return false;

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

    // V------------------ [수정된 SwapSlots 함수: QuickSlot Manager 연동 로직 우선 처리] ------------------V
    /// <summary>
    /// 두 인벤토리 슬롯의 아이템 위치를 서로 교환합니다.
    /// QuickSlotManager가 연결되어 있으면 QuickSlot과의 교환도 처리합니다.
    /// </summary>
    public void SwapSlots(int indexA, int indexB)
    {
        // 1. QuickSlotManager 연동 확인 (가장 먼저 수행)
        bool isAQuickSlot = false;
        bool isBQuickSlot = false;

        if (quickSlotManager != null)
        {
            isAQuickSlot = quickSlotManager.IsQuickSlotIndex(indexA);
            isBQuickSlot = quickSlotManager.IsQuickSlotIndex(indexB);
        }

        if (isAQuickSlot || isBQuickSlot)
        {
            // 하나 이상의 슬롯이 퀵슬롯에 속하는 경우, QuickSlotManager에 처리를 위임
            if (quickSlotManager != null)
            {
                quickSlotManager.HandleInventorySwap(indexA, indexB);
                Debug.Log($"[Inventory] Swap Delegated to QuickSlotManager: Indices ({indexA}, {indexB})");
                return; // 퀵슬롯 매니저가 처리했으므로 종료
            }
        }

        // 2. 순수 인벤토리 슬롯 간의 교환 (위임 실패 또는 퀵슬롯이 아닌 경우)

        // 유효성 검사를 이 시점에 다시 수행합니다. (순수 인벤토리 범위 내인지 확인)
        if (indexA < 0 || indexA >= capacity || indexB < 0 || indexB >= capacity)
        {
            Debug.LogWarning($"[Inventory] SwapSlots Error: 유효하지 않은 순수 인벤토리 인덱스 ({indexA}, {indexB}). QuickSlotManager 연결/처리 오류.");
            return;
        }

        // 3. 실제 스왑 실행
        InventorySlot temp = slots[indexA];
        slots[indexA] = slots[indexB];
        slots[indexB] = temp;

        Debug.Log($"[Inventory] Swap Success: Slot {indexA} <-> Slot {indexB} (Internal Inventory Swap)");
        RefreshAllInventoryUI();
    }
    // A-----------------------------------------------------------------------------A

    // ----------------------------------------------------
    // [내부 호출 함수]
    // ----------------------------------------------------

    private bool TryEquipItem(int slotIndex, EquippableItemSO equipItem)
    {
        slots[slotIndex] = InventorySlot.Empty;

        // EquippableItemSO oldItem = equipmentManager.Equip(equipItem); // 실제 로직은 그대로 둡니다.

        // if (oldItem != null)
        // {
        //     slots[slotIndex] = new InventorySlot(oldItem, 1); // 실제 로직은 그대로 둡니다.
        // }

        RefreshAllInventoryUI();
        return true;
    }

    private bool ConsumeItem(int slotIndex)
    {
        ConsumableItemSO consumable = slots[slotIndex].itemData as ConsumableItemSO;
        if (consumable == null) return false;

        Debug.Log($"[Inventory] Consuming {consumable.itemName} from slot {slotIndex}");

        RemoveItem(slotIndex, 1);
        return true;
    }
}