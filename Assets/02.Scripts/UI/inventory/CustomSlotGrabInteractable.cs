// CustomSlotGrabInteractable.cs
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine; // Debug.LogWarning을 위해 추가

// 기존 XRGrabInteractable 대신 이 스크립트를 사용합니다.
public class CustomSlotGrabInteractable : XRGrabInteractable
{
    // Inventory.Instance.IsSlotEmpty(slotIndex)를 호출할 수 있도록 
    // 외부에서 slotIndex를 받아와야 합니다.
    public int slotIndex { get; set; } = -1;

    // 🔥 핵심: 이 슬롯이 비어 있을 때도 Grab이 가능한지 여부 (Inspector 설정)
    // 장비 슬롯이 "장착된 아이템을 잡는 용도"라면 이 값은 False여야 합니다. (기본값)
    [Header("Custom Logic")]
    [Tooltip("체크 시: 슬롯이 비어있어도 Select 가능 (예: 드롭 타겟용 빈 슬롯)")]
    [SerializeField]
    private bool allowGrabWhenEmpty = false;


    /// <summary>
    /// Select 상호작용을 시작하기 전에 호출됩니다.
    /// 이 메서드가 false를 반환하면 Grab이 취소됩니다.
    /// </summary>
    public override bool IsSelectableBy(IXRSelectInteractor interactor)
    {
        // 1. 기본 Select 가능 여부를 확인
        if (!base.IsSelectableBy(interactor))
        {
            return false;
        }

        // 2. 추가적인 조건 (빈 슬롯 검사)

        // allowGrabWhenEmpty가 true이면 빈 슬롯 검사를 건너뛰고 바로 Grab 가능
        if (allowGrabWhenEmpty)
        {
            // Debug.Log($"[Grab] Slot {slotIndex}: AllowGrabWhenEmpty=True. Grab 가능.");
            return true;
        }

        // allowGrabWhenEmpty가 false일 때 (현재 장비 슬롯의 목표)만 빈 슬롯 검사를 수행합니다.
        if (Inventory.Instance != null && Inventory.Instance.IsSlotEmpty(this.slotIndex))
        {
            // 🔥 빈 슬롯이면 Grab 불가 (아이템이 있어야 잡을 수 있도록 함)
            // Debug.LogWarning($"[Grab] Slot {slotIndex}: Empty. Grab 불가.");
            return false;
        }

        // 3. 모든 조건 통과 시 Grab 가능
        // Debug.Log($"[Grab] Slot {slotIndex}: Item Present. Grab 가능.");
        return true;
    }
}