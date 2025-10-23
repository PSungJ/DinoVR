using UnityEngine;
using UnityEngine.UI;
using Game.Gameplay;
using Game.Foundation;

namespace Game.UI.Interface
{
    /// <summary>
    /// 각 장비 슬롯(무기, 방어구 등)의 아이콘과 상태를 표시하며,
    /// 클릭/VR 선택 시 장비 해제 기능을 제공합니다.
    /// </summary>
    public class EquipmentSlotUI : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private EquipSlotType slotType;

        [Header("UI Elements")]
        [SerializeField] private Image itemIcon;
        [SerializeField] private GameObject emptyState;

        private IEquipmentService equipmentService;
        private IInventoryService inventoryService;

        public EquipSlotType SlotType => slotType;

        private void Awake()
        {
            equipmentService = ServiceLocator.Get<IEquipmentService>();
            inventoryService = ServiceLocator.Get<IInventoryService>();
        }

        private void Start()
        {
            // 초기 상태 반영
            if (equipmentService != null &&
                equipmentService.CurrentEquipment.TryGetValue(slotType, out var equippedItem))
            {
                UpdateSlotUI(equippedItem);
            }
            else
            {
                UpdateSlotUI(null);
            }
        }

        // ----------------------------------------------------
        // [UI 갱신 로직]
        // ----------------------------------------------------
        public void UpdateSlotUI(EquippableItemSO currentItem)
        {
            bool isEquipped = (currentItem != null);

            if (itemIcon != null)
            {
                itemIcon.sprite = isEquipped ? currentItem.itemIcon : null;
                itemIcon.enabled = isEquipped;
            }

            if (emptyState != null)
                emptyState.SetActive(!isEquipped);
        }

        // ----------------------------------------------------
        // [장비 해제 로직]
        // ----------------------------------------------------
        public void OnSlotSelect()
        {
            if (equipmentService == null)
            {
                Debug.LogError("[EquipmentSlotUI] IEquipmentService를 찾을 수 없습니다!");
                return;
            }

            var unequippedItem = equipmentService.Unequip(slotType);
            if (unequippedItem == null) return;

            if (inventoryService != null)
            {
                bool added = inventoryService.AddItem(unequippedItem, 1);
                if (!added)
                {
                    equipmentService.Equip(unequippedItem, -1);
                    Debug.LogWarning($"[EquipmentSlotUI] 인벤토리가 가득 찼습니다. {unequippedItem.itemName}을 다시 장착했습니다.");
                }
            }

            UpdateSlotUI(null);
        }
    }
}
