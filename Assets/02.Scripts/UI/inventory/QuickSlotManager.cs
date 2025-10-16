using UnityEngine;
using System;
using UnityEngine.XR.Interaction.Toolkit;

// [참고] ItemBaseSO, InventorySlot 등의 정의가 필요합니다.

public class QuickSlotManager : MonoBehaviour
{
    // --- [필요한 필드 (Inventory.cs와의 연동을 위해)] ---
    [Header("QuickSlot Setup")]
    [SerializeField] private int quickSlotStartIndex = 30;
    [SerializeField] private int quickSlotCount = 5;      // 퀵슬롯의 개수

    // 퀵슬롯의 실제 아이템 데이터를 저장하는 배열
    public InventorySlot[] quickSlots;

    [Header("UI References")]
    [SerializeField] private SlotUIUpdater[] quickSlotUIs; // 퀵슬롯 UI를 업데이트하기 위한 배열


    private void Awake()
    {
        quickSlots = new InventorySlot[quickSlotCount];
        for (int i = 0; i < quickSlotCount; i++)
        {
            // InventorySlot.Empty로 초기화
            quickSlots[i] = InventorySlot.Empty;
        }
    }

    // ----------------------------------------------------
    // [Inventory.cs 에서 호출되는 필수 함수]
    // ----------------------------------------------------

    /// <summary>
    /// 인덱스가 QuickSlot 영역에 속하는지 확인합니다.
    /// </summary>
    public bool IsQuickSlotIndex(int index)
    {
        return index >= quickSlotStartIndex && index < quickSlotStartIndex + quickSlotCount;
    }

    /// <summary>
    /// 🔥 [핵심 추가] 퀵슬롯 인덱스에 아이템이 없는지 확인합니다. (Inventory.cs에서 호출됨)
    /// </summary>
    public bool IsQuickSlotEmpty(int index)
    {
        if (!IsQuickSlotIndex(index))
        {
            // 퀵슬롯 인덱스가 아님
            return true;
        }

        // 가상 인덱스를 내부 배열 인덱스 (0부터 시작)로 변환
        int internalIndex = index - quickSlotStartIndex;

        // 내부 배열의 데이터 확인
        if (internalIndex >= 0 && internalIndex < quickSlotCount)
        {
            // InventorySlot.IsEmpty 속성 사용
            return quickSlots[internalIndex].IsEmpty;
        }

        return true;
    }

    /// <summary>
    /// 인벤토리 슬롯과 퀵슬롯 간의 아이템 교환을 처리합니다.
    /// </summary>
    public void HandleInventorySwap(int indexA, int indexB)
    {
        // Inventory.Instance가 static으로 접근 가능하다고 가정
        Inventory inventory = Inventory.Instance;
        if (inventory == null) return;

        // 1. A와 B 중 어느 쪽이 QuickSlot이고 InventorySlot인지 판별
        bool aIsQuick = IsQuickSlotIndex(indexA);
        bool bIsQuick = IsQuickSlotIndex(indexB);

        // 2. 인덱스를 실제 배열 인덱스로 변환 
        int aInternalIndex = aIsQuick ? indexA - quickSlotStartIndex : indexA;
        int bInternalIndex = bIsQuick ? indexB - quickSlotStartIndex : indexB;

        // 3. 현재 데이터 가져오기 
        InventorySlot slotA_Data = aIsQuick ? quickSlots[aInternalIndex] : inventory.slots[aInternalIndex];
        InventorySlot slotB_Data = bIsQuick ? quickSlots[bInternalIndex] : inventory.slots[bInternalIndex];

        // 4. 데이터 교환
        if (aIsQuick)
        {
            quickSlots[aInternalIndex] = slotB_Data;
        }
        else
        {
            inventory.slots[aInternalIndex] = slotB_Data;
        }

        if (bIsQuick)
        {
            quickSlots[bInternalIndex] = slotA_Data;
        }
        else
        {
            inventory.slots[bInternalIndex] = slotA_Data;
        }

        Debug.Log($"[QuickSlotManager] Swap Executed: Inventory/QuickSlot Indices ({indexA} <-> {indexB})");

        // 5. UI 갱신 (잔상 문제 해결)
        if (!aIsQuick || !bIsQuick)
        {
            // 인벤토리 데이터가 변경되었을 경우 인벤토리 전체를 갱신
            inventory.RefreshAllInventoryUI();
        }

        RefreshAllQuickSlotUI();
    }

    // ----------------------------------------------------
    // [UI 갱신 로직]
    // ----------------------------------------------------

    /// <summary>
    /// QuickSlot UI를 전체 갱신합니다.
    /// </summary>
    private void RefreshAllQuickSlotUI()
    {
        if (quickSlotUIs == null) return;

        // 배열 길이 안전 체크
        int updateCount = Mathf.Min(quickSlotCount, quickSlotUIs.Length);

        for (int i = 0; i < updateCount; i++)
        {
            if (quickSlotUIs[i] != null)
            {
                quickSlotUIs[i].UpdateSlotUI(quickSlots[i].itemData, quickSlots[i].stackSize);
            }
        }
    }
}