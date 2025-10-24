using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Concat을 사용하기 위해 필요

public class VRSlotInteraction : MonoBehaviour
{
    // --- [필드] ---
    [Header("Slot Data")]
    [SerializeField] private int slotIndex = -1;

    private Image slotImage;
    // ⭐ 변경: CustomSlotGrabInteractable로 타입 변경
    private CustomSlotGrabInteractable grabInteractable;
    private Collider slotCollider;
    private SlotUIUpdater uiUpdater;

    // Layer Switching 필드
    private int originalLayer;
    private int grabbedLayer;

    [Header("Highlight Settings")]
    [SerializeField] private Image highlightImage;
    [SerializeField] private Color defaultHighlightColor = Color.clear;
    [SerializeField] private Color hoverHighlightColor = Color.yellow;
    [SerializeField] private Color defaultHoverColor = Color.cyan;

    // Grabbed Index (출발지)
    private static int grabbedIndex = -1;

    // Hover 기반 스왑 타겟으로 사용 (XR 이벤트에만 의존)
    private static VRSlotInteraction lastHoveredSlot = null;

    // [위치 복원용 필드]
    private Transform originalParent;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;

    // Static Dictionary: 모든 슬롯 인스턴스를 추적
    private static Dictionary<int, VRSlotInteraction> allSlotInteractions = new Dictionary<int, VRSlotInteraction>();


    // ----------------------------------------------------\
    // [Awake & Start]
    // ----------------------------------------------------\
    private void Awake()
    {
        // 컴포넌트 가져오기 및 초기화
        slotImage = GetComponent<Image>();
        slotCollider = GetComponent<Collider>();
        uiUpdater = GetComponent<SlotUIUpdater>();

        // ⭐ CustomSlotGrabInteractable 가져오기 (타입 변경 반영)
        grabInteractable = GetComponentInChildren<CustomSlotGrabInteractable>();

        if (grabInteractable != null)
        {
            // 인덱스 연결
            grabInteractable.slotIndex = this.slotIndex;

            // 이벤트 연결
            grabInteractable.selectEntered.AddListener(OnSelectStart);
            grabInteractable.selectExited.AddListener(OnSelectEndWithDelay);
            grabInteractable.hoverEntered.AddListener(OnHoverStart);
            grabInteractable.hoverExited.AddListener(OnHoverEnd);
            grabInteractable.activated.AddListener(OnActivatedForUse);

            // 위치 보정용 이벤트 연결 (SelectEntered가 부모 재설정 전에 호출되도록)
            grabInteractable.selectEntered.AddListener(OnSelectStartedOverrideParenting);

            // 원본 레이어 저장 (Grab 시 레이어 변경을 위해)
            originalLayer = grabInteractable.gameObject.layer;
            grabbedLayer = LayerMask.NameToLayer("GrabbedItem"); // "GrabbedItem" 레이어가 정의되어 있다고 가정
        }

        // Static Dictionary에 등록
        if (!allSlotInteractions.ContainsKey(slotIndex))
        {
            allSlotInteractions.Add(slotIndex, this);
        }
    }

    private void Start()
    {
        if (slotImage != null)
        {
            originalParent = transform.parent;
            initialLocalPosition = transform.localPosition;
            initialLocalRotation = transform.localRotation;
        }

        if (highlightImage != null)
        {
            highlightImage.color = defaultHighlightColor;
        }
    }

    // ----------------------------------------------------\
    // [Grab/Drop 로직]
    // ----------------------------------------------------\

    // ⭐ Select Entered 시점에 부모 관계를 재정의하여 아이템이 핸드 트래킹을 따르도록 합니다.
    private void OnSelectStartedOverrideParenting(SelectEnterEventArgs args)
    {
        if (grabInteractable.transform.parent != null)
        {
            // 부모 관계를 일시적으로 Interactor의 Hand Attachment로 변경
            // (Grab Interactable의 Re-parenting 로직에 의해 자동으로 처리될 수 있지만, 명시적으로 추가)
        }

        // 아이템의 레이어를 변경하여 Raycast 충돌을 방지합니다.
        grabInteractable.gameObject.layer = grabbedLayer;
    }

