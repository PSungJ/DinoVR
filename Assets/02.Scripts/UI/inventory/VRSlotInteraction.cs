using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using System;

public class VRSlotInteraction : MonoBehaviour
{
    // --- [필드] ---
    [Header("Slot Data")]
    // 이 슬롯의 인덱스 번호 (Inspector에서 설정)
    [SerializeField] private int slotIndex = -1;

    private Image slotImage;
    private XRGrabInteractable grabInteractable;

    // 아이템을 잡기 시작한 슬롯의 인덱스를 저장하기 위한 static 변수
    private static int grabbedIndex = -1;

    // [추가된 필드] 원래 부모 (Inventory Panel)와 초기 위치를 저장합니다.
    private Transform originalParent;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;


    void Awake()
    {
        // 1. Image 컴포넌트 가져오기
        slotImage = GetComponent<Image>();
        if (slotImage == null)
        {
            Debug.LogError($"VRSlotInteraction Error: Image 컴포넌트를 찾을 수 없습니다. (오브젝트 이름: {gameObject.name})");
            return;
        }

        // 2. XRGrabInteractable 컴포넌트 가져오기
        grabInteractable = GetComponentInChildren<XRGrabInteractable>();

        if (grabInteractable == null)
        {
            Debug.LogError($"VRSlotInteraction Error: XRGrabInteractable 컴포넌트를 찾을 수 없습니다. (오브젝트 이름: {gameObject.name})");
            return;
        }

        // [수정] 원래 부모와 초기 위치/회전을 저장합니다.
        // Grab 시 계층 구조 이탈 후 복원하기 위함입니다.
        originalParent = transform.parent;
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;

        // 3. XRGrabInteractable 이벤트 연결
        grabInteractable.selectEntered.AddListener(OnSelectStart);
        grabInteractable.selectExited.AddListener(OnSelectEnd);
    }

    /// <summary>
    /// Select Entered 이벤트 발생 시 (잡기 시작) 호출
    /// </summary>
    public void OnSelectStart(SelectEnterEventArgs args)
    {
        Debug.Log($"[INPUT SUCCESS] Select Start: Slot {slotIndex} 잡기 시작");

        // 1. Grab 시작 인덱스 저장
        grabbedIndex = this.slotIndex;

        // 2. 시각적 피드백: 불투명도를 낮춥니다.
        if (slotImage != null)
        {
            Color color = slotImage.color;
            color.a = 0.5f;
            slotImage.color = color;
        }
    }

    /// <summary>
    /// Select Exited 이벤트 발생 시 (잡기 종료 = 드롭) 호출
    /// </summary>
    public void OnSelectEnd(SelectExitEventArgs args)
    {
        Debug.Log($"[INPUT SUCCESS] Select End: Slot {slotIndex} 드롭됨");

        // 1. 시각적 피드백: 불투명도를 원래대로 복원합니다.
        if (slotImage != null)
        {
            Color color = slotImage.color;
            color.a = 1.0f;
            slotImage.color = color;
        }

        // 2. Swap 로직 호출
        if (grabbedIndex != -1 && grabbedIndex != this.slotIndex)
        {
            if (Inventory.Instance != null)
            {
                Inventory.Instance.SwapSlots(grabbedIndex, this.slotIndex);
            }
            else
            {
                Debug.LogError("Inventory.Instance를 찾을 수 없습니다. 싱글톤 초기화 상태를 확인하세요.");
            }
        }

        // 3. Grab 상태 초기화
        grabbedIndex = -1;

        // --- [핵심 문제 해결 로직] ---
        // Grab 시 계층 구조 밖으로 이탈하는 문제와 위치/회전 틀어짐을 해결합니다.

        // 4. 부모를 강제로 원래 부모 (InventoryPanel)로 복원합니다.
        if (transform.parent != originalParent)
        {
            transform.SetParent(originalParent);
            Debug.Log($"[RESTORE] Slot {slotIndex} Parent restored to {originalParent.name}.");
        }

        // 5. 위치와 회전을 Awake() 때 저장해둔 초기 값으로 강제 복원합니다.
        transform.localPosition = initialLocalPosition;
        transform.localRotation = initialLocalRotation;

        Debug.Log($"[RESTORE] Slot {slotIndex} Position and Rotation restored.");
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnSelectStart);
            grabInteractable.selectExited.RemoveListener(OnSelectEnd);
        }
    }
}