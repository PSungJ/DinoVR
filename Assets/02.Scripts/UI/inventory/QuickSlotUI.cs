using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class QuickSlotUI : MonoBehaviour
{
    [Header("Dependencies")]
    // QuickSlotManager 참조 필수 (선택 인덱스를 받아옴)
    [SerializeField] private QuickSlotManager quickSlotManager;

    [Header("UI Elements (5 Slots)")]
    // 5개의 슬롯 UI GameObject 루트를 순서대로 할당합니다.
    [SerializeField] private List<GameObject> slotUIRoots = new List<GameObject>(5); 
    // 각 슬롯의 아이콘 Image 컴포넌트를 순서대로 할당합니다.
    [SerializeField] private List<Image>slotIcons = new List<Image>(5); 

    [Header("Selection Indicator")]
    // 현재 선택된 슬롯을 강조하는 UI 요소 (예: 테두리, 하이라이트 이미지 등)
    [SerializeField] private GameObject selectionIndicator; 

    // ----------------------------------------------------
    // [아이콘 업데이트]
    // ----------------------------------------------------
    // Manager에서 아이템이 할당/제거될 때 호출됩니다.
    public void UpdateSlot(int index, ItemBaseSO item)
    {
        if (index >= 0 && index < slotIcons.Count && slotIcons[index] != null)
        {
            bool hasItem = (item != null);
            slotIcons[index].sprite = hasItem ? item.icon : null;
            slotIcons[index].enabled = hasItem;
        }
    }

    // ----------------------------------------------------
    // [선택 상태 업데이트]
    // ----------------------------------------------------
    // Manager에서 슬롯이 변경되거나 초기화될 때 호출됩니다.
    public void UpdateSelection(int newIndex)
    {
        if (newIndex >= 0 && newIndex < slotUIRoots.Count && selectionIndicator != null)
        {
            // 선택된 슬롯의 위치로 강조 UI를 이동시킵니다.
            selectionIndicator.transform.position = slotUIRoots[newIndex].transform.position;
            // 강조 UI를 활성화합니다.
            selectionIndicator.SetActive(true);
        }
        else if (selectionIndicator != null)
        {
            // 유효하지 않은 인덱스인 경우 비활성화 (선택 해제)
            selectionIndicator.SetActive(false);
        }
    }
    
    // TODO: Awake/Start에서 Manager로부터 초기 데이터를 받아와 전체 UI를 설정하는 로직 추가
}