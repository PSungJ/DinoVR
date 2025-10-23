using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Game.Gameplay;
using Game.Foundation;

namespace Game.UI.Interface
{
    /// <summary>
    /// 플레이어 상태(체력, 스태미나, 상태 이상 등)를 UI로 표시합니다.
    /// PlayerStats 서비스에서 데이터를 받아 자동 업데이트됩니다.
    /// </summary>
    public class StatusUI : MonoBehaviour
    {
        [Header("Bar UI Elements")]
        [SerializeField] private Image healthBarFill;
        [SerializeField] private Image staminaBarFill;

        [Header("Status Effect Icons")]
        [SerializeField] private GameObject fractureIcon;
        [SerializeField] private GameObject foodPoisoningIcon;
        [SerializeField] private GameObject fatigueIcon;

        [Header("Text UI Elements")]
        [SerializeField] private TextMeshProUGUI attackValueText;

        private IPlayerStatsService statsService;

        private Dictionary<StatusEffectType, GameObject> statusIcons;

        private void Awake()
        {
            statsService = ServiceLocator.Get<IPlayerStatsService>();

            // 상태 이상 아이콘 매핑
            statusIcons = new Dictionary<StatusEffectType, GameObject>
            {
                { StatusEffectType.Fracture, fractureIcon },
                { StatusEffectType.FoodPoisoning, foodPoisoningIcon },
                { StatusEffectType.Fatigue, fatigueIcon }
            };

            // 초기화: 모든 아이콘 비활성화
            foreach (var icon in statusIcons.Values)
            {
                if (icon != null)
                    icon.SetActive(false);
            }
        }

        private void Start()
        {
            UpdateUI();
        }

        // ----------------------------------------------------
        // [UI 업데이트]
        // ----------------------------------------------------
        public void UpdateUI()
        {
            if (statsService == null)
            {
                Debug.LogWarning("[StatusUI] PlayerStatsService를 찾을 수 없습니다!");
                return;
            }

            // 1. 체력 및 스태미나 바
            if (healthBarFill != null)
                healthBarFill.fillAmount = statsService.GetHealthPercentage();

            if (staminaBarFill != null)
                staminaBarFill.fillAmount = statsService.GetStaminaPercentage();

            // 2. 상태 이상 아이콘
            foreach (var kvp in statusIcons)
            {
                if (kvp.Value != null)
                    kvp.Value.SetActive(statsService.IsEffectActive(kvp.Key));
            }

            // 3. 공격력 텍스트
            if (attackValueText != null)
                attackValueText.text = statsService.FinalAttack.ToString();
        }
    }
}
