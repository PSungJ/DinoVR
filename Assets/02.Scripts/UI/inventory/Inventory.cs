using UnityEngine;
using System.Linq;
using System;

// InventorySlot, ItemBaseSO 등 외부 정의는 생략합니다.

public class Inventory : MonoBehaviour
{
    // --- [싱글톤 인스턴스] ---
    public static Inventory Instance { get; private set; }

    // 🔥 이벤트 정의: 외부에서 구독 가능하도록 public event로 유지
    /// <summary>특정 인벤토리 슬롯의 내용물이 변경될 때 발생합니다.</summary>
    public event Action<int> OnInventoryChanged;

    [Header("Dependencies")]
    [SerializeField] private int capacity = 30;

    // ⭐ [FIX: 외부에서 capacity를 읽을 수 있도록 public Capacity 속성 추가]
    public int Capacity => capacity;

    [SerializeField] private EquipmentManager equipmentManager;
    [SerializeField] private QuickSlotManager quickSlotManager;

    // 인벤토리의 실제 아이템 데이터를 저장하는 배열
    public InventorySlot[] slots; // InventorySlot은 외부에서 정의됨

    [Header("Initial Setup (Test)")]
    [SerializeField] private EquippableItemSO initialWeapon;
    [SerializeField] private ConsumableItemSO initialConsumable;
    [SerializeField] private int initialConsumableAmount = 5;

    [Header("UI References")]
    [SerializeField] private SlotUIUpdater[] inventorySlotUIs;


    private void Awake()
    {
        // --- [싱글톤 초기화 로직] ---
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 인벤토리 슬롯 배열 초기화
        slots = new InventorySlot[capacity];
        for (int i = 0; i < capacity; i++)
        {
            slots[i] = InventorySlot.Empty;
        }
    }

    private void Start()
    {
        // 테스트용 초기 아이템 추가
        if (initialWeapon != null) AddItem(initialWeapon, 1);
        if (initialConsumable != null) AddItem(initialConsumable, initialConsumableAmount);

        // 초기 UI 갱신
        RefreshAllInventoryUI();
    }


    // ----------------------------------------------------
    // [UI 갱신 로직]
    // ----------------------------------------------------

