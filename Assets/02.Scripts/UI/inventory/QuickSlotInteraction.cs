using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine;

public class QuickSlotInteraction : MonoBehaviour
{
    // 🔥 추가: 이 상호작용 오브젝트가 담당하는 퀵슬롯의 내부 인덱스 (0, 1, 2...)
    [Header("Quick Slot Configuration")]
    [Tooltip("이 오브젝트가 담당하는 퀵슬롯의 내부 인덱스 (0부터 시작)")]
    [SerializeField] private int quickSlotIndex = -1;

    // 퀵슬롯 매니저 참조 (Inspector에서 할당하거나, FindObjectOfType으로 찾습니다.)
    private QuickSlotManager quickSlotManager;

    // [SerializeField] private ItemBaseSO itemInSlot; // 퀵슬롯에 연결된 아이템 데이터 (Manager에서 데이터를 가져옴)

    private void Awake()
    {
        // Manager 인스턴스 찾기
        quickSlotManager = FindObjectOfType<QuickSlotManager>();

        if (quickSlotManager == null)
        {
            Debug.LogError("[QuickSlotInteraction] QuickSlotManager를 씬에서 찾을 수 없습니다.");
            return;
        }

        // 1. XR Interactable 컴포넌트 가져오기 (Select 이벤트를 받을 준비)
        var interactable = GetComponent<XRBaseInteractable>();
        if (interactable != null)
        {
            // 2. Select Entered 이벤트에 아이템 사용 로직 연결
            interactable.selectEntered.AddListener(OnQuickSlotSelected);
        }

        // quickSlotIndex가 설정되었는지 확인
        if (quickSlotIndex == -1)
        {
            Debug.LogError("[QuickSlotInteraction] quickSlotIndex가 설정되지 않았습니다. Inspector를 확인하세요.");
        }
    }

    private void OnQuickSlotSelected(SelectEnterEventArgs args)
    {
        // ⭐ 수정: Manager를 통해 아이템 사용을 요청합니다.
        if (quickSlotManager != null && quickSlotIndex != -1)
        {
            // QuickSlotManager에게 해당 인덱스의 아이템을 사용하도록 요청
            quickSlotManager.UseItemAtQuickSlotIndex(quickSlotIndex);
        }
        else
        {
            // 매니저가 없거나 인덱스가 잘못된 경우
            Debug.LogWarning("퀵슬롯 사용 요청 실패: 매니저가 없거나 인덱스가 잘못되었습니다.");
        }
    }
}