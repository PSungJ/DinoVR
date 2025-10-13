using UnityEngine;
using System.Linq; // List.Where 등의 LINQ 확장 메서드를 사용한다면 필요

// 인벤토리 슬롯의 데이터 구조 (예: InventorySlotData 구조체)
// 이전에 정의한 InventorySlotData를 사용하거나, 아래와 같이 클래스로 정의할 수 있습니다.
// 스크립트 오브젝트 (ScriptableObject)로 정의하는 것이 더 일반적입니다.
// 현재 코드에서는 ItemBaseSO와 int stackSize를 포함하는 InventorySlot 클래스를 사용하고 있습니다.

// 이 클래스는 InventorySlotData 대신 InventorySlot 자체를 저장합니다.
// InventorySlot 구조체 또는 클래스가 별도로 정의되어 있다고 가정합니다.
// 예시:
/*
[System.Serializable]
public class InventorySlot
{
    public ItemBaseSO itemData;
    public int stackSize;
    public bool IsEmpty => itemData == null || stackSize <= 0;

    public InventorySlot(ItemBaseSO item, int amount)
    {
        itemData = item;
        stackSize = amount;
    }

    public static InventorySlot Empty => new InventorySlot(null, 0);
}
*/

// ItemBaseSO, EquippableItemSO, ConsumableItemSO, ItemType 등의 정의가 필요합니다.
// 이 부분은 사용자님의 기존 프로젝트에 맞춰져 있을 것입니다.

public class Inventory : MonoBehaviour
{
    // --- [싱글톤 인스턴스] ---
    // 게임 내에서 이 Inventory 클래스의 유일한 인스턴스에 접근하기 위한 정적 필드
    public static Inventory Instance { get; private set; } // 어디서든 Inventory.Instance.AddItem()으로 접근 가능

    [Header("Dependencies")]
    [SerializeField] private int capacity = 30; // 인벤토리 슬롯의 총 개수

    // EquipmentManager 참조 (아이템 장착 로직을 호출하기 위함)
    [SerializeField] private EquipmentManager equipmentManager;
    // QuickSlotManager 참조 (아이템 추가 등의 이벤트 발생 시 QuickSlotManager에 알리기 위함)
    [SerializeField] private QuickSlotManager quickSlotManager;

    // 인벤토리의 실제 아이템 데이터를 저장하는 배열
    public InventorySlot[] slots;

    [Header("Initial Setup (Test)")]
    [SerializeField] private EquippableItemSO initialWeapon; // 초기 무기 아이템 (테스트용)
    [SerializeField] private ConsumableItemSO initialConsumable; // 초기 소모품 아이템 (테스트용)
    [SerializeField] private int initialConsumableAmount = 5; // 초기 소모품 수량

    [Header("UI References")]
    // 각 인벤토리 슬롯 UI 오브젝트에 붙어있는 SlotUIUpdater 스크립트들을 참조
    // 이 배열은 Inspector에서 직접 연결해주어야 합니다.
    [SerializeField] private SlotUIUpdater[] inventorySlotUIs; // UI 갱신 담당 스크립트 참조


    private void Awake()
    {
        // --- [싱글톤 초기화 로직] ---
        // Inventory 클래스의 인스턴스가 아직 없다면, 이 오브젝트를 인스턴스로 설정
        if (Instance == null)
        {
            Instance = this;
            // 만약 인벤토리가 씬이 바뀌어도 유지되어야 한다면 주석 해제
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            // 이미 인스턴스가 존재한다면, 현재 오브젝트는 중복이므로 파괴
            Debug.LogWarning("Inventory: 이미 다른 인스턴스가 존재합니다. 이 오브젝트를 제거합니다.");
            Destroy(gameObject);
            return; // 중복 인스턴스라면 Awake 이후의 코드를 실행하지 않음
        }

        // 인벤토리 슬롯 배열 초기화
        slots = new InventorySlot[capacity];
        // 모든 슬롯을 비어있는 상태로 초기화합니다.
        for (int i = 0; i < capacity; i++)
        {
            slots[i] = InventorySlot.Empty;
        }
    }

    private void Start()
    {
        // 1. 초기 아이템 할당 로직 (테스트용)
        // 게임 시작 시 미리 정해진 아이템들을 인벤토리에 추가합니다.
        if (initialWeapon != null)
        {
            AddItem(initialWeapon, 1);
        }

        if (initialConsumable != null)
        {
            AddItem(initialConsumable, initialConsumableAmount);
        }

        // 2. 초기화 완료 후 UI 전체 갱신
        // 아이템이 추가된 후, 인벤토리 UI를 한번 갱신하여 화면에 표시합니다.
        RefreshAllInventoryUI();
    }

