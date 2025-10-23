using System.Collections.Generic;
using UnityEngine;
using Game.Foundation;
using Game.Gameplay;
using Game.UI.Interface;

namespace Game.Gameplay
{
    /// <summary>
    /// 플레이어 장비 시스템.  
    /// 무기, 방어구 등의 장착/해제를 관리하며,  
    /// 스탯 및 UI와의 연동을 담당합니다.
    /// </summary>
    public class EquipmentManager : MonoBehaviour, IEquipmentService
    {
        [Header("UI References")]
        [SerializeField] private EquipmentSlotUI[] equipmentSlotUIs;

        private readonly Dictionary<EquipSlotType, EquippableItemSO> equippedItems = new();

        private void Awake()
        {
            ServiceLocator.Register<IEquipmentService>(this);

            foreach (EquipSlotType slot in System.Enum.GetValues(typeof(EquipSlotType)))
                equippedItems[slot] = null;
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IEquipmentService>(this);
        }

        // ----------------------------------------------------
        // [Public API]
        // ----------------------------------------------------

        public IReadOnlyDictionary<EquipSlotType, EquippableItemSO> CurrentEquipment => equippedItems;

        public EquippableItemSO Equip(EquippableItemSO newItem, int inventorySlotIndex)
        {
            if (newItem == null)
            {
                Debug.LogWarning("[EquipmentManager] null 아이템은 장착할 수 없습니다.");
                return null;
            }

            var slotType = newItem.equipSlotType;
            EquippableItemSO oldItem = null;

            if (equippedItems.TryGetValue(slotType, out var current) && current != null)
            {
                oldItem = current;
                equippedItems[slotType] = null;
                Debug.Log($"[EquipmentManager] {slotType} 슬롯에서 {oldItem.itemName} 해제.");
            }

            equippedItems[slotType] = newItem;
            Debug.Log($"[EquipmentManager] {newItem.itemName} 장착 완료 → {slotType}");

            // UI 갱신
            UpdateEquipmentUI(slotType, newItem);

            // 스탯 반영
            var stats = ServiceLocator.Get<IPlayerStatsService>();
            if (stats != null)
            {
                stats.ApplyEquipmentModifiers(newItem);
            }

            return oldItem;
        }

        public EquippableItemSO Unequip(EquipSlotType slotType)
        {
            if (!equippedItems.TryGetValue(slotType, out var current) || current == null)
                return null;

            equippedItems[slotType] = null;
            Debug.Log($"[EquipmentManager] {slotType} 슬롯에서 {current.itemName} 해제 완료.");

            // 스탯 반영
            var stats = ServiceLocator.Get<IPlayerStatsService>();
            stats?.RemoveEquipmentModifiers(current);

            // UI 갱신
            UpdateEquipmentUI(slotType, null);

            return current;
        }

        public void UpdateEquipmentUI(EquipSlotType slotType, EquippableItemSO item)
        {
            foreach (var ui in equipmentSlotUIs)
            {
                if (ui != null && ui.SlotType == slotType)
                {
                    ui.UpdateSlotUI(item);
                    break;
                }
            }
        }

        public void UnequipAll()
        {
            foreach (var key in new List<EquipSlotType>(equippedItems.Keys))
            {
                Unequip(key);
            }
        }
    }
}
