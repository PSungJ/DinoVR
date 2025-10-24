using System;
using System.Collections.Generic;

namespace Game.Foundation
{
    /// <summary>
    /// 전역적인 게임 서비스 접근 관리용 싱글톤.
    /// 예: ServiceLocator.Get<IInventoryService>().UseItem(0);
    /// </summary>
    public static class ServiceLocator
    {
        /// <summary>
        /// 모든 서비스 인스턴스를 저장하는 딕셔너리.
        /// </summary>
        private static readonly Dictionary<Type, IGameService> _services = new();

        /// <summary>
        /// 서비스 등록
        /// </summary>
        public static void Register<T>(T service) where T : class, IGameService
        {
            if (service == null)
            {
                UnityEngine.Debug.LogError($"[ServiceLocator] Attempted to register null service of type {typeof(T)}");
                return;
            }

            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                UnityEngine.Debug.LogWarning($"[ServiceLocator] Service {type.Name} is already registered. Overwriting...");
                _services[type] = service;
            }
            else
            {
                _services.Add(type, service);
            }

            UnityEngine.Debug.Log($"[ServiceLocator] Registered service: {type.Name}");
        }

        /// <summary>
        /// 서비스 해제
        /// </summary>
        public static void Unregister<T>(T service) where T : class, IGameService
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                _services.Remove(type);
                UnityEngine.Debug.Log($"[ServiceLocator] Unregistered service: {type.Name}");
            }
        }

        /// <summary>
        /// 등록된 서비스 가져오기
        /// </summary>
        public static T Get<T>() where T : class, IGameService
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out IGameService service))
            {
                return service as T;
            }

            UnityEngine.Debug.LogWarning($"[ServiceLocator] Service of type {type.Name} not found!");
            return null;
        }

        /// <summary>
        /// 모든 서비스 초기화 해제 (씬 전환 시 호출 가능)
        /// </summary>
        public static void ClearAll()
        {
            _services.Clear();
            UnityEngine.Debug.Log("[ServiceLocator] Cleared all registered services.");
        }
    }
}
