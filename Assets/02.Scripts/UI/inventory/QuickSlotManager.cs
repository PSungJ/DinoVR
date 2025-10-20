using UnityEngine;
using System;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using System.Collections.Generic;

// Inventory, SlotUIUpdater, ItemBaseSO, InventorySlot, ItemType은 전역 범위에서 참조됩니다.

public class QuickSlotManager : MonoBehaviour
{
    // --- [필요한 필 (Inventory.cs와의 연동을 위해)] ---
    [Header("QuickSlot Setup")]
    [SerializeField] public int quickSlotStartIndex = 30; // 인벤토리 배열에서 퀵슬롯이 시작되는 인덱스 (public으로 변경하여 Inventory.cs에서 참조)
    [SerializeField] private int quickSlotCount = 3;

    // QuickSlotManager가 직접 관리하는 퀵슬롯 데이터 (InventorySlot 직접 사용)
    public InventorySlot[] quickSlots;

    [Header("UI References")]
    // SlotUIUpdater 클래스를 직접 사용합니다.
    [SerializeField] private SlotUIUpdater[] quickSlotUIReferences;

    // 실제 Inventory 컴포넌트를 연결해야 합니다. 🔥 Inspector에서 이 필드가 반드시 연결되어야 합니다.
    [SerializeField] private Inventory inventoryReference;

    [SerializeField] private GameObject inventoryUIRoot;

    // --- [퀵슬롯 선택 및 사용 로직을 위한 추가 변수] ---
    [Header("QuickSlot Usage")]
    [Tooltip("현재 선택된 퀵슬롯의 내부 인덱스 (0, 1, 2)")]
    private int selectedSlotIndex = 0;

    // --- [XR Input Actions] ---
    [Header("XR Input Actions (Left Hand)")]
    [Tooltip("왼쪽 컨트롤러 Button West (B) - 인벤토리/퀵슬롯 토글")]
    [SerializeField] private InputActionProperty toggleInventoryAction;

    [Tooltip("왼쪽 컨트롤러 Trigger - 현재 퀵슬롯 아이템 사용")]
    [SerializeField] private InputActionProperty useQuickSlotAction;

    [Tooltip("왼쪽 컨트롤러 Button North (Y) - 퀵슬롯 순환")]
    [SerializeField] private InputActionProperty cycleQuickSlotAction;

    private void Awake()
    {
        // quickSlots 배열 초기화 (퀵슬롯 개수만큼)
        quickSlots = new InventorySlot[quickSlotCount]; // InventorySlot 직접 사용
        for (int i = 0 < quickSlotCount; i++)
        {
            quickSlots[i] = InventorySlot.Empty; // InventorySlot.Empty 직접 사용
        }

        // 초기 선택 슬롯 하이라이트 설정
        RefreshQuickSlotHighlights();

        // 초기에는 UI를 비활성화합니다.
        if (inventoryUIRoot != null)
        {
            inventoryUIRoot.SetActive(false);
        }
    }

    // ----------------------------------------------------
    // [Input Action 구독 및 해제]
    // ----------------------------------------------------
    private void OnEnable()
    {
        toggleInventoryAction.action.performed += OnToggleInventory;
        useQuickSlotAction.action.performed += OnUseQuickSlot;
        cycleQuickSlotAction.action.performed += OnCycleQuickSlot;

        toggleInventoryAction.action.Enable();
        useQuickSlotAction.action.Enable();
        cycleQuickSlotAction.action.Enable();
    }

    private void OnDisable()
    {
        toggleInventoryAction.action.performed -= OnToggleInventory;
        useQuickSlotAction.action.performed -= OnUseQuickSlot;
        cycleQuickSlotAction.action.performed -= OnCycleQuickSlot;

        toggleInventoryAction.action.Disable();
        useQuickSlotAction.action.Disable();
        cycleQuickSlotAction.action.Disable();
    }

    // ----------------------------------------------------
    // [Input Action 이벤트 핸들러]
    // ----------------------------------------------------
    private void OnToggleInventory(InputAction.CallbackContext context)
    {
        ToggleInventoryAndQuickSlotUI();
    }

    private void OnUseQuickSlot(InputAction.CallbackContext context)
    {
        UseSelectedQuickSlotItem();
    }

    private void OnCycleQuickSlot(InputAction.CallbackContext context)
    {
        CycleNextSlot();
    }

    /// <summary>
    /// Inventory 및 QuickSlot UI를 활성화/비활성화합니다.
    /// </summary>
    public void ToggleInventoryAndQuickSlotUI()
    {
        if (inventoryUIRoot != null)
        {
            bool currentState = inventoryUIRoot.activeSelf;
            inventoryUIRoot.SetActive(!currentState);

            Debug.Log($"[QuickSlotManager] Inventory/QuickSlot UI Toggled to: {!currentState}");
        }
        else
        {
            Debug.LogWarning("[QuickSlotManager] inventoryUIRoot is null. Cannot toggle UI visibility directly.");
        }
    }

    /// <summary>
    /// 다음 퀵슬롯을 선택합니다.
    /// </summary>
    public void CycleNextSlot()
    {
        selectedSlotIndex = (selectedSlotIndex + 1) % quickSlotCount;

        Debug.Log($"[QuickSlotManager] QuickSlot cycled. New selection index: {selectedSlotIndex}");

        RefreshQuickSlotHighlights();
    }

