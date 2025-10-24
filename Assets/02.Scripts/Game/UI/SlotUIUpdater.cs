using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Gameplay;
using Game.InventorySystem;

namespace Game.UI
{
    /// <summary>
    /// 인벤토리, 퀵슬롯 등의 슬롯 UI를 갱신하는 공용 컴포넌트.
    /// 아이템 아이콘, 개수 텍스트, 선택 하이라이트를 관리합니다.
    /// </summary>
    public class SlotUIUpdater : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI stackCountText;
        [SerializeField] private GameObject highlight;

        /// <summary>
        /// 슬롯에 표시할 아이템 데이터를 UI에 반영합니다.
        /// </summary>
        public void UpdateSlotUI(ItemBaseSO item, int count)
        {
            bool hasItem = item != null && count > 0;

            if (itemIcon != null)
            {
                itemIcon.enabled = hasItem;
                itemIcon.sprite = hasItem ? item.itemIcon : null;
            }

            if (stackCountText != null)
            {
                stackCountText.enabled = hasItem;
                stackCountText.text = (count > 1) ? count.ToString() : string.Empty;
            }
        }

        /// <summary>
        /// 현재 선택된 슬롯인지 여부를 시각적으로 표시합니다.
        /// </summary>
        public void SetHighlight(bool isSelected)
        {
            if (highlight != null)
                highlight.SetActive(isSelected);
        }

        /// <summary>
        /// 슬롯 UI를 완전히 초기화합니다. (아이콘 및 수량 제거)
        /// </summary>
        public void Clear()
        {
            if (itemIcon != null)
            {
                itemIcon.sprite = null;
                itemIcon.enabled = false;
            }

            if (stackCountText != null)
            {
                stackCountText.text = string.Empty;
                stackCountText.enabled = false;
            }

            if (highlight != null)
                highlight.SetActive(false);
        }
    }
}
