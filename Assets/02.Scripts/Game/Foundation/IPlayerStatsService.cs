using Game.UI.Interface;
using Game.Gameplay; // ✅ StatusEffectType enum 참조용

namespace Game.Foundation
{
    /// <summary>
    /// 플레이어 스탯(체력, 스태미나, 공격력, 상태 이상 등)을 관리하는 서비스 인터페이스입니다.
    /// </summary>
    public interface IPlayerStatsService : IGameService
    {
        float CurrentHealth { get; }
        float GetHealthPercentage();
        float GetStaminaPercentage();
        int GetFinalAttack();

        bool IsEffectActive(StatusEffectType type);

        void ApplyEquipmentModifiers(EquippableItemSO item);
        void RemoveEquipmentModifiers(EquippableItemSO item);

        void Restore(string effectType, float amount);
        void ApplyStatus(StatusEffectType type, float duration);
        void RemoveStatus(StatusEffectType type);
    }
}