    private void OnSelectStart(SelectEnterEventArgs args)
    {
        // 이 슬롯이 출발지임을 기록
        grabbedIndex = this.slotIndex;
        // 출발지 슬롯의 하이라이트를 끕니다.
        ClearHighlightVisual();

        // 출발 슬롯 UI를 시각적으로 비워주기
        uiUpdater.UpdateSlotUI(null, 0);
    }

    private void OnSelectEndWithDelay(SelectExitEventArgs args)
    {
        // Select Exited는 Grab을 놓았을 때 호출됩니다.
        // 드롭된 위치나 마지막 Hover 슬롯을 확인합니다.

        int targetIndex = -1;

        if (lastHoveredSlot != null && lastHoveredSlot.slotIndex != grabbedIndex)
        {
            // 1. 다른 슬롯에 Hover 후 드롭한 경우 (스왑)
            targetIndex = lastHoveredSlot.slotIndex;

            // 스왑 로직 요청
            CallSwapManager(grabbedIndex, targetIndex);
        }
        else
        {
            // 2. 빈 공간 또는 출발지 슬롯에 드롭한 경우 (원래 위치로 복원)
            targetIndex = grabbedIndex;

            // 아이템을 원래 위치로 복원하는 로직이 필요합니다.
            // 인벤토리/퀵슬롯 매니저에게 복원 요청은 필요 없지만, UI를 갱신해야 합니다.
            RestoreItem(targetIndex);
        }

        // 인덱스 초기화 및 레이어 복구
        grabbedIndex = -1;
        lastHoveredSlot = null;
        grabInteractable.gameObject.layer = originalLayer;

        // 아이템의 위치를 원래대로 복원 (Re-parenting 로직에 의해 자동 처리될 수 있으나, 안전장치)
        // grabInteractable.transform.SetParent(originalParent); 
    }

    /// <summary>
    /// 아이템을 원래 위치로 복원하고 UI를 갱신합니다.
    /// </summary>
    private void RestoreItem(int slotIndexToRefresh)
    {
        // 인벤토리/퀵슬롯 매니저에서 해당 인덱스의 아이템 데이터를 다시 가져와 UI를 갱신하도록 요청합니다.
        if (Inventory.Instance != null && slotIndexToRefresh < Inventory.Instance.Capacity)
        {
            Inventory.Instance.RefreshSlotUI(slotIndexToRefresh);
        }
        else if (QuickSlotManager.Instance != null)
        {
            int quickIndex = QuickSlotManager.Instance.GetQuickSlotInternalIndex(slotIndexToRefresh);
            // QuickSlotManager에는 RefreshSlotUI 퍼블릭 메서드가 없으므로, 
            // 퀵슬롯 매니저의 내부 로직을 통해 UI를 갱신해야 합니다.
            // QuickSlotManager.UpdateQuickSlotUI(quickIndex); // private 이므로 직접 호출 불가

            // Inventory.RefreshSlotUI와 유사하게 QuickSlotManager에 퍼블릭 메서드를 추가해야 합니다.
            // 임시로, QuickSlotManager에서 UI가 갱신된다고 가정합니다.
            // 또는, 퀵슬롯 UI를 직접 갱신합니다 (권장되지 않음).
            if (allSlotInteractions.TryGetValue(slotIndexToRefresh, out VRSlotInteraction slot))
            {
                InventorySlot data = QuickSlotManager.Instance.GetSlotData(slotIndexToRefresh);
                slot.uiUpdater.UpdateSlotUI(data.itemData, data.stackSize);
            }
        }

        // 아이템 오브젝트를 원래의 위치로 되돌립니다.
        // grabInteractable.transform.localPosition = initialLocalPosition;
        // grabInteractable.transform.localRotation = initialLocalRotation;
    }

    /// <summary>
    /// 인벤토리 또는 퀵슬롯 매니저에게 스왑을 요청합니다.
    /// </summary>
    private void CallSwapManager(int fromIndex, int toIndex)
    {
        // 퀵슬롯 범위 확인
        if (QuickSlotManager.Instance != null)
        {
            // QuickSlotManager는 인벤토리/퀵슬롯 간의 모든 스왑을 처리할 수 있으므로,
            // QuickSlotManager를 통해 스왑 로직을 일원화합니다.
            QuickSlotManager.Instance.GlobalSwapItems(fromIndex, toIndex);
        }
        else
        {
            Debug.LogError("[VRSlotInteraction] QuickSlotManager가 없어 스왑 로직을 실행할 수 없습니다.");
            // 인벤토리 매니저만 있다면 인벤토리 내 스왑만 처리
            // if (Inventory.Instance != null && fromIndex < Inventory.Instance.Capacity && toIndex < Inventory.Instance.Capacity)
            // {
            //     Inventory.Instance.SwapItems(fromIndex, toIndex); // Inventory에 SwapItems가 없다면 오류 발생
            // }
        }
    }

