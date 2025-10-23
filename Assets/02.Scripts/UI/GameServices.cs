// GameServices.cs
// 프로젝트 전역에서 공통적으로 사용할 서비스 로케이터와 인터페이스 정의
// - 싱글톤 남용을 줄이고, 초기화/테스트/주입을 유연하게 합니다.
// - 기존 Instance 패턴과 병행 사용 가능: 각 매니저 Awake에서 Register 호출만 추가하면 됩니다.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Foundation
{
    /// <summary>
    /// 마커 인터페이스: 모든 게임 서비스는 이를 구현합니다.
    /// </summary>
    public interface IGameService { }

    /// <summary>
    /// 아이템 소비(Use) 행위를 노출하는 공통 인터페이스.
    /// 인벤토리/퀵슬롯 모두 이 규약을 따르게 하여 중복 로직 제거/호환성 확보.
    /// </summary>
    public interface IItemConsumer : IGameService
    {
        /// <summary>
        /// 해당 슬롯 인덱스의 아이템 사용 시도. 성공 여부 반환.
        /// 인덱스는 구현체 기준(인벤토리/퀵슬롯)에 맞게 해석합니다.
        /// </summary>
        bool TryUseItem(int slotIndex);
    }

    /// <summary>
    /// 인벤토리 데이터/이벤트를 노출하는 인터페이스(필요 최소한).
    /// </summary>
    public interface IInventoryService : IGameService
    {
        int Capacity { get; }
        bool IsSlotEmpty(int index);
        InventorySlot GetSlot(int index);
        void SetSlot(int index, InventorySlot slot);
        event Action<int, InventorySlot> OnSlotChanged;
        void RefreshAllInventoryUI(); // 점진적 마이그레이션용(초기엔 그대로 호출)
    }

    /// <summary>
    /// 장비 장착/해제 서비스를 노출.
    /// </summary>
    public interface IEquipmentService : IGameService
    {
        EquippableItemSO Equip(EquippableItemSO itemToEquip, int inventorySlotIndex);
        EquippableItemSO Unequip(EquipSlotType slotType);
    }

    /// <summary>
    /// 플레이어 체력/마나/피해 처리를 노출.
    /// </summary>
    public interface IPlayerHealthService : IGameService
    {
        void Heal(int amount);
        void TakeDamage(int amount);
        void RestoreMana(int amount);
    }

    /// <summary>
    /// 아주 가벼운 서비스 로케이터.
    /// - 씬 초기화 순서 이슈를 줄이고, 테스트/모킹을 쉽게 합니다.
    /// - Register/Unregister는 각 매니저의 Awake/OnDestroy에서 호출합니다.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, IGameService> _services = new();

        public static void Register<T>(T service) where T : class, IGameService
        {
            var key = typeof(T);
            _services[key] = service;
#if UNITY_EDITOR
            Debug.Log($"[ServiceLocator] Registered: {key.Name} → {service.GetType().Name}");
#endif
        }

        public static void Unregister<T>(T service) where T : class, IGameService
        {
            var key = typeof(T);
            if (_services.TryGetValue(key, out var existing) && ReferenceEquals(existing, service))
            {
                _services.Remove(key);
#if UNITY_EDITOR
                Debug.Log($"[ServiceLocator] Unregistered: {key.Name}");
#endif
            }
        }

        /// <summary>
        /// 등록된 서비스 가져오기. 등록되지 않았으면 null 반환(안전).
        /// </summary>
        public static T GetOrNull<T>() where T : class, IGameService
        {
            _services.TryGetValue(typeof(T), out var svc);
            return svc as T;
        }

        /// <summary>
        /// 반드시 있어야 하는 경우 사용. 없으면 에러 로그(에디터)/예외(옵션)로 빠르게 문제 파악.
        /// </summary>
        public static T Get<T>() where T : class, IGameService
        {
            var svc = GetOrNull<T>();
            if (svc == null)
            {
#if UNITY_EDITOR
                Debug.LogError($"[ServiceLocator] Service not found: {typeof(T).Name}. Ensure it is registered in Awake().");
#endif
            }
            return svc;
        }
    }
}
