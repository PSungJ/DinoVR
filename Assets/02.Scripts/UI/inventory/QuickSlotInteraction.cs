using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine;

public class QuickSlotInteraction : MonoBehaviour
{
    [SerializeField] private ItemBaseSO itemInSlot; // 퀵슬롯에 연결된 아이템 데이터

    private void Awake()
    {
        // 1. XR Interactable 컴포넌트 가져오기 (Select 이벤트를 받을 준비)
        var interactable = GetComponent<XRBaseInteractable>();
        if (interactable != null)
        {
            // 2. Select Entered 이벤트에 아이템 사용 로직 연결
            interactable.selectEntered.AddListener(OnQuickSlotSelected);
        }
    }

    private void OnQuickSlotSelected(SelectEnterEventArgs args)
    {
        if (itemInSlot != null)
        {
            // Inventory 싱글톤을 통해 아이템 사용 함수 호출
            Inventory.Instance.UseItemByData(itemInSlot);
        }
        else
        {
            Debug.Log("퀵슬롯이 비어있습니다.");
        }
    }
}