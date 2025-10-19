using UnityEngine;
using System;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

// 이 파일은 ItemType, ItemBaseSO, InventorySlot, Inventory 클래스가 정의되어 있다고 가정합니다.

public class QuickSlotManager : MonoBehaviour
{
    // --- [필요한 필드 (Inventory.cs와의 연동을 위해)] ---
    [Header("QuickSlot Setup")]
    // Inventory.cs의 capacity 이후에 QuickSlot이 이어서 붙어 있다고 가정합니다.
    [SerializeField] private int quickSlotStartIndex = 30;
    [SerializeField] private int quickSlotCount = 3;

    // 퀵슬롯의 실제 아이템 데이터를 저장하는 배열
    public InventorySlot[] quickSlots;

    [Header("UI References")]
    // 퀵슬롯 HUD UI와 커스터마이징 창의 슬롯 UI를 모두 참조해야 합니다.
    [SerializeField] private SlotUIUpdater[] quickSlotUIs;

    // --- [퀵슬롯 선택 및 사용 로직을 위한 추가 변수] ---
    [Header("QuickSlot Usage")]
    [Tooltip("현재 선택된 퀵슬롯의 내부 인덱스 (0, 1, 2)")]
    private int selectedSlotIndex = 0;

    [SerializeField] private Color selectedHighlightColor = Color.green; // 선택 시 초록색 하이라이트

    // VR 입력 시뮬레이션용 임시 키 코드 (실제 VR 환경에서는 Input Action으로 대체해야 함)
    // X 버튼 -> Use (예: KeyCode.G)
    // Y 버튼 -> Cycle (예: KeyCode.H)
    [Header("Input Simulation (Testing Only)")]
    [SerializeField] private KeyCode useKey = KeyCode.G;
    [SerializeField] private KeyCode cycleKey = KeyCode.H;

    private void Awake()
    {
        // quickSlots 배열 초기화 (퀵슬롯 개수만큼)
        quickSlots = new InventorySlot[quickSlotCount];
        for (int i = 0; i < quickSlotCount; i++)
        {
            // InventorySlot.Empty로 초기화
            quickSlots[i] = InventorySlot.Empty;
        }

        // 초기 선택 슬롯 하이라이트 설정
        RefreshQuickSlotHighlights();
    }

    // ----------------------------------------------------
    // [VR 입력 처리]
    // ----------------------------------------------------
    private void Update()
    {
        // Y 키 입력: 다음 퀵슬롯으로 전환
        if (Input.GetKeyDown(cycleKey))
        {
            CycleNextSlot();
        }

        // X 키 입력: 현재 선택된 퀵슬롯 아이템 사용
        if (Input.GetKeyDown(useKey))
        {
            UseSelectedQuickSlotItem();
        }
    }

    /// <summary>
    /// Y 버튼 (Cycle Key) 입력 시 다음 퀵슬롯을 선택합니다.
    /// </summary>
    public void CycleNextSlot()
    {
        int previousIndex = selectedSlotIndex;

        // 인덱스를 증가시키고, quickSlotCount (3)으로 나눈 나머지를 사용하여 순환시킵니다.
        selectedSlotIndex = (selectedSlotIndex + 1) % quickSlotCount;

        Debug.Log($"[QuickSlotManager] QuickSlot cycled. New selection index: {selectedSlotIndex}");

        // UI 하이라이트 갱신
        RefreshQuickSlotHighlights();
    }

