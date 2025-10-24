// CustomSlotGrabInteractable.cs
using UnityEngine.XR.Interaction.Toolkit;

// 기존 XRGrabInteractable 대신 이 스크립트를 사용합니다.
public class CustomSlotGrabInteractable : XRGrabInteractable
{
    // Inventory.Instance.IsSlotEmpty(slotIndex)를 호출할 수 있도록 
    // 외부에서 slotIndex를 받아와야 합니다.
    public int slotIndex { get; set; } = -1;

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
        if (Inventory.Instance != null && Inventory.Instance.IsSlotEmpty(this.slotIndex))
        {
            // 빈 슬롯이면 Grab 불가
            return false;
        }

        // 3. 모든 조건 통과 시 Grab 가능
        return true;
    }
}