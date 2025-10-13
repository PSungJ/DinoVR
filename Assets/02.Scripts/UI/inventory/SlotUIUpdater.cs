using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro를 사용한다고 가정

public class SlotUIUpdater : MonoBehaviour
{
    // [Inspector에서 연결]
    [Header("UI Elements")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemAmountText;

    // 인벤토리 클래스에서 호출될 함수 (아이템 데이터를 받아 UI를 갱신)
    public void UpdateSlotUI(ItemBaseSO itemData, int stackSize)
    {
        // 1. 슬롯이 비어있을 때
        if (itemData == null || stackSize <= 0)
        {
            itemIcon.enabled = false;
            itemAmountText.text = "";
            return;
        }

        // 2. 슬롯에 아이템이 있을 때
        itemIcon.enabled = true;

        // 아이템 아이콘 설정
        if (itemIcon != null && itemData.itemIcon != null)
        {
            itemIcon.sprite = itemData.itemIcon;
        }

        // 아이템 개수 텍스트 설정
        if (itemAmountText != null)
        {
            itemAmountText.text = stackSize > 1 ? stackSize.ToString() : "";
        }
    }
}