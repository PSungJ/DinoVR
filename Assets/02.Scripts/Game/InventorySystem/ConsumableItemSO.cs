using UnityEngine;
using Game.Gameplay;
using Game.Foundation;

namespace Game.InventorySystem
{
    /// <summary>
    /// 사용 시 플레이어의 체력, 스태미나 등을 회복시키는 아이템 데이터.
    /// </summary>
    [CreateAssetMenu(menuName = "Inventory/Items/Consumable Item")]
    public class ConsumableItemSO : ItemBaseSO
    {
        [Header("Consumable Effects")]
        [Tooltip("사용 시 회복되는 체력량")]
        public int healthRestoreAmount = 25;

        [Tooltip("사용 시 회복되는 스태미나량 (선택사항)")]
        public int staminaRestoreAmount = 0;

        private void OnEnable()
        {
            itemType = ItemType.Consumable;
            maxStackSize = Mathf.Max(maxStackSize, 1);
        }

        /// <summary>
        /// 소비형 아이템 사용 시 로직 — 체력 또는 스태미나 회복.
        /// </summary>
        public override void Use(int slotIndex, IPlayerHealthService playerHealth, IEquipmentService equipment)
        {
            base.Use(slotIndex, playerHealth, equipment);

            if (playerHealth == null)
            {
                Debug.LogWarning($"[ConsumableItemSO] No player health service found for {itemName}");
                return;
            }

            if (healthRestoreAmount > 0)
            {
                playerHealth.Heal(healthRestoreAmount);
                Debug.Log($"[ConsumableItemSO] {itemName}: +{healthRestoreAmount} HP restored.");
            }

            if (staminaRestoreAmount > 0)
            {
                playerHealth.RestoreMana(staminaRestoreAmount);
                Debug.Log($"[ConsumableItemSO] {itemName}: +{staminaRestoreAmount} stamina restored.");
            }
        }
    }
}
