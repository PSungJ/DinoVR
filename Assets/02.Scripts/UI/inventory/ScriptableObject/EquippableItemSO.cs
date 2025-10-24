// EquippableItemSO.cs (전체 코드)
using UnityEngine;

// EquippableItemSO는 ItemBaseSO를 상속받습니다.
// ItemType, InventorySlot 구조체, EquipSlotType enum은 다른 파일(ItemBaseSO.cs, EquipmentManager.cs)에 정의되어 있습니다.

[CreateAssetMenu(fileName = "NewEquippableItem", menuName = "Inventory/Equippable Item")]
public class EquippableItemSO : ItemBaseSO
{
    // 장착 위치 (EquipmentManager.cs에 정의된 EquipSlotType 사용)
    public EquipSlotType equipSlotType;

    [Header("Equipment Stats")]
    // PlayerStats에 반영될 공격력 보너스
    public int attackModifier;

    [Tooltip("PlayerStats에 반영될 방어력 보너스")]
    public int defenseModifier;

    private void OnEnable()
    {
        // 장비 아이템은 스택 불가 (최대 1개)
        maxStackSize = 1;

        // 장착 슬롯 타입에 따라 ItemType을 자동 설정합니다.
        try
        {
            // EquipSlotType의 이름을 ItemType으로 파싱하여 할당합니다.
            itemType = (ItemType)System.Enum.Parse(typeof(ItemType), equipSlotType.ToString());
        }
        catch (System.ArgumentException e)
        {
            // ItemType에 해당하는 항목이 없을 경우, Equipment 기본 타입으로 대체합니다.
            Debug.LogError($"[EquippableItemSO] Failed to parse ItemType from {equipSlotType.ToString()}. Check if ItemType enum contains this value. Falling back to ItemType.Equipment. Error: {e.Message}");
            itemType = ItemType.Equipment;
        }
    }

    /// <summary>
    /// 장비 아이템 사용 로직 (장비 장착 시도)
    /// </summary>
    // 🔥 시그니처 확인: ItemBaseSO와 동일한 (int slotIndex, PlayerHealthComponent playerHealth, EquipmentManager equipmentManager)를 사용합니다.
    public override void Use(int slotIndex, PlayerHealthComponent playerHealth, EquipmentManager equipmentManager)
    {
        // 기본 디버그 로그 호출
        base.Use(slotIndex, playerHealth, equipmentManager);

        if (equipmentManager != null)
        {
            // EquipmentManager에게 이 아이템을 장착하도록 요청합니다.
            // 성공적으로 장착되면 Inventory.cs/QuickSlotManager.cs에서 슬롯 데이터가 갱신됩니다.
            equipmentManager.Equip(this, slotIndex);
        }
        else
        {
            Debug.LogWarning("[EquippableItemSO] EquipmentManager 인스턴스를 찾을 수 없어 아이템을 장착할 수 없습니다.");
        }
    }
}