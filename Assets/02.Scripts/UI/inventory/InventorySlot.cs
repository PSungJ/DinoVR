using System;
using UnityEngine; // ItemBaseSO의 직렬화를 위해 필요할 수 있음

// Unity의 기본 직렬화(Serialization)를 위해 [Serializable] 속성을 추가합니다.
[Serializable]
public struct InventorySlot
{
    // 슬롯에 할당된 아이템 ScriptableObject 데이터
    public ItemBaseSO itemData;

    // 현재 슬롯에 쌓여있는 아이템 개수
    public int stackSize;

    // 슬롯이 비어있는지 확인하는 속성
    public bool IsEmpty => itemData == null || stackSize <= 0;

    // 아이템이 있는 슬롯을 생성하는 생성자 (C# 9.0 이하에서도 호환)
    public InventorySlot(ItemBaseSO data, int amount)
    {
        // struct 생성자는 모든 필드를 명시적으로 초기화해야 합니다.
        this.itemData = data;
        this.stackSize = amount;
    }

   
    public static InventorySlot Empty => new InventorySlot(null, 0);
}