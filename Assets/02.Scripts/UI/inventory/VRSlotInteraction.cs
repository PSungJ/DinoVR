using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

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
    [SerializeField] private Color hoverHighlightColor = Color.yellow; // Grab 중 Hover 색상
    [SerializeField] private Color defaultHoverColor = Color.cyan; // 일반 포인팅 시 Hover 색상

    // Grabbed Index (출발지)
    private static int grabbedIndex = -1;

    // ⭐ Hover 기반 스왑 타겟으로 사용
    private static VRSlotInteraction lastHoveredSlot = null;

    // [위치 복원용 필드]
    private Transform originalParent;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;

    // 하이라이트/인스턴스 클린업을 위한 Static Dictionary 추가
    private static Dictionary<int, VRSlotInteraction> allSlotInteractions = new Dictionary<int, VRSlotInteraction>();

    // [XRIT 3.x 대응 필드] Interactable의 초기 Interaction Layer Mask를 저장
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

        // 3. XRIT 3.x 대응: 원래 Interaction Layer Mask 저장 (interactionLayers 사용)
        if (grabInteractable != null)
        {
            originalInteractionLayers = grabInteractable.interactionLayers;
        }

        // 4. XRGrabInteractable 이벤트 연결
        grabInteractable.selectEntered.AddListener(OnSelectStartedOverrideParenting);
        grabInteractable.selectEntered.AddListener(OnSelectStart);
        grabInteractable.selectExited.AddListener(OnSelectEndWithDelay);
        grabInteractable.hoverEntered.AddListener(OnHoverStart);
        grabInteractable.hoverExited.AddListener(OnHoverEnd);
        grabInteractable.activated.AddListener(OnActivatedForUse);

        // 하이라이트 초기화
        ClearHighlight();

        // 5. 인스턴스 등록
        if (slotIndex != -1)
        {
            if (allSlotInteractions.ContainsKey(slotIndex))
            {
                Debug.LogWarning($"Duplicate Slot Index detected: {slotIndex}");
                allSlotInteractions[slotIndex] = this;
            }
            else
            {
                allSlotInteractions.Add(slotIndex, this);
            }
        }

        // 시작 시 콜라이더는 일반 콜라이더(Is Trigger = false)여야 합니다. (유지)
        if (slotCollider != null)
        {
            slotCollider.isTrigger = false;
        }
    }

    // --- (하이라이트 로직) ---
    public void OnHoverStart(HoverEnterEventArgs args)
    {
        Debug.Log($"[DEBUG HOVER 1] OnHoverStart Called on Slot {this.slotIndex} | Grabbed Index: {grabbedIndex}");

        if (highlightImage != null)
        {
            // 현재 잡고 있는 슬롯은 하이라이트 하지 않습니다.
            if (this.slotIndex != grabbedIndex)
            {
                // 이전에 하이라이트된 슬롯이 있고 현재 슬롯과 다르다면 클리어 (단일 하이라이트 강제)
                if (lastHoveredSlot != null && lastHoveredSlot != this)
                {
                    lastHoveredSlot.ClearHighlight();
                }

                Color targetColor;

                if (grabbedIndex != -1)
                {
                    targetColor = hoverHighlightColor;
                }
                else
                {
                    targetColor = defaultHoverColor;
                }

                targetColor.a = 1.0f;
                highlightImage.color = targetColor;

                // ⭐ 마지막으로 Hover된 슬롯을 추적 (Swap Target으로 사용될 후보)
                lastHoveredSlot = this;
            }
            else
            {
                Debug.Log($"[DEBUG HOVER 3] Highlight Blocked: Slot {this.slotIndex} is the Grabbed Slot.");
            }
        }
    }

    public void OnHoverEnd(HoverExitEventArgs args)
    {
        if (lastHoveredSlot == this)
        {
            ClearHighlight();
            // Ray가 벗어날 때 lastHoveredSlot을 바로 null로 만들지 않습니다.
            // Select End가 발생할 때까지 마지막 유효 Hover를 유지합니다.
            // lastHoveredSlot = null; // 이 줄을 주석 처리하여 유지
        }
    }

    public void ClearHighlight()
    {
        if (highlightImage != null)
        {
            Color color = defaultHighlightColor;
            color.a = 0.0f;
            highlightImage.color = color;
        }
        if (lastHoveredSlot == this)
        {
            lastHoveredSlot = null; // ClearHighlight이 호출될 때만 안전하게 해제
        }
    }
    // ---

    // Grab 시작 시 호출 (빈 슬롯 방지 로직 + Interaction Layer 변경)
    public void OnSelectStart(SelectEnterEventArgs args)
    {
        if (Inventory.Instance != null && Inventory.Instance.IsSlotEmpty(this.slotIndex))
        {
            Debug.LogWarning($"[GRAB ABORTED] Slot {slotIndex} is empty. Cannot grab.");

            if (grabInteractable != null)
            {
                grabInteractable.enabled = false;
                grabInteractable.enabled = true;
            }
            return;
        }

        Debug.Log($"[INPUT SUCCESS] Select Start: Slot {slotIndex} 잡기 시작");

        grabbedIndex = this.slotIndex;

        // Grab 시 Raycast에서 제외
        if (grabInteractable != null)
        {
            grabInteractable.interactionLayers = 0;
            Debug.Log($"[XRIT MASK CHANGE] Slot {slotIndex} Interaction Layers changed to Nothing (0) during Grab.");
        }

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

            Debug.LogWarning("[PARENT OVERRIDE] Parent was changed by XRIT, forcing restoration to original parent.");
        }
    }

    // Select 종료 시 호출 (Layer Mask 복원 로직 추가)
    public void OnSelectEndWithDelay(SelectExitEventArgs args)
    {
        if (grabbedIndex != this.slotIndex)
        {
            Debug.LogWarning($"[SELECT END ABORTED] Slot {slotIndex} was not the successfully grabbed slot ({grabbedIndex}). Skipping swap logic.");

            if (grabInteractable != null)
            {
                grabInteractable.interactionLayers = originalInteractionLayers;
            }

            RestoreSlotVisualAndPhysics();
            return;
        }

        // Layer Mask 복원
        if (grabInteractable != null)
        {
            grabInteractable.interactionLayers = originalInteractionLayers;
            Debug.Log($"[XRIT MASK RESTORE] Slot {slotIndex} Interaction Layers restored to original value.");
        }

        StartCoroutine(HandleSelectEndDelayed());
    }

    private IEnumerator HandleSelectEndDelayed()
    {
        yield return null;

        Debug.Log($"[INPUT SUCCESS] Select End: Slot {slotIndex} 드롭됨 (지연 후 처리)");

        // 1. Swap Target 찾기 로직: Hover 정보를 사용
        int targetIndex = -1;
        VRSlotInteraction targetSlotInstance = lastHoveredSlot;

        if (targetSlotInstance != null && targetSlotInstance.slotIndex != this.slotIndex)
        {
            targetIndex = targetSlotInstance.slotIndex;
            Debug.Log($"[TARGET FOUND VIA HOVER] Target Index: {targetIndex} (Using last hovered slot)");
        }
        else if (targetSlotInstance != null && targetSlotInstance.slotIndex == this.slotIndex)
        {
            Debug.LogWarning("[TARGET CHECK ABORTED] Target is self (last hovered slot). Dropping item to self.");
        }

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
        if (targetSlotInstance != null)
        {
            targetSlotInstance.ClearHighlight();
            lastHoveredSlot = null; // 확실하게 초기화
        }

        grabbedIndex = -1;

        RestoreSlotVisualAndPhysics();
        Debug.Log($"[RESTORE] Slot {slotIndex} Position and Rotation restored.");
    }

    // 이 함수는 이제 HandleSelectEndDelayed에서 사용되지 않습니다.
    private int GetTargetSlotIndex(Vector3 dropPosition)
    {
        // 1. LayerMask 확인
        string layerName = "GrabUI";
        int slotLayer = LayerMask.GetMask(layerName);

        // 검색 반경 0.3f 적용
        float searchRadius = 0.3f;

        if (slotLayer == 0)
        {
            Debug.LogError($"[DEBUG ERROR 1] Layer Mask Error: '{layerName}' Layer not found or invalid. Check your Unity Layer setup!");
            return -1;
        }

        // 2. OverlapSphere 실행
        Collider[] hitColliders = Physics.OverlapSphere(dropPosition, searchRadius, slotLayer);

        Debug.Log($"[OVERLAP SEARCH] Position: {dropPosition}, Radius: {searchRadius}m, Found Colliders: {hitColliders.Length} (Layer: {layerName})");

        if (hitColliders.Length == 0)
        {
            Debug.LogWarning($"[DEBUG ERROR 2] No Colliders Found. The dropped item is not overlapping any other '{layerName}' slot within {searchRadius}m.");
            return -1;
        }

        // 3. 가장 가까운 VRSlotInteraction 컴포넌트를 가진 슬롯을 찾습니다.
        VRSlotInteraction closestTarget = null;
        float minDistance = float.MaxValue;
        bool foundTarget = false;

        foreach (var hitCollider in hitColliders)
        {
            VRSlotInteraction targetSlot = hitCollider.GetComponent<VRSlotInteraction>();

            if (targetSlot != null && targetSlot.slotIndex != this.slotIndex)
            {
                foundTarget = true;

                float distance = Vector3.Distance(hitCollider.bounds.center, dropPosition);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestTarget = targetSlot;
                }
            }
        }

        if (closestTarget != null)
        {
            Debug.Log($"[TARGET FOUND SUCCESS] Closest Target Index: {closestTarget.slotIndex}, Distance: {minDistance:F3}m");
            return closestTarget.slotIndex;
        }

        if (foundTarget)
        {
            Debug.LogWarning("[DEBUG ERROR 3] Valid Collider(s) Found, but no valid *different* target slot (only self or component missing).");
        }
        else
        {
            Debug.LogWarning("[DEBUG ERROR 3] Collider Found, but no valid target slot (VRSlotInteraction component missing on all detected colliders).");
        }

        return -1;
    }

    private void RestoreSlotVisualAndPhysics()
    {
        if (slotImage != null)
        {
            Color color = slotImage.color;
            color.a = 1.0f;
            slotImage.color = color;
        }

        if (transform.parent != originalParent)
        {
            transform.SetParent(originalParent);
        }
        transform.localPosition = initialLocalPosition;
        transform.localRotation = initialLocalRotation;

        if (slotCollider != null)
        {
            slotCollider.enabled = true;
            slotCollider.isTrigger = false;
        }
    }

    public void OnActivatedForUse(ActivateEventArgs args)
    {
        if (Inventory.Instance != null && Inventory.Instance.IsSlotEmpty(this.slotIndex))
        {
            Debug.LogWarning($"[USE ABORTED] Slot {slotIndex} is empty. Cannot use.");
            return;
        }

        if (Inventory.Instance != null)
        {
            Inventory.Instance.UseItem(this.slotIndex);
            Debug.Log($"[INPUT SUCCESS] Item used in slot {slotIndex} via Secondary Button (Activate).");
        }
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnSelectStart);
            grabInteractable.selectExited.RemoveListener(OnSelectEndWithDelay);
            grabInteractable.hoverEntered.RemoveListener(OnHoverStart);
            grabInteractable.hoverExited.RemoveListener(OnHoverEnd);
            grabInteractable.activated.RemoveListener(OnActivatedForUse);
            grabInteractable.selectEntered.RemoveListener(OnSelectStartedOverrideParenting);
        }

        if (allSlotInteractions.ContainsKey(slotIndex))
        {
            allSlotInteractions.Remove(slotIndex);
        }
    }
}