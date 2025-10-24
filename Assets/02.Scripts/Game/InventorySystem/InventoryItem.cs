using UnityEngine;
using System;

// 인벤토리 데이터를 저장할 구조체/클래스
[System.Serializable]
public class InventoryItem
{
    // 아이템을 식별하는 고유 ID 또는 이름
    public string itemId;

    // 인벤토리 슬롯에 표시될 이미지
    public Sprite icon;

    // 아이템의 개수 (스택 가능)
    public int stackCount;

    // 해당 슬롯이 비어있는지 확인하는 속성
    public bool IsEmpty => string.IsNullOrEmpty(itemId) || stackCount <= 0;

    // 빈 아이템을 생성하는 정적 함수
    public static InventoryItem GetEmptyItem()
    {
        return new InventoryItem { itemId = string.Empty, icon = null, stackCount = 0 };
    }

    // 아이템 초기화 생성자
    public InventoryItem(string id, Sprite itemIcon, int count = 1)
    {
        itemId = id;
        icon = itemIcon;
        stackCount = count;
    }

    // 빈 생성자 (직렬화용)
    public InventoryItem() { }
}