using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Game.Foundation;

public class QuickSlotInteraction : MonoBehaviour
{
    [SerializeField] private ItemBaseSO itemInSlot; // 퀵슬롯에 연결된 아이템 데이터

    private void Awake()
    {
        var interactable = GetComponent<XRBaseInteractable>();
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnQuickSlotSelected);
        }
    }

    private void OnQuickSlotSelected(SelectEnterEventArgs args)
    {
        var inventory = ServiceLocator.Get<IInventoryService>();

        if (itemInSlot != null && inventory != null)
        {
            // 🔍 아이템을 가진 슬롯을 찾는다.
            for (int i = 0; i < inventory.Capacity; i++)
            {
                var slot = inventory.GetSlot(i);
                if (slot.itemData == itemInSlot && !slot.IsEmpty)
                {
                    inventory.UseItem(i);
                    return;
                }
            }

            Debug.Log($"[QuickSlotInteraction] {itemInSlot.itemName} 아이템을 인벤토리에서 찾을 수 없습니다.");
        }
        else
        {
            Debug.LogWarning("[QuickSlotInteraction] 퀵슬롯이 비어있거나 인벤토리 서비스를 찾을 수 없습니다.");
        }
    }
}
