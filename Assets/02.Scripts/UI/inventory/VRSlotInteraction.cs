using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections.Generic;

public class VRSlotInteraction : MonoBehaviour
{
    // --- [필드] ---
    [Header("Slot Data")]
    [SerializeField] private int slotIndex = -1;

    private Image slotImage;
    private XRGrabInteractable grabInteractable;
    private Collider slotCollider;
    private SlotUIUpdater uiUpdater;

    [Header("Highlight Settings")]
    [SerializeField] private Image highlightImage;
    [SerializeField] private Color defaultHighlightColor = Color.clear;
    [SerializeField] private Color hoverHighlightColor = Color.yellow;

    private static int grabbedIndex = -1;

    // [위치 복원용 필드]
    private Transform originalParent;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;

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

        // Parent 변경 복원 리스너 추가 (XR Interaction Toolkit의 부모 변경 방지)
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnSelectStartedOverrideParenting);
        }

        // 2. 부모/위치/회전 저장
        originalParent = transform.parent;
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;

        // 3. XRGrabInteractable 이벤트 연결
        grabInteractable.selectEntered.AddListener(OnSelectStart);
        grabInteractable.selectExited.AddListener(OnSelectEnd);
        grabInteractable.hoverEntered.AddListener(OnHoverStart);
        grabInteractable.hoverExited.AddListener(OnHoverEnd);
        grabInteractable.activated.AddListener(OnActivatedForUse); // Activate (사용) 리스너

        // 하이라이트 초기화
        if (highlightImage != null)
        {
            highlightImage.color = defaultHighlightColor;
        }
    }

    // --- (OnHoverStart, OnHoverEnd 함수 유지) ---
    public void OnHoverStart(HoverEnterEventArgs args)
    {
        if (grabbedIndex != -1 && highlightImage != null)
        {
            highlightImage.color = hoverHighlightColor;
        }
    }

    public void OnHoverEnd(HoverExitEventArgs args)
    {
        if (highlightImage != null)
        {
            highlightImage.color = defaultHighlightColor;
        }
    }
    // ---

    // Grab 시작 시 호출 (빈 슬롯 방지 로직 - 이전 버전의 강제 취소 로직 유지)
    public void OnSelectStart(SelectEnterEventArgs args)
    {
        if (Inventory.Instance != null && Inventory.Instance.IsSlotEmpty(this.slotIndex))
        {
            Debug.LogWarning($"[GRAB ABORTED] Slot {slotIndex} is empty. Cannot grab.");

            // Grab 강제 취소 (이전 로직 유지)
            if (grabInteractable != null)
            {
                grabInteractable.enabled = false;
                grabInteractable.enabled = true;
            }
            return;
        }

        Debug.Log($"[INPUT SUCCESS] Select Start: Slot {slotIndex} 잡기 시작");

        grabbedIndex = this.slotIndex;

        // Grab 시 콜라이더 비활성화
        if (slotCollider != null)
        {
            slotCollider.enabled = false;
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
        // 빈 슬롯이면 Parent Override 복원 시도도 건너뜁니다.
        if (Inventory.Instance != null && Inventory.Instance.IsSlotEmpty(this.slotIndex)) return;

        if (transform.parent != originalParent)
        {
            transform.SetParent(originalParent);
            transform.localPosition = initialLocalPosition;
            transform.localRotation = initialLocalRotation;

            Debug.LogWarning("[PARENT OVERRIDE] Parent was changed by XRIT, forcing restoration to original parent.");
        }
    }

    // OnSelectEnd (Swap 로직)
    public void OnSelectEnd(SelectExitEventArgs args)
    {
        // Grab이 실제로 성공하지 않은 경우 (빈 슬롯 Grab 취소 등)는 무시
        if (grabbedIndex != this.slotIndex)
        {
            Debug.LogWarning($"[SELECT END ABORTED] Slot {slotIndex} was not the successfully grabbed slot ({grabbedIndex}). Skipping swap logic.");
            RestoreSlotVisualAndPhysics();
            return;
        }

        Debug.Log($"[INPUT SUCCESS] Select End: Slot {slotIndex} 드롭됨");

        // 1. Swap Target 찾기 로직
        int targetIndex = -1;

        // V------------------ [최종 수정: GetValidTargets 기반으로 안정화된 로직] ------------------V
        if (args.interactorObject is IXRInteractor interactor)
        {
            // Interactor가 현재 Hover하고 있는 모든 유효한 Interactable 대상을 가져옵니다.
            List<IXRInteractable> validTargets = new List<IXRInteractable>();
            interactor.GetValidTargets(validTargets);

            // 유효 대상 목록에서 현재 잡고 있는 슬롯 자신(this.grabInteractable)을 제외하고 첫 번째 대상을 찾습니다.
            IXRInteractable hoverTarget = validTargets
                .Where(interactable => interactable.transform != this.grabInteractable.transform)
                .FirstOrDefault();

            if (hoverTarget != null)
            {
                // Hover된 오브젝트의 부모에서 VRSlotInteraction을 찾습니다. (UI 슬롯의 구조)
                VRSlotInteraction targetSlotInteraction = hoverTarget.transform.GetComponentInParent<VRSlotInteraction>();

                if (targetSlotInteraction != null)
                {
                    targetIndex = targetSlotInteraction.slotIndex;
                    Debug.Log($"[SWAP TARGET FOUND] Target Slot Index: {targetIndex}");
                }
            }
        }
        // A---------------------------------------------------------------------------------A

        Debug.Log($"[SWAP CHECK] Grabbed Index: {this.slotIndex}, Dropped Index: {targetIndex}");

        // 2. Swap 실행
        if (Inventory.Instance != null && targetIndex != -1 && this.slotIndex != targetIndex)
        {
            Inventory.Instance.SwapSlots(this.slotIndex, targetIndex);
            Debug.Log($"[INVENTORY SWAP SUCCESS] Swapped {this.slotIndex} and {targetIndex}");
        }
        else
        {
            Debug.LogWarning($"[ACTION ABORTED] Grabbed: {this.slotIndex}, Dropped: {targetIndex}. No swap needed or target invalid.");
        }

        // 3. Grab 상태 초기화 및 복원
        grabbedIndex = -1;
        RestoreSlotVisualAndPhysics();
        Debug.Log($"[RESTORE] Slot {slotIndex} Position and Rotation restored.");
    }

    // 재사용을 위해 복원 로직을 별도 함수로 분리했습니다.
    private void RestoreSlotVisualAndPhysics()
    {
        // 시각적 피드백 복구 (알파값 1.0)
        if (slotImage != null)
        {
            Color color = slotImage.color;
            color.a = 1.0f;
            slotImage.color = color;
        }

        // 부모/위치/회전 강제 복원
        if (transform.parent != originalParent)
        {
            transform.SetParent(originalParent);
        }
        transform.localPosition = initialLocalPosition;
        transform.localRotation = initialLocalRotation;

        // 콜라이더 활성화
        if (slotCollider != null)
        {
            slotCollider.enabled = true;
        }
    }

    // 아이템 사용 (Secondary 버튼 또는 Activate 이벤트)
    public void OnActivatedForUse(ActivateEventArgs args)
    {
        if (Inventory.Instance != null && Inventory.Instance.IsSlotEmpty(this.slotIndex))
        {
            Debug.LogWarning($"[USE ABORTED] Slot {slotIndex} is empty. Cannot use.");
            return;
        }

        if (Inventory.Instance != null)
        {
            // Inventory.cs 내부의 UseItem 함수가 장착/소모품 로직을 처리합니다.
            Inventory.Instance.UseItem(this.slotIndex);
            Debug.Log($"[INPUT SUCCESS] Item used in slot {slotIndex} via Secondary Button (Activate).");
        }
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            // 모든 리스너 제거
            grabInteractable.selectEntered.RemoveListener(OnSelectStart);
            grabInteractable.selectExited.RemoveListener(OnSelectEnd);
            grabInteractable.hoverEntered.RemoveListener(OnHoverStart);
            grabInteractable.hoverExited.RemoveListener(OnHoverEnd);
            grabInteractable.activated.RemoveListener(OnActivatedForUse);
            grabInteractable.selectEntered.RemoveListener(OnSelectStartedOverrideParenting);
        }
    }
}