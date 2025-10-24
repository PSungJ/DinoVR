using UnityEngine;

namespace Game.InventorySystem
{
    /// <summary>
    /// 장착 가능한 슬롯의 종류를 정의합니다.
    /// EquipmentManager, EquippableItemSO, EquipmentSlotUI에서 사용됩니다.
    /// </summary>
    public enum EquipSlotType
    {
        Weapon,
        Helmet,
        Armor,
        Boots
    }
}
