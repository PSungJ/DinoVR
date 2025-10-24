using UnityEngine;
using System.Collections.Generic;
using Game.Gameplay;
using Game.InventorySystem;

namespace Game.UI
{
    /// <summary>
    /// 퀵슬롯 UI를 관리하고, QuickSlotManager에서 받은 데이터를 반영합니다.
    /// </summary>
    public class QuickSlotUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("QuickSlotManager 인스턴스 (ServiceLocator를 통해 참조 가능)")]
        [SerializeField] private GameObject quickSlotRoot;
        [SerializeField] private List<SlotUIUpdater> slotUpdaters = new List<SlotUIUpdater>(3);
        [SerializeField] private RectTransform selectionIndicator;

        private int currentIndex = -1;

        /// <summary>
        /// 퀵슬롯 전체 UI를 갱신합니다.
        /// </summary>
        public void UpdateAllSlots(InventorySlot[] slots)
        {
            if (slots == null || slotUpdaters == null) return;

            for (int i = 0; i < Mathf.Min(slots.Length, slotUpdaters.Count); i++)
            {
                var updater = slotUpdaters[i];
                if (updater == null) continue;

                updater.UpdateSlotUI(slots[i].itemData, slots[i].stackSize);
                updater.SetHighlight(i == currentIndex);
            }
        }

        /// <summary>
        /// 선택 인디케이터를 이동시켜 현재 선택된 슬롯을 시각적으로 표시합니다.
        /// </summary>
        public void UpdateSelection(int newIndex)
        {
            currentIndex = newIndex;

            // 하이라이트 갱신
            for (int i = 0; i < slotUpdaters.Count; i++)
            {
                if (slotUpdaters[i] != null)
                    slotUpdaters[i].SetHighlight(i == currentIndex);
            }

            // 인디케이터 이동
            if (selectionIndicator != null && currentIndex >= 0 && currentIndex < slotUpdaters.Count)
            {
                selectionIndicator.position = slotUpdaters[currentIndex].transform.position;
                selectionIndicator.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// 퀵슬롯 UI 전체를 숨기거나 표시합니다.
        /// </summary>
        public void SetVisible(bool isVisible)
        {
            if (quickSlotRoot != null)
                quickSlotRoot.SetActive(isVisible);
        }
    }
}