    /// <summary>
    /// X 버튼 (Use Key) 입력 시 현재 선택된 퀵슬롯의 아이템을 사용합니다.
    /// </summary>
    public void UseSelectedQuickSlotItem()
    {
        InventorySlot slot = quickSlots[selectedSlotIndex];

        if (slot.IsEmpty)
        {
            Debug.Log("[QuickSlotManager] Selected quick slot is empty.");
            return;
        }

        // 이미 Consumable만 들어오도록 제한했지만, 안전을 위해 최종 확인합니다.
        if (slot.itemData.itemType != ItemType.Consumable)
        {
            Debug.LogWarning($"[QuickSlotManager] Cannot use {slot.itemData.itemName}. It is not a Consumable.");
            return;
        }

        // 1. 아이템 사용 로직 실행 (ConsumableItemSO의 Use() 메소드가 실행될 것을 기대)
        // ItemBaseSO에 Use(int slotIndex) 함수가 virtual로 정의되어 있어야 합니다.
        slot.itemData.Use(selectedSlotIndex + quickSlotStartIndex); // 인벤토리 전체 인덱스 전달

        // 2. 스택 개수 감소
        slot.stackSize--;

        // 3. 스택이 0이 되면 슬롯을 비웁니다.
        if (slot.stackSize <= 0)
        {
            quickSlots[selectedSlotIndex] = InventorySlot.Empty;
            Debug.Log($"[QuickSlotManager] {slot.itemData.itemName} consumed. Slot {selectedSlotIndex} is now empty.");
        }
        else
        {
            quickSlots[selectedSlotIndex] = slot;
            Debug.Log($"[QuickSlotManager] {slot.itemData.itemName} consumed. Remaining stack: {slot.stackSize}");
        }

        // 4. UI 갱신
        RefreshAllQuickSlotUI();
    }

    // ----------------------------------------------------
    // [UI 갱신 로직]
    // ----------------------------------------------------

    /// <summary>
    /// QuickSlot UI의 하이라이트 상태를 갱신합니다.
    /// </summary>
    private void RefreshQuickSlotHighlights()
    {
        if (quickSlotUIs == null || quickSlotUIs.Length == 0) return;

        for (int i = 0; i < quickSlotCount; i++)
        {
            if (quickSlotUIs[i] != null)
            {
                bool isSelected = (i == selectedSlotIndex);
                // SlotUIUpdater에 있는 SetHighlight 함수를 호출하여 초록색 하이라이트를 적용합니다.
                quickSlotUIs[i].SetHighlight(isSelected, selectedHighlightColor);
            }
        }
    }


    /// <summary>
    /// QuickSlot UI를 전체 갱신합니다.
    /// </summary>
    private void RefreshAllQuickSlotUI()
    {
        if (quickSlotUIs == null) return;

        int updateCount = Mathf.Min(quickSlotCount, quickSlotUIs.Length);

        for (int i = 0; i < updateCount; i++)
        {
            if (quickSlotUIs[i] != null)
            {
                quickSlotUIs[i].UpdateSlotUI(quickSlots[i].itemData, quickSlots[i].stackSize);
            }
        }
    }

    // ----------------------------------------------------
    // [Inventory.cs 에서 호출되는 필수 함수]
    // ----------------------------------------------------
    // (기존의 유효성 검사 및 스왑 로직 유지)

    /// <summary>
    /// 인덱스가 QuickSlot 영역에 속하는지 확인합니다.
    /// </summary>
    public bool IsQuickSlotIndex(int index)
    {
        return index >= quickSlotStartIndex && index < quickSlotStartIndex + quickSlotCount;
    }

    /// <summary>
    /// 퀵슬롯 인덱스에 아이템이 없는지 확인합니다. (Inventory.cs에서 호출됨)
    /// </summary>
    public bool IsQuickSlotEmpty(int index)
    {
        if (!IsQuickSlotIndex(index)) return true;

        int internalIndex = index - quickSlotStartIndex;

        if (internalIndex >= 0 && internalIndex < quickSlotCount)
        {
            return quickSlots[internalIndex].IsEmpty;
        }

        return true;
    }

    /// <summary>
    /// 퀵슬롯에 오직 Consumable만 허용하고, 다른 모든 타입은 금지합니다.
    /// </summary>
    private bool IsItemForbiddenInQuickSlot(ItemBaseSO itemData)
    {
        if (itemData == null) return false;

        // 아이템 타입이 Consumable이 아닌 경우, 즉시 진입 금지 처리
        if (itemData.itemType != ItemType.Consumable)
        {
            return true;
        }

        return false;
    }


