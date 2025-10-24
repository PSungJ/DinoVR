using Game.UI.Interface;

namespace Game.Foundation
{
    /// <summary>
    /// 플레이어의 체력 및 마나 회복, 피해 처리를 담당하는 서비스 인터페이스입니다.
    /// </summary>
    public interface IPlayerHealthService : IGameService
    {
        int CurrentHealth { get; }
        int MaxHealth { get; }
        int CurrentMana { get; }
        int MaxMana { get; }

        void Heal(int amount);
        void TakeDamage(int amount);
        void RestoreMana(int amount);
    }
}
