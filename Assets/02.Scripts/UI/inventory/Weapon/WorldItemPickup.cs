using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

/// <summary>
/// 월드에 배치된 물리적 아이템에 부착되어 VR 상호작용(Grab)을 감지하고
/// 아이템을 플레이어의 인벤토리에 추가하는 역할을 수행합니다.
/// </summary>
[RequireComponent(typeof(XRBaseInteractable))]
public class WorldItemPickup : MonoBehaviour
{
    [Header("Item Data")]
    // 이 물리적 오브젝트가 나타내는 장비 스크립터블 오브젝트 데이터
    [SerializeField] private EquippableItemSO itemData;

    private XRBaseInteractable interactable;

    private void Awake()
    {
        // XRGrabInteractable 또는 XRSimpleInteractable 컴포넌트를 가져옵니다.
        interactable = GetComponent<XRBaseInteractable>();

        if (interactable == null)
        {
            Debug.LogError($"[Pickup] {gameObject.name}에 XRBaseInteractable 컴포넌트가 없습니다.");
            return;
        }

        // ⭐ Ray Interactor가 아이템을 '선택(Grab)'했을 때 이벤트를 구독합니다.
        interactable.selectEntered.AddListener(OnItemSelected);
    }

    /// <summary>
    /// Ray Interactor가 아이템을 성공적으로 잡았을 때 호출됩니다.
    /// </summary>
    /// <param name="args">선택 이벤트를 발생시킨 Interactor 정보</param>
    private void OnItemSelected(SelectEnterEventArgs args)
    {
        if (Inventory.Instance == null)
        {
            Debug.LogError("[Pickup] Inventory.Instance가 씬에 없습니다. 아이템 획득 불가.");
            return;
        }

        // 1. 인벤토리에 아이템 추가 시도
        // 무기 아이템이므로 항상 1개를 추가합니다.
        bool added = Inventory.Instance.AddItem(itemData, 1);

        if (added)
        {
            // 2. 성공: 물리적 오브젝트 파괴
            // 이벤트 구독 해제 후 오브젝트를 파괴합니다.
            interactable.selectEntered.RemoveListener(OnItemSelected);
            Destroy(gameObject);
            Debug.Log($"[Pickup] {itemData.itemName}을(를) 성공적으로 획득하여 인벤토리에 추가했습니다.");
        }
        else
        {
            // 3. 실패 (인벤토리가 가득 찼을 때):
            // 플레이어가 아이템을 계속 잡고 있지 못하도록 선택을 해제합니다.
            Debug.LogWarning($"[Pickup] 인벤토리가 가득 찼습니다. {itemData.itemName}을(를) 획득할 수 없습니다.");

            // Interactor에게 현재 상호작용을 중지하도록 요청합니다.
            if (args.manager != null)
            {
                // [FIX] Deprecation 경고 수정: CancelInteractableSelection 메서드는 IXRSelectInteractable 인터페이스를 기대합니다.
                // XRBaseInteractable이 해당 인터페이스를 구현하므로 캐스팅하여 사용합니다.
                args.manager.CancelInteractableSelection((IXRSelectInteractable)interactable);
            }

            // TODO: 사용자에게 인벤토리 가득 참을 알리는 UI/사운드 피드백 제공
        }
    }

    private void OnDestroy()
    {
        // 씬 종료 시 안전하게 이벤트 구독 해제
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnItemSelected);
        }
    }
}
