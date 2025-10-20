using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro를 사용한다고 가정

/// <summary>
/// 퀵슬롯 UI 슬롯의 아이템 표시 및 하이라이트를 관리하는 컴포넌트입니다.
/// </summary>
public class SlotUIUpdater : MonoBehaviour
{
    // Inspector에서 연결해야 할 필드입니다.
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI stackCountText;
    [SerializeField] private GameObject highlight;

    public void UpdateSlotUI(ItemBaseSO item, int count)
    {
        bool hasItem = item != null && count > 0;

        if (itemIcon != null)
        {
            itemIcon.enabled = hasItem;
            if (hasItem) itemIcon.sprite = item.itemIcon;
        }

        if (stackCountText != null)
        {
            stackCountText.enabled = hasItem;
            stackCountText.text = count > 1 ? count.ToString() : "";
        }
    }

    public void SetHighlight(bool isSelected)
    {
        if (highlight != null)
        {
            highlight.SetActive(isSelected);
        }
    }
}
