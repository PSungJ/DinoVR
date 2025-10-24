namespace Game.Gameplay
{
    /// <summary>
    /// 플레이어에게 적용될 수 있는 상태 이상 종류.
    /// UI 및 PlayerStats 시스템이 참조합니다.
    /// </summary>
    public enum StatusEffectType
    {
        Fracture,       // 골절 (이동 속도 저하)
        FoodPoisoning,  // 식중독 (지속 피해)
        Fatigue         // 피로 (회복 속도 저하)
    }
}
