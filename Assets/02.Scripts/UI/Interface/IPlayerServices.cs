using Game.Gameplay; // StatusEffectType enum이 이 네임스페이스에 있다고 가정

namespace Game.UI.Interface
{
    public interface IPlayerStatsService : IGameService
    {
        event System.Action OnStatsChanged;
        float HealthPercent { get; }
        float StaminaPercent { get; }
        int AttackPower { get; }
        bool IsEffectActive(StatusEffectType type);
    }

    public interface IPlayerHealthService : IGameService
    {
        void Heal(int amount);
        void TakeDamage(int amount);
        void RestoreMana(int amount);
    }
}
