using UnityEngine;
using UnityEngine.UI;


public class EquipmentSlotUI : MonoBehaviour
{
    [Header("Configuration")]
    // 이 슬롯이 담당하는 장착 위치
    [SerializeField] private EquipSlotType slotType;

    [Header("Dependencies")]
    // 장비 해제 로직을 호출하기 위한 참조
    [SerializeField] private EquipmentManager equipmentManager;
    // 해제된 아이템을 돌려보낼 인벤토리 참조
    [SerializeField] private Inventory inventoryManager;

    [Header("UI Elements")]
    [SerializeField] private Image itemIcon;
    // 장비가 없을 때 아이콘을 비활성화하기 위한 부모 GameObject (선택 사항)
    [SerializeField] private GameObject emptyState;

    // 이 슬롯이 어떤 타입의 장비를 담당하는지 반환
    public EquipSlotType SlotType => slotType;

    private void Start()
    {
        // 초기 UI 상태를 EquipmentManager로부터 받아와 설정
        UpdateSlotUI(equipmentManager.CurrentEquipment[slotType]);
    }

    // ----------------------------------------------------
    // [UI 업데이트 로직] (EquipmentManager에서 호출됨)
    // ----------------------------------------------------
    // 장비 매니저에서 이 슬롯의 장비가 변경될 때 호출됩니다.
    public void UpdateSlotUI(EquippableItemSO currentItem)
    {
        bool isEquipped = (currentItem != null);
        
        if (itemIcon != null)
        {
            itemIcon.sprite = isEquipped? currentItem.icon : null;
    itemIcon.enabled = isEquipped;
        }

if (emptyState != null)
{
    emptyState.SetActive(!isEquipped);
}
    }
    
    // ----------------------------------------------------
    // [장비 해제 로직] (VR Select 버튼 또는 UI 버튼과 연결)
    // ----------------------------------------------------
    // UI 버튼의 OnClick 이벤트나 VR Select 상호작용에 연결됩니다.
    public void OnSlotSelect()
    {
    // 1. EquipmentManager에 장비 해제 요청 (아이템 반환받음)
    EquippableItemSO itemToReturn = equipmentManager.Unequip (slotType);

    if (itemToReturn != null)
    {
        // 2. 인벤토리에 아이템 추가 요청
        bool wasAdded = inventoryManager.AddItem (itemToReturn, 1);

        if (!wasAdded)
        {
            // 3. 인벤토리가 가득 찼을 경우: 다시 장비 (해제 실패 처리)
            equipmentManager.Equip (itemToReturn);
            Debug.LogWarning("Inventory is full! Unequip failed.");
            // TODO: 사용자에게 인벤토리가 가득 찼음을 알리는 UI 피드백 제공
        }
        // AddItem이 성공하면 EquipmentManager가 UpdateSlotUI를 호출할 필요가 없습니다.
        // EquipmentManager.Unequip() 호출 시 이미 UI 업데이트가 발생했기 때문입니다.
    }
}
}