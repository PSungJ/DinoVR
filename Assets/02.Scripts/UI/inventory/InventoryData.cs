using System;
using System.Collections.Generic;

namespace Game.InventorySystem
{
    /// <summary>
    /// 인벤토리 전체 데이터를 직렬화 가능한 형태로 저장하는 클래스.
    /// Save/Load 시스템과 연동 시 사용됩니다.
    /// </summary>
    [Serializable]
    public class InventoryData
    {
        /// <summary>
        /// 인벤토리의 각 슬롯 데이터 리스트.
        /// </summary>
        public List<InventorySlot> slots = new List<InventorySlot>();

        /// <summary>
        /// 인벤토리 최대 크기.
        /// </summary>
        public int capacity;

        public InventoryData() { }

        public InventoryData(int capacity)
        {
            this.capacity = capacity;
            slots = new List<InventorySlot>(capacity);

            // 모든 슬롯을 비어있는 상태로 초기화
            for (int i = 0; i < capacity; i++)
            {
                slots.Add(InventorySlot.Empty);
            }
        }

        /// <summary>
        /// 지정된 슬롯의 데이터를 반환합니다.
        /// </summary>
        public InventorySlot GetSlot(int index)
        {
            if (index < 0 || index >= slots.Count)
                return InventorySlot.Empty;
            return slots[index];
        }

        /// <summary>
        /// 특정 슬롯의 데이터를 수정합니다.
        /// </summary>
        public void SetSlot(int index, InventorySlot slot)
        {
            if (index < 0 || index >= slots.Count)
                return;
            slots[index] = slot;
        }

        /// <summary>
        /// 모든 슬롯을 비워 초기화합니다.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < slots.Count; i++)
                slots[i] = InventorySlot.Empty;
        }
    }
}
