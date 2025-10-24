using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

    // [XRIT Layer Mask 저장]
    private InteractionLayerMask originalInteractionLayers;

    void Awake()
    {
        // 1. 컴포넌트 가져오기
        slotImage = GetComponent<Image>();
        // ⭐ 변경: CustomSlotGrabInteractable로 가져옵니다.
        grabInteractable = GetComponentInChildren<CustomSlotGrabInteractable>();
        slotCollider = GetComponent<Collider>();
        uiUpdater = GetComponent<SlotUIUpdater>();

        if (slotImage == null || grabInteractable == null || slotCollider == null || uiUpdater == null)
        {
            Debug.LogError($"VRSlotInteraction Error: 필수 컴포넌트를 찾을 수 없습니다. (오브젝트 이름: {gameObject.name})");
            return;
        }

        // ⭐ [FIX]: CustomSlotGrabInteractable에 인덱스 전달
        if (grabInteractable != null)
        {
            grabInteractable.slotIndex = this.slotIndex;
            // grabInteractable.selectEnterChecking = (interactor) => CanSelectOverride(interactor); (오류 코드 제거)
        }

        // 2. 부모/위치/회전 저장
        originalParent = transform.parent;
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;

        // 3. XRIT Layer Mask 저장
        originalInteractionLayers = grabInteractable.interactionLayers;

        // 4. 레이어 ID 저장 및 원래 레이어 저장
        originalLayer = gameObject.layer;
        grabbedLayer = LayerMask.NameToLayer("Grabbed");

        if (grabbedLayer == -1)
        {
            Debug.LogError("Error: 'Grabbed' Layer not found! Please create it in Unity Layers.");
        }

        // 5. XRGrabInteractable 이벤트 연결
        grabInteractable.selectEntered.AddListener(OnSelectStartedOverrideParenting);
        grabInteractable.selectEntered.AddListener(OnSelectStart);
        grabInteractable.selectExited.AddListener(OnSelectEndWithDelay);
        grabInteractable.hoverEntered.AddListener(OnHoverStart);
        grabInteractable.hoverExited.AddListener(OnHoverEnd);
        grabInteractable.activated.AddListener(OnActivatedForUse);

        // 하이라이트 초기화
        ClearHighlightVisual();
        lastHoveredSlot = null;

        // 6. 인스턴스 등록
        if (slotIndex != -1)
        {
            if (allSlotInteractions.ContainsKey(slotIndex))
            {
                allSlotInteractions[slotIndex] = this;
            }
            else
            {
                allSlotInteractions.Add(slotIndex, this);
            }
        }

        // 7. 콜라이더 초기 설정
        if (slotCollider != null)
        {
            slotCollider.isTrigger = false;
        }
    }

    /// <summary>
    /// ⭐ [제거] 이 함수는 CustomSlotGrabInteractable.IsSelectableBy로 대체되었습니다.
    /// </summary>
    /*
    private bool CanSelectOverride(IXRSelectInteractor interactor)
    {
        if (Inventory.Instance != null && Inventory.Instance.IsSlotEmpty(this.slotIndex))
        {
            return false;
        }
        return true;
    }
    */


    // [재귀 함수] 오브젝트와 모든 자식의 Layer를 변경합니다.
    private static void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;

        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    /// <summary>
    /// ⭐ [Optimized] 아이템 제거 후 XRGrabInteractable이 비활성화되는 버그 방지를 위해 모든 슬롯을 강제 활성화합니다.
    /// </summary>
    public static void RefreshAllInteractables()
    {
        foreach (var slot in allSlotInteractions.Values)
        {
            if (slot != null && slot.grabInteractable != null)
            {
                slot.grabInteractable.enabled = true;
                slot.ClearHighlightVisual(); // 잔상 제거 안전장치
            }
        }
    }

    // --- (Highlight/Hover 로직) ---
    public void OnHoverStart(HoverEnterEventArgs args)
    {
        if (grabbedIndex == -1)
        {
            // 일반 Hover (Grab 전): 하이라이트 켜기
            ApplyDefaultHoverHighlight();
        }
        else if (this.slotIndex != grabbedIndex)
        {
            // Grab 중 Hover (Swap Target): 타겟 설정 로직만 유지

            if (lastHoveredSlot != null && lastHoveredSlot != this)
            {
                lastHoveredSlot.ClearHighlightVisual();
            }
            lastHoveredSlot = this;
        }
    }

    public void OnHoverEnd(HoverExitEventArgs args)
    {
        if (grabbedIndex == -1)
        {
            // 일반 Hover (Grab 전): 하이라이트 끄기
            ClearHighlightVisual();
        }
        else if (lastHoveredSlot == this)
        {
            // Grab 중 Hover 종료: 타겟 해제
            lastHoveredSlot = null;
        }
    }

    public void ApplyDefaultHoverHighlight()
    {
        if (highlightImage != null)
        {
            Color targetColor = defaultHoverColor;
            targetColor.a = 1.0f;
            highlightImage.color = targetColor;
        }
    }

    public void ApplyFlashHighlight()
    {
        if (highlightImage != null)
        {
            Color targetColor = hoverHighlightColor;
            targetColor.a = 1.0f;
            highlightImage.color = targetColor;
        }
        // 플래시 시작 시 슬롯 이미지의 투명도 복원 (Grab 시 0.5로 설정되었으므로)
        if (slotImage != null)
        {
            Color color = slotImage.color;
            color.a = 1.0f;
            slotImage.color = color;
        }
    }

    public void ClearHighlightVisual()
    {
        if (highlightImage != null)
        {
            Color color = defaultHighlightColor;
            color.a = 0.0f;
            highlightImage.color = color;
        }
    }

    // 드롭 시 0.1초 동안 하이라이트 플래시
    private IEnumerator FlashHighlight(float duration = 0.1f)
    {
        ApplyFlashHighlight(); // 하이라이트 켜고 슬롯 투명도 복원
        yield return new WaitForSeconds(duration);
        ClearHighlightVisual(); // 하이라이트 끄기
    }

    // --- (Select/Grab 로직) ---
    public void OnSelectStart(SelectEnterEventArgs args)
    {
        // ⭐ [CLEANUP] 빈 슬롯 확인 로직은 이제 CustomSlotGrabInteractable에서 처리됩니다.

        grabbedIndex = this.slotIndex;

        // 재귀 함수를 사용하여 잡은 슬롯과 모든 자식의 Layer를 Grabbed로 변경
        if (grabbedLayer != -1)
        {
            SetLayerRecursively(gameObject, grabbedLayer);
        }

        // 시각적 피드백 (반투명화)
        if (slotImage != null)
        {
            Color color = slotImage.color;
            color.a = 0.5f;
            slotImage.color = color;
        }
    }

    private void OnSelectStartedOverrideParenting(SelectEnterEventArgs args)
    {
        if (Inventory.Instance != null && Inventory.Instance.IsSlotEmpty(this.slotIndex)) return;

        if (transform.parent != originalParent)
        {
            transform.SetParent(originalParent);
            transform.localPosition = initialLocalPosition;
            transform.localRotation = initialLocalRotation;
        }
    }

    public void OnSelectEndWithDelay(SelectExitEventArgs args)
    {
        if (grabbedIndex != this.slotIndex)
        {
            // 스왑 대상이었던 경우라도, 잡았던 슬롯의 상태는 복원되어야 합니다.
            RestoreSlotVisualAndPhysics();
            return;
        }

        if (grabInteractable != null)
        {
            grabInteractable.interactionLayers = originalInteractionLayers;
        }

        StartCoroutine(HandleSelectEndDelayed());
    }

    private IEnumerator HandleSelectEndDelayed()
    {
        yield return null; // 1. Interactor의 제어권 해제를 위한 첫 프레임 대기

        int targetIndex = -1;
        VRSlotInteraction targetSlotInstance = lastHoveredSlot;
        VRSlotInteraction flashTarget = this; // 기본 플래시 대상은 시작 슬롯 (제자리 드롭)

        // 스왑 타겟 확인 
        if (targetSlotInstance != null && targetSlotInstance.slotIndex != this.slotIndex)
        {
            targetIndex = targetSlotInstance.slotIndex;
        }

        // Swap 실행
        if (Inventory.Instance != null && targetIndex != -1)
        {
            // Inventory.SwapSlots 호출 (QuickSlotManager 연동 로직 포함)
            Inventory.Instance.SwapSlots(this.slotIndex, targetIndex);

            // 스왑 성공 시 플래시 대상을 타겟 슬롯으로 변경
            flashTarget = targetSlotInstance;

            // ⭐ [CRITICAL FIX] 타겟 슬롯의 물리/상호작용 상태를 강제로 복원합니다.
            targetSlotInstance.RestoreSlotVisualAndPhysics();
        }

        // Grab 상태 초기화 및 타겟 해제
        lastHoveredSlot = null;
        grabbedIndex = -1;

        // 1단계: 잡았던 슬롯의 위치/레이어/투명도 등 물리적 상태를 즉시 복구 (Visual Snap-Back)
        RestoreSlotVisualAndPhysics();

        // 2단계: 물리적 복구가 렌더링에 반영되어 아이템이 제자리를 찾을 때까지 한 프레임 더 대기
        yield return null;

        // ⭐ [Optimized] Interactable 비활성화 버그 방지를 위해 전체 슬롯을 강제로 활성화
        RefreshAllInteractables();

        // 3단계: 최종 목적지 슬롯에서 하이라이트 플래시를 시작하고 완료될 때까지 기다림
        yield return StartCoroutine(flashTarget.FlashHighlight(0.1f));
    }

    private void RestoreSlotVisualAndPhysics()
    {
        // 시각적 피드백 복구
        if (slotImage != null)
        {
            Color color = slotImage.color;
            color.a = 1.0f;
            slotImage.color = color;
        }

        // 위치/회전 강제 복원 (Visual Snap)
        if (transform.parent != originalParent)
        {
            transform.SetParent(originalParent);
        }
        transform.localPosition = initialLocalPosition;
        transform.localRotation = initialLocalRotation;

        // Layer 원상 복구 (재귀적으로)
        SetLayerRecursively(gameObject, originalLayer);

        // Collider 활성화
        if (slotCollider != null)
        {
            slotCollider.enabled = true;
            slotCollider.isTrigger = false;
        }

        // ⭐ [FIX] 아이템 제거 후 XRGrabInteractable이 비활성화되는 현상을 방지
        if (grabInteractable != null)
        {
            grabInteractable.enabled = true;
        }

        ClearHighlightVisual(); // 안전장치로 하이라이트 잔상 제거
    }

    public void OnActivatedForUse(ActivateEventArgs args)
    {
        // 사용 시도 전, 빈 슬롯 여부 확인 (이중 확인)
        if (Inventory.Instance != null && Inventory.Instance.IsSlotEmpty(this.slotIndex))
        {
            return;
        }

        if (Inventory.Instance != null)
        {
            Inventory.Instance.UseItem(this.slotIndex);
        }
    }

    private int GetTargetSlotIndex(Vector3 dropPosition)
    {
        // 현재 Hover 기반 스왑 로직을 사용하므로 이 함수는 사용되지 않습니다.
        return -1;
    }

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

            // ⭐ [CLEANUP] selectEnterChecking 대신 사용된 CustomGrabInteractable의 참조는 자동으로 GC됩니다.
        }

        // 인스턴스 등록 해제
        if (allSlotInteractions.ContainsKey(slotIndex))
        {
            allSlotInteractions.Remove(slotIndex);
        }
    }
}