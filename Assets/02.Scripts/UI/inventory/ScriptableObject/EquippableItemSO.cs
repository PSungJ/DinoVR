using UnityEngine;
using Game.Foundation;
using Game.Gameplay;

namespace Game.InventorySystem
{
    /// <summary>
    /// 장비형 아이템 ScriptableObject (무기, 방어구 등)
    /// </summary>
    [CreateAssetMenu(fileName = "NewEquippableItem", menuName = "Inventory/Items/Equippable Item")]
    public class EquippableItemSO : ItemBaseSO
    {
        [Header("Equip Settings")]
        [Tooltip("이 아이템이 장착될 슬롯 종류 (Weapon, Armor 등)")]
        public EquipSlotType equipSlotType;

        [Header("Stat Modifiers")]
        [Tooltip("플레이어의 공격력 증가량")]
        public int attackModifier = 0;

        [Tooltip("플레이어의 방어력 증가량")]
        public int defenseModifier = 0;

        private void OnEnable()
        {
            // 장비 아이템은 스택 불가
            maxStackSize = 1;

            // EquipSlotType 이름을 기반으로 ItemType 자동 설정
            try
            {
                itemType = (ItemType)System.Enum.Parse(typeof(ItemType), equipSlotType.ToString());
            }
            catch
            {
                itemType = ItemType.Equipment;
            }
        }

        /// <summary>
        /// 장비 아이템을 사용할 때 호출되는 로직 (ServiceLocator 기반)
        /// </summary>
        public override void Use(int slotIndex, IPlayerHealthService playerHealth, IEquipmentService equipmentManager)
        {
            base.Use(slotIndex, playerHealth, equipmentManager);

            // ✅ ServiceLocator를 통해 EquipmentService 가져오기
            var equipmentService = equipmentManager ?? ServiceLocator.Get<IEquipmentService>();

            if (equipmentService != null)
            {
                equipmentService.EquipItem(this, slotIndex);
            }
            else
            {
                Debug.LogWarning($"[EquippableItemSO] EquipmentService를 찾을 수 없습니다. {itemName} 장착 실패");
            }
        }
    }
}
