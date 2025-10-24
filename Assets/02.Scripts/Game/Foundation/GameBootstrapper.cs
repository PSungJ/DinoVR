using UnityEngine;
using Game.Foundation;
using Game.InventorySystem;
using Game.Gameplay;

namespace Game.Initialization
{
    /// <summary>
    /// 모든 글로벌 서비스(Inventory, QuickSlot, Equipment, Player 등)를
    /// 실행 시 ServiceLocator에 자동 등록하는 부트스트랩 컴포넌트입니다.
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        [Header("Core Services")]
        [Tooltip("인벤토리 시스템 참조 (필수)")]
        [SerializeField] private Inventory inventory;

        [Tooltip("퀵슬롯 매니저 참조 (필수)")]
        [SerializeField] private QuickSlotManager quickSlotManager;

        [Tooltip("장비 매니저 참조 (필수)")]
        [SerializeField] private EquipmentManager equipmentManager;

        [Tooltip("플레이어 체력 컴포넌트 참조 (필수)")]
        [SerializeField] private PlayerHealthComponent playerHealth;

        [Tooltip("플레이어 스탯 컴포넌트 참조 (선택 사항)")]
        [SerializeField] private PlayerStats playerStats;

        [Header("Settings")]
        [Tooltip("게임 실행 시 자동 초기화할지 여부")]
        [SerializeField] private bool initializeOnAwake = true;

        private void Awake()
        {
            if (initializeOnAwake)
            {
                InitializeGameServices();
            }
        }

        /// <summary>
        /// 모든 주요 서비스들을 ServiceLocator에 등록합니다.
        /// </summary>
        public void InitializeGameServices()
        {
            Debug.Log("🎮 [GameBootstrapper] Initializing all core game services...");

            if (inventory != null)
                ServiceLocator.Register<IInventoryService>(inventory);
            else
                Debug.LogWarning("[GameBootstrapper] Inventory reference is missing!");

            if (quickSlotManager != null)
                ServiceLocator.Register<IQuickSlotService>(quickSlotManager);
            else
                Debug.LogWarning("[GameBootstrapper] QuickSlotManager reference is missing!");

            if (equipmentManager != null)
                ServiceLocator.Register<IEquipmentService>(equipmentManager);
            else
                Debug.LogWarning("[GameBootstrapper] EquipmentManager reference is missing!");

            if (playerHealth != null)
                ServiceLocator.Register<IPlayerHealthService>(playerHealth);
            else
                Debug.LogWarning("[GameBootstrapper] PlayerHealthComponent reference is missing!");

            if (playerStats != null)
                ServiceLocator.Register<IPlayerStatsService>(playerStats);

            Debug.Log("✅ [GameBootstrapper] Core services successfully registered!");
        }

        private void OnDestroy()
        {
            // 모든 서비스 해제 (씬 전환 시 안전하게 정리)
            ServiceLocator.Unregister<IInventoryService>(inventory);
            ServiceLocator.Unregister<IQuickSlotService>(quickSlotManager);
            ServiceLocator.Unregister<IEquipmentService>(equipmentManager);
            ServiceLocator.Unregister<IPlayerHealthService>(playerHealth);
            ServiceLocator.Unregister<IPlayerStatsService>(playerStats);

            Debug.Log("🧹 [GameBootstrapper] Services unregistered on destroy.");
        }
    }
}
