using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;

public class VRSlotInteraction : MonoBehaviour
{
    // --- [새롭게 추가된 필드] ---
    [Header("Slot Data")]
    // 이 슬롯의 인덱스 번호 (Inspector에서 수동으로 0부터 N까지 설정해야 합니다!)
    [SerializeField] private int slotIndex = -1;

    private Image slotImage;
    private XRGrabInteractable grabInteractable;

    // 아이템을 잡기 시작한 슬롯의 인덱스를 저장하기 위한 static 변수
    // (모든 VRSlotInteraction 인스턴스가 공유)
    private static int grabbedIndex = -1;


    void Awake()
    {
        // 1. Image 컴포넌트 가져오기
        slotImage = GetComponent<Image>();
        if (slotImage == null)
        {
            Debug.LogError("VRSlotInteraction: Image 컴포넌트가 슬롯에 없습니다.");
            return;
        }

        // 2. XRGrabInteractable 컴포넌트 가져오기
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            Debug.LogError("VRSlotInteraction: XRGrabInteractable 컴포넌트가 슬롯에 없습니다.");
            return;
        }

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
        // 아이템을 잡았을 때, 이 슬롯의 인덱스를 static 변수에 저장합니다.
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
        // Grab 시작 인덱스(-1이 아니어야 함)와 현재 드롭된 슬롯의 인덱스가 달라야 스왑합니다.
        if (grabbedIndex != -1 && grabbedIndex != this.slotIndex)
        {
            // Inventory 싱글톤 인스턴스의 SwapSlots 함수를 호출합니다.
            if (Inventory.Instance != null)
            {
                // grabbedIndex (잡은 위치)와 this.slotIndex (놓은 위치)를 인자로 전달합니다.
                Inventory.Instance.SwapSlots(grabbedIndex, this.slotIndex);
            }
            else
            {
                Debug.LogError("Inventory.Instance를 찾을 수 없습니다. Inventory 스크립트가 씬에 있는지 확인하고 싱글톤이 초기화되었는지 확인하세요.");
            }
        }

        // 3. Grab 상태 초기화
        grabbedIndex = -1;
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