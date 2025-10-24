using Game.InventorySystem;

namespace Game.Foundation
{
    /// <summary>
    /// 퀵슬롯 시스템이 제공해야 하는 공통 기능 인터페이스.
    /// </summary>
    public interface IQuickSlotService : IGameService
    {
        int CurrentSlotIndex { get; }
        bool IsQuickSlotEmpty(int index);
        bool IsQuickSlotIndex(int index);
        void HandleInventorySwap(int from, int to);
        bool HandleQuickSlotUse(int slotIndex);
        void CycleNextSlot();
        void RefreshAllQuickSlotUI();
        void ToggleInventoryUI();
    }
}
