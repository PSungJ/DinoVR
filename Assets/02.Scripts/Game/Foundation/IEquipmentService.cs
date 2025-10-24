using Game.Gameplay;
using Game.InventorySystem;
using Game.UI.Interface;

namespace Game.Foundation
{
    /// <summary>
    /// 장비 장착/해제 서비스를 외부로 노출하는 인터페이스입니다.
    /// EquipmentManager가 이 인터페이스를 구현하여 ServiceLocator에 등록됩니다.
    /// </summary>
    public interface IEquipmentService : IGameService
    {
        /// <summary>
        /// 새로운 장비 아이템을 장착합니다.
        /// </summary>
        /// <param name="itemToEquip">장착할 아이템 데이터</param>
        /// <param name="inventorySlotIndex">이 아이템이 온 인벤토리 슬롯 인덱스(-1은 외부)</param>
        /// <returns>이전에 장착되어 있던 아이템 (없으면 null)</returns>
        EquippableItemSO Equip(EquippableItemSO itemToEquip, int inventorySlotIndex);

        /// <summary>
        /// 특정 슬롯의 장비를 해제합니다.
        /// </summary>
        /// <param name="slotType">해제할 장비 슬롯</param>
        /// <returns>해제된 아이템 (없으면 null)</returns>
        EquippableItemSO Unequip(EquipSlotType slotType);
    }
}