    /// <summary>
    /// 인벤토리 슬롯과 퀵슬롯 간의 아이템 교환을 처리합니다.
    /// </summary>
    public void HandleInventorySwap(int indexA, int indexB)
    {
        // [참고] Inventory 클래스와 RefreshAllInventoryUI() 메서드가 필요합니다.
        // Inventory 클래스가 싱글톤으로 존재하고 slots 배열이 있다고 가정합니다.
        Inventory inventory = Inventory.Instance;
        if (inventory == null)
        {
            Debug.LogError("[QuickSlotManager] Inventory.Instance is null. Cannot perform swap.");
            return;
        }

        // 1. A와 B 중 어느 쪽이 QuickSlot이고 InventorySlot인지 판별
        bool aIsQuick = IsQuickSlotIndex(indexA);
        bool bIsQuick = IsQuickSlotIndex(indexB);

        // 2. 인덱스를 실제 배열 인덱스로 변환 (퀵슬롯은 0부터 시작하도록 변환)
        int aInternalIndex = aIsQuick ? indexA - quickSlotStartIndex : indexA;
        int bInternalIndex = bIsQuick ? indexB - quickSlotStartIndex : indexB;


        // 인덱스 유효성 검사 (퀵슬롯 내부 인덱스가 범위를 벗어나는지 확인)
        if ((aIsQuick && (aInternalIndex < 0 || aInternalIndex >= quickSlotCount)) ||
            (bIsQuick && (bInternalIndex < 0 || bInternalIndex >= quickSlotCount)))
        {
            Debug.LogError("[QuickSlotManager] Internal index out of bounds.");
            return;
        }


        // 3. 현재 데이터 가져오기 
        InventorySlot slotA_Data = aIsQuick ? quickSlots[aInternalIndex] : inventory.slots[aInternalIndex];
        InventorySlot slotB_Data = bIsQuick ? quickSlots[bInternalIndex] : inventory.slots[bInternalIndex];

        // ----------------------------------------------------
        // ⭐ [핵심 로직] 퀵슬롯 진입 유효성 검사 (ItemType 기반)
        // ----------------------------------------------------

        // A -> B 교환 시 B가 퀵슬롯일 때 (퀵슬롯이 목적지)
        if (!slotA_Data.IsEmpty && bIsQuick && !aIsQuick)
        {
            if (IsItemForbiddenInQuickSlot(slotA_Data.itemData))
            {
                Debug.LogWarning($"[QuickSlotManager] {slotA_Data.itemData.itemName} ({slotA_Data.itemData.itemType})는 퀵슬롯에 허용되지 않습니다. (Consumable만 가능)");
                // 스왑 중단
                return;
            }
        }

        // B -> A 교환 시 A가 퀵슬롯일 때 (퀵슬롯이 목적지)
        if (!slotB_Data.IsEmpty && aIsQuick && !bIsQuick)
        {
            if (IsItemForbiddenInQuickSlot(slotB_Data.itemData))
            {
                Debug.LogWarning($"[QuickSlotManager] {slotB_Data.itemData.itemName} ({slotB_Data.itemData.itemType})는 퀵슬롯에 허용되지 않습니다. (Consumable만 가능)");
                // 스왑 중단
                return;
            }
        }

        // 퀵슬롯 간의 교환 (aIsQuick && bIsQuick) 또는 인벤토리 내 교환은 유효성 검사 없이 허용됩니다.

        // ----------------------------------------------------
        // 4. 데이터 교환 (유효성 검사 통과 후)
        // ----------------------------------------------------

        // A 위치에 B 데이터를 덮어씌우기
        if (aIsQuick)
        {
            quickSlots[aInternalIndex] = slotB_Data;
        }
        else
        {
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
        if (!aIsQuick || !bIsQuick)
        {
            inventory.RefreshAllInventoryUI();
        }

        RefreshAllQuickSlotUI();
        RefreshQuickSlotHighlights(); // 아이템 교환 후에도 하이라이트를 유지해야 합니다.
    }
}
