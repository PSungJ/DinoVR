using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Items/Consumable Item")]
public class ConsumableItemSO : ItemBaseSO
{
    [Header("Consumable Stats")]
    [Tooltip("사용 시 회복되는 체력량")]
    public int healthRestoreAmount = 25;

    protected void OnEnable()
    {
        // Consumable 아이템 타입 강제 설정
        itemType = ItemType.Consumable;
    }

    /// <summary>
    /// Consumable 아이템 사용 로직 (체력 회복 구현)
    /// </summary>
    // 🔥 수정: 시그니처를 ItemBaseSO.Use(int, PlayerHealthComponent, EquipmentManager)와 일치시켰습니다.
    public override void Use(int slotIndex, PlayerHealthComponent playerHealth, EquipmentManager equipmentManager)
    {
        // 기본 디버그 로그 호출
        base.Use(slotIndex, playerHealth, equipmentManager);

        // PlayerHealthComponent를 사용하여 체력 회복
        if (playerHealth != null)
        {
            playerHealth.Heal(healthRestoreAmount);
            // Debug.Log($"[Consumable Item] {itemName} 사용: 체력 {healthRestoreAmount} 회복 시도."); 
        }
        else
        {
            Debug.LogWarning("[ConsumableItemSO] Cannot find PlayerHealthComponent to heal. Check Scene setup.");
        }

        // 소비 아이템이므로, 아이템 사용 후에는 이 ConsumableItemSO를 사용하는
        // Inventory.cs 또는 QuickSlotManager.cs에서 스택 감소 처리가 진행됩니다.
    }
}