using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using System;

/// <summary>
/// 인벤토리, 퀵슬롯 UI 슬롯에 부착되어
/// VR Ray Interactor를 통한 드래그 앤 드롭(IBeginDragHandler, IDropHandler 등) 이벤트를 처리합니다.
/// </summary>
[RequireComponent(typeof(Image))] // Raycast Target을 위해 Image 컴포넌트 필요
public class InventorySlotUIHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    // ⭐ 외부에서 이 슬롯이 인벤토리의 몇 번 인덱스를 나타내는지 설정해야 합니다.
    [Header("Runtime Data")]
    [Tooltip("이 슬롯이 InventoryManager의 slots 배열에서 차지하는 인덱스입니다.")]
    public int slotIndex = -1;

    [Header("Dependencies")]
    [Tooltip("캔버스 최상단에 있는 드래그 아이콘 Image (커서를 따라다님)")]
    [SerializeField] private Image dragIconImage;

    // 이 슬롯의 아이템 아이콘 Image (SlotUIUpdater가 업데이트하는 그 아이콘)
    private Image itemIconImage;

    // Ray Interactor의 현재 드래그 상태를 추적 (UI를 잡고 있는지 여부)
    private static bool isDragging = false;

    private void Awake()
    {
        // ItemIconImage는 UI 갱신 스크립트(SlotUIUpdater)가 관리한다고 가정합니다.
        Transform iconTransform = transform.Find("Icon");
        if (iconTransform != null)
        {
            itemIconImage = iconTransform.GetComponent<Image>();
        }

        if (itemIconImage == null)
        {
            Debug.LogWarning($"[UI Handler] Slot {gameObject.name}: 아이템 아이콘 Image를 찾을 수 없습니다 (자식 이름 'Icon' 확인).");
        }

        if (dragIconImage == null)
        {
            Debug.LogError("[UI Handler] Drag Icon Image가 Inspector에 할당되지 않았습니다.");
        }

        // 부모 Image (Raycast Target)가 존재하는지 확인
        if (GetComponent<Image>() == null || !GetComponent<Image>().raycastTarget)
        {
            Debug.LogError($"[UI Handler] Slot {gameObject.name}: Ray Interactor 인식을 위해 부모 Image 컴포넌트에 Raycast Target이 켜져 있어야 합니다!");
        }
    }

    // --------------------------------------------------------------------------------
    // [1. 드래그 시작: IBeginDragHandler]
    // --------------------------------------------------------------------------------
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[UI Handler] Drag Failed: InventoryManager.Instance not found.");
            return;
        }

        // ⭐ [FIX: IsSlotEmpty 함수 사용]
        if (InventoryManager.Instance.IsSlotEmpty(slotIndex))
        {
            // 아이템이 없으면 드래그를 시작할 수 없습니다.
            Debug.LogWarning($"[UI Handler] Drag Failed: Slot {slotIndex} is empty.");
            return;
        }

        // 드래그 상태 플래그 설정
        isDragging = true;

        // ⭐ [FIX: InventoryManager의 GetItemIcon 함수 사용]
        Sprite iconSprite = InventoryManager.Instance.GetItemIcon(slotIndex);
        string itemName = InventoryManager.Instance.slots[slotIndex].itemName;


        if (itemIconImage != null)
        {
            // 2. 원본 아이콘 숨기기
            itemIconImage.enabled = false;

            // 3. 드래그 아이콘 활성화 및 설정
            if (dragIconImage != null)
            {
                dragIconImage.sprite = iconSprite; // 아이콘 설정
                dragIconImage.color = new Color(1f, 1f, 1f, 0.8f); // 투명도 약간 조절
                dragIconImage.gameObject.SetActive(true);
            }

            Debug.Log($"[UI Handler] Drag Started from Index: {slotIndex}, Item: {itemName}");
        }
        else
        {
            // 아이콘을 찾을 수 없거나 데이터가 유효하지 않으면 드래그 취소
            isDragging = false;
            Debug.LogWarning($"[UI Handler] Drag Failed: Icon Image is missing for slot {slotIndex}");
        }
    }

    // ⭐ [CLEANUP] 임시 헬퍼 함수 제거 (InventoryManager로 위임됨)
    // private Sprite GetTemporaryIconSprite(InventorySlotData data) { ... }

    // --------------------------------------------------------------------------------
    // [2. 드래그 중: IDragHandler]
    // --------------------------------------------------------------------------------
    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging && dragIconImage != null)
        {
            // 드래그 아이콘을 마우스(VR Ray) 위치로 따라가도록 설정
            dragIconImage.transform.position = eventData.position;
        }
    }

    // --------------------------------------------------------------------------------
    // [3. 드래그 종료: IEndDragHandler]
    // --------------------------------------------------------------------------------
    public void OnEndDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            isDragging = false;

            // 드래그 아이콘 비활성화
            if (dragIconImage != null)
            {
                dragIconImage.gameObject.SetActive(false);
            }

            // 원본 아이콘 다시 표시 (UI 갱신은 InventoryManager가 완료 후 명령할 것이므로 안전하게 켜줍니다)
            if (itemIconImage != null)
            {
                itemIconImage.enabled = true;
            }

            Debug.Log($"[UI Handler] Drag Ended from Index: {slotIndex}");
        }
    }

    // --------------------------------------------------------------------------------
    // [4. 드롭: IDropHandler]
    // --------------------------------------------------------------------------------
    public void OnDrop(PointerEventData eventData)
    {
        // 1. 드래그를 시작한 소스 슬롯의 핸들러를 가져옵니다.
        InventorySlotUIHandler sourceHandler = eventData.pointerDrag.GetComponent<InventorySlotUIHandler>();

        if (sourceHandler != null && sourceHandler.slotIndex != slotIndex)
        {
            int sourceIndex = sourceHandler.slotIndex;
            int targetIndex = this.slotIndex;

            // ⭐ [유지] InventoryManager.SwapItems 호출
            if (InventoryManager.Instance != null)
            {
                // Inventory Manager에게 두 슬롯의 아이템 데이터를 교환하도록 요청합니다.
                InventoryManager.Instance.SwapItems(sourceIndex, targetIndex);
                Debug.Log($"[UI Handler] Drop Success: Slot {sourceIndex} -> Slot {targetIndex}");
            }
            else
            {
                Debug.LogError("[UI Handler] Drop Failed: InventoryManager.Instance is null.");
            }
        }
    }

    // --------------------------------------------------------------------------------
    // [5. 포인터 오버: IPointerEnterHandler, IPointerExitHandler] (툴팁 용도)
    // --------------------------------------------------------------------------------
    public void OnPointerEnter(PointerEventData eventData)
    {
        // TODO: 툴팁 활성화 로직 (슬롯 인덱스를 사용하여 아이템 정보 표시)
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // TODO: 툴팁 비활성화 로직
    }
}
