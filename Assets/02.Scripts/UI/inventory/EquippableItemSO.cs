using UnityEngine;

[CreateAssetMenu(fileName = "NewEquippableItem", menuName = "Inventory/Equippable Item")]
public class EquippableItemSO : ItemBaseSO
{
    // 장착 위치 (Weapon, Cyberware)
    public EquipSlotType equipSlotType;

    [Header("Equipment Stats")]
    // PlayerStats에 반영될 공격력 보너스
    public int attackModifier;

    private void OnEnable()
    {
        // 장비 아이템은 스택 불가 (최대 1개)
        maxStackSize = 1;

        // 장착 슬롯 타입에 따라 ItemType을 자동 설정
        itemType = (ItemType)System.Enum.Parse(typeof(ItemType), equipSlotType.ToString());
    }
}