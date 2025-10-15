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
    private SlotUIUpdater uiUpdater; // (이전 단계에서 추가됨)

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
        uiUpdater = GetComponent<SlotUIUpdater>(); // (이전 단계에서 추가됨)

        if (slotImage == null || grabInteractable == null || slotCollider == null || uiUpdater == null)
        {
            Debug.LogError($"VRSlotInteraction Error: 필수 컴포넌트를 찾을 수 없습니다. (오브젝트 이름: {gameObject.name}, uiUpdater: {uiUpdater == null})");
            return;
        }

        // V------------------ [Grab 움직임 활성화 로직 - 유지] ------------------V
        if (grabInteractable != null)
        {
            // Parent 변경이 발생했을 때 즉시 복원하기 위해 추가적인 리스너를 추가 (유지)
            grabInteractable.selectEntered.AddListener(OnSelectStartedOverrideParenting);
        }
        // A-----------------------------------------------------------------------------A

        // 2. 부모/위치/회전 저장
        originalParent = transform.parent;
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;

        // 3. XRGrabInteractable 이벤트 연결 
        grabInteractable.selectEntered.AddListener(OnSelectStart);
        grabInteractable.selectExited.AddListener(OnSelectEnd);
        grabInteractable.hoverEntered.AddListener(OnHoverStart);
        grabInteractable.hoverExited.AddListener(OnHoverEnd);

        // V------------------ [Step 2-1: Activate 리스너 추가] ------------------V
        grabInteractable.activated.AddListener(OnActivatedForUse);
        // A---------------------------------------------------------------------V

        // 하이라이트 초기화
        if (highlightImage != null)
        {
            highlightImage.color = defaultHighlightColor;
        }
    }

    // ... (OnHoverStart, OnHoverEnd 함수 유지)
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
    // ...

    // Grab 시작 시 호출 (빈 슬롯 방지 최종 로직 적용)
    public void OnSelectStart(SelectEnterEventArgs args)
    {
        // V------------------ [빈 슬롯 처리 로직 - 유지] ------------------V
        if (Inventory.Instance != null && Inventory.Instance.IsSlotEmpty(this.slotIndex))
        {
            Debug.LogWarning($"[GRAB ABORTED] Slot {slotIndex} is empty. Cannot grab.");

            // Grab 강제 취소 (이전 로직 유지)
            if (grabInteractable != null)
            {
                grabInteractable.enabled = false;
                grabInteractable.enabled = true;
            }

            return; // 이후의 Grab 로직을 실행하지 않습니다.
        }
        // A-----------------------------------------------------------------A

        // --- (아이템이 있을 경우의 기존 Grab 로직) ---
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

    // V------------------ [Parent Override 함수 - 유지] ------------------V
    private void OnSelectStartedOverrideParenting(SelectEnterEventArgs args)
    {
        if (Inventory.Instance != null && Inventory.Instance.IsSlotEmpty(this.slotIndex)) return;

        if (transform.parent != originalParent)
        {
            transform.SetParent(originalParent);
            transform.localPosition = initialLocalPosition;
            transform.localRotation = initialLocalRotation;

            Debug.LogWarning("[PARENT OVERRIDE] Parent was changed by XRIT, forcing restoration to original parent.");
        }
    }
    // A---------------------------------------------------------------------

    // OnSelectEnd (Step 1-1 수정 적용: Swap 전용)
    public void OnSelectEnd(SelectExitEventArgs args)
    {
        Debug.Log($"[INPUT SUCCESS] Select End: Slot {slotIndex} 드롭됨");

        // 시각적 피드백 복구
        if (slotImage != null)
        {
            Color color = slotImage.color;
            color.a = 1.0f;
            slotImage.color = color;
        }

        // --- [Swap Target 찾기 로직 - 유지] ---
        int droppedIndex = -1;

        if (args.interactorObject is IXRInteractor interactor)
        {
            List<IXRInteractable> validTargets = new List<IXRInteractable>();
            interactor.GetValidTargets(validTargets);

            IXRInteractable hoverTarget = validTargets.FirstOrDefault();

            if (hoverTarget != null)
            {
                VRSlotInteraction droppedSlot = hoverTarget.transform.GetComponentInParent<VRSlotInteraction>();

                if (droppedSlot != null)
                {
                    droppedIndex = droppedSlot.slotIndex;
                }
            }
        }

        Debug.Log($"[SWAP CHECK] Grabbed Index: {grabbedIndex}, Dropped Index: {droppedIndex}");

        // V------------------ [수정: Swap 전용 로직] ------------------V
        if (grabbedIndex != -1 && droppedIndex != -1 && grabbedIndex != droppedIndex)
        {
            // 2. 다른 슬롯에 드롭 = 위치 교환 (Swap)
            if (Inventory.Instance != null)
            {
                Inventory.Instance.SwapSlots(grabbedIndex, droppedIndex);
                Debug.Log($"[INVENTORY SWAP SUCCESS] Swapped {grabbedIndex} and {droppedIndex}");
            }
            else
            {
                Debug.LogError("Inventory.Instance를 찾을 수 없습니다. 싱글톤 초기화 상태를 확인하세요.");
            }
        }
        else
        {
            Debug.LogWarning($"[ACTION ABORTED] Grabbed: {grabbedIndex}, Dropped: {droppedIndex}. No swap needed or target invalid.");
        }
        // A-----------------------------------------------------------------------------A


        // 3. Grab 상태 초기화
        grabbedIndex = -1;

        // 4. 부모/위치/회전 강제 복원 
        if (transform.parent != originalParent)
        {
            transform.SetParent(originalParent);
        }
        transform.localPosition = initialLocalPosition;
        transform.localRotation = initialLocalRotation;

        // 5. Drop 완료 후 콜라이더 활성화
        if (slotCollider != null)
        {
            slotCollider.enabled = true;
        }

        Debug.Log($"[RESTORE] Slot {slotIndex} Position and Rotation restored.");
    }

    // V------------------ [Step 2-2: OnActivatedForUse 함수 추가] ------------------V
    public void OnActivatedForUse(ActivateEventArgs args)
    {
        // 1. 빈 슬롯 체크 (Use 시도 전에 항상 체크)
        if (Inventory.Instance != null && Inventory.Instance.IsSlotEmpty(this.slotIndex))
        {
            Debug.LogWarning($"[USE ABORTED] Slot {slotIndex} is empty. Cannot use.");
            return;
        }

        // 2. 아이템 사용 로직 호출
        if (Inventory.Instance != null)
        {
            // Inventory.cs 내부의 UseItem 함수가 장착/소모품 로직을 처리합니다.
            Inventory.Instance.UseItem(this.slotIndex);
            Debug.Log($"[INPUT SUCCESS] Item used in slot {slotIndex} via Secondary Button (Activate).");
        }
    }
    // A-----------------------------------------------------------------------------A

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            // 리스너 제거 
            grabInteractable.selectEntered.RemoveListener(OnSelectStart);
            grabInteractable.selectExited.RemoveListener(OnSelectEnd);
            grabInteractable.hoverEntered.RemoveListener(OnHoverStart);
            grabInteractable.hoverExited.RemoveListener(OnHoverEnd);
            grabInteractable.activated.RemoveListener(OnActivatedForUse); // 💡 추가된 리스너 제거

            // 새로 추가된 리스너도 제거
            grabInteractable.selectEntered.RemoveListener(OnSelectStartedOverrideParenting);
        }
    }
}