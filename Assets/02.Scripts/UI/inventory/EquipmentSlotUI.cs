using UnityEngine;
using UnityEngine.UI;
using System; // Action 델리게이트 사용을 위해 추가


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

    // ----------------------------------------------------
    // [이벤트 구독/해제]
    // ----------------------------------------------------
    private void OnEnable()
    {
        if (equipmentManager != null)
        {
            // EquipmentManager의 장비 변경 이벤트를 구독합니다.
            equipmentManager.OnEquipmentChanged += OnEquipmentChangedHandler;
        }
    }

    private void OnDisable()
    {
        if (equipmentManager != null)
        {
            // 이벤트 구독을 해제합니다.
            equipmentManager.OnEquipmentChanged -= OnEquipmentChangedHandler;
        }
    }

    private void Start()
    {
        // 초기 UI 상태를 EquipmentManager로부터 받아와 설정합니다.
        // OnEquipmentChanged 이벤트 핸들러를 수동으로 한 번 호출하여 초기화합니다.
        if (equipmentManager != null && equipmentManager.CurrentEquipment.ContainsKey(slotType))
        {
            UpdateSlotUI(equipmentManager.CurrentEquipment[slotType]);
        }
    }


    // ----------------------------------------------------
    // [UI 업데이트 로직] (이벤트 핸들러)
    // ----------------------------------------------------

    /// <summary>
    /// EquipmentManager에서 장비 변경 이벤트가 발생했을 때 호출되는 핸들러입니다.
    /// </summary>
    private void OnEquipmentChangedHandler(EquipSlotType changedSlotType, EquippableItemSO currentItem)
    {
        // 현재 UI 슬롯이 담당하는 장비 위치의 변경 사항인지 확인합니다.
        if (changedSlotType == slotType)
        {
            UpdateSlotUI(currentItem);
        }
    }

    /// <summary>
    /// UI를 실제로 업데이트하는 내부 메서드입니다.
    /// </summary>
    public void UpdateSlotUI(EquippableItemSO currentItem)
    {
        bool isEquipped = (currentItem != null);

        if (itemIcon != null)
        {
            itemIcon.sprite = isEquipped ? currentItem.itemIcon : null;
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
        if (equipmentManager == null || inventoryManager == null)
        {
            Debug.LogError("[EquipmentSlotUI] Dependencies (Manager) not set.");
            return;
        }

        // 1. EquipmentManager에 장비 해제 요청 (아이템 반환받음)
        EquippableItemSO itemToReturn = equipmentManager.Unequip(slotType);

        if (itemToReturn != null)
        {
            // 2. 인벤토리에 아이템 추가 요청
            bool wasAdded = inventoryManager.AddItem(itemToReturn, 1);

            if (!wasAdded)
            {
                // 3. 인벤토리가 가득 찼을 경우: 다시 장비 (해제 실패 처리)
                // EquipmentManager.Equip 메서드의 두 번째 매개변수(inventorySlotIndex)는 
                // 인벤토리에서 장착을 시작했을 때의 인덱스이므로, 여기서는 -1을 전달합니다.
                equipmentManager.Equip(itemToReturn, -1);
                Debug.LogWarning($"[EquipmentSlotUI] Inventory is full. {itemToReturn.itemName} re-equipped.");

                // NOTE: Equip 호출 시 OnEquipmentChanged 이벤트가 다시 발생하여 UI가 재갱신됩니다.
            }
            // 인벤토리에 추가 성공 시, Unequip 호출로 이미 OnEquipmentChanged(slotType, null) 이벤트가 발생했으므로 
            // UpdateSlotUI(null)이 호출되어 UI는 빈 상태로 업데이트됩니다.
        }
    }
}