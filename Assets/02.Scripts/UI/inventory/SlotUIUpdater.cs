using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit; // XRGrabInteractable을 사용하기 위해 추가

public class SlotUIUpdater : MonoBehaviour
{
    // [Inspector에서 연결]
    [Header("UI Elements")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemAmountText;

    [Header("Interaction")]
    //  [핵심 추가] 각 슬롯의 XRGrabInteractable 컴포넌트를 Inspector에서 연결해야 합니다.
    [SerializeField] private XRGrabInteractable grabInteractable;


    /// <summary>
    /// 아이템 데이터를 기반으로 슬롯 UI를 갱신합니다.
    /// </summary>
    public void UpdateSlotUI(ItemBaseSO itemData, int stackSize)
    {
        // 아이템이 유효한지 판단
        bool hasItem = itemData != null && stackSize > 0;

        // 1. 아이콘 GameObject 활성화/비활성화 (이미지 표시 및 잔상 문제 해결)
        if (itemIcon != null)
        {
            // 컴포넌트 enabled 대신 GameObject 자체를 켜고 끕니다.
            itemIcon.gameObject.SetActive(hasItem);
        }

        // 🔥 2. Grab Interactable 활성화/비활성화 (퀵슬롯 Grab 문제 해결)
        // 아이템이 있을 때만 잡을 수 있도록 합니다.
        if (grabInteractable != null)
        {
            grabInteractable.enabled = hasItem;
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