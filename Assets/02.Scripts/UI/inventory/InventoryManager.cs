using UnityEngine;
using System.Collections.Generic; // 리스트를 사용한다면 필요

// 인벤토리 슬롯에 저장되는 데이터의 구조 (구조체 또는 스크립터블 오브젝트 형태를 가정)
[System.Serializable]
public struct InventorySlotData
{
    public string itemName;
    public int itemID;
    public int count;
    // 여기에 아이템 아이콘(Sprite), 3D 모델 프리팹 등의 정보가 추가될 수 있습니다.
}


public class InventoryManager : MonoBehaviour
{
    // InventoryManager를 싱글톤 패턴으로 관리하는 경우가 많습니다.
    // public static InventoryManager Instance { get; private set; } 

    [Header("Inventory Settings")]
    [SerializeField] private int inventorySize = 20; // 인벤토리 크기

    // 인벤토리의 실제 데이터 (배열 또는 리스트)
    [Tooltip("실제 인벤토리 데이터를 저장하는 배열")]
    public InventorySlotData[] slots;

    // 인벤토리 UI를 업데이트하는 모든 슬롯 오브젝트 리스트 (UI 갱신용)
    // [SerializeField] private List<GameObject> slotUIs; 


    void Awake()
    {
        // if (Instance == null) Instance = this; else Destroy(gameObject);

        // 인벤토리 배열 초기화
        if (slots == null || slots.Length == 0)
        {
            slots = new InventorySlotData[inventorySize];
            InitializeSlots();
        }
    }

    private void InitializeSlots()
    {
        for (int i = 0; i < inventorySize; i++)
        {
            // 빈 슬롯으로 초기화 (아이디 0, 카운트 0 등)
            slots[i] = new InventorySlotData { itemName = "Empty", itemID = 0, count = 0 };
        }
    }

    // --- 핵심 기능: VR Grab & Drop 후 호출될 아이템 교환 로직 ---
    /// <summary>
    /// 두 인벤토리 슬롯의 아이템 데이터를 서로 교환합니다.
    /// VRSlotInteraction 스크립트에서 드롭 이벤트 발생 시 호출됩니다.
    /// </summary>
    /// <param name="fromIndex">아이템을 잡았던 원래 슬롯의 인덱스</param>
    /// <param name="toIndex">아이템이 드롭된 대상 슬롯의 인덱스</param>
    public void SwapItems(int fromIndex, int toIndex)
    {
        // 1. 인덱스 유효성 검사
        if (fromIndex < 0 || fromIndex >= slots.Length ||
            toIndex < 0 || toIndex >= slots.Length)
        {
            Debug.LogError($"[InventoryManager] Swap Error: 유효하지 않은 인덱스 ({fromIndex} 또는 {toIndex})");
            return;
        }

        // 2. 아이템 데이터 교환 (Swap)
        InventorySlotData tempItem = slots[fromIndex]; // 임시 변수에 fromIndex 데이터 저장
        slots[fromIndex] = slots[toIndex]; // toIndex 데이터를 fromIndex로 이동
        slots[toIndex] = tempItem; // 임시 변수에 저장했던 데이터를 toIndex로 이동

        Debug.Log($"[InventoryManager] 아이템 교환 성공: Slot {fromIndex} <-> Slot {toIndex}");

        // 3. UI 업데이트 (실제 UI Image, Text 등을 갱신하는 함수 호출)
        // 이 부분은 사용자님의 기존 UI 업데이트 로직에 따라 달라집니다.
        // 예를 들어: UpdateSlotUI(fromIndex); UpdateSlotUI(toIndex);
        UpdateAllSlotUIs();
    }

    // 이 함수는 실제 UI 오브젝트의 Image나 Text를 갱신하는 로직을 포함해야 합니다.
    private void UpdateAllSlotUIs()
    {
        // 예시: 모든 슬롯 UI 오브젝트를 순회하며 데이터를 반영
        // for (int i = 0; i < slots.Length; i++)
        // {
        //     // slotUIs[i].GetComponent<SlotUIUpdater>().Update(slots[i]);
        // }
    }

    // --- 기타 인벤토리 기능들 (예시) ---

    // 아이템 추가
    public bool AddItem(InventorySlotData item)
    {
        // 여기에 아이템 추가 로직 구현
        return true;
    }

    // 아이템 제거
    public void RemoveItem(int index)
    {
        // 여기에 아이템 제거 로직 구현
        // slots[index] = new InventorySlotData { itemName = "Empty", itemID = 0, count = 0 };
    }
}