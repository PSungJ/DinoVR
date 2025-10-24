using UnityEngine;
using Game.Foundation;
using Game.InventorySystem;

namespace Game.Gameplay
{
    /// <summary>
    /// 무기, 방어구 등 플레이어가 장착할 수 있는 아이템 데이터.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEquippableItem", menuName = "Inventory/Items/Equippable Item")]
    public class EquippableItemSO : ItemBaseSO
    {
        [Header("Equipment Settings")]
        [Tooltip("이 아이템이 장착될 슬롯 종류")]
        public EquipSlotType equipSlotType = EquipSlotType.Weapon;

        [Header("Stat Modifiers")]
        [Tooltip("플레이어의 공격력에 추가되는 수치")]
        public int attackModifier;

        [Tooltip("플레이어의 방어력에 추가되는 수치")]
        public int defenseModifier;

        private void OnEnable()
        {
            // 장착 아이템은 스택 불가
            maxStackSize = 1;

            // ItemType 자동 설정
            switch (equipSlotType)
            {
                case EquipSlotType.Weapon: itemType = ItemType.Weapon; break;
                case EquipSlotType.Helmet: itemType = ItemType.Helmet; break;
                case EquipSlotType.Armor: itemType = ItemType.Armor; break;
                case EquipSlotType.Boots: itemType = ItemType.Boots; break;
                default: itemType = ItemType.Equipment; break;
            }
        }

        /// <summary>
        /// 장비 아이템 사용 시 — 장비 매니저를 통해 장착 시도.
        /// </summary>
        public override void Use(int slotIndex, IPlayerHealthService playerHealth, IEquipmentService equipment)
        {
            base.Use(slotIndex, playerHealth, equipment);

            if (equipment == null)
            {
                Debug.LogWarning($"[EquippableItemSO] Cannot equip {itemName}: No IEquipmentService found.");
                return;
            }

            // 장비 서비스에 직접 요청
            equipment.Equip(this, slotIndex);
            Debug.Log($"[EquippableItemSO] {itemName} equipped in {equipSlotType} slot.");
        }
    }

 
}
