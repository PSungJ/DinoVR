using System;
using UnityEngine;
using Game.Foundation;
using Game.Gameplay;

namespace Game.InventorySystem
{
    /// <summary>
    /// 인벤토리 전체 로직을 관리하는 핵심 매니저.
    /// 아이템 추가/제거/사용 및 UI 업데이트를 담당합니다.
    /// </summary>
    public class Inventory : MonoBehaviour, IInventoryService
    {
        [Header("Inventory Settings")]
        [SerializeField] private int capacity = 30;

        [Header("References")]
        [SerializeField] private SlotUIUpdater[] inventorySlotUIs;
        [SerializeField] private QuickSlotManager quickSlotManager;
        [SerializeField] private EquipmentManager equipmentManager;

        private InventorySlot[] slots;

        // IInventoryService 프로퍼티 구현
        public int Capacity => capacity;

        public event Action<int, InventorySlot> OnSlotChanged;

        private void Awake()
        {
            // ServiceLocator에 등록
            ServiceLocator.Register<IInventoryService>(this);

            // 슬롯 초기화
            slots = new InventorySlot[capacity];
            for (int i = 0; i < capacity; i++)
                slots[i] = InventorySlot.Empty;
        }

        private void OnDestroy()
        {
            // 서비스 해제
            ServiceLocator.Unregister<IInventoryService>(this);
        }

        // --------------------------------------------------------
        // [슬롯 접근 관련 메서드]
        // --------------------------------------------------------
        public InventorySlot GetSlot(int index)
        {
            if (index < 0 || index >= capacity)
                return InventorySlot.Empty;
            return slots[index];
        }

        public void SetSlot(int index, InventorySlot slot)
        {
            if (index < 0 || index >= capacity)
                return;

            slots[index] = slot;
            OnSlotChanged?.Invoke(index, slot);
        }

        public bool IsSlotEmpty(int index)
        {
            if (index < 0 || index >= capacity)
                return true;
            return slots[index].IsEmpty;
        }

        // --------------------------------------------------------
        // [아이템 추가 및 제거]
        // --------------------------------------------------------
        public bool AddItem(ItemBaseSO item, int amount = 1)
        {
            if (item == null || amount <= 0)
                return false;

            // 1️⃣ 기존 동일 아이템 스택 확인
            for (int i = 0; i < capacity; i++)
            {
                if (slots[i].itemData == item && slots[i].stackSize < item.maxStackSize)
                {
                    slots[i].stackSize = Mathf.Min(slots[i].stackSize + amount, item.maxStackSize);
                    RefreshAllInventoryUI();
                    OnSlotChanged?.Invoke(i, slots[i]);
                    return true;
                }
            }

            // 2️⃣ 빈 슬롯에 새로 추가
            for (int i = 0; i < capacity; i++)
            {
                if (slots[i].IsEmpty)
                {
                    slots[i] = new InventorySlot(item, amount);
                    RefreshAllInventoryUI();
                    OnSlotChanged?.Invoke(i, slots[i]);
                    return true;
                }
            }

            Debug.LogWarning("[Inventory] 인벤토리가 가득 찼습니다.");
            return false;
        }

        public void RemoveItem(int slotIndex, int amount = 1)
        {
            if (slotIndex < 0 || slotIndex >= capacity)
                return;

            var slot = slots[slotIndex];
            if (slot.IsEmpty)
                return;

            slot.stackSize -= amount;
            if (slot.stackSize <= 0)
                slot = InventorySlot.Empty;

            slots[slotIndex] = slot;
            OnSlotChanged?.Invoke(slotIndex, slot);
            RefreshAllInventoryUI();
        }

        // --------------------------------------------------------
        // [아이템 사용 로직]
        // --------------------------------------------------------
        public bool UseItem(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= capacity || slots[slotIndex].IsEmpty)
                return false;

            var item = slots[slotIndex].itemData;

            // 소비 아이템
            if (item.itemType == ItemType.Consumable)
            {
                var healthService = ServiceLocator.Get<IPlayerHealthService>();
                var equipService = ServiceLocator.Get<IEquipmentService>();
                item.Use(slotIndex, healthService, equipService);

                slots[slotIndex].ConsumeOne();
                OnSlotChanged?.Invoke(slotIndex, slots[slotIndex]);
                RefreshAllInventoryUI();
                return true;
            }

            // 장비 아이템
            if (item.itemType == ItemType.Equipment || item is EquippableItemSO)
            {
                var equipService = ServiceLocator.Get<IEquipmentService>();
                item.Use(slotIndex, null, equipService);
                RefreshAllInventoryUI();
                return true;
            }

            return false;
        }

        public bool UseItemByData(ItemBaseSO item)
        {
            for (int i = 0; i < capacity; i++)
            {
                if (slots[i].itemData == item)
                    return UseItem(i);
            }
            return false;
        }

        // --------------------------------------------------------
        // [UI 갱신]
        // --------------------------------------------------------
        public void RefreshAllInventoryUI()
        {
            if (inventorySlotUIs == null) return;

            for (int i = 0; i < Mathf.Min(inventorySlotUIs.Length, capacity); i++)
            {
                if (inventorySlotUIs[i] != null)
                    inventorySlotUIs[i].UpdateSlotUI(slots[i].itemData, slots[i].stackSize);
            }
        }
    }
}
