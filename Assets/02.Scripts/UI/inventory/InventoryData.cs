using System;
using System.Collections.Generic;

// Unity의 기본 직렬화(Serialization)를 위해 [Serializable] 속성을 추가합니다.
[Serializable]
public class InventoryData
{
    // Inventory.slots 배열의 데이터를 저장합니다.
    // List로 정의하는 것이 JSON 또는 바이너리 직렬화에 유리합니다.
    public List<InventorySlot> slots;

    // 이외에 저장할 데이터 (예: 인벤토리 크기 등)
    public int capacity;

    public InventoryData()
    {
        slots = new List<InventorySlot>();
        capacity = 0;
    }
}