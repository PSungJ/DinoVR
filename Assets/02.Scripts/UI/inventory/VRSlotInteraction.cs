using UnityEngine;

// 이 스크립트는 모든 인벤토리/퀵슬롯 Slot UI GameObject에 부착됩니다.
public class VRSlotInteraction : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("이 슬롯이 인벤토리 그리드의 일부이면 True, 퀵슬롯이면 False")]
    public bool isInventorySlot = true;

    [Header("Dependencies")]
    // InventoryManager 참조 (Swap 및 Use 호출용)
    [SerializeField] private Inventory inventoryManager;
    // QuickSlotManager 참조 (Assign 호출용)
    [SerializeField] private QuickSlotManager quickSlotManager;

    // 이 슬롯의 인덱스 (인벤토리: 0~29, 퀵슬롯: 0~4)
    public int slotIndex;

    [Header("Custom VR Input for Swap")]
    [SerializeField] private string grabButtonName = "VR_Grab_Button";

    // 드래그 상태 추적을 위한 정적 필드 (어디서 시작했는지)
    private static int dragSourceIndex = -1;
    private static ItemBaseSO draggedItemData = null; // 드래그 중인 아이템 데이터
    private static Inventory dragSourceInventory = null; // 드래그 시작 인벤토리 참조

    // VR Interaction Raycast hit 시 Grab 버튼이 눌렸을 때 호출 (Unity Event/Input System 연결 필요)
    public void OnGrabPressed()
    {
        // 1. 드래그 시작 (Source)
        if (dragSourceIndex == -1) 
        {
            if (isInventorySlot && !inventoryManager.slots[slotIndex].IsEmpty)
            {
                dragSourceIndex = slotIndex;
                draggedItemData = inventoryManager.slots[slotIndex].itemData;
                dragSourceInventory = inventoryManager;
                // TODO: UI 피드백 (아이콘을 포인터에 부착)
            }
        }
        // 2. 드롭 (Target)
        else // 드래그 상태인 경우, 드롭 대상으로 처리
{
    // 드래그 시작점이 인벤토리인지 확인
    if (dragSourceInventory == inventoryManager)
    {
        if (isInventorySlot)
        {
            // Case A: 인벤토리 -> 인벤토리 (Swap)
            if (dragSourceIndex != slotIndex)
            {
                inventoryManager.SwapSlots(dragSourceIndex, slotIndex);
            }
        }
        else // isQuickSlot == true (퀵슬롯 UI 컴포넌트에 isInventorySlot=false로 설정)
        {
            // Case B: 인벤토리 -> 퀵슬롯 (Assign)
            quickSlotManager.AssignItemToSlot(draggedItemData, slotIndex);
        }
    }

    // 상태 초기화
    dragSourceIndex = -1;
    draggedItemData = null;
    dragSourceInventory = null;
    // TODO: UI 피드백 제거
}
    }

    // VR Interaction Raycast hit 시 Select 버튼이 눌렸을 때 호출 (Unity Event/Input System 연결 필요)
    public void OnSelectPressed() {
    // 드래그 중이라면 Select 버튼도 드롭으로 처리할 수 있습니다.
    if (dragSourceIndex != -1)
    {
        OnGrabPressed(); // 드롭 로직 실행
    }
    else if (isInventorySlot)
    {
        // 인벤토리 슬롯이라면 Use/Equip 로직 호출
        inventoryManager.UseItem(slotIndex);
    }
    else // isQuickSlot
    {
        // 퀵슬롯 슬롯이라면 현재 선택된 퀵슬롯 아이템 사용 로직 호출
        quickSlotManager.UseCurrentSlotItem();
    }
}
}