using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit; // XR Interaction Toolkit 사용을 위해 추가
// EquipSlotType, EquippableItemSO, InventorySlot 등은 외부 파일(EquipmentManager 등)에서 정의되었다고 가정합니다.

/// <summary>
/// 장비 슬롯 UI를 관리하며, XRGrabInteractable을 제어하여 슬롯 아이템의 Grab 가능 여부를 설정합니다.
/// </summary>
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
    // ⭐[필수] 드래그 및 드롭을 위해 슬롯에 부착된 XRGrabInteractable 참조
    [SerializeField] private XRGrabInteractable grabInteractable;

    [Header("UI Elements")]
    [SerializeField] private Image itemIcon;
    // 장비가 없을 때 아이콘을 비활성화하기 위한 부모 GameObject (선택 사항)
    [SerializeField] private GameObject emptyState;

    // ⭐[추가됨] Inspector에 설정된 기본 아이콘 스프라이트를 저장합니다.
    private Sprite defaultSprite;

    // 이 슬롯이 어떤 타입의 장비를 담당하는지 반환
    public EquipSlotType SlotType => slotType;

    private void Start()
    {
        // ⭐[점검 사항] grabInteractable이 Inspector에 연결되지 않은 경우, 자동으로 GetComponent 시도
        if (grabInteractable == null)
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            if (grabInteractable == null)
            {
                Debug.LogError($"[EquipmentSlotUI - {slotType}] XRGrabInteractable 컴포넌트를 찾을 수 없습니다. Inspector에 연결하거나 GameObject에 추가하세요.");
            }
        }

        // ⭐[수정됨] 초기 Inspector에 설정된 기본 아이콘을 저장합니다.
        if (itemIcon != null)
        {
            defaultSprite = itemIcon.sprite;
        }


        // 초기 UI 상태를 EquipmentManager로부터 받아와 설정
        EquippableItemSO initialItem = null;
        if (equipmentManager != null && equipmentManager.CurrentEquipment != null && equipmentManager.CurrentEquipment.TryGetValue(slotType, out initialItem))
        {
            // 초기화 시에도 EquipmentManager가 요구하는 두 개의 인수를 전달
            UpdateSlotUI(initialItem, initialItem != null ? 1 : 0);
        }
        else
        {
            UpdateSlotUI(null, 0);
        }
    }

    // ----------------------------------------------------
    // [UI 업데이트 로직] (EquipmentManager에서 호출됨)
    // ----------------------------------------------------
    /// <summary>
    /// 장비 매니저에서 이 슬롯의 장비가 변경될 때 호출됩니다.
    /// </summary>
    public void UpdateSlotUI(EquippableItemSO currentItem, int stackSize)
    {
        // 스택 사이즈가 1 이상일 경우 장착되었다고 판단합니다.
        bool isEquipped = (currentItem != null && stackSize > 0);
        string itemName = isEquipped ? currentItem.itemName : "Empty";

        // ⭐[디버그 추가] UpdateSlotUI가 호출될 때 전달된 데이터 확인
        Debug.Log($"[EquipmentSlotUI - {slotType}] Update UI Received: Item='{itemName}', Equipped={isEquipped}, Stack={stackSize}.");


        // 1. UI 시각적 업데이트
        if (itemIcon != null)
        {
            if (isEquipped)
            {
                // 아이템이 장착된 경우, 아이템의 아이콘 표시
                itemIcon.sprite = currentItem.itemIcon;
                // ⭐[디버그 추가] 아이템 장착 시 아이콘 변경 확인
                Debug.Log($"[EquipmentSlotUI - {slotType}] Item Equipped: Icon set to {currentItem.itemIcon?.name}.");
            }
            else
            {
                // 아이템이 장착되지 않은 경우, 기본값(Inspector 설정값)으로 되돌립니다.
                itemIcon.sprite = defaultSprite;
                // ⭐[디버그 추가] 빈 슬롯으로 설정 시 아이콘 변경 확인
                Debug.Log($"[EquipmentSlotUI - {slotType}] Slot Empty: Icon reset to Default Sprite ({defaultSprite?.name}).");
            }

            // itemIcon.enabled는 항상 true 상태를 유지합니다.
        }

        // emptyState는 슬롯이 비어있을 때 (isEquipped = false) 보여줄 배경/플레이스홀더 이미지라고 가정합니다.
        if (emptyState != null)
        {
            emptyState.SetActive(!isEquipped);
        }

        // 2. ⭐[핵심] XR 상호작용 기능 활성화/비활성화 제어
        if (grabInteractable != null)
        {
            // 아이템이 있을 때만 Grab이 가능하여 드래그 앤 드롭으로 해제가 가능합니다.
            grabInteractable.enabled = isEquipped;

            // ⭐ 디버깅: 현재 슬롯의 Grab 가능 상태 출력
            Debug.Log($"[EquipmentSlotUI - {slotType}] Slot updated. Item: {itemName}. GrabInteractable Enabled: {isEquipped}");
        }
        else
        {
            // grabInteractable 연결 누락 시 경고
            Debug.LogWarning($"[EquipmentSlotUI - {slotType}] grabInteractable이 Inspector에 연결되지 않았습니다. Grab 기능이 작동하지 않을 수 있습니다.");
        }
    }

    // ----------------------------------------------------
    // [장비 해제 로직] (VR Select 버튼 또는 UI 버튼과 연결)
    // ----------------------------------------------------
    public void OnSlotSelect()
    {
        // ⭐ 디버깅: 이 함수가 호출되는지 확인 (Grab이 시작될 때가 아니라, Select 완료 시점)
        Debug.Log($"[EquipmentSlotUI - {slotType}] OnSlotSelect Called. Attempting Unequip.");

        // 1. EquipmentManager에 장비 해제 요청 (아이템 반환받음)
        EquippableItemSO itemToReturn = equipmentManager.Unequip(slotType);

        if (itemToReturn != null)
        {
            // ⭐[디버그 추가] 해제된 아이템이 인벤토리로 돌아가기 직전 확인
            Debug.Log($"[EquipmentSlotUI - {slotType}] Unequip Success: Item '{itemToReturn.itemName}' retrieved. Attempting to return to Inventory.");

            // 2. 인벤토리에 아이템 추가 요청
            // Inventory.cs의 AddItem(item, count) 시그니처를 사용한다고 가정합니다.
            bool wasAdded = inventoryManager.AddItem(itemToReturn, 1);

            if (wasAdded) // ⭐[추가된 디버그] 아이템이 성공적으로 인벤토리에 추가된 경우
            {
                Debug.Log($"[EquipmentSlotUI - {slotType}] Item '{itemToReturn.itemName}' successfully returned to Inventory.");
            }
            else
            {
                // 3. 인벤토리가 가득 찼을 경우: 다시 장비 (해제 실패 처리)
                // ⭐[디버그 추가] 인벤토리 복귀 실패 및 재장착 시도 확인
                Debug.LogWarning($"[EquipmentSlotUI - {slotType}] Inventory is full! Item '{itemToReturn.itemName}' re-equipped via Equip(item, -1).");
                equipmentManager.Equip(itemToReturn, -1);

                // TODO: 사용자에게 인벤토리가 가득 찼음을 알리는 UI 피드백 제공
            }
        }
        else
        {
            // ⭐[디버그 추가] 해제 시도했지만 슬롯이 비어있거나 EquipmentManager에서 반환된 아이템이 null인 경우
            Debug.Log($"[EquipmentSlotUI - {slotType}] Unequip finished. No item returned (Slot was likely empty).");
        }
    }
}