    /// <summary>
    /// 현재 선택된 퀵슬롯의 아이템을 사용합니다.
    /// </summary>
    public void UseSelectedQuickSlotItem()
    {
        InventorySlot slot = quickSlots[selectedSlotIndex]; // InventorySlot 직접 사용

        if (slot.IsEmpty)
        {
            Debug.Log("[QuickSlotManager] Selected quick slot is empty.");
            return;
        }

        if (slot.itemData.itemType != ItemType.Consumable) // ItemType 직접 사용
        {
            // Equipment/Weapon 아이템이 QuickSlot에 있으면 사용을 막고 경고를 줍니다.
            Debug.LogWarning($"[QuickSlotManager] Cannot use {slot.itemData.itemName}. QuickSlot is reserved for Consumables.");
            return;
        }

        // ----------------------------------------------------
        // 🔥 [로직] Inventory 클래스의 UseItem에 처리를 위임
        // ----------------------------------------------------

        // 퀵슬롯의 외부 인덱스 계산
        int quickSlotExternalIndex = selectedSlotIndex + quickSlotStartIndex;

        // Inventory 클래스 인스턴스 참조
        if (inventoryReference != null)
        {
            // Inventory의 UseItem을 호출하여 효과 적용 및 재고 감소를 요청합니다.
            bool success = inventoryReference.UseItem(quickSlotExternalIndex);

            if (success)
            {
                Debug.Log($"[QuickSlotManager] Consumable item use requested to Inventory for external index {quickSlotExternalIndex}.");
            }
            else
            {
                Debug.LogError($"[QuickSlotManager] Inventory.UseItem failed for quick slot {quickSlotExternalIndex}.");
            }
        }
        else
        {
            // 이 로그가 발생하면, Inspector에서 inventoryReference를 연결해야 합니다.
            Debug.LogError("[QuickSlotManager] Inventory instance is null. Cannot use item. Please check the Inspector assignment.");
        }

        // Inventory에서 quickSlots 배열을 업데이트했으므로, UI만 갱신합니다.
        RefreshAllQuickSlotUI();
    }

    // ----------------------------------------------------
    // [UI 갱신 로직]
    // ----------------------------------------------------
    private void RefreshQuickSlotHighlights()
    {
        if (quickSlotUIReferences == null || quickSlotUIReferences.Length == 0) return;

        for (int i = 0; i < quickSlotCount; i++)
        {
            if (quickSlotUIReferences[i] != null)
            {
                bool isSelected = (i == selectedSlotIndex);
                // SlotUIUpdater 클래스에 SetHighlight 함수가 있다고 가정
                quickSlotUIReferences[i].SetHighlight(isSelected);
            }
        }
    }

    public void RefreshAllQuickSlotUI() // Inventory.cs에서 호출되도록 public으로 변경
    {
        if (quickSlotUIReferences == null) return;

        int updateCount = Mathf.Min(quickSlotCount, quickSlotUIReferences.Length);

        for (int i = 0; i < updateCount; i++)
        {
            if (quickSlotUIReferences[i] != null)
            {
                // ItemBaseSO, stackSize 사용
                quickSlotUIReferences[i].UpdateSlotUI(quickSlots[i].itemData, quickSlots[i].stackSize);
            }
        }
    }

    // ----------------------------------------------------
    // [Inventory.cs 에서 호출되는 필수 함수]
    // ----------------------------------------------------
    public bool IsQuickSlotIndex(int index)
    {
        return index >= quickSlotStartIndex && index < quickSlotStartIndex + quickSlotCount;
    }

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

    private bool IsItemForbiddenInQuickSlot(ItemBaseSO itemData) // ItemBaseSO 직접 사용
    {
        if (itemData == null) return false;

        // 아이템 타입이 Consumable이 아닌 경우, 즉시 진입 금지 처리
        if (itemData.itemType != ItemType.Consumable) // ItemType 직접 사용
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
        Inventory inventoryInstance = inventoryReference;

        if (inventoryInstance == null)
        {
            // 🔥 이 로그가 발생하면 Inspector에서 inventoryReference를 연결해야 합니다.
            Debug.LogError("[QuickSlotManager] Inventory instance is null. Cannot perform swap.");
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
        // 퀵슬롯 데이터에 접근할 때는 internalIndex를 사용합니다.
        // 인벤토리 데이터에 접근할 때는 indexA/indexB가 이미 인벤토리 배열의 인덱스이므로,
        // 이를 aInternalIndex/bInternalIndex로 사용하는 것이 맞습니다.
        InventorySlot slotA_Data = aIsQuick ? quickSlots[aInternalIndex] : inventoryInstance.slots[aInternalIndex];
        InventorySlot slotB_Data = bIsQuick ? quickSlots[bInternalIndex] : inventoryInstance.slots[bInternalIndex];

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
            inventoryInstance.slots[aInternalIndex] = slotB_Data;
        }

        // B 위치에 A 데이터를 덮어씌우기
        if (bIsQuick)
        {
            quickSlots[bInternalIndex] = slotA_Data;
        }
        else
        {
            inventoryInstance.slots[bInternalIndex] = slotA_Data;
        }

        Debug.Log($"[QuickSlotManager] Swap Executed: Inventory/QuickSlot Indices ({indexA} <-> {indexB})");

        // 5. UI 갱신
        if (!aIsQuick || !bIsQuick)
        {
            // 인벤토리의 해당 슬롯 인덱스가 변경되었으므로 전체 UI를 갱신합니다.
            inventoryInstance.RefreshAllInventoryUI();
        }

        // 퀵슬롯 UI 갱신
        RefreshAllQuickSlotUI();
        RefreshQuickSlotHighlights();
    }
}