    /// <summary>
    /// QuickSlotManager에서 안전하게 UI 갱신 및 이벤트 발생을 요청하기 위한 퍼블릭 메서드.
    /// </summary>
    public void RefreshSlotUI(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < capacity)
        {
            UpdateSingleSlotUI(slotIndex);

            // 이벤트를 발생시킵니다.
            OnInventoryChanged?.Invoke(slotIndex);
        }
    }

    /// <summary>
    /// 🔥 추가: 장비 교체/장착 후 인벤토리 슬롯을 갱신하는 메서드. (CS1061 오류 해결)
    /// </summary>
    /// <param name="slotIndex">아이템이 장착된 인벤토리 슬롯 인덱스</param>
    /// <param name="oldItem">이전에 장착되어 해제된 아이템 (없으면 null)</param>
    public void UpdateSlotWithNewEquippedItem(int slotIndex, EquippableItemSO oldItem)
    {
        if (slotIndex < 0 || slotIndex >= capacity) return;

        if (oldItem != null)
        {
            // 1. 이전에 장착되어 있던 아이템을 인벤토리 슬롯에 다시 넣습니다.
            slots[slotIndex] = new InventorySlot(oldItem, 1);
        }
        else
        {
            // 2. 교체된 아이템이 없으면 (새로운 장착) 해당 슬롯을 비웁니다.
            slots[slotIndex] = InventorySlot.Empty;
        }

        // 3. UI 갱신 및 이벤트 발생
        RefreshSlotUI(slotIndex);
    }


    /// <summary>
    /// 하나의 슬롯 UI만 업데이트합니다.
    /// </summary>
    private void UpdateSingleSlotUI(int index)
    {
        if (index >= 0 && index < inventorySlotUIs.Length && inventorySlotUIs[index] != null)
        {
            InventorySlot slotData = slots[index];
            inventorySlotUIs[index].UpdateSlotUI(slotData.itemData, slotData.stackSize);
        }
    }

    /// <summary>
    /// 모든 인벤토리 슬롯 UI를 갱신합니다.
    /// </summary>
    private void RefreshAllInventoryUI()
    {
        for (int i = 0; i < capacity; i++)
        {
            UpdateSingleSlotUI(i);
        }
    }


    // ----------------------------------------------------
    // [핵심 로직: 아이템 추가, 사용, 제거 등]
    // ----------------------------------------------------

    public bool AddItem(ItemBaseSO itemData, int amount)
    {
        if (itemData == null || amount <= 0) return false;

        // 1. 스택 가능한 아이템: 기존 슬롯에 추가
        if (itemData.maxStackSize > 1)
        {
            for (int i = 0; i < capacity; i++)
            {
                if (!slots[i].IsEmpty && slots[i].itemData == itemData && slots[i].stackSize < itemData.maxStackSize)
                {
                    int canAdd = itemData.maxStackSize - slots[i].stackSize;
                    int actualAdd = Mathf.Min(amount, canAdd);

                    slots[i] = new InventorySlot(itemData, slots[i].stackSize + actualAdd);
                    amount -= actualAdd;

                    RefreshSlotUI(i); // UI 갱신 및 이벤트 발생

                    if (amount <= 0) return true; // 모두 추가됨
                }
            }
        }

        // 2. 남은 아이템: 빈 슬롯에 새로 추가
        for (int i = 0; i < capacity; i++)
        {
            if (slots[i].IsEmpty)
            {
                int actualAdd = Mathf.Min(amount, itemData.maxStackSize);

                slots[i] = new InventorySlot(itemData, actualAdd);
                amount -= actualAdd;

                RefreshSlotUI(i); // UI 갱신 및 이벤트 발생

                if (amount <= 0) return true; // 모두 추가됨
            }
        }

        if (amount > 0)
        {
            Debug.LogWarning($"[Inventory] 인벤토리가 가득 찼습니다. {itemData.itemName} {amount}개를 추가하지 못했습니다.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 특정 슬롯의 아이템을 사용합니다.
    /// </summary>
    public bool UseItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= capacity || slots[slotIndex].IsEmpty) return false;

        InventorySlot slot = slots[slotIndex];
        ItemBaseSO item = slot.itemData;

        // 종속성 참조
        PlayerHealthComponent playerHealth = PlayerHealthComponent.Instance;
        EquipmentManager equipmentManagerInstance = EquipmentManager.Instance;

        if (playerHealth == null || equipmentManagerInstance == null)
        {
            Debug.LogError("[Inventory] UseItem 실패: PlayerHealth/EquipmentManager 인스턴스를 찾을 수 없습니다.");
            return false;
        }

        // 1. 아이템 사용 로직 실행 (ItemBaseSO.Use 호출)
        // 이 호출을 통해 EquippableItemSO.Use는 Equip을 시도하고, 인벤토리의 데이터 갱신을 
        // UpdateSlotWithNewEquippedItem(slotIndex, oldItem)을 통해 요청하게 됩니다.
        item.Use(slotIndex, playerHealth, equipmentManagerInstance);

        // 2. 사용 후 데이터 처리 (Consumable만 여기서 직접 처리)
        if (item is ConsumableItemSO)
        {
            // 소비 아이템: 스택 감소
            slots[slotIndex] = new InventorySlot(item, slot.stackSize - 1);
            if (slots[slotIndex].stackSize <= 0)
            {
                slots[slotIndex] = InventorySlot.Empty;
            }

            RefreshSlotUI(slotIndex); // UI 갱신 및 이벤트 발생
            return true;
        }
        else if (item is EquippableItemSO)
        {
            // 장비 아이템: 
            // EquippableItemSO.Use() 내에서 Inventory.UpdateSlotWithNewEquippedItem()을 호출했으므로, 
            // 여기서는 추가적인 처리 없이 성공으로 반환합니다.
            return true;
        }

        return false;
    }

    // (이하 생략된 유틸리티 메서드: IsSlotEmpty 등)
    public bool IsSlotEmpty(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= capacity) return true;
        return slots[slotIndex].IsEmpty;
    }
}