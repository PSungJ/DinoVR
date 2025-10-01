using UnityEngine;

[CreateAssetMenu(fileName = "NewConsumableItem", menuName = "Inventory/Consumable Item")]
public class ConsumableItemSO : ItemBaseSO
{
    [Header("Consumption Effect")]
    [Tooltip("회복 효과의 종류 (예: Health, Stamina)")]
    public string effectType = "Health";

    [Tooltip("회복량 또는 효과 지속 시간")]
    public float restoreAmount = 25f;

    [Header("Status Effects (Optional)")]
    // 이 소모품 사용 시 제거할 상태 이상 (예: 통증 제거제)
    public StatusEffectType statusToRemove = StatusEffectType.Fatigue;
    [Tooltip("제거할 상태 이상이 없을 경우 None으로 설정")]
    public bool clearsStatus = false;

    private void OnEnable()
    {
        // 소모품은 스택 가능 (기본 99개)
        maxStackSize = 99;
        itemType = ItemType.Consumable;
    }
}