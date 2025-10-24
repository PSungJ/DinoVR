using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Game.Foundation;
using Game.InventorySystem;

namespace Game.Gameplay.Interaction
{
    /// <summary>
    /// XR 환경에서 퀵슬롯 아이템을 선택하거나 사용할 수 있게 하는 인터랙션 컴포넌트입니다.
    /// </summary>
    [RequireComponent(typeof(XRBaseInteractable))]
    public class QuickSlotInteraction : MonoBehaviour
    {
        [Header("Quick Slot Config")]
        [Tooltip("이 인터랙션이 담당하는 슬롯 인덱스 (절대 인벤토리 인덱스).")]
        [SerializeField] private int slotIndex = -1;

        [Tooltip("해당 퀵슬롯에 들어있는 아이템 데이터 (자동 업데이트 예정)")]
        [SerializeField] private ItemBaseSO itemInSlot;

        private XRBaseInteractable interactable;

        private void Awake()
        {
            interactable = GetComponent<XRBaseInteractable>();
            if (interactable != null)
            {
                interactable.selectEntered.AddListener(OnQuickSlotSelected);
            }
        }

        private void OnDestroy()
        {
            if (interactable != null)
                interactable.selectEntered.RemoveListener(OnQuickSlotSelected);
        }

        /// <summary>
        /// VR 컨트롤러가 슬롯을 선택했을 때 호출됩니다.
        /// </summary>
        private void OnQuickSlotSelected(SelectEnterEventArgs args)
        {
            var inventory = ServiceLocator.Get<IInventoryService>();
            if (inventory == null)
            {
                Debug.LogError("[QuickSlotInteraction] Inventory service not found.");
                return;
            }

            // 슬롯 인덱스가 유효하고 아이템이 존재하면 사용 시도
            if (slotIndex >= 0 && !inventory.IsSlotEmpty(slotIndex))
            {
                bool used = inventory.UseItem(slotIndex);
                if (used)
                    Debug.Log($"[QuickSlotInteraction] Used item in slot {slotIndex}");
                else
                    Debug.LogWarning($"[QuickSlotInteraction] Item use failed for slot {slotIndex}");
            }
            else
            {
                Debug.Log("[QuickSlotInteraction] Slot is empty or invalid.");
            }
        }

        /// <summary>
        /// 외부에서 아이템 데이터가 바뀔 때 호출됩니다.
        /// (UI나 QuickSlotManager에서 연결)
        /// </summary>
        public void UpdateSlotItem(ItemBaseSO newItem)
        {
            itemInSlot = newItem;
        }
    }
}
