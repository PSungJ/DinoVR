using UnityEngine;
using Game.Foundation;
using Game.InventorySystem;
using Game.Gameplay;

namespace Game.Debugging
{
    /// <summary>
    /// ServiceLocator가 모든 게임 서비스를 정상적으로 등록했는지 테스트하고,
    /// 간단한 디버그 입력으로 아이템 사용, 회복, 장비 테스트를 수행합니다.
    /// </summary>
    public class GameServiceDebugger : MonoBehaviour
    {
        private IInventoryService inventory;
        private IQuickSlotService quickSlots;
        private IEquipmentService equipment;
        private IPlayerHealthService playerHealth;
        private IPlayerStatsService playerStats;

        private void Start()
        {
            inventory = ServiceLocator.Get<IInventoryService>();
            quickSlots = ServiceLocator.Get<IQuickSlotService>();
            equipment = ServiceLocator.Get<IEquipmentService>();
            playerHealth = ServiceLocator.Get<IPlayerHealthService>();
            playerStats = ServiceLocator.Get<IPlayerStatsService>();

            if (inventory == null || quickSlots == null || equipment == null || playerHealth == null)
                Debug.LogWarning("⚠️ [Debugger] Some services are missing! Check GameBootstrapper references.");
            else
                Debug.Log("✅ [Debugger] All core services detected!");
        }

        private void Update()
        {
            // 테스트용 단축키 (Editor / Keyboard Debug)
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Debug.Log("🧪 Testing: Use Item at Slot 0");
                inventory?.UseItem(0);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Debug.Log("🧪 Testing: Cycle QuickSlot");
                quickSlots?.CycleNextSlot();
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                Debug.Log("🧪 Testing: Heal Player +25");
                playerHealth?.Heal(25);
            }

            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                Debug.Log("🧪 Testing: Print Player Attack Stat");
                if (playerStats != null)
                    Debug.Log($"[PlayerStats] Final Attack = {playerStats.GetFinalAttack()}");
            }

            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                Debug.Log("🧪 Testing: Toggle Inventory UI");
                quickSlots?.ToggleInventoryUI();
            }
        }
    }
}
