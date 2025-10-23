using System;
using Game.Gameplay;

namespace Game.InventorySystem
{
    /// <summary>
    /// 인벤토리, 퀵슬롯, 장비창 등에서 사용하는 공통 슬롯 구조체.
    /// 아이템 ScriptableObject 참조와 수량을 보유합니다.
    /// </summary>
    [Serializable]
    public struct InventorySlot
    {
        public ItemBaseSO itemData;
        public int stackSize;

        public bool IsEmpty => itemData == null || stackSize <= 0;

        public InventorySlot(ItemBaseSO data, int amount)
        {
            itemData = data;
            stackSize = amount;
        }

        /// <summary>
        /// 슬롯을 완전히 비웁니다.
        /// </summary>
        public static InventorySlot Empty => new InventorySlot(null, 0);

        /// <summary>
        /// 슬롯의 아이템을 하나 사용(감소)합니다.
        /// </summary>
        public void ConsumeOne()
        {
            if (stackSize > 0)
                stackSize--;

            if (stackSize <= 0)
                this = Empty;
        }

        /// <summary>
        /// 동일한 아이템을 추가로 스택할 수 있는지 검사합니다.
        /// </summary>
        public bool CanStack(ItemBaseSO newItem)
        {
            if (IsEmpty || newItem == null)
                return false;

            return itemData == newItem && stackSize < newItem.maxStackSize;
        }

        /// <summary>
        /// 새 아이템을 스택합니다.
        /// </summary>
        public bool TryAddToStack(ItemBaseSO newItem, int amount)
        {
            if (!CanStack(newItem))
                return false;

            stackSize = Math.Min(stackSize + amount, newItem.maxStackSize);
            return true;
        }
    }
}
