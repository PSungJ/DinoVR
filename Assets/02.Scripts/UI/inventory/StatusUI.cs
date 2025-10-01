using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// public enum StatusEffectType
// {
//     Fracture,      // 골절
//     FoodPoisoning, // 식중
//     Fatigue        // 피로
// }

public class StatusUI : MonoBehaviour
{
    [Header("Dependencies")]
    // PlayerStats 참조 필수 (데이터를 가져올 곳)
    [SerializeField] private PlayerStats playerStats;

    [Header("Row 1 & 2: Health & Stamina Bars")]
    // Image 컴포넌트 (Image Type: Filled) 할당
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image staminaBarFill;

    [Header("Row 3: Status Icons")]
    // 상태 이상 아이콘 GameObject 할당 (활성화/비활성화 처리용)
    [SerializeField] private GameObject fractureIcon;
    [SerializeField] private GameObject foodPoisoningIcon;
    [SerializeField] private GameObject fatigueIcon;

    // 최종 공격력 텍스트 (옵션)
    [SerializeField] private Text attackValueText;

    // 상태 이상 아이콘을 일관성 있게 관리하기 위한 Dictionary
    private Dictionary<StatusEffectType, GameObject> statusIconMap;


    private void Awake()
    {
        // PlayerStats에 정의된 StatusEffectType을 문제 없이 사용할 수 있습니다.
        statusIconMap = new Dictionary<StatusEffectType, GameObject>
        {
            { StatusEffectType.Fracture, fractureIcon },
            { StatusEffectType.FoodPoisoning, foodPoisoningIcon },
            { StatusEffectType.Fatigue, fatigueIcon }
        };

        // 초기에는 모든 아이콘을 숨깁니다.
        foreach (var icon in statusIconMap.Values)
        {
            if (icon != null) icon.SetActive(false);
        }
    }

    private void Start()
    {
        // 초기 UI 상태를 한 번 업데이트합니다.
        if (playerStats != null)
        {
            UpdateUI();
        }
    }

    // 💡 핵심: UI 업데이트 함수 (PlayerStats의 변화 시 호출됨)
    public void UpdateUI()
    {
        if (playerStats == null) return;
        
        // 1. 체력 및 스태미나 바 업데이트
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = playerStats.GetHealthPercentage();
        }
// TODO: GetStaminaPercentage() 함수 구현 시 주석 해제 필요
// if (staminaBarFill != null)
// {
//     staminaBarFill.fillAmount = playerStats.GetStaminaPercentage();
// }

// 2. 상태 이상 아이콘 업데이트
foreach (var pair in statusIconMap)
{
    if (pair.Value != null)
    {
        // PlayerStats에서 해당 상태 이상이 활성 상태인지 확인합니다.
        bool isActive = playerStats.IsEffectActive(pair.Key);
        pair.Value.SetActive(isActive);
    }
}

// 3. 최종 공격력 텍스트 업데이트
if (attackValueText != null)
{
    attackValueText.text = playerStats.GetFinalAttack().ToString();
}
    }
}