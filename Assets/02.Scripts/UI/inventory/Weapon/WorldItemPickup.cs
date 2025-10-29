using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

/// <summary>
/// 월드에 배치된 물리적 아이템에 부착되어 
/// VR 상호작용(Grab 해제)을 감지하고
/// 아이템을 플레이어의 인벤토리에 추가하는 역할을 수행합니다.
/// </summary>
[RequireComponent(typeof(XRBaseInteractable))]
public class WorldItemPickup : MonoBehaviour
{
    [Header("Item Data")]
    // 이 물리적 오브젝트가 나타내는 장비 스크립터블 오브젝트 데이터
    [SerializeField] private ItemBaseSO itemData;


    // ✅ 장비에서 생성된 아이템은 인벤토리 추가 로직을 무시하도록 하는 플래그
    private bool ignorePickup = false;

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

        // ⭐ Ray Interactor가 아이템을 '놓았을 때(Grab 해제)' 이벤트를 구독합니다.
        interactable.selectExited.AddListener(OnItemReleased);
    }

    /// <summary>
    /// EquipmentManager에서 생성된 장비 프리팹임을 표시하고,
    /// 인벤토리 자동 추가 로직을 무시하게 합니다.
    /// </summary>
    public void MarkAsEquippedItem()
    {
        ignorePickup = true;
    }

    /// <summary>
    /// Ray Interactor가 아이템을 놓았을 때 호출됩니다.
    /// </summary>
    /// <param name="args">상호작용 이벤트를 발생시킨 Interactor 정보</param>
    private void OnItemReleased(SelectExitEventArgs args)
    {
        // ✅ 장비 시스템에서 생성된 프리팹은 인벤토리 추가를 무시
        if (ignorePickup)
        {
            Debug.Log($"[Pickup] '{gameObject.name}'은(는) 장비 시스템에서 생성된 아이템이므로 인벤토리에 추가하지 않습니다.");
            return;
        }

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
            interactable.selectExited.RemoveListener(OnItemReleased);
            Destroy(gameObject);
            Debug.Log($"[Pickup] {itemData.itemName}을(를) 성공적으로 획득하여 인벤토리에 추가했습니다.");
        }
        else
        {
            // 3. 실패 (인벤토리가 가득 찼을 때):
            Debug.LogWarning($"[Pickup] 인벤토리가 가득 찼습니다. {itemData.itemName}을(를) 획득할 수 없습니다.");

            // Grab 해제 시점이므로 별도의 선택 취소는 필요하지 않습니다.
            // TODO: 사용자에게 인벤토리 가득 참을 알리는 UI/사운드 피드백 제공
        }
    }

    private void OnDestroy()
    {
        // 씬 종료 시 안전하게 이벤트 구독 해제
        if (interactable != null)
        {
            interactable.selectExited.RemoveListener(OnItemReleased);
        }
    }
}
