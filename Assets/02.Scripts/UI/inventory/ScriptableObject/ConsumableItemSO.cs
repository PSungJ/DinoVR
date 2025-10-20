using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Items/Consumable Item")]
public class ConsumableItemSO : ItemBaseSO
{
    [Header("Consumable Stats")]
    [Tooltip("사용 시 회복되는 체력량")]
    public int healthRestoreAmount = 25;

    private void OnEnable()
    {
        // Consumable 아이템 타입 강제 설정
        itemType = ItemType.Consumable;
    }

    /// <summary>
    /// Consumable 아이템 사용 로직 (체력 회복 구현)
    /// </summary>
    public override void Use(int slotIndex, PlayerHealthComponent playerHealth, EquipmentManager equipmentManager)
    {
        // ⭐ 수정: base.Use 호출 시 playerHealth와 equipmentManager를 전달합니다.
        base.Use(slotIndex, playerHealth, equipmentManager); // 기본 디버그 로그 호출

        // PlayerHealthComponent를 사용하여 체력 회복
        if (playerHealth != null)
        {
            playerHealth.Heal(healthRestoreAmount);
            // 아이템 스택 감소 로직은 QuickSlotManager에서 처리됩니다.
        }
        else
        {
            Debug.LogWarning("[ConsumableItemSO] Cannot find PlayerHealthComponent to heal. Check if PlayerHealthComponent.Instance is correctly initialized.");
        }
    }
}
