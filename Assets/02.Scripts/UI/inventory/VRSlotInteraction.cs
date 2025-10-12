using UnityEngine;
using UnityEngine.UI; // Image 사용
using TMPro; // 💡 Text Mesh Pro 사용을 위해 추가

// 이 스크립트는 모든 인벤토리/퀵슬롯 Slot UI GameObject에 부착됩니다.
public class VRSlotInteraction : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("이 슬롯의 인덱스 (인벤토리: 0~29, 퀵슬롯: 0~4)")]
    public int slotIndex;
    [Tooltip("이 슬롯이 인벤토리 그리드의 일부이면 True, 퀵슬롯이면 False")]
    public bool isInventorySlot = true;

    [Header("Dependencies")]
    [SerializeField] private Inventory inventoryManager; // 인벤토리 관리자 참조 (필수)
    [SerializeField] private QuickSlotManager quickSlotManager; // 퀵슬롯 관리자 참조 (선택)

    [Header("UI Display")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI stackSizeText; // Text 대신 TextMeshProUGUI 사용

    // 드래그 상태 추적을 위한 정적 필드 (임시)
    private static int dragSourceIndex = -1;
    private static Inventory dragSourceInventory = null;

    // ----------------------------------------------------
    // UI 갱신 함수 (InventoryManager에서 호출됨)
    // ----------------------------------------------------
    public void UpdateSlotUI(ItemBaseSO item, int stack)
    {
        // 아이템이 null이 아니거나 스택 크기가 0보다 클 때만 아이템이 있는 것으로 간주
        bool hasItem = (item != null && stack > 0);

        // 1. 아이콘 업데이트
        if (itemIcon != null)
        {
            itemIcon.sprite = hasItem ? item.icon : null;
            itemIcon.enabled = hasItem;
        }

        // 2. 수량 텍스트 업데이트
        if (stackSizeText != null)
        {
            // 스택 가능 아이템이 있고 수량이 1보다 클 경우에만 표시
            bool showStack = hasItem && item.maxStackSize > 1 && stack > 1;
            stackSizeText.text = showStack ? stack.ToString() : "";
            stackSizeText.enabled = showStack;
        }
    }

    // ----------------------------------------------------
    // VR 상호작용 로직 (간소화됨)
    // ----------------------------------------------------

    // VR Interaction Raycast hit 시 Grab 버튼이 눌렸을 때 호출 (Swap 시작/종료)
    public void OnGrabPressed()
    {
        // 1. 드래그 시작 (Source) 로직
        if (dragSourceIndex == -1)
        {
            if (isInventorySlot && inventoryManager != null && !inventoryManager.slots[slotIndex].IsEmpty)
            {
                dragSourceIndex = slotIndex;
                dragSourceInventory = inventoryManager;
                // ... (드래그 시작 UI 피드백)
            }
        }
        // 2. 드롭 (Target) 로직
        else
        {
            // 인벤토리 내 슬롯 교환
            if (isInventorySlot && dragSourceInventory == inventoryManager)
            {
                inventoryManager.SwapSlots(dragSourceIndex, slotIndex);
            }

            // 상태 초기화
            dragSourceIndex = -1;
            dragSourceInventory = null;
            // ... (드롭 종료 UI 피드백)
        }
    }

    // VR Interaction Raycast hit 시 Select 버튼이 눌렸을 때 호출 (아이템 사용/장착)
    public void OnSelectPressed()
    {
        if (isInventorySlot && inventoryManager != null)
        {
            Debug.Log("Select Input Received on Slot " + slotIndex);
            // InventoryManager의 UseItem 함수 호출 (장착/소모 로직 실행)
            inventoryManager.UseItem(slotIndex);
        }
    }

    // 초기화 및 연결 검사 (선택 사항)
    private void Start()
    {
        if (inventoryManager == null)
        {
            Debug.LogError($"VRSlotInteraction on slot {gameObject.name} needs InventoryManager reference.");
        }
    }
}