using UnityEngine;
using System; // Action을 사용하기 위해 필요
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
    // --- [싱글톤 정의] ---
    public static QuickSlotManager Instance { get; private set; } // 싱글톤 인스턴스

    // 🔥 핵심 1: UI 갱신을 위한 Action 이벤트 정의
    // (int index, ItemBaseSO item, int count) -> 해당 슬롯의 데이터가 변경될 때 호출
    public event Action<int, ItemBaseSO, int> OnQuickSlotChanged;

    // 🔥 핵심 2: 선택 상태 갱신을 위한 Action 이벤트 정의
    // (int newSelectedIndex) -> 선택된 슬롯 인덱스가 변경될 때 호출
    public event Action<int> OnQuickSlotSelectionChanged;

    // --- [필요한 필 (Inventory.cs와의 연동을 위해)] ---
    [Header("QuickSlot Setup")]
    // 이 인덱스부터는 Inventory.slots 배열과 겹치지 않는 퀵슬롯 영역으로 간주합니다.
    [SerializeField] public int quickSlotStartIndex = 30;
    // Inspector에서 설정한 퀵슬롯 개수. quickSlotUIReferences.Length와 일치해야 합니다.
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
    public int selectedSlotIndex = 0;

    // VR 입력 시스템 액션
    [Header("Input Action")]
    [SerializeField] private InputActionProperty nextSlotAction; // 다음 슬롯 선택
    [SerializeField] private InputActionProperty previousSlotAction; // 이전 슬롯 선택
    [SerializeField] private InputActionProperty useItemAction; // 선택된 아이템 사용


    private void Awake()
    {
        // --- [싱글톤 초기화 로직] ---
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 필요에 따라 추가
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 퀵슬롯 배열 초기화
        quickSlots = new InventorySlot[quickSlotCount];
        for (int i = 0; i < quickSlotCount; i++)
        {
            quickSlots[i] = InventorySlot.Empty;
        }

        // UI 참조 개수 확인
        if (quickSlotUIReferences.Length != quickSlotCount)
        {
            Debug.LogError($"[QuickSlotManager] quickSlotUIReferences 배열 크기가 quickSlotCount({quickSlotCount})와 일치하지 않습니다. ({quickSlotUIReferences.Length})");
        }
    }

    private void Start()
    {
        // 초기 UI 갱신 (모두 비어있음)
        for (int i = 0; i < quickSlotCount; i++)
        {
            NotifySlotChanged(i);
        }

        // 초기 선택 슬롯 UI 갱신
        NotifySelectionChanged(selectedSlotIndex);

        // Inventory가 로드되어 있는지 확인
        if (inventoryReference == null)
        {
            Debug.LogError("[QuickSlotManager] inventoryReference가 Inspector에서 할당되지 않았습니다. 인벤토리와의 상호작용이 불가능합니다.");
        }
    }


    private void OnEnable()
    {
        // 입력 액션 구독
        if (nextSlotAction.action != null)
        {
            nextSlotAction.action.Enable();
            nextSlotAction.action.performed += OnNextSlot;
        }
        if (previousSlotAction.action != null)
        {
            previousSlotAction.action.Enable();
            previousSlotAction.action.performed += OnPreviousSlot;
        }
        if (useItemAction.action != null)
        {
            useItemAction.action.Enable();
            useItemAction.action.performed += OnUseItem;
        }
    }

    private void OnDisable()
    {
        // 입력 액션 구독 해제
        if (nextSlotAction.action != null)
        {
            nextSlotAction.action.performed -= OnNextSlot;
            nextSlotAction.action.Disable();
        }
        if (previousSlotAction.action != null)
        {
            previousSlotAction.action.performed -= OnPreviousSlot;
            previousSlotAction.action.Disable();
        }
        if (useItemAction.action != null)
        {
            useItemAction.action.performed -= OnUseItem;
            useItemAction.action.Disable();
        }
    }

    // ----------------------------------------------------
    // [입력 처리]
    // ----------------------------------------------------

    private void OnNextSlot(InputAction.CallbackContext context)
    {
        // 인벤토리 UI가 열려있을 때는 퀵슬롯 선택을 막습니다. (옵션)
        if (inventoryUIRoot != null && inventoryUIRoot.activeInHierarchy) return;

        int newIndex = (selectedSlotIndex + 1) % quickSlotCount;
        SelectSlot(newIndex);
    }

    private void OnPreviousSlot(InputAction.CallbackContext context)
    {
        // 인벤토리 UI가 열려있을 때는 퀵슬롯 선택을 막습니다. (옵션)
        if (inventoryUIRoot != null && inventoryUIRoot.activeInHierarchy) return;

        // C# Modulo 연산의 음수 처리
        int newIndex = (selectedSlotIndex - 1 + quickSlotCount) % quickSlotCount;
        SelectSlot(newIndex);
    }

    private void OnUseItem(InputAction.CallbackContext context)
    {
        // 인벤토리 UI가 열려있을 때는 아이템 사용을 막습니다. (옵션)
        if (inventoryUIRoot != null && inventoryUIRoot.activeInHierarchy) return;

        UseSelectedQuickSlotItem();
    }

    /// <summary>
    /// 지정된 내부 인덱스(0, 1, 2...)의 퀵슬롯을 선택하고 UI를 갱신합니다.
    /// </summary>
    /// <param name="newIndex">선택할 퀵슬롯의 내부 인덱스 (0부터 시작)</param>
    public void SelectSlot(int newIndex)
    {
        if (newIndex < 0 || newIndex >= quickSlotCount) return;

        if (selectedSlotIndex != newIndex)
        {
            selectedSlotIndex = newIndex;
            Debug.Log($"[QuickSlotManager] QuickSlot {selectedSlotIndex} 선택됨.");
            NotifySelectionChanged(selectedSlotIndex);
        }
    }

    /// <summary>
    /// 현재 선택된 퀵슬롯의 아이템을 사용합니다.
    /// </summary>
    private void UseSelectedQuickSlotItem()
    {
        InventorySlot slot = quickSlots[selectedSlotIndex];
        if (!slot.IsEmpty)
        {
            // Inventory의 UseItemByData 또는 ConsumeItem 함수를 호출
            if (inventoryReference != null)
            {
                // ItemBaseSO.Use는 slotIndex를 요구합니다. 퀵슬롯에서는 전역 인덱스를 전달하는 것이 좋습니다.
                ConsumableItemSO consumable = slot.itemData as ConsumableItemSO;
                if (consumable != null)
                {
                    // Inventory.cs 코드가 불완전하므로 QuickSlotManager에서 직접 소모 로직을 처리합니다.
                    UseItemAndConsume(selectedSlotIndex, consumable);
                }
                else
                {
                    Debug.LogWarning($"[QuickSlotManager] QuickSlot {selectedSlotIndex}의 아이템은 Consumable 타입이 아니어서 사용할 수 없습니다. (Type: {slot.itemData.itemType})");
                }
            }
        }
        else
        {
            Debug.Log($"[QuickSlotManager] QuickSlot {selectedSlotIndex}가 비어있습니다.");
        }
    }


    /// <summary>
    /// 퀵슬롯 아이템을 사용하고 스택을 감소시키며 UI를 갱신합니다. (Inventory.cs와의 복잡한 의존성 회피를 위한 임시 구현)
    /// </summary>
    /// <param name="internalIndex">퀵슬롯 내부 인덱스 (0, 1, 2...)</param>
    /// <param name="consumable">소모할 ConsumableItemSO</param>
    private void UseItemAndConsume(int internalIndex, ConsumableItemSO consumable)
    {
        // 1. 아이템 효과 적용 (PlayerHealthComponent가 필요)
        PlayerHealthComponent playerHealth = PlayerHealthComponent.Instance;
        EquipmentManager equipmentManager = EquipmentManager.Instance;

        if (playerHealth != null)
        {
            // ItemBaseSO.Use 호출 (ConsumableItemSO.Use가 오버라이드됨)
            // 퀵슬롯은 인벤토리가 아닌 자체 배열을 사용하므로, slotIndex는 -1 (미사용) 또는 퀵슬롯의 전역 인덱스를 전달
            int globalIndex = quickSlotStartIndex + internalIndex;
            consumable.Use(globalIndex, playerHealth, equipmentManager);
        }
        else
        {
            Debug.LogError("[QuickSlotManager] PlayerHealthComponent 인스턴스를 찾을 수 없습니다. 아이템 효과가 적용되지 않았습니다.");
        }


        // 2. 스택 감소
        if (quickSlots[internalIndex].stackSize > 0)
        {
            quickSlots[internalIndex].stackSize--;
        }

        // 3. 스택이 0이 되면 슬롯 비우기
        if (quickSlots[internalIndex].stackSize <= 0)
        {
            quickSlots[internalIndex] = InventorySlot.Empty;
        }

        // 4. UI 갱신 요청
        NotifySlotChanged(internalIndex);

        // 5. [중요]: Inventory에서도 아이템을 제거해야 합니다. (Inventory.cs의 RemoveItemBySO 함수가 필요합니다.)
    }

    // ----------------------------------------------------
    // [UI/데이터 갱신 알림]
    // ----------------------------------------------------

    /// <summary>
    /// 특정 퀵슬롯의 데이터 변경을 UI에 알립니다.
    /// </summary>
    /// <param name="internalIndex">퀵슬롯의 내부 인덱스 (0, 1, 2...)</param>
    public void NotifySlotChanged(int internalIndex)
    {
        if (internalIndex < 0 || internalIndex >= quickSlotCount) return;

        InventorySlot slot = quickSlots[internalIndex];

        // 이벤트 발동
        OnQuickSlotChanged?.Invoke(internalIndex, slot.itemData, slot.stackSize);

        // SlotUIUpdater 직접 호출을 통한 UI 갱신 (옵션: 이벤트 사용을 권장)
        if (quickSlotUIReferences.Length > internalIndex && quickSlotUIReferences[internalIndex] != null)
        {
            quickSlotUIReferences[internalIndex].UpdateSlotUI(slot.itemData, slot.stackSize);
        }
    }

    /// <summary>
    /// 선택된 퀵슬롯 인덱스의 변경을 UI에 알립니다.
    /// </summary>
    /// <param name="newSelectedIndex">새로운 선택 인덱스 (0, 1, 2...)</param>
    private void NotifySelectionChanged(int newSelectedIndex)
    {
        // 이벤트 발동
        OnQuickSlotSelectionChanged?.Invoke(newSelectedIndex);

        // SlotUIUpdater의 하이라이트 직접 제어 (옵션: QuickSlotUI 컴포넌트 사용을 권장)
        // 현재는 QuickSlotUI.cs가 이 이벤트를 구독하여 처리하는 것이 일반적입니다.
    }


    // ----------------------------------------------------
    // [인벤토리와의 연동 (핵심)]
    // ----------------------------------------------------

    /// <summary>
    /// 인벤토리/퀵슬롯 간의 아이템 교환 로직의 진입점입니다.
    /// Inventory.cs 또는 VRSlotInteraction.cs에서 호출됩니다.
    /// </summary>
    /// <param name="aIndex">슬롯 A의 전역 인덱스 (Inventory.slots의 인덱스 또는 quickSlotStartIndex 이상의 퀵슬롯 인덱스)</param>
    /// <param name="bIndex">슬롯 B의 전역 인덱스</param>
    public void GlobalSwapItems(int aIndex, int bIndex)
    {
        if (inventoryReference == null)
        {
            Debug.LogError("[QuickSlotManager] Inventory Reference is null. Cannot perform swap.");
            return;
        }

        bool aIsQuick = IsQuickSlotIndex(aIndex);
        bool bIsQuick = IsQuickSlotIndex(bIndex);

        // 1. 두 슬롯 모두 인벤토리 영역에 있을 때 (QuickSlotManager는 아무것도 하지 않음)
        if (!aIsQuick && !bIsQuick)
        {
            // Inventory에서 SwapItems(aIndex, bIndex)를 직접 처리해야 합니다.
            Debug.Log($"[QuickSlotManager] 양쪽 모두 Inventory 슬롯. (Swap: {aIndex} <-> {bIndex})");
            // Inventory.cs의 SwapItems가 이미 호출되었다고 가정하고 종료.
            return;
        }

        // 2. 인덱스를 내부 인덱스로 변환하고, 슬롯 데이터 가져오기
        int aInternalIndex = aIsQuick ? GetQuickSlotInternalIndex(aIndex) : aIndex;
        int bInternalIndex = bIsQuick ? GetQuickSlotInternalIndex(bIndex) : bIndex;

        InventorySlot slotA_Data = aIsQuick ? quickSlots[aInternalIndex] : inventoryReference.slots[aInternalIndex];
        InventorySlot slotB_Data = bIsQuick ? quickSlots[bInternalIndex] : inventoryReference.slots[bInternalIndex];

        // 3. 퀵슬롯 아이템 타입 유효성 검사 (퀵슬롯에는 Consumable만 허용)
        // A -> B 교환 시 A가 퀵슬롯일 때 & B가 인벤토리일 때
        if (!slotA_Data.IsEmpty && aIsQuick && !bIsQuick)
        {
            if (IsItemForbiddenInQuickSlot(slotA_Data.itemData))
            {
                Debug.LogWarning($"[QuickSlotManager] {slotA_Data.itemData.itemName} ({slotA_Data.itemData.itemType})는 퀵슬롯에 허용되지 않습니다. (Consumable만 가능)");
                return;
            }
        }

        // B -> A 교환 시 A가 퀵슬롯일 때 & B가 인벤토리일 때 (A가 목적지)
        if (!slotB_Data.IsEmpty && aIsQuick && !bIsQuick)
        {
            if (IsItemForbiddenInQuickSlot(slotB_Data.itemData))
            {
                Debug.LogWarning($"[QuickSlotManager] {slotB_Data.itemData.itemName} ({slotB_Data.itemData.itemType})는 퀵슬롯에 허용되지 않습니다. (Consumable만 가능)");
                return;
            }
        }

        // A -> B 교환 시 B가 퀵슬롯일 때 & A가 인벤토리일 때 (B가 목적지)
        if (!slotA_Data.IsEmpty && bIsQuick && !aIsQuick)
        {
            if (IsItemForbiddenInQuickSlot(slotA_Data.itemData))
            {
                Debug.LogWarning($"[QuickSlotManager] {slotA_Data.itemData.itemName} ({slotA_Data.itemData.itemType})는 퀵슬롯에 허용되지 않습니다. (Consumable만 가능)");
                return;
            }
        }

        // B -> A 교환 시 B가 퀵슬롯일 때 & A가 인벤토리일 때 (B가 출발지)
        if (!slotB_Data.IsEmpty && bIsQuick && !aIsQuick)
        {
            if (IsItemForbiddenInQuickSlot(slotB_Data.itemData))
            {
                Debug.LogWarning($"[QuickSlotManager] {slotB_Data.itemData.itemName} ({slotB_Data.itemData.itemType})는 퀵슬롯에 허용되지 않습니다. (Consumable만 가능)");
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
            inventoryReference.slots[aInternalIndex] = slotB_Data;
        }

        // B 위치에 A 데이터를 덮어씌우기
        if (bIsQuick)
        {
            quickSlots[bInternalIndex] = slotA_Data;
        }
        else
        {
            inventoryReference.slots[bInternalIndex] = slotA_Data;
        }

        Debug.Log($"[QuickSlotManager] Swap Executed: Inventory/QuickSlot {aIndex} <-> Inventory/QuickSlot {bIndex}");

        // 5. UI 갱신 요청
        // A 위치가 퀵슬롯 영역이라면 퀵슬롯 UI 갱신
        if (aIsQuick)
        {
            NotifySlotChanged(aInternalIndex);
        }
        // B 위치가 퀵슬롯 영역이라면 퀵슬롯 UI 갱신
        if (bIsQuick)
        {
            NotifySlotChanged(bInternalIndex);
        }

        // Inventory UI 갱신은 Inventory.cs에서 RefreshAllInventoryUI()를 호출하여 처리해야 합니다.
    }


    // ----------------------------------------------------
    // [유틸리티]
    // ----------------------------------------------------

    /// <summary>
    /// 전역 인덱스가 퀵슬롯 영역에 해당하는지 확인합니다.
    /// </summary>
    public bool IsQuickSlotIndex(int globalIndex)
    {
        return globalIndex >= quickSlotStartIndex && globalIndex < quickSlotStartIndex + quickSlotCount;
    }

    /// <summary>
    /// 전역 퀵슬롯 인덱스를 내부 인덱스 (0, 1, 2...)로 변환합니다.
    /// </summary>
    public int GetQuickSlotInternalIndex(int globalIndex)
    {
        if (IsQuickSlotIndex(globalIndex))
        {
            return globalIndex - quickSlotStartIndex;
        }
        return -1;
    }

    /// <summary>
    /// 퀵슬롯에 허용되지 않는 아이템 타입인지 확인합니다. (장비류)
    /// </summary>
    private bool IsItemForbiddenInQuickSlot(ItemBaseSO item)
    {
        if (item == null) return false;

        // Consumable (소모품)이 아닌 모든 아이템은 금지한다고 가정합니다.
        return item.itemType != ItemType.Consumable;
    }

    /// <summary>
    /// 지정된 퀵슬롯 내부 인덱스의 아이템을 사용합니다. (VRSlotInteraction에서 사용)
    /// </summary>
    /// <param name="internalIndex">사용할 퀵슬롯의 내부 인덱스 (0, 1, 2...)</param>
    public void UseItemAtQuickSlotIndex(int internalIndex)
    {
        if (internalIndex < 0 || internalIndex >= quickSlotCount) return;

        InventorySlot slot = quickSlots[internalIndex];
        if (!slot.IsEmpty)
        {
            ConsumableItemSO consumable = slot.itemData as ConsumableItemSO;
            if (consumable != null)
            {
                // 핵심 로직 재활용: UseItemAndConsume은 아이템을 사용하고 스택을 줄입니다.
                UseItemAndConsume(internalIndex, consumable);
            }
            else
            {
                Debug.LogWarning($"[QuickSlotManager] QuickSlot {internalIndex}의 아이템은 Consumable 타입이 아니어서 사용할 수 없습니다. (Type: {slot.itemData.itemType})");
            }
        }
        else
        {
            Debug.Log($"[QuickSlotManager] QuickSlot {internalIndex}가 비어있습니다.");
        }
    }
    /// <summary>
    /// 전역 인덱스를 사용하여 해당 퀵슬롯의 데이터를 가져옵니다.
    /// 퀵슬롯 인덱스가 아닌 경우 InventorySlot.Empty를 반환합니다.
    /// </summary>
    public InventorySlot GetSlotData(int globalIndex)
    {
        // 1. 전역 인덱스를 퀵슬롯 내부 인덱스(0, 1, 2...)로 변환합니다.
        int internalIndex = GetQuickSlotInternalIndex(globalIndex);

        // 2. 유효한 퀵슬롯 내부 인덱스인 경우 해당 데이터를 반환합니다.
        if (internalIndex != -1)
        {
            return quickSlots[internalIndex];
        }

        // 3. 퀵슬롯 영역이 아닌 경우 비어있는 슬롯을 반환합니다.
        // InventorySlot 구조체가 정의되어 있다고 가정합니다.
        return InventorySlot.Empty;
    }
}
