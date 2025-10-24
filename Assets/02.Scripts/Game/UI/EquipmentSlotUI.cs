using UnityEngine;
using UnityEngine.UI;
using Game.Gameplay;
using Game.InventorySystem;
using Game.Foundation;

namespace Game.UI
{
    /// <summary>
    /// 장비 슬롯 UI — 각 슬롯(무기, 방어구 등)의 아이콘 표시 및 해제 기능 담당.
    /// </summary>
    public class EquipmentSlotUI : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private EquipSlotType slotType; // 이 슬롯이 담당하는 장착 위치

        [Header("Dependencies")]
        [SerializeField] private Image itemIcon;          // 아이템 아이콘
        [SerializeField] private GameObject emptyState;   // 빈 슬롯 시 표시 오브젝트 (optional)

        private IEquipmentService equipmentService;
        private IInventoryService inventoryService;

        // 현재 슬롯 타입 반환 (외부에서 참조 가능)
        public EquipSlotType SlotType => slotType;

        private void Awake()
        {
            // 서비스 로드
            equipmentService = ServiceLocator.Get<IEquipmentService>();
            inventoryService = ServiceLocator.Get<IInventoryService>();
        }

        private void Start()
        {
            // 초기 상태를 장비 매니저로부터 가져옴
            var currentEquipment = equipmentService?.GetAllEquipment();
            if (currentEquipment != null && currentEquipment.TryGetValue(slotType, out var equippedItem))
            {
                UpdateSlotUI(equippedItem);
            }
            else
            {
                UpdateSlotUI(null);
            }
        }

        /// <summary>
        /// UI 업데이트 — 아이콘과 빈 상태 토글.
        /// </summary>
        public void UpdateSlotUI(EquippableItemSO item)
        {
            bool hasItem = item != null;

            if (itemIcon != null)
            {
                itemIcon.sprite = hasItem ? item.itemIcon : null;
                itemIcon.enabled = hasItem;
            }

            if (emptyState != null)
            {
                emptyState.SetActive(!hasItem);
            }
        }

        /// <summary>
        /// 장비 해제 이벤트 (버튼 클릭 또는 VR 선택 시 호출)
        /// </summary>
        public void OnSlotSelect()
        {
            if (equipmentService == null || inventoryService == null)
            {
                Debug.LogWarning("[EquipmentSlotUI] Services not found. Ensure ServiceLocator is initialized.");
                return;
            }

            // 1. 해당 슬롯의 장비 해제
            var unequippedItem = equipmentService.Unequip(slotType);
            if (unequippedItem == null)
            {
                Debug.Log($"[EquipmentSlotUI] No item equipped in {slotType}");
                return;
            }

            // 2. 인벤토리에 아이템 반환
            bool added = inventoryService.AddItem(unequippedItem, 1);
            if (!added)
            {
                // 인벤토리가 가득 찼다면 다시 장비
                equipmentService.Equip(unequippedItem, -1);
                Debug.LogWarning($"[EquipmentSlotUI] Inventory full. Re-equipped {unequippedItem.itemName}.");
            }
        }
    }
}
