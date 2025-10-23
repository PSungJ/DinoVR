using System;
using System.Collections.Generic;
using Game.UI.Interface;

namespace Game.Foundation
{
    /// <summary>
    /// 게임 전역 서비스 등록 / 조회 시스템.
    /// 싱글톤 패턴을 대체하는 안전한 의존성 주입 방식입니다.
    /// </summary>
    public static class ServiceLocator
    {
        // 등록된 서비스 저장소
        private static readonly Dictionary<Type, IGameService> _services = new();

        /// <summary>
        /// 서비스 등록 (예: ServiceLocator.Register<IInventoryService>(this))
        /// </summary>
        public static void Register<T>(T service) where T : class, IGameService
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                _services[type] = service;
            }
            else
            {
                _services.Add(type, service);
            }
        }

        /// <summary>
        /// 서비스 해제
        /// </summary>
        public static void Unregister<T>(T service) where T : class, IGameService
        {
            var type = typeof(T);
            if (_services.ContainsKey(type) && _services[type] == (object)service)
            {
                _services.Remove(type);
            }
        }

        /// <summary>
        /// 등록된 서비스 가져오기 (없으면 오류 로그)
        /// </summary>
        public static T Get<T>() where T : class, IGameService
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out IGameService service))
            {
                return service as T;
            }

            UnityEngine.Debug.LogError($"[ServiceLocator] 서비스 {type.Name}을(를) 찾을 수 없습니다.");
            return null;
        }

        /// <summary>
        /// 모든 서비스 초기화
        /// </summary>
        public static void Clear()
        {
            _services.Clear();
        }
    }
}