    // ----------------------------------------------------
    // [UI 갱신 로직]
    // ----------------------------------------------------

    /// <summary>
    /// 인벤토리의 모든 슬롯 UI를 현재 InventorySlot[] 데이터에 맞춰 갱신합니다.
    /// 데이터 변경 시 (아이템 추가, 제거, 교환 등) 호출되어야 합니다.
    /// </summary>
    private void RefreshAllInventoryUI()
    {
        for (int i = 0; i < capacity; i++)
        {
            // UI 참조가 제대로 연결되어 있는지 확인합니다.
            if (inventorySlotUIs == null || i >= inventorySlotUIs.Length || inventorySlotUIs[i] == null)
            {
                Debug.LogError($"UI 연결 오류: Inventory Slot UIs 배열의 Element {i}가 연결되지 않았습니다. Inspector를 확인하세요.");
                continue; // 다음 슬롯으로 넘어감
            }

            // SlotUIUpdater 스크립트의 UpdateSlotUI 함수를 호출하여 UI를 갱신합니다.
            inventorySlotUIs[i].UpdateSlotUI(slots[i].itemData, slots[i].stackSize);
        }
    }

    // ----------------------------------------------------
    // [아이템 재고 관리 로직]
    // ----------------------------------------------------

    /// <summary>
    /// 인벤토리에 아이템을 추가합니다.
    /// </summary>
    /// <param name="itemToAdd">추가할 아이템 데이터</param>
    /// <param name="amount">추가할 수량</param>
    /// <returns>아이템 추가 성공 여부</returns>
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
                // 동일한 아이템이 있고, 해당 슬롯이 스택 가능한 최대치를 넘지 않았다면
                if (slots[i].itemData == itemToAdd && slots[i].stackSize < itemToAdd.maxStackSize)
                {
                    slots[i].stackSize += amount; // 수량 추가
                    slots[i].stackSize = Mathf.Min(slots[i].stackSize, itemToAdd.maxStackSize); // 최대 스택 제한

                    Debug.Log($"[Inventory] Item Stacked in Slot {i}: {itemToAdd.itemName}, New Amount: {slots[i].stackSize}");
                    RefreshAllInventoryUI(); // UI 갱신 호출
                    return true;
                }
            }
        }

        // 2. 스택할 곳이 없거나 스택 불가능한 아이템이라면, 빈 슬롯을 찾아서 추가합니다.
        for (int i = 0; i < capacity; i++)
        {
            if (slots[i].IsEmpty) // 슬롯이 비어있다면
            {
                slots[i] = new InventorySlot(itemToAdd, amount); // 새로운 아이템으로 슬롯 채움

                Debug.Log($"[Inventory] Item Added to Slot {i}: {itemToAdd.itemName}, Amount: {amount}");
                RefreshAllInventoryUI(); // UI 갱신 호출
                return true;
            }
        }

        // 3. 인벤토리가 가득 차서 아이템을 추가할 수 없을 때
        Debug.LogWarning($"[Inventory] AddItem failed for {itemToAdd.itemName}. Inventory is full.");
        return false;
    }

    /// <summary>
    /// 특정 인덱스의 슬롯에서 아이템을 제거합니다.
    /// </summary>
    /// <param name="slotIndex">제거할 아이템이 있는 슬롯의 인덱스</param>
    /// <param name="amount">제거할 수량</param>
    public void RemoveItem(int slotIndex, int amount)
    {
        if (slotIndex >= 0 && slotIndex < capacity && !slots[slotIndex].IsEmpty)
        {
            slots[slotIndex].stackSize -= amount; // 수량 감소
            if (slots[slotIndex].stackSize <= 0) // 수량이 0 이하라면 슬롯 비움
            {
                slots[slotIndex] = InventorySlot.Empty;
            }
            RefreshAllInventoryUI(); // UI 갱신 호출
        }
    }

    /// <summary>
    /// 특정 아이템 데이터에 해당하는 아이템을 인벤토리에서 찾아 제거합니다.
    /// (주로 소모품 사용 시)
    /// </summary>
    /// <param name="itemToRemove">제거할 아이템 데이터</param>
    /// <param name="amount">제거할 수량</param>
    /// <returns>아이템 제거 성공 여부</returns>
    public bool RemoveItemByData(ItemBaseSO itemToRemove, int amount)
    {
        for (int i = 0; i < capacity; i++)
        {
            if (slots[i].itemData == itemToRemove && slots[i].stackSize >= amount)
            {
                RemoveItem(i, amount); // 해당 슬롯에서 제거
                return true;
            }
        }
        return false;
    }

    // ----------------------------------------------------
    // [VR 상호작용 및 사용 로직]
    // ----------------------------------------------------

    /// <summary>
    /// 인벤토리 슬롯에 있는 아이템을 사용 (장착 또는 소모)합니다.
    /// VR 컨트롤러의 Select 입력 시 호출됩니다.
    /// </summary>
    /// <param name="slotIndex">사용할 아이템이 있는 슬롯의 인덱스</param>
    /// <returns>아이템 사용 성공 여부</returns>
    public bool UseItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= capacity || slots[slotIndex].IsEmpty) return false;

        ItemBaseSO item = slots[slotIndex].itemData;

        // 아이템 타입에 따라 다른 사용 로직 호출
        if (item is EquippableItemSO equipItem) // 장비 아이템일 경우
        {
            return TryEquipItem(slotIndex, equipItem);
        }
        else if (item.itemType == ItemType.Consumable) // 소모품 아이템일 경우
        {
            return ConsumeItem(slotIndex);
        }

        return false; // 기타 정의되지 않은 아이템 타입
    }

    /// <summary>
    /// 아이템 데이터로 인벤토리에서 아이템을 찾아 사용합니다. (퀵슬롯 연동 등)
    /// </summary>
    /// <param name="itemToUse">사용할 아이템 데이터</param>
    /// <returns>아이템 사용 성공 여부</returns>
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

    /// <summary>
    /// 두 인벤토리 슬롯의 아이템 위치를 서로 교환합니다.
    /// VR Grab & Drop 상호작용 후 호출되어야 합니다.
    /// </summary>
    /// <param name="indexA">첫 번째 슬롯의 인덱스</param>
    /// <param name="indexB">두 번째 슬롯의 인덱스</param>
    public void SwapSlots(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= capacity || indexB < 0 || indexB >= capacity)
        {
            Debug.LogWarning($"[Inventory] SwapSlots Error: 유효하지 않은 인덱스 ({indexA}, {indexB})");
            return;
        }

        // 임시 변수를 사용한 표준 교환 로직
        InventorySlot temp = slots[indexA];
        slots[indexA] = slots[indexB];
        slots[indexB] = temp;

        Debug.Log($"[Inventory] Swap Success: Slot {indexA} <-> Slot {indexB}"); // 로직 성공 로그
        RefreshAllInventoryUI(); // UI 갱신 호출
    }

    // ----------------------------------------------------
    // [내부 호출 함수]
    // ----------------------------------------------------

    // 장비 장착 시도 로직
    // (아이템을 EquipmentManager로 보내고, 되돌아온 아이템은 슬롯에 재배치)
    private bool TryEquipItem(int slotIndex, EquippableItemSO equipItem)
    {
        // 1. 인벤토리 해당 슬롯을 비웁니다.
        slots[slotIndex] = InventorySlot.Empty;

        // 2. EquipmentManager에 장착 요청을 합니다. (기존 장비가 있다면 반환됩니다)
        EquippableItemSO oldItem = equipmentManager.Equip(equipItem);

        // 3. 반환된 기존 아이템이 있다면 인벤토리 빈 슬롯으로 되돌립니다.
        if (oldItem != null)
        {
            // 이전에 비워두었던 슬롯에 다시 넣어주는 것이 일반적입니다.
            slots[slotIndex] = new InventorySlot(oldItem, 1);
        }

        RefreshAllInventoryUI(); // UI 갱신 호출
        return true;
    }

    // 소모품 사용 로직
    private bool ConsumeItem(int slotIndex)
    {
        ConsumableItemSO consumable = slots[slotIndex].itemData as ConsumableItemSO;
        if (consumable == null) return false;

        // TODO: PlayerStats.Restore(...) 호출 (실제 플레이어 스탯 회복 로직 구현 필요)
        Debug.Log($"[Inventory] Consuming {consumable.itemName} from slot {slotIndex}");

        RemoveItem(slotIndex, 1); // RemoveItem 내부에서 UI 갱신 처리됨
        return true;
    }
}