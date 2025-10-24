using UnityEngine;
using Game.Foundation;
using Game.Gameplay;
using System.Linq;
using Game.UI;

namespace Game.InventorySystem
{
    /// <summary>
    /// 인벤토리의 아이템 데이터를 관리하고, 다른 시스템과 연동하는 핵심 클래스.
    /// </summary>
    public class Inventory : MonoBehaviour, IInventoryService
    {
        [Header("Settings")]
        [SerializeField] private int capacity = 30;
        public int Capacity => capacity;

        [Header("Dependencies")]
        [SerializeField] private SlotUIUpdater[] inventorySlotUIs;

        private IEquipmentService equipmentService;
        private IQuickSlotService quickSlotService;

        // 실제 아이템 데이터 배열
        private InventorySlot[] slots;

        // 이벤트
        public event System.Action<int, InventorySlot> OnSlotChanged;

        private void Awake()
        {
            ServiceLocator.Register<IInventoryService>(this);

            slots = new InventorySlot[capacity];
            for (int i = 0; i < capacity; i++)
                slots[i] = InventorySlot.Empty;
        }

        private void Start()
        {
            equipmentService = ServiceLocator.Get<IEquipmentService>();
            quickSlotService = ServiceLocator.Get<IQuickSlotService>();

            RefreshAllInventoryUI();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IInventoryService>(this);
        }

        // ------------------------------------------------------------
        // ✅ IInventoryService 구현부
        // ------------------------------------------------------------

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
            RefreshAllInventoryUI();
        }

        public bool IsSlotEmpty(int index)
        {
            if (index < 0 || index >= capacity)
                return true;
            return slots[index].IsEmpty;
        }

        public bool AddItem(ItemBaseSO item, int count)
        {
            if (item == null || count <= 0) return false;

            // 스택 가능한 아이템이면 기존 슬롯에 추가
            if (item.maxStackSize > 1)
            {
                for (int i = 0; i < capacity; i++)
                {
                    if (slots[i].itemData == item && slots[i].stackSize < item.maxStackSize)
                    {
                        slots[i].stackSize = Mathf.Min(slots[i].stackSize + count, item.maxStackSize);
                        OnSlotChanged?.Invoke(i, slots[i]);
                        RefreshAllInventoryUI();
                        return true;
                    }
                }
            }

            // 빈 슬롯에 새 아이템 추가
            for (int i = 0; i < capacity; i++)
            {
                if (slots[i].IsEmpty)
                {
                    slots[i] = new InventorySlot(item, count);
                    OnSlotChanged?.Invoke(i, slots[i]);
                    RefreshAllInventoryUI();
                    return true;
                }
            }

            Debug.LogWarning("[Inventory] AddItem failed: Inventory full");
            return false;
        }

        public bool RemoveItem(int index, int count)
        {
            if (index < 0 || index >= capacity) return false;
            if (slots[index].IsEmpty) return false;

            slots[index].stackSize -= count;
            if (slots[index].stackSize <= 0)
                slots[index] = InventorySlot.Empty;

            OnSlotChanged?.Invoke(index, slots[index]);
            RefreshAllInventoryUI();
            return true;
        }

        public bool UseItem(int index)
        {
            if (index < 0 || index >= capacity || slots[index].IsEmpty)
                return false;

            var item = slots[index].itemData;

            // 퀵슬롯 전용 처리
            if (quickSlotService != null && quickSlotService.IsQuickSlotIndex(index))
                return quickSlotService.HandleQuickSlotUse(index);

            // 일반 인벤토리 아이템 처리
            var playerHealth = ServiceLocator.Get<IPlayerHealthService>();
            var equipment = ServiceLocator.Get<IEquipmentService>();

            if (item is EquippableItemSO equipItem)
            {
                var unequipped = equipment.Equip(equipItem, index);
                if (unequipped != null)
                    AddItem(unequipped, 1);

                RemoveItem(index, 1);
                return true;
            }

            if (item is ConsumableItemSO consumable)
            {
                consumable.Use(index, playerHealth, equipment);
                RemoveItem(index, 1);
                return true;
            }

            return false;
        }

        public void SwapSlots(int indexA, int indexB)
        {
            if (indexA == indexB) return;

            // 퀵슬롯 교환은 QuickSlotService에 위임
            if (quickSlotService != null &&
                (quickSlotService.IsQuickSlotIndex(indexA) || quickSlotService.IsQuickSlotIndex(indexB)))
            {
                quickSlotService.HandleInventorySwap(indexA, indexB);
                return;
            }

            // 일반 슬롯 간 교환
            if (indexA < 0 || indexA >= capacity || indexB < 0 || indexB >= capacity)
                return;

            (slots[indexA], slots[indexB]) = (slots[indexB], slots[indexA]);

            OnSlotChanged?.Invoke(indexA, slots[indexA]);
            OnSlotChanged?.Invoke(indexB, slots[indexB]);

            RefreshAllInventoryUI();
        }

        public void RefreshAllInventoryUI()
        {
            if (inventorySlotUIs == null || inventorySlotUIs.Length == 0)
                return;

            for (int i = 0; i < inventorySlotUIs.Length && i < capacity; i++)
            {
                var ui = inventorySlotUIs[i];
                if (ui != null)
                    ui.UpdateSlotUI(slots[i].itemData, slots[i].stackSize);
            }
        }
    }
}
