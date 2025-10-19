using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlotUIUpdater : MonoBehaviour
{
    // [Inspector에서 연결]
    [Header("UI Elements")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemAmountText;

    // ⭐ 하이라이트 효과를 위한 Image 컴포넌트
    [Header("Highlight Elements")]
    [SerializeField] private Image highlightBorder;

    private Color originalBorderColor;

    private void Awake()
    {
        // 하이라이트용 Image 컴포넌트가 있다면 초기 색상을 저장합니다.
        if (highlightBorder != null)
        {
            originalBorderColor = highlightBorder.color;
            // 초기에는 비활성화 상태
            highlightBorder.enabled = false;
        }
    }

    /// <summary>
    /// 현재 슬롯의 하이라이트 상태를 설정합니다.
    /// QuickSlotManager에서 호출되어 현재 선택된 슬롯을 초록색으로 표시합니다.
    /// </summary>
    /// <param name="isHighlighted">하이라이트 활성화 여부</param>
    /// <param name="color">하이라이트 색상 (예: Color.green)</param>
    public void SetHighlight(bool isHighlighted, Color color)
    {
        if (highlightBorder == null)
        {
            Debug.LogError("[SlotUIUpdater] highlightBorder Image is not assigned. Cannot set highlight.");
            return;
        }

        if (isHighlighted)
        {
            // 하이라이트 활성화 및 색상 적용
            highlightBorder.enabled = true;
            highlightBorder.color = color;
        }
        else
        {
            // 하이라이트 비활성화
            highlightBorder.enabled = false;
        }
    }

    // 인벤토리 클래스에서 호출될 함수 (아이템 데이터를 받아 UI를 갱신)
    public void UpdateSlotUI(ItemBaseSO itemData, int stackSize)
    {
        // 아이템이 유효한지 판단
        bool hasItem = itemData != null && stackSize > 0;

        // 아이템 아이콘 게임 오브젝트 활성화/비활성화
        if (itemIcon != null)
        {
            itemIcon.gameObject.SetActive(hasItem);
        }

        if (hasItem)
        {
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
        else
        {
            // 슬롯이 비어있을 때
            if (itemAmountText != null)
            {
                itemAmountText.text = "";
            }
        }
    }
}
