using UnityEngine;

// QuickSlotManager에서 사용되는 아이템 타입 열거형 (ItemBaseSO와 함께 정의되어야 함)
public enum ItemType
{
    Default,
    Consumable, // 퀵슬롯에서 사용하는 아이템 타입
    Equipment,
    Weapon // ⭐ [추가됨] EquippableItemSO에서 참조하는 Weapon 타입
}

// 모든 아이템 데이터의 기반이 되는 ScriptableObject입니다.
public abstract class ItemBaseSO : ScriptableObject
{
    [Header("Base Item Data")]
    public string itemName;
    public ItemType itemType = ItemType.Default;
    public Sprite itemIcon;

    // ⭐ [추가됨] 최대 스택 크기 정의
    [Header("Stacking")]
    [Tooltip("이 아이템이 한 슬롯에 쌓일 수 있는 최대 개수")]
    public int maxStackSize = 99; // 기본값 99로 설정하여 인스펙터에서 수정 가능

    /// <summary>
    /// 아이템 사용 로직의 기반 함수입니다. 
    /// QuickSlotManager에서 이 함수를 호출할 때 컴파일 오류가 발생하지 않도록 여기에 virtual로 정의합니다.
    /// 파생 클래스(예: ConsumableItemSO)는 이 함수를 override하여 구체적인 로직을 구현합니다.
    /// </summary>
    /// <param name="slotIndex">아이템이 사용된 인벤토리 또는 퀵슬롯의 인덱스입니다.</param>
    public virtual void Use(int slotIndex)
    {
        // 기본 아이템은 사용 시 아무것도 하지 않습니다.
        Debug.Log($"[ItemBaseSO] {itemName} (Type: {itemType}) was used but has no specific use logic defined.");
    }
}
