using UnityEngine;
using System.Linq;

/// <summary>
/// 인벤토리 UI 패널의 최상위 컨트롤러입니다.
/// Inventory.OnInventoryChanged 이벤트를 구독하여 개별 슬롯 UI를 갱신합니다.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("Dependencies")]
    // Inventory 싱글톤 인스턴스 참조 (Awake에서 가져옴)
    private Inventory inventory;

    [Header("UI References")]
    [Tooltip("모든 인벤토리 슬롯 UI를 순서대로 할당하세요. (SlotUIUpdater 컴포넌트 포함)")]
    // Inventory.slots 배열의 인덱스와 1:1 매칭되는 SlotUIUpdater 배열
    [SerializeField] private SlotUIUpdater[] inventorySlotUIs;

    private void Awake()
    {
        // Inventory 싱글톤 인스턴스 가져오기
        inventory = Inventory.Instance;

        if (inventory == null)
        {
            Debug.LogError("[InventoryUI] Inventory 인스턴스를 찾을 수 없습니다.");
        }

        // 인벤토리 UI 슬롯 배열의 크기가 Inventory.Capacity와 일치하는지 검사
        if (inventory != null && inventorySlotUIs.Length != inventory.Capacity)
        {
            Debug.LogWarning($"[InventoryUI] 인벤토리 UI 슬롯 개수({inventorySlotUIs.Length})가 Inventory의 용량({inventory.Capacity})과 일치하지 않습니다. 인스펙터 설정을 확인하세요.");
            // 오류를 방지하기 위해 최소 크기로 제한합니다.
            inventorySlotUIs = inventorySlotUIs.Take(Mathf.Min(inventorySlotUIs.Length, inventory.Capacity)).ToArray();
        }
    }

    private void OnEnable()
    {
        if (inventory != null)
        {
            // 🔥 이벤트 구독: 인벤토리 데이터 변경 시 UI 갱신 함수 연결
            inventory.OnInventoryChanged += UpdateSingleSlotUI;

            // UI가 활성화될 때 (인벤토리 창이 열릴 때) 현재 데이터로 전체 UI를 갱신
            RefreshAllInventoryUI();
        }
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            // 🔥 이벤트 구독 해제: 오브젝트가 비활성화될 때 연결 해제
            inventory.OnInventoryChanged -= UpdateSingleSlotUI;
        }
    }

    // ----------------------------------------------------
    // [이벤트 핸들러] (Inventory.OnInventoryChanged에 연결됨)
    // ----------------------------------------------------

    /// <summary>
    /// Inventory Manager로부터 슬롯 데이터 변경 이벤트를 받아 해당 UI만 갱신합니다.
    /// </summary>
    /// <param name="slotIndex">데이터가 변경된 인벤토리 슬롯의 인덱스</param>
    private void UpdateSingleSlotUI(int slotIndex)
    {
        // 1. 유효성 검사
        if (inventory == null || slotIndex < 0 || slotIndex >= inventorySlotUIs.Length)
        {
            Debug.LogWarning($"[InventoryUI] 유효하지 않은 슬롯 인덱스({slotIndex})로 UI 갱신 요청을 받았습니다.");
            return;
        }

        // 2. 인벤토리 데이터 가져오기
        InventorySlot slotData = inventory.slots[slotIndex];
        SlotUIUpdater uiUpdater = inventorySlotUIs[slotIndex];

        // 3. SlotUIUpdater를 사용하여 UI 갱신 실행
        if (uiUpdater != null)
        {
            uiUpdater.UpdateSlotUI(slotData.itemData, slotData.stackSize);
            Debug.Log($"[InventoryUI] Slot {slotIndex} UI 갱신: {(slotData.IsEmpty ? "Empty" : slotData.itemData.itemName)}");
        }
    }

    // ----------------------------------------------------
    // [초기화/전체 갱신 로직]
    // ----------------------------------------------------

    /// <summary>
    /// UI가 처음 활성화될 때 또는 강제로 모든 슬롯 UI를 갱신할 때 사용합니다.
    /// </summary>
    public void RefreshAllInventoryUI()
    {
        for (int i = 0; i < inventorySlotUIs.Length; i++)
        {
            UpdateSingleSlotUI(i);
        }
    }
}