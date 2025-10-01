using UnityEngine;

// 모든 아이템의 종류를 구분하는 Enum
public enum ItemType
{
    General,
    Consumable,
    Weapon,
    Cyberware
}

public abstract class ItemBaseSO : ScriptableObject
{
    [Header("Base Item Data")]
    public string itemName = "New Item";
    [TextArea(3, 5)]
    public string description = "Item description.";
    public Sprite icon;

    // 아이템의 종류 (Inspector에서 설정)
    public ItemType itemType = ItemType.General;

    // 아이템의 최대 스택 수 (기본 1)
    [Min(1)]
    public int maxStackSize = 1;

    // TODO: 추가적인 아이템 속성 (예: Value, Weight 등)
}