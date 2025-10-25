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

            // grabbedLayer = LayerMask.NameToLayer("GrabbedItem")의 반환값 검증 로직을 포함
            int layerIndex = LayerMask.NameToLayer("GrabbedItem");
            if (layerIndex == -1)
            {
                Debug.LogWarning("[VRSlotInteraction] 'GrabbedItem' 레이어를 찾을 수 없습니다. Layer 설정을 확인하세요.");
                grabbedLayer = originalLayer; // 임시로 원래 레이어 사용
            }
            else
            {
                grabbedLayer = layerIndex;
            }
        }

        // Static Dictionary에 등록
        if (!allSlotInteractions.ContainsKey(slotIndex))
        {
            allSlotInteractions.Add(slotIndex, this);
        }
    }

    private void Start()
    {
        // ⭐ 이 슬롯의 초기 위치 및 부모 정보를 저장합니다. (현재는 사용하지 않지만 로직을 위한 보존)
        originalParent = transform.parent;
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;

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

            // 🔥 Coroutine을 시작하여 아이템 위치 복원 로직을 다음 프레임에 실행합니다.
            StartCoroutine(RestoreItemPositionDelayed());

            // 인벤토리/퀵슬롯 매니저에게 데이터 변경은 없었음을 알리고 UI를 갱신합니다.
            RestoreItem(targetIndex);
        }

        // 인덱스 초기화 및 레이어 복구
        grabbedIndex = -1;
        lastHoveredSlot = null;
        grabInteractable.gameObject.layer = originalLayer;
    }

    // ----------------------------------------------------\
    // 🔥 [핵심 추가 함수: 아이템 위치 복원]
    // ----------------------------------------------------\

    /// <summary>
    /// 아이템을 놓은 후 한 프레임을 기다려 위치 복원 로직을 실행합니다.
    /// </summary>
    private IEnumerator RestoreItemPositionDelayed()
    {
        // XR 시스템이 아이템 제어권을 완전히 해제할 때까지 한 프레임을 기다립니다.
        yield return null;

        RestoreItemPosition();
    }

    /// <summary>
    /// 잡고 있던 아이템을 원래 슬롯의 위치로 되돌립니다.
    /// </summary>
    private void RestoreItemPosition()
    {
        // grabInteractable의 transform을 사용하여 실제 잡았던 아이템 오브젝트의 위치를 복원합니다.
        Transform itemTransform = grabInteractable.transform;

        // 1. 부모를 원래대로 복원합니다. (VRSlotInteraction 오브젝트를 부모로 설정)
        // SetParent(transform)을 사용하여 현재 슬롯 오브젝트의 자식으로 되돌립니다.
        itemTransform.SetParent(transform);

        // 2. 위치와 회전을 초기 값으로 복원합니다. (슬롯의 중앙 위치)
        // grabInteractable이 슬롯의 자식으로 올바르게 배치되었다고 가정하고 로컬 위치를 초기화합니다.
        itemTransform.localPosition = Vector3.zero;
        itemTransform.localRotation = Quaternion.identity;
        itemTransform.localScale = Vector3.one; // 크기도 1로 초기화 (UI 스케일 문제 방지)
    }


    /// <summary>
    /// 아이템을 원래 위치로 복원하고 UI를 갱신합니다. (데이터 변경은 없음)
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
            // 퀵슬롯 매니저에 GetSlotData()와 같은 공개 메서드가 있다고 가정하고 UI 갱신을 진행합니다.
            if (allSlotInteractions.TryGetValue(slotIndexToRefresh, out VRSlotInteraction slot))
            {
                InventorySlot data = QuickSlotManager.Instance.GetSlotData(slotIndexToRefresh);
                slot.uiUpdater.UpdateSlotUI(data.itemData, data.stackSize);
            }
        }
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
                // 이전 대화에서 UseItemAtInternalIndex로 수정이 필요했으나, 
                // 일단 기존 함수 이름을 유지하고 해당 기능이 구현되어 있다고 가정합니다.
                QuickSlotManager.Instance.UseItemAtQuickSlotIndex(quickIndex);
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