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
    public override void Use(int slotIndex, PlayerHealthComponent playerHealth, EquipmentManager equipmentManager)
    {
        // base.Use 호출 시 playerHealth와 equipmentManager를 전달합니다.
        base.Use(slotIndex, playerHealth, equipmentManager); // 기본 디버그 로그 호출

        // PlayerHealthComponent를 사용하여 체력 회복
        if (playerHealth != null)
        {
            playerHealth.Heal(healthRestoreAmount);
            // ⭐ 테스트: TLS 오류가 사라지는지 확인하기 위해 로그를 주석 처리합니다.
            // Debug.Log($"[Consumable Item] {itemName} 사용: 체력 {healthRestoreAmount} 회복 시도."); 
        }
        else
        {
            // ⭐ 테스트: TLS 오류가 사라지는지 확인하기 위해 로그를 주석 처리합니다.
            // Debug.LogWarning("[ConsumableItemSO] Cannot find PlayerHealthComponent to heal. Check if PlayerHealthComponent.Instance is correctly initialized.");
        }
    }
}