    // ----------------------------------------------------\
    // [Hover/Highlight 로직]
    // ----------------------------------------------------\

    private void OnHoverStart(HoverEnterEventArgs args)
    {
        // 출발지가 지정되었고, 현재 슬롯이 출발지가 아닐 경우
        if (grabbedIndex != -1 && this.slotIndex != grabbedIndex)
        {
            // 타겟 하이라이트 표시
            SetHighlightVisual(hoverHighlightColor);
            lastHoveredSlot = this;
        }
        // 출발지가 없거나 현재 슬롯이 출발지인 경우
        else
        {
            SetHighlightVisual(defaultHoverColor);
        }
    }

    private void OnHoverEnd(HoverExitEventArgs args)
    {
        // 하이라이트 제거
        ClearHighlightVisual();
        if (lastHoveredSlot == this)
        {
            lastHoveredSlot = null;
        }
    }

    private void SetHighlightVisual(Color color)
    {
        if (highlightImage != null)
        {
            highlightImage.color = color;
        }
    }

    private void ClearHighlightVisual()
    {
        if (highlightImage != null)
        {
            highlightImage.color = defaultHighlightColor;
        }
    }

    // ----------------------------------------------------\
    // [아이템 사용 로직]
    // ----------------------------------------------------\

    // VR Interactor의 Activate 버튼 입력 시 호출됩니다.
    public void OnActivatedForUse(ActivateEventArgs args)
    {
        // 1. 사용 시도 전, 빈 슬롯 여부 확인 (인벤토리 데이터는 Inventory.cs에서 관리)
        if (Inventory.Instance == null || Inventory.Instance.IsSlotEmpty(this.slotIndex))
        {
            // 퀵슬롯 인덱스도 확인 (QuickSlotManager에서 처리되지만, 안전장치)
            if (QuickSlotManager.Instance == null) return;

            int quickIndex = QuickSlotManager.Instance.GetQuickSlotInternalIndex(this.slotIndex);
            if (quickIndex != -1 && QuickSlotManager.Instance.quickSlots[quickIndex].IsEmpty)
            {
                return;
            }
        }

        // 2. 인벤토리 슬롯인 경우 아이템 사용 요청
        if (Inventory.Instance != null && this.slotIndex < Inventory.Instance.Capacity)
        {
            // 인벤토리 매니저에게 아이템 사용 요청
            Inventory.Instance.UseItem(this.slotIndex);
        }
        // 3. 퀵슬롯 슬롯인 경우 아이템 사용 요청
        else if (QuickSlotManager.Instance != null)
        {
            int quickIndex = QuickSlotManager.Instance.GetQuickSlotInternalIndex(this.slotIndex);
            if (quickIndex != -1)
            {
                QuickSlotManager.Instance.UseItemAtQuickSlotIndex(quickIndex); // 🔥 QuickSlotManager 함수 호출
            }
        }
    }

    // ----------------------------------------------------\
    // [Clean Up]
    // ----------------------------------------------------\

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            // 모든 리스너 제거
            grabInteractable.selectEntered.RemoveListener(OnSelectStart);
            grabInteractable.selectExited.RemoveListener(OnSelectEndWithDelay);
            grabInteractable.hoverEntered.RemoveListener(OnHoverStart);
            grabInteractable.hoverExited.RemoveListener(OnHoverEnd);
            grabInteractable.activated.RemoveListener(OnActivatedForUse);
            grabInteractable.selectEntered.RemoveListener(OnSelectStartedOverrideParenting);
        }

        // Static Dictionary에서 제거
        if (allSlotInteractions.ContainsKey(slotIndex))
        {
            allSlotInteractions.Remove(slotIndex);
        }
    }
}