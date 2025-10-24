using UnityEngine;
using Game.Gameplay;
using Game.InventorySystem;
using Game.Foundation;

namespace Game.Gameplay
{
    /// <summary>
    /// 모든 아이템 ScriptableObject의 공통 부모 클래스.
    /// </summary>
    public abstract class ItemBaseSO : ScriptableObject
    {
        [Header("Base Item Info")]
        public string itemName;
        public ItemType itemType = ItemType.Default;
        public Sprite itemIcon;
        public GameObject itemPrefab;

        [TextArea(3, 5)]
        public string description;

        [Header("Stack Settings")]
        [Tooltip("이 아이템이 한 슬롯에 쌓일 수 있는 최대 수량.")]
        public int maxStackSize = 1;

        /// <summary>
        /// 아이템 사용 시 실행되는 기본 로직.
        /// 자식 클래스(ConsumableItemSO, EquippableItemSO 등)에서 반드시 재정의해야 합니다.
        /// </summary>
        public virtual void Use(int slotIndex, IPlayerHealthService playerHealth, IEquipmentService equipment)
        {
            Debug.Log($"[ItemBaseSO] Using {itemName} (Type: {itemType}) from slot {slotIndex}");
        }

        /// <summary>
        /// 아이템의 기본 정보 문자열을 반환 (디버그/툴팁 용도)
        /// </summary>
        public virtual string GetTooltipText()
        {
            return $"{itemName}\n<size=80%>{description}</size>";
        }
    }

    /// <summary>
    /// 아이템 타입을 정의하는 열거형. 장비/소비/기타로 구분됩니다.
    /// </summary>
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
}
