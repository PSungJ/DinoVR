using UnityEngine;

// ItemType enum이 ItemBaseSO의 itemType 필드에서 사용됩니다.
public enum ItemType
{
    Default,
    Consumable,
    Equipment,
    Weapon,
    Helmet,
    Armor,
    Boots
}

// NOTE: InventorySlot 구조체 정의를 이 파일에서 제거하여 '모호성 오류'를 해결했습니다.
// InventorySlot은 이제 전역에서 참조됩니다.

public abstract class ItemBaseSO : ScriptableObject
{
    [Header("Base Item Data")]
    public string itemName;
    public ItemType itemType;
    public Sprite itemIcon;
    public GameObject itemPrefab;

    [TextArea(3, 5)]
    public string description;

    // 최대 스택 크기
    [Header("Stacking")]
    public int maxStackSize = 1;

    /// <summary>
    /// 아이템 사용 시 호출되는 기본 로직입니다.
    /// </summary>
    // PlayerHealthComponent와 EquipmentManager가 정의되어 있지 않으므로 컴파일 오류가 발생할 수 있습니다.
    // 임시로 주석 처리하거나, 해당 클래스들이 전역에 정의되어 있다고 가정합니다.
    public virtual void Use(int slotIndex, PlayerHealthComponent playerHealth, EquipmentManager equipmentManager)
    {
        Debug.Log($"[ItemBaseSO] Using {itemName} at slot {slotIndex}. Type: {itemType}");
    }
}
