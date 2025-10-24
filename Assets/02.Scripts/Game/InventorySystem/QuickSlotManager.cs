using UnityEngine;
using UnityEngine.InputSystem;
using Game.Foundation;
using Game.Gameplay;
using Game.UI;

namespace Game.InventorySystem
{
    /// <summary>
    /// 인벤토리의 퀵슬롯 3칸을 관리하며, 입력 및 아이템 사용을 담당.
    /// </summary>
    public class QuickSlotManager : MonoBehaviour, IQuickSlotService
    {
        [Header("QuickSlot Setup")]
        [SerializeField] private int quickSlotStartIndex = 30;
        [SerializeField] private int quickSlotCount = 3;

        [Header("UI References")]
        [SerializeField] private SlotUIUpdater[] quickSlotUIs;
        [SerializeField] private GameObject inventoryUIRoot;

        [Header("XR Input Actions (Left Hand)")]
        [SerializeField] private InputActionProperty toggleInventoryAction;
        [SerializeField] private InputActionProperty useQuickSlotAction;
        [SerializeField] private InputActionProperty cycleQuickSlotAction;

        private int selectedIndex = 0;
        private InventorySlot[] quickSlots;

        private IInventoryService inventoryService;
        private IPlayerHealthService playerHealth;
        private IEquipmentService equipmentService;

        private void Awake()
        {
            ServiceLocator.Register<IQuickSlotService>(this);

            quickSlots = new InventorySlot[quickSlotCount];
            for (int i = 0; i < quickSlotCount; i++)
                quickSlots[i] = InventorySlot.Empty;
        }

        private void Start()
        {
            inventoryService = ServiceLocator.Get<IInventoryService>();
            playerHealth = ServiceLocator.Get<IPlayerHealthService>();
            equipmentService = ServiceLocator.Get<IEquipmentService>();

            BindInput();
            RefreshAllQuickSlotUI();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IQuickSlotService>(this);
            UnbindInput();
        }

        // ------------------------------------------------------------
        // XR Input Binding
        // ------------------------------------------------------------
        private void BindInput()
        {
            if (toggleInventoryAction.action != null)
                toggleInventoryAction.action.performed += _ => ToggleInventoryUI();

            if (useQuickSlotAction.action != null)
                useQuickSlotAction.action.performed += _ => UseSelectedQuickSlotItem();

            if (cycleQuickSlotAction.action != null)
                cycleQuickSlotAction.action.performed += _ => CycleNextSlot();

            toggleInventoryAction.action?.Enable();
            useQuickSlotAction.action?.Enable();
            cycleQuickSlotAction.action?.Enable();
        }

        private void UnbindInput()
        {
            toggleInventoryAction.action?.Disable();
            useQuickSlotAction.action?.Disable();
            cycleQuickSlotAction.action?.Disable();
        }

        // ------------------------------------------------------------
        // UI / Input Logic
        // ------------------------------------------------------------
        public void ToggleInventoryUI()
        {
            if (inventoryUIRoot == null) return;

            bool next = !inventoryUIRoot.activeSelf;
            inventoryUIRoot.SetActive(next);

            Debug.Log($"[QuickSlot] Inventory UI toggled: {next}");
        }

        public void CycleNextSlot()
        {
            selectedIndex = (selectedIndex + 1) % quickSlotCount;
            RefreshQuickSlotHighlights();
        }

        public void UseSelectedQuickSlotItem()
        {
            var slot = quickSlots[selectedIndex];
            if (slot.IsEmpty)
            {
                Debug.Log("[QuickSlot] Empty slot.");
                return;
            }

            if (slot.itemData.itemType != ItemType.Consumable)
            {
                Debug.LogWarning("[QuickSlot] Only consumables can be used from quick slots.");
                return;
            }

            if (UseItemFromQuickSlot(selectedIndex))
                Debug.Log($"[QuickSlot] Used item in slot {selectedIndex}");
        }

        private bool UseItemFromQuickSlot(int index)
        {
            var slot = quickSlots[index];
            if (slot.IsEmpty) return false;

            if (slot.itemData is not ConsumableItemSO consumable)
                return false;

            consumable.Use(index, playerHealth, equipmentService);

            slot.stackSize--;
            quickSlots[index] = slot.stackSize <= 0 ? InventorySlot.Empty : slot;

            RefreshAllQuickSlotUI();
            return true;
        }

        // ------------------------------------------------------------
        // UI Refresh
        // ------------------------------------------------------------
        private void RefreshQuickSlotHighlights()
        {
            for (int i = 0; i < quickSlotUIs.Length; i++)
            {
                if (quickSlotUIs[i] != null)
                    quickSlotUIs[i].SetHighlight(i == selectedIndex);
            }
        }

        public void RefreshAllQuickSlotUI()
        {
            for (int i = 0; i < quickSlotCount && i < quickSlotUIs.Length; i++)
            {
                var ui = quickSlotUIs[i];
                if (ui != null)
                    ui.UpdateSlotUI(quickSlots[i].itemData, quickSlots[i].stackSize);
            }

            RefreshQuickSlotHighlights();
        }

        // ------------------------------------------------------------
        // IQuickSlotService 구현부
        // ------------------------------------------------------------
        public bool IsQuickSlotIndex(int index)
        {
            return index >= quickSlotStartIndex && index < quickSlotStartIndex + quickSlotCount;
        }

        public bool IsQuickSlotEmpty(int index)
        {
            if (!IsQuickSlotIndex(index)) return true;
            int relative = index - quickSlotStartIndex;
            return quickSlots[relative].IsEmpty;
        }

        public bool HandleQuickSlotUse(int absoluteIndex)
        {
            int relative = absoluteIndex - quickSlotStartIndex;
            if (relative < 0 || relative >= quickSlotCount)
                return false;

            return UseItemFromQuickSlot(relative);
        }

        public void HandleInventorySwap(int indexA, int indexB)
        {
            bool aIsQuick = IsQuickSlotIndex(indexA);
            bool bIsQuick = IsQuickSlotIndex(indexB);

            if (!aIsQuick && !bIsQuick)
                return;

            int aInternal = aIsQuick ? indexA - quickSlotStartIndex : indexA;
            int bInternal = bIsQuick ? indexB - quickSlotStartIndex : indexB;

            InventorySlot slotA = aIsQuick ? quickSlots[aInternal] : inventoryService.GetSlot(aInternal);
            InventorySlot slotB = bIsQuick ? quickSlots[bInternal] : inventoryService.GetSlot(bInternal);

            // 퀵슬롯 진입 제한 (비소비형 금지)
            if (!slotA.IsEmpty && bIsQuick && slotA.itemData.itemType != ItemType.Consumable)
            {
                Debug.LogWarning("[QuickSlot] Non-consumable cannot be assigned.");
                return;
            }

            if (!slotB.IsEmpty && aIsQuick && slotB.itemData.itemType != ItemType.Consumable)
            {
                Debug.LogWarning("[QuickSlot] Non-consumable cannot be assigned.");
                return;
            }

            // 교환
            if (aIsQuick) quickSlots[aInternal] = slotB;
            else inventoryService.SetSlot(aInternal, slotB);

            if (bIsQuick) quickSlots[bInternal] = slotA;
            else inventoryService.SetSlot(bInternal, slotA);

            RefreshAllQuickSlotUI();
            inventoryService.RefreshAllInventoryUI();

            Debug.Log($"[QuickSlot] Swap complete ({indexA} <-> {indexB})");
        }
    }
}
