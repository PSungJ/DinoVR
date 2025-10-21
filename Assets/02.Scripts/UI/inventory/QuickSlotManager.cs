using UnityEngine;
using System;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using System.Collections.Generic;

// =======================================================================
// ⭐ [전제 조건]
// Inventory, InventorySlot, ItemBaseSO, SlotUIUpdater 클래스와 
// 독립적으로 정의된 ItemType Enum이 전역에서 참조 가능해야 합니다.
// =======================================================================

public class QuickSlotManager : MonoBehaviour
{
    // --- [필요한 필 (Inventory.cs와의 연동을 위해)] ---
    [Header("QuickSlot Setup")]
    [SerializeField] public int quickSlotStartIndex = 30;
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
        quickSlots = new InventorySlot[quickSlotCount];
        for (int i = 0; i < quickSlotCount; i++)
        {
            // InventorySlot은 struct이므로 new InventorySlot()으로 초기화하면 
            // itemData=null, stackSize=0으로 안전하게 초기화됩니다.
            quickSlots[i] = new InventorySlot();
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
        if (toggleInventoryAction.action != null) toggleInventoryAction.action.performed += OnToggleInventory;
        if (useQuickSlotAction.action != null) useQuickSlotAction.action.performed += OnUseQuickSlot;
        if (cycleQuickSlotAction.action != null) cycleQuickSlotAction.action.performed += OnCycleQuickSlot;

        toggleInventoryAction.action.Enable();
        useQuickSlotAction.action.Enable();
        cycleQuickSlotAction.action.Enable();
    }

    private void OnDisable()
    {
        if (toggleInventoryAction.action != null) toggleInventoryAction.action.performed -= OnToggleInventory;
        if (useQuickSlotAction.action != null) useQuickSlotAction.action.performed -= OnUseQuickSlot;
        if (cycleQuickSlotAction.action != null) cycleQuickSlotAction.action.performed -= OnCycleQuickSlot;

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
        // ⭐ 이 콜백 함수가 실행될 때 TLS 오류가 뜹니다.
        // 이제 UseSelectedQuickSlotItem()은 Inventory.UseItem을 재귀적으로 호출하지 않습니다.
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
    /// 현재 선택된 퀵슬롯의 아이템을 사용합니다. (XR 입력 기반 사용)
    /// </summary>
    public void UseSelectedQuickSlotItem()
    {
        // InventorySlot은 struct이므로, quickSlots[selectedSlotIndex] 접근 시 안전합니다.
        InventorySlot slot = quickSlots[selectedSlotIndex];

        if (slot.IsEmpty)
        {
            Debug.Log("[QuickSlotManager] Selected quick slot is empty.");
            return;
        }

        // itemData는 null이 아님이 보장됩니다 (IsEmpty 체크 통과).
        if (slot.itemData.itemType != ItemType.Consumable)
        {
            // Equipment/Weapon 아이템이 QuickSlot에 있으면 사용을 막고 경고를 줍니다.
            Debug.LogWarning($"[QuickSlotManager] Cannot use {slot.itemData.itemName}. QuickSlot is reserved for Consumables.");
            return;
        }

        // ----------------------------------------------------
        // 🚨 [RECURSION FIX] Inventory.UseItem을 호출하는 대신,
        // 내부 함수를 직접 호출하여 무한 재귀를 방지합니다.
        // ----------------------------------------------------
        bool success = UseItemFromRelativeQuickSlotIndex(selectedSlotIndex);

        if (success)
        {
            Debug.Log($"[QuickSlotManager] Consumable item use successful via selected slot index {selectedSlotIndex}.");
        }
        else
        {
            Debug.LogError($"[QuickSlotManager] Item use failed for selected quick slot {selectedSlotIndex}.");
        }

        // UseItemFromRelativeQuickSlotIndex에서 UI 갱신이 이미 처리됩니다.
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
                // SlotUIUpdater 클래스에 SetHighlight 함수가 있다고 가정하고 호출
                quickSlotUIReferences[i].SetHighlight(isSelected);
            }
        }
    }

    public void RefreshAllQuickSlotUI() // Inventory.cs에서 호출되도록 public으로 변경
    {
        if (quickSlotUIReferences == null) return;

        // Mathf.Min은 System.Linq 대신 UnityEngine에 포함되어 있습니다.
        int updateCount = Mathf.Min(quickSlotCount, quickSlotUIReferences.Length);

        for (int i = 0; i < updateCount; i++)
        {
            if (quickSlotUIReferences[i] != null)
            {
                // InventorySlot struct의 public 필드 itemData와 stackSize에 접근
                quickSlotUIReferences[i].UpdateSlotUI(quickSlots[i].itemData, quickSlots[i].stackSize);
            }
        }
    }

    // ----------------------------------------------------
    // [Inventory.cs 에서 호출되는 필수 함수]
    // ----------------------------------------------------

    /// <summary>
    /// Inventory에서 호출되어 글로벌 인덱스를 받아 아이템 사용을 처리합니다.
    /// Inventory.UseItem이 이 함수를 호출하며, 이 함수는 다시 Inventory.UseItem을 호출하면 안 됩니다.
    /// </summary>
    public bool HandleQuickSlotUse(int absoluteIndex)
    {
        // 1. 글로벌 인덱스를 퀵슬롯의 상대 인덱스(0, 1, 2)로 변환
        int quickSlotRelativeIndex = absoluteIndex - quickSlotStartIndex;

        if (quickSlotRelativeIndex < 0 || quickSlotRelativeIndex >= quickSlots.Length)
        {
            Debug.LogError($"[QuickSlotManager] HandleQuickSlotUse failed: Invalid relative index {quickSlotRelativeIndex}.");
            return false;
        }

        // 2. 변환된 상대 인덱스로 실제 아이템 사용 로직을 호출합니다.
        // 🚨 RECURSION FIX: 이 함수 내부에서 직접 아이템 소비를 처리합니다.
        return UseItemFromRelativeQuickSlotIndex(quickSlotRelativeIndex);
    }

    /// <summary>
    /// 상대 인덱스를 사용하여 아이템 사용을 처리하고 Inventory에 위임합니다.
    /// </summary>
    private bool UseItemFromRelativeQuickSlotIndex(int quickSlotRelativeIndex)
    {
        // InventorySlot은 struct이므로, quickSlots[selectedSlotIndex] 접근 시 안전합니다.
        InventorySlot slot = quickSlots[quickSlotRelativeIndex];

        if (slot.IsEmpty)
        {
            Debug.Log("[QuickSlotManager] Relative quick slot is empty. No item to use.");
            return false;
        }

        if (slot.itemData.itemType != ItemType.Consumable)
        {
            Debug.LogWarning($"[QuickSlotManager] Item at index {quickSlotRelativeIndex} is not consumable.");
            return false;
        }

        // 퀵슬롯의 외부 인덱스 (글로벌 인덱스) 계산
        int quickSlotExternalIndex = quickSlotRelativeIndex + quickSlotStartIndex;

        // ----------------------------------------------------
        // 🔥 [RECURSION FIX: 아이템 효과 적용 및 데이터 직접 수정]
        // Inventory.UseItem을 호출하지 않고 소비 로직을 여기서 완료합니다.
        // ----------------------------------------------------

        ConsumableItemSO consumable = slot.itemData as ConsumableItemSO;
        if (consumable == null) return false;

        PlayerHealthComponent playerHealth = PlayerHealthComponent.Instance;
        EquipmentManager equipmentManager = EquipmentManager.Instance;

        if (playerHealth == null || equipmentManager == null)
        {
            Debug.LogError("[QuickSlotManager] PlayerHealthComponent 또는 EquipmentManager 인스턴스를 찾을 수 없습니다. 아이템 효과가 적용되지 않았습니다.");
            return false;
        }

        // 1. 아이템 효과 적용
        // ConsumableItemSO.Use 함수를 직접 호출하여 효과를 적용합니다.
        consumable.Use(quickSlotExternalIndex, playerHealth, equipmentManager);

        // 2. 수량 감소 (struct의 복사본을 수정)
        slot.stackSize--;

        // 3. 슬롯 비우기 및 배열 업데이트 (struct의 변경사항 반영)
        if (slot.stackSize <= 0)
        {
            quickSlots[quickSlotRelativeIndex] = InventorySlot.Empty; // 슬롯 비우기
        }
        else
        {
            quickSlots[quickSlotRelativeIndex] = slot; // 수량 변경 반영
        }

        Debug.Log($"[QuickSlotManager] Item use successful via relative index {quickSlotRelativeIndex}. New Stack: {slot.stackSize}");

        // 4. UI 갱신
        RefreshAllQuickSlotUI();

        return true;
    }

    /// <summary>
    /// 주어진 글로벌 인덱스가 퀵슬롯의 범위에 속하는지 확인합니다.
    /// </summary>
    public bool IsQuickSlotIndex(int index)
    {
        return index >= quickSlotStartIndex && index < quickSlotStartIndex + quickSlotCount;
    }

    /// <summary>
    /// 주어진 글로벌 인덱스의 퀵슬롯이 비어있는지 확인합니다.
    /// </summary>
    public bool IsQuickSlotEmpty(int index)
    {
        if (!IsQuickSlotIndex(index)) return true;

        int internalIndex = index - quickSlotStartIndex;

        if (internalIndex >= 0 && internalIndex < quickSlotCount)
        {
            // struct이므로 복사본에 접근하지만, IsEmpty는 안전합니다.
            return quickSlots[internalIndex].IsEmpty;
        }

        return true;
    }

    private bool IsItemForbiddenInQuickSlot(ItemBaseSO itemData) // ItemBaseSO 직접 사용
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

        // A, B 모두 인벤토리 슬롯인 경우: InventoryManager가 처리해야 하므로 중단
        if (!aIsQuick && !bIsQuick) return;

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
