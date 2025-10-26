using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; // 필요하지 않을 수 있지만, 안전을 위해 유지

// EquipSlotType, EquippableItemSO 등 외부 정의는 생략합니다.

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
    // 스택 크기 표시를 위한 텍스트 필드 (옵션)
    [SerializeField] private TMPro.TextMeshProUGUI stackSizeText;

    // 이 슬롯이 어떤 타입의 장비를 담당하는지 반환
    public EquipSlotType SlotType => slotType;

    private void Start()
    {
        // 초기 UI 상태를 EquipmentManager로부터 받아와 설정
        // 장비 아이템은 스택 크기가 항상 1이므로, 1을 전달하거나 null인 경우 0을 전달합니다.
        EquippableItemSO initialItem = equipmentManager.CurrentEquipment[slotType];
        UpdateSlotUI(initialItem, initialItem != null ? 1 : 0);
    }

    // ----------------------------------------------------
    // [UI 업데이트 로직] (EquipmentManager에서 호출됨)
    // ----------------------------------------------------

    /// <summary>
    /// 🔥 FIX: EquipmentManager에서 호출하는 두 개의 인수를 받는 메서드를 추가합니다.
    /// 이 메서드가 메인 UI 업데이트 로직을 호출합니다.
    /// </summary>
    public void UpdateSlotUI(EquippableItemSO currentItem, int stackSize)
    {
        // 스택 크기 인수는 무시하고, 아이템 존재 여부만 확인하여 UI를 갱신합니다.
        bool isEquipped = (currentItem != null && stackSize > 0);

        if (itemIcon != null)
        {
            itemIcon.sprite = isEquipped ? currentItem.itemIcon : null;
            itemIcon.enabled = isEquipped;
        }

        if (emptyState != null)
        {
            emptyState.SetActive(!isEquipped);
        }

        // 스택 크기 텍스트가 있다면 업데이트
        if (stackSizeText != null)
        {
            // 장비 슬롯이므로 스택 크기를 표시하지 않거나, 1로 고정하여 표시할 수 있습니다.
            stackSizeText.text = stackSize > 1 ? stackSize.ToString() : "";
            stackSizeText.enabled = stackSize > 1; // 스택이 1보다 클 때만 표시
        }
    }

    // 이전 코드와의 호환성을 위해 남겨둡니다. (ItemBaseSO 대신 EquippableItemSO 사용)
    public void UpdateSlotUI(EquippableItemSO currentItem)
    {
        // 인수가 하나일 때도 처리 가능하도록 위임
        UpdateSlotUI(currentItem, currentItem != null ? 1 : 0);
    }

    // ----------------------------------------------------
    // [장비 해제 로직] (VR Select 버튼 또는 UI 버튼과 연결)
    // ----------------------------------------------------
    // UI 버튼의 OnClick 이벤트나 VR Select 상호작용에 연결됩니다.
    public void OnSlotSelect()
    {
        // 1. EquipmentManager에 장비 해제 요청 (아이템 반환받음)
        EquippableItemSO itemToReturn = equipmentManager.Unequip(slotType);

        if (itemToReturn != null)
        {
            // 2. 인벤토리에 아이템 추가 요청
            bool wasAdded = inventoryManager.AddItem(itemToReturn, 1);

            if (!wasAdded)
            {
                // 3. 인벤토리가 가득 찼을 경우: 다시 장비 (해제 실패 처리)
                // Equip 메서드에 필수 매개변수인 inventorySlotIndex를 -1로 전달합니다.
                equipmentManager.Equip(itemToReturn, -1);
                Debug.LogWarning("Inventory is full! Unequip failed. Item re-equipped.");
                // TODO: 사용자에게 인벤토리가 가득 찼음을 알리는 UI 피드백 제공
            }
            // 참고: AddItem이 성공하면 Unequip()에서 이미 UI 업데이트가 발생했으므로 추가 호출 불필요
        }
    }
}