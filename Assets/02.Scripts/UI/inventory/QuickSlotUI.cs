using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System; // Action 이벤트를 사용하기 위해 추가

/// <summary>
/// 퀵슬롯 전체 UI를 관리하는 컴포넌트입니다.
/// QuickSlotManager의 이벤트를 구독하여 아이템 및 선택 상태를 갱신합니다.
/// </summary>
public class QuickSlotUI : MonoBehaviour
{
    [Header("Dependencies")]
    // QuickSlotManager 인스턴스 참조 (Awake에서 가져옴)
    private QuickSlotManager quickSlotManager;

    // 인벤토리/퀵슬롯 공용 슬롯 UI 업데이트 컴포넌트
    // 이 배열/리스트에는 QuickSlotManager.quickSlots 배열과 1:1로 매칭되는 UI 슬롯이 할당되어야 합니다.
    [Header("UI Elements (Slots)")]
    // SlotUIUpdater.cs에 정의된 컴포넌트를 사용합니다.
    [SerializeField] private List<SlotUIUpdater> slotUpdaters = new List<SlotUIUpdater>();

    [Header("Selection Indicator")]
    // 현재 선택된 슬롯을 강조하는 UI 요소 (예: 테두리, 하이라이트 이미지 등)
    [SerializeField] private GameObject selectionIndicator;

    // QuickSlotManager.quickSlots의 크기와 slotUpdaters의 크기가 일치하는지 확인
    private int quickSlotCount = 0;


    private void Awake()
    {
        // 싱글톤 인스턴스 가져오기
        quickSlotManager = QuickSlotManager.Instance;

        if (quickSlotManager == null)
        {
            Debug.LogError("[QuickSlotUI] QuickSlotManager.Instance를 찾을 수 없습니다. 실행 순서를 확인하세요.");
            return;
        }

        quickSlotCount = slotUpdaters.Count;

        // 퀵슬롯 매니저에 **이벤트 리스너 등록**
        quickSlotManager.OnQuickSlotChanged += UpdateSlotUI;
        quickSlotManager.OnQuickSlotSelectionChanged += UpdateSelectionUI;
    }

    private void Start()
    {
        // 초기 UI 상태를 설정합니다.

        // 1. 모든 슬롯 아이콘 초기화 (Manager의 데이터로)
        // quickSlots 배열의 크기와 slotUpdaters 리스트의 크기가 다르면 오류가 발생할 수 있습니다.
        if (quickSlotManager.quickSlots != null && quickSlotManager.quickSlots.Length == quickSlotCount)
        {
            for (int i = 0; i < quickSlotManager.quickSlots.Length; i++)
            {
                InventorySlot slotData = quickSlotManager.quickSlots[i];
                // ItemBaseSO와 count를 포함하여 UpdateSlotUI 호출
                UpdateSlotUI(i, slotData.itemData, slotData.stackSize);
            }
        }
        else
        {
            Debug.LogError("[QuickSlotUI] QuickSlotManager의 quickSlots 배열 크기가 QuickSlotUI의 SlotUpdaters 크기와 일치하지 않습니다. 인스펙터 설정을 확인하세요.");
        }


        // 2. 초기 선택 상태 하이라이트 설정
        UpdateSelectionUI(quickSlotManager.selectedSlotIndex);
    }

    // ----------------------------------------------------
    // [아이템 업데이트] (Manager의 OnQuickSlotChanged 이벤트 핸들러)
    // ----------------------------------------------------
    /// <summary>
    /// Manager로부터 슬롯 변경 알림을 받아 UI를 갱신합니다.
    /// </summary>
    public void UpdateSlotUI(int index, ItemBaseSO item, int count)
    {
        if (index < 0 || index >= quickSlotCount || slotUpdaters[index] == null)
        {
            return;
        }

        // SlotUIUpdater의 공용 함수를 사용하여 UI 갱신
        slotUpdaters[index].UpdateSlotUI(item, count);
    }

    /// <summary>
    /// 현재 선택된 퀵슬롯의 하이라이트를 갱신합니다. (Manager의 OnQuickSlotSelectionChanged 이벤트 핸들러)
    /// </summary>
    public void UpdateSelectionUI(int newSelectedIndex)
    {
        if (newSelectedIndex < 0 || newSelectedIndex >= quickSlotCount) return;

        // 1. 모든 슬롯의 하이라이트 제거
        foreach (var updater in slotUpdaters)
        {
            if (updater != null)
            {
                updater.SetHighlight(false);
            }
        }

        // 2. 선택된 슬롯에 하이라이트 적용
        if (slotUpdaters[newSelectedIndex] != null)
        {
            slotUpdaters[newSelectedIndex].SetHighlight(true);

            // 3. 선택 표시기(예: 테두리 이미지)를 해당 슬롯의 위치로 이동 (선택 사항)
            if (selectionIndicator != null)
            {
                // UIUpdater 컴포넌트의 Transform을 사용합니다.
                selectionIndicator.transform.position = slotUpdaters[newSelectedIndex].transform.position;
                selectionIndicator.SetActive(true);
            }
        }
    }

    private void OnDestroy()
    {
        if (quickSlotManager != null)
        {
            // 구독 해제 (GC 이슈 방지)
            quickSlotManager.OnQuickSlotChanged -= UpdateSlotUI;
            quickSlotManager.OnQuickSlotSelectionChanged -= UpdateSelectionUI;
        }
    }
}