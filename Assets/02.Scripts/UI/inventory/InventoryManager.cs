using UnityEngine;
using System.Collections.Generic;
using System;

// 인벤토리 슬롯에 저장되는 데이터의 구조
[System.Serializable]
public struct InventorySlotData
{
    public string itemName;
    public int itemID;
    public int count;
    // ⭐ [FIX: 임시 테스트용] 아이콘 스프라이트 필드를 추가합니다.
    // 실제 프로젝트에서는 ItemBaseSO와 같은 데이터에서 가져와야 합니다.
    public Sprite itemIcon;
}


public class InventoryManager : MonoBehaviour
{
    // ⭐ [FIX: 싱글톤 활성화] 외부에서 접근할 수 있도록 싱글톤 인스턴스를 활성화합니다.
    public static InventoryManager Instance { get; private set; }

    [Header("Inventory Settings")]
    [SerializeField] private int inventorySize = 20; // 인벤토리 크기

    // 인벤토리의 실제 데이터 (배열 또는 리스트)
    [Tooltip("실제 인벤토리 데이터를 저장하는 배열")]
    public InventorySlotData[] slots;

    // 인벤토리 UI를 업데이트하는 모든 슬롯 오브젝트 리스트 (UI 갱신용)
    // [SerializeField] private List<GameObject> slotUIs; 


    void Awake()
    {
        // ⭐ [FIX: 싱글톤 로직]
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

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
            // InventorySlotData 구조체가 ItemIcon 필드를 가지도록 수정했습니다.
            slots[i] = new InventorySlotData { itemName = "Empty", itemID = 0, count = 0, itemIcon = null };
        }

        // ⭐ [TEST: 초기 아이템 추가] 테스트를 위해 0번 슬롯에 더미 아이템을 채웁니다.
        slots[0] = new InventorySlotData { itemName = "Test Potion", itemID = 1, count = 5, itemIcon = null };
    }

    // ----------------------------------------------------
    // [UI Handler의 유효성 검사를 위한 헬퍼 함수]
    // ----------------------------------------------------
    /// <summary>
    /// 특정 인덱스의 슬롯이 비어있는지 확인합니다.
    /// InventorySlotUIHandler.cs의 OnBeginDrag에서 호출되어 드래그 유효성을 검사합니다.
    /// </summary>
    public bool IsSlotEmpty(int index)
    {
        if (index < 0 || index >= slots.Length)
        {
            return true; // 인덱스가 유효하지 않으면 비어있다고 가정
        }
        // count가 0이거나 itemName이 "Empty"인 경우 비어있음으로 처리
        return slots[index].count <= 0 || slots[index].itemName == "Empty";
    }

    /// <summary>
    /// InventorySlotUIHandler가 드래그를 시작할 때 아이콘을 표시하기 위해 호출됩니다.
    /// </summary>
    public Sprite GetItemIcon(int index)
    {
        if (IsSlotEmpty(index)) return null;

        // InventorySlotData의 itemIcon 필드에서 Sprite를 반환합니다.
        return slots[index].itemIcon;
    }


    // ----------------------------------------------------
    // [핵심 기능: VR Grab & Drop 후 호출될 아이템 교환 로직]
    // ----------------------------------------------------
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
        InventorySlotData tempItem = slots[fromIndex];
        slots[fromIndex] = slots[toIndex];
        slots[toIndex] = tempItem;

        Debug.Log($"[InventoryManager] 아이템 교환 성공: Slot {fromIndex} <-> Slot {toIndex}");

        // 3. UI 업데이트 
        UpdateAllSlotUIs();
    }

    // 이 함수는 실제 UI 오브젝트의 Image나 Text를 갱신하는 로직을 포함해야 합니다.
    private void UpdateAllSlotUIs()
    {
        // ⭐ [TODO: 실제 UIUpdater 로직으로 대체] 
        // 현재는 콘솔 로그만 출력합니다. Inventory.cs에서처럼 SlotUIUpdater 배열을 사용하여 갱신해야 합니다.
        Debug.Log("[InventoryManager] UI 갱신 요청: UpdateAllSlotUIs 호출됨. (SlotUIUpdater 연동 필요)");
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
