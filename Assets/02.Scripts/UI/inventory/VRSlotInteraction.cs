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
    private XRGrabInteractable grabInteractable;
    private Collider slotCollider;
    private SlotUIUpdater uiUpdater;

    // Layer Switching 필드
    private int originalLayer;
    private int grabbedLayer;

    [Header("Highlight Settings")]
    [SerializeField] private Image highlightImage;
    [SerializeField] private Color defaultHighlightColor = Color.clear;
    [SerializeField] private Color hoverHighlightColor = Color.yellow; // 플래시 색상으로 사용
    [SerializeField] private Color defaultHoverColor = Color.cyan; // 일반 포인팅 시 Hover 색상

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
        grabInteractable = GetComponentInChildren<XRGrabInteractable>();
        slotCollider = GetComponent<Collider>();
        uiUpdater = GetComponent<SlotUIUpdater>();

        if (slotImage == null || grabInteractable == null || slotCollider == null || uiUpdater == null)
        {
            Debug.LogError($"VRSlotInteraction Error: 필수 컴포넌트를 찾을 수 없습니다. (오브젝트 이름: {gameObject.name})");
            return;
        }

        // 2. 부모/위치/회전 저장
        originalParent = transform.parent;
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;

        // 3. XRIT Layer Mask 저장
        if (grabInteractable != null)
        {
            originalInteractionLayers = grabInteractable.interactionLayers;
        }

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

    // ⭐ [추가] 모든 슬롯의 XRInteractable 상태를 강제로 활성화합니다.
    public static void RefreshAllInteractables()
    {
        foreach (var slot in allSlotInteractions.Values)
        {
            if (slot != null && slot.grabInteractable != null)
            {
                // 이미 RestoreSlotVisualAndPhysics 내부에 포함되어 있지만, 
                // 전체 복원을 위해 안전하게 Restore 함수를 호출합니다.
                slot.RestoreSlotVisualAndPhysics();
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
            // Grab 중 Hover (Swap Target): 비주얼 없이 타겟 설정 로직만 유지

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

    // 일반 Hover 시 하이라이트 적용 함수
    public void ApplyDefaultHoverHighlight()
    {
        if (highlightImage != null)
        {
            Color targetColor = defaultHoverColor;
            targetColor.a = 1.0f;
            highlightImage.color = targetColor;
        }
    }

    // 드롭 플래시 하이라이트 적용 함수 (Post-drop Flash에서 사용)
    public void ApplyFlashHighlight()
    {
        if (highlightImage != null)
        {
            Color targetColor = hoverHighlightColor;
            targetColor.a = 1.0f;
            highlightImage.color = targetColor;
        }
        // 플래시 시작 시 슬롯 이미지의 투명도도 복원 (Grab 시 0.5로 설정되었으므로)
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
        // 슬롯이 비어있으면 Grab 시도 무시
        if (Inventory.Instance != null && Inventory.Instance.IsSlotEmpty(this.slotIndex))
        {
            if (grabInteractable != null)
            {
                // XR Interactor의 상태를 재설정하여 빈 슬롯을 잡는 것을 방지
                grabInteractable.enabled = false;
                grabInteractable.enabled = true;
            }
            return;
        }

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
            Inventory.Instance.SwapSlots(this.slotIndex, targetIndex);

            // 스왑 성공 시 플래시 대상을 타겟 슬롯으로 변경
            flashTarget = targetSlotInstance;

            // ⭐ [핵심 수정] 타겟 슬롯의 물리/상호작용 상태를 강제로 복원합니다.
            targetSlotInstance.RestoreSlotVisualAndPhysics();
        }

        // Grab 상태 초기화 및 타겟 해제
        lastHoveredSlot = null;
        grabbedIndex = -1;

        // 1단계: 잡았던 슬롯의 위치/레이어/투명도 등 물리적 상태를 즉시 복구 (Visual Snap-Back)
        RestoreSlotVisualAndPhysics();

        // 2단계: 물리적 복구가 렌더링에 반영되어 아이템이 제자리를 찾을 때까지 한 프레임 더 대기
        yield return null;

        // ⭐ [추가] Inventory.Instance.SwapSlots() 호출로 인해 혹시라도 비활성화된 
        // 다른 퀵슬롯들을 포함, 모든 슬롯의 Interactable을 강제로 활성화합니다.
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

        // ⭐ 버그 수정: 아이템 제거 후 XRGrabInteractable이 비활성화되는 현상을 방지
        // (이 함수가 호출되는 모든 경우에 대해 Interactable을 강제로 활성화합니다.)
        if (grabInteractable != null)
        {
            grabInteractable.enabled = true;
        }

        ClearHighlightVisual(); // 안전장치로 하이라이트 잔상 제거
    }

    public void OnActivatedForUse(ActivateEventArgs args)
    {
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
        }

        // 인스턴스 등록 해제
        if (allSlotInteractions.ContainsKey(slotIndex))
        {
            allSlotInteractions.Remove(slotIndex);
        }
    }
}
