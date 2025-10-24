using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Game.Foundation;
using Game.InventorySystem;

namespace Game.UI
{
    /// <summary>
    /// XR 및 마우스 입력 모두 지원하는 인벤토리 슬롯 UI 핸들러.
    /// 드래그 앤 드롭, 툴팁 표시, 아이템 교환 등을 관리합니다.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class InventorySlotUIHandler : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [Header("Configuration")]
        [Tooltip("이 슬롯이 인벤토리에서 차지하는 인덱스 (0~N).")]
        public int slotIndex = -1;

        [Header("UI References")]
        [SerializeField] private Image dragIconImage;
        [SerializeField] private Image itemIconImage;

        private static bool isDragging = false;
        private IInventoryService inventory;
        private IQuickSlotService quickSlot;

        private void Awake()
        {
            inventory = ServiceLocator.Get<IInventoryService>();
            quickSlot = ServiceLocator.Get<IQuickSlotService>();

            if (itemIconImage == null)
            {
                Transform icon = transform.Find("Icon");
                if (icon != null)
                    itemIconImage = icon.GetComponent<Image>();
            }

            if (dragIconImage != null)
                dragIconImage.gameObject.SetActive(false);
        }

        // --------------------------------------------------------
        // [드래그 시작]
        // --------------------------------------------------------
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (inventory == null || inventory.IsSlotEmpty(slotIndex))
                return;

            var slot = inventory.GetSlot(slotIndex);
            if (slot.itemData == null) return;

            isDragging = true;

            // 드래그 아이콘 표시
            if (dragIconImage != null)
            {
                dragIconImage.sprite = slot.itemData.itemIcon;
                dragIconImage.color = new Color(1f, 1f, 1f, 0.9f);
                dragIconImage.gameObject.SetActive(true);
            }

            // 원본 아이콘 숨기기
            if (itemIconImage != null)
                itemIconImage.enabled = false;
        }

        // --------------------------------------------------------
        // [드래그 중]
        // --------------------------------------------------------
        public void OnDrag(PointerEventData eventData)
        {
            if (isDragging && dragIconImage != null)
                dragIconImage.transform.position = eventData.position;
        }

        // --------------------------------------------------------
        // [드래그 종료]
        // --------------------------------------------------------
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;
            isDragging = false;

            if (dragIconImage != null)
                dragIconImage.gameObject.SetActive(false);

            if (itemIconImage != null)
                itemIconImage.enabled = true;
        }

        // --------------------------------------------------------
        // [드롭]
        // --------------------------------------------------------
        public void OnDrop(PointerEventData eventData)
        {
            var sourceHandler = eventData.pointerDrag?.GetComponent<InventorySlotUIHandler>();
            if (sourceHandler == null || sourceHandler.slotIndex == slotIndex)
                return;

            if (inventory == null)
            {
                Debug.LogError("[InventorySlotUIHandler] InventoryService를 찾을 수 없습니다.");
                return;
            }

            // 슬롯 간 교환 처리
            inventory.SwapSlots(sourceHandler.slotIndex, slotIndex);
        }

        // --------------------------------------------------------
        // [툴팁 등 마우스/VR Hover 이벤트]
        // --------------------------------------------------------
        public void OnPointerEnter(PointerEventData eventData)
        {
            // TODO: 아이템 정보 툴팁 표시 로직 추가 (Inventory.GetSlot(slotIndex))
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // TODO: 툴팁 닫기 로직
        }
    }
}
