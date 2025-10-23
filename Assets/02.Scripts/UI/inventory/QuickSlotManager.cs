using UnityEngine;
using UnityEngine.InputSystem;
using Game.Foundation;
using Game.Gameplay;

namespace Game.InventorySystem
{
    /// <summary>
    /// 퀵슬롯 시스템: 플레이어가 빠르게 아이템을 선택/사용할 수 있게 관리.
    /// </summary>
    public class QuickSlotManager : MonoBehaviour, IQuickSlotService
    {
        [Header("QuickSlot Settings")]
        [SerializeField] private int quickSlotStartIndex = 30;
        [SerializeField] private int quickSlotCount = 3;

        [Header("References")]
        [SerializeField] private SlotUIUpdater[] quickSlotUIs;
        [SerializeField] private GameObject quickSlotRootUI;
        [SerializeField] private InputActionProperty cycleSlotAction;
        [SerializeField] private InputActionProperty useSlotAction;

        private InventorySlot[] quickSlots;
        private int selectedIndex = 0;

        private void Awake()
        {
            ServiceLocator.Register<IQuickSlotService>(this);

            quickSlots = new InventorySlot[quickSlotCount];
            for (int i = 0; i < quickSlotCount; i++)
                quickSlots[i] = InventorySlot.Empty;

            RefreshUI();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IQuickSlotService>(this);
        }

        private void OnEnable()
        {
            if (cycleSlotAction.action != null)
                cycleSlotAction.action.performed += OnCycleSlot;

            if (useSlotAction.action != null)
                useSlotAction.action.performed += OnUseSlot;

            cycleSlotAction.action?.Enable();
            useSlotAction.action?.Enable();
        }

        private void OnDisable()
        {
            cycleSlotAction.action?.Disable();
            useSlotAction.action?.Disable();

            if (cycleSlotAction.action != null)
                cycleSlotAction.action.performed -= OnCycleSlot;

            if (useSlotAction.action != null)
                useSlotAction.action.performed -= OnUseSlot;
        }

        // --------------------------------------------------------
        // [슬롯 순환 및 사용 입력 처리]
        // --------------------------------------------------------
        private void OnCycleSlot(InputAction.CallbackContext ctx)
        {
            selectedIndex = (selectedIndex + 1) % quickSlotCount;
            RefreshUI();
        }

        private void OnUseSlot(InputAction.CallbackContext ctx)
        {
            UseSelectedSlot();
        }

        // --------------------------------------------------------
        // [핵심 기능]
        // --------------------------------------------------------
        public void UseSelectedSlot()
        {
            var slot = quickSlots[selectedIndex];
            if (slot.IsEmpty)
            {
                Debug.Log("[QuickSlot] 현재 슬롯이 비어있습니다.");
                return;
            }

            var inventory = ServiceLocator.Get<IInventoryService>();
            if (inventory == null)
            {
                Debug.LogError("[QuickSlot] InventoryService를 찾을 수 없습니다.");
                return;
            }

            // 인벤토리 아이템 사용 로직 호출
            inventory.UseItemByData(slot.itemData);
            RefreshUI();
        }

        public void SetQuickSlot(int index, InventorySlot slot)
        {
            if (index < 0 || index >= quickSlotCount)
                return;

            quickSlots[index] = slot;
            RefreshUI();
        }

        public InventorySlot GetQuickSlot(int index)
        {
            if (index < 0 || index >= quickSlotCount)
                return InventorySlot.Empty;
            return quickSlots[index];
        }

        public bool IsQuickSlotEmpty(int index)
        {
            if (index < 0 || index >= quickSlotCount)
                return true;
            return quickSlots[index].IsEmpty;
        }

        public bool IsQuickSlotIndex(int absoluteIndex)
        {
            return absoluteIndex >= quickSlotStartIndex &&
                   absoluteIndex < quickSlotStartIndex + quickSlotCount;
        }

        // --------------------------------------------------------
        // [UI 갱신]
        // --------------------------------------------------------
        public void RefreshUI()
        {
            if (quickSlotUIs == null) return;

            for (int i = 0; i < quickSlotUIs.Length; i++)
            {
                if (quickSlotUIs[i] == null) continue;
                var slot = quickSlots[i];
                quickSlotUIs[i].UpdateSlotUI(slot.itemData, slot.stackSize);
                quickSlotUIs[i].SetHighlight(i == selectedIndex);
            }

            if (quickSlotRootUI != null)
                quickSlotRootUI.SetActive(true);
        }
    }
}
