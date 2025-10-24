using System;
using Game.UI.Interface;

namespace Game.Foundation
{
    /// <summary>
    /// 인벤토리 시스템 전반의 공용 인터페이스입니다.
    /// Inventory, QuickSlotManager 등에서 참조됩니다.
    /// </summary>
    public interface IInventoryService : IGameService
    {
        int Capacity { get; }

        InventorySlot GetSlot(int index);
        void SetSlot(int index, InventorySlot slot);

        /// <summary>
        /// 슬롯 변경 시 호출되는 이벤트 (UI 갱신용)
        /// </summary>
        event Action<int, InventorySlot> OnSlotChanged;

        bool IsSlotEmpty(int index);
        bool AddItem(ItemBaseSO itemToAdd, int amount);
        bool RemoveItemByData(ItemBaseSO itemToRemove, int amount);
        void RemoveItem(int slotIndex, int amount);
        void RefreshAllInventoryUI();

        bool UseItem(int slotIndex);
        bool UseItemByData(ItemBaseSO itemToUse);

        /// <summary>
        /// 슬롯 간 교환 (퀵슬롯 포함)
        /// </summary>
        void SwapSlots(int indexA, int indexB);
    }
}
