using System.Collections.Generic;
using UnityEngine;
using Game.Foundation;
using Game.InventorySystem;
using Game.UI.Interface;

namespace Game.Gameplay
{
    /// <summary>
    /// 플레이어의 장비 아이템을 관리하는 시스템.
    /// - 장착 / 해제 / UI 업데이트 / 통계 적용
    /// </summary>
    public class EquipmentManager : MonoBehaviour, IEquipmentService
    {
        // 🔹 현재 장착된 아이템 목록 (슬롯별)
        private readonly Dictionary<EquipSlotType, EquippableItemSO> equippedItems = new();

        // 🔹 의존성: UI와 플레이어 스탯
        [SerializeField] private EquipmentSlotUI[] equipmentSlotUIs;
        [SerializeField] private PlayerStats playerStats;

        // 🔹 초기화
        private void Awake()
        {
            // 모든 슬롯 초기화
            foreach (EquipSlotType slot in System.Enum.GetValues(typeof(EquipSlotType)))
            {
                if (!equippedItems.ContainsKey(slot))
                    equippedItems.Add(slot, null);
            }

            // ServiceLocator 등록
            ServiceLocator.Register<IEquipmentService>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IEquipmentService>(this);
        }

        // ------------------------------------------------------------
        // ✅ IEquipmentService 구현부
        // ------------------------------------------------------------

        public EquippableItemSO Equip(EquippableItemSO item, int inventorySlotIndex)
        {
            if (item == null)
            {
                Debug.LogWarning("[EquipmentManager] Equip failed: item is null.");
                return null;
            }

            EquipSlotType slotType = item.equipSlotType;
            EquippableItemSO oldItem = null;

            // 기존 아이템 해제
            if (equippedItems.TryGetValue(slotType, out var currentItem) && currentItem != null)
            {
                oldItem = currentItem;
                RemoveEquipmentModifiers(currentItem);
                equippedItems[slotType] = null;
            }

            // 새 아이템 장착
            equippedItems[slotType] = item;
            ApplyEquipmentModifiers(item);

            Debug.Log($"[EquipmentManager] Equipped {item.itemName} into {slotType}");

            // UI 업데이트
            UpdateEquipmentUI(slotType, item);

            return oldItem;
        }

        public EquippableItemSO Unequip(EquipSlotType slotType)
        {
            if (!equippedItems.TryGetValue(slotType, out var currentItem) || currentItem == null)
                return null;

            // 스탯 제거 및 슬롯 비움
            RemoveEquipmentModifiers(currentItem);
            equippedItems[slotType] = null;

            // UI 갱신
            UpdateEquipmentUI(slotType, null);

            Debug.Log($"[EquipmentManager] Unequipped {currentItem.itemName} from {slotType}");
            return currentItem;
        }

        public IReadOnlyDictionary<EquipSlotType, EquippableItemSO> GetAllEquipment()
        {
            return equippedItems;
        }

        // ------------------------------------------------------------
        // ⚙️ 내부 유틸리티 메서드
        // ------------------------------------------------------------

        private void UpdateEquipmentUI(EquipSlotType slotType, EquippableItemSO newItem)
        {
            foreach (var uiSlot in equipmentSlotUIs)
            {
                if (uiSlot != null && uiSlot.SlotType == slotType)
                {
                    uiSlot.UpdateSlotUI(newItem);
                    break;
                }
            }
        }

        private void ApplyEquipmentModifiers(EquippableItemSO item)
        {
            if (playerStats != null)
                playerStats.ApplyEquipmentModifiers(item);
        }

        private void RemoveEquipmentModifiers(EquippableItemSO item)
        {
            if (playerStats != null)
                playerStats.RemoveEquipmentModifiers(item);
        }
    }
}
