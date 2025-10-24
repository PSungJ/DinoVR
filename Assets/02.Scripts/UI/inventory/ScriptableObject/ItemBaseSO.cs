// ItemBaseSO.cs (수정된 전체 코드)
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
    // 🔥 핵심: virtual 키워드와 3개의 매개변수를 가짐
    public virtual void Use(int slotIndex, PlayerHealthComponent playerHealth, EquipmentManager equipmentManager)
    {
        Debug.Log($"[ItemBaseSO] Using {itemName} at slot {slotIndex}. Type: {itemType}");
    }

}