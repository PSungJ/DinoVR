using UnityEngine;

// [참고] ItemBaseSO, InventorySlot 등의 정의가 필요합니다.

public class QuickSlotManager : MonoBehaviour
{
    // --- [필요한 필드 (Inventory.cs와의 연동을 위해)] ---
    // Inventory.cs의 capacity 이후에 QuickSlot이 이어서 붙어 있다고 가정합니다.
    [Header("QuickSlot Setup")]
    // [중요] InventoryManager와 QuickSlotManager가 InventorySlot 인덱스를 공유한다고 가정할 때,
    // 퀵슬롯이 시작하는 인덱스 (예: 인벤토리 30칸 이후 30부터 시작)
    [SerializeField] private int quickSlotStartIndex = 30;
    [SerializeField] private int quickSlotCount = 5;      // 퀵슬롯의 개수

    // 퀵슬롯의 실제 아이템 데이터를 저장하는 배열
    public InventorySlot[] quickSlots;

    [Header("UI References")]
    [SerializeField] private SlotUIUpdater[] quickSlotUIs; // 퀵슬롯 UI를 업데이트하기 위한 배열


    private void Awake()
    {
        // quickSlots 배열 초기화 (퀵슬롯 개수만큼)
        quickSlots = new InventorySlot[quickSlotCount];
        for (int i = 0; i < quickSlotCount; i++)
        {
            quickSlots[i] = InventorySlot.Empty;
        }

        // [참고] QuickSlotManager 싱글톤 또는 초기화 로직이 필요할 수 있습니다.
    }

    // ----------------------------------------------------
    // [Inventory.cs 에서 호출되는 필수 함수]
    // ----------------------------------------------------

    /// <summary>
    /// 인덱스가 QuickSlot 영역에 속하는지 확인합니다. (Inventory.cs에서 호출됨)
    /// </summary>
    /// <param name="index">InventorySlot.slots[] 배열 인덱스 (가상 인덱스)</param>
    /// <returns>QuickSlot 범위에 속하면 true</returns>
    public bool IsQuickSlotIndex(int index)
    {
        // 인벤토리의 가상 인덱스가 퀵슬롯의 시작 인덱스와 끝 인덱스 사이에 있는지 확인
        return index >= quickSlotStartIndex && index < quickSlotStartIndex + quickSlotCount;
    }

    /// <summary>
    /// 인벤토리 슬롯과 퀵슬롯 간의 아이템 교환을 처리합니다. (Inventory.cs에서 위임받음)
    /// </summary>
    public void HandleInventorySwap(int indexA, int indexB)
    {
        Inventory inventory = Inventory.Instance;
        if (inventory == null) return;

        // 1. A와 B 중 어느 쪽이 QuickSlot이고 InventorySlot인지 판별
        bool aIsQuick = IsQuickSlotIndex(indexA);
        bool bIsQuick = IsQuickSlotIndex(indexB);

        // 2. 인덱스를 실제 배열 인덱스로 변환 (퀵슬롯은 0부터 시작하도록 변환)
        int aInternalIndex = aIsQuick ? indexA - quickSlotStartIndex : indexA;
        int bInternalIndex = bIsQuick ? indexB - quickSlotStartIndex : indexB;

        // 3. 현재 데이터 가져오기
        InventorySlot slotA_Data = aIsQuick ? quickSlots[aInternalIndex] : inventory.slots[aInternalIndex];
        InventorySlot slotB_Data = bIsQuick ? quickSlots[bInternalIndex] : inventory.slots[bInternalIndex];

        // 4. 데이터 교환
        // A 위치에 B 데이터를 덮어씌우기
        if (aIsQuick)
        {
            quickSlots[aInternalIndex] = slotB_Data;
        }
        else
        {
            // inventory.slots 배열은 인벤토리 최대 크기(capacity)까지만 접근해야 함
            inventory.slots[aInternalIndex] = slotB_Data;
        }

        // B 위치에 A 데이터를 덮어씌우기
        if (bIsQuick)
        {
            quickSlots[bInternalIndex] = slotA_Data;
        }
        else
        {
            inventory.slots[bInternalIndex] = slotA_Data;
        }

        Debug.Log($"[QuickSlotManager] Swap Executed: Inventory/QuickSlot Indices ({indexA} <-> {indexB})");

        // 5. UI 갱신
        inventory.RefreshAllInventoryUI(); // Inventory UI 갱신 (Inventory.cs에 public 함수가 있다고 가정)
        RefreshAllQuickSlotUI();           // QuickSlot UI 갱신
    }

    // ----------------------------------------------------
    // [UI 갱신 로직]
    // ----------------------------------------------------

    /// <summary>
    /// QuickSlot UI를 전체 갱신합니다.
    /// </summary>
    private void RefreshAllQuickSlotUI()
    {
        if (quickSlotUIs == null) return;

        for (int i = 0; i < quickSlotCount; i++)
        {
            if (i < quickSlotUIs.Length && quickSlotUIs[i] != null)
            {
                quickSlotUIs[i].UpdateSlotUI(quickSlots[i].itemData, quickSlots[i].stackSize);
            }
        }
        // [참고] quickSlotUIs 배열의 크기가 quickSlotCount와 일치해야 합니다.
    }

    // ----------------------------------------------------
    // [QuickSlot 고유 로직]
    // ----------------------------------------------------

    // [TODO] 퀵슬롯 아이템 사용 (예: 숫자 키 입력 시) 로직 등이 여기에 구현됩니다.
}