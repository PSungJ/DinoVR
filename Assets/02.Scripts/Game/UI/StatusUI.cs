using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Game.Gameplay;
using Game.Foundation;

namespace Game.UI
{
    /// <summary>
    /// 플레이어의 체력, 스태미나, 상태 이상을 UI로 표시합니다.
    /// </summary>
    public class StatusUI : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private Image healthBarFill;
        [SerializeField] private Image staminaBarFill;

        [Header("Status Effect Icons")]
        [SerializeField] private GameObject fractureIcon;
        [SerializeField] private GameObject foodPoisoningIcon;
        [SerializeField] private GameObject fatigueIcon;

        [Header("Optional Texts")]
        [SerializeField] private TextMeshProUGUI attackValueText;

        private Dictionary<StatusEffectType, GameObject> statusIconMap;
        private IPlayerStatsService playerStats;

        private void Awake()
        {
            playerStats = ServiceLocator.Get<IPlayerStatsService>();

            // 상태 이펙트 타입별 아이콘 매핑
            statusIconMap = new Dictionary<StatusEffectType, GameObject>
            {
                { StatusEffectType.Fracture, fractureIcon },
                { StatusEffectType.FoodPoisoning, foodPoisoningIcon },
                { StatusEffectType.Fatigue, fatigueIcon }
            };

            // 모든 아이콘을 초기 비활성화
            foreach (var icon in statusIconMap.Values)
            {
                if (icon != null) icon.SetActive(false);
            }
        }

        private void Start()
        {
            UpdateUI();
        }

        /// <summary>
        /// 플레이어 스탯 상태를 UI에 반영합니다.
        /// </summary>
        public void UpdateUI()
        {
            if (playerStats == null) return;

            // 1️⃣ 체력 / 스태미나 바
            if (healthBarFill != null)
                healthBarFill.fillAmount = playerStats.GetHealthPercentage();

            if (staminaBarFill != null)
                staminaBarFill.fillAmount = playerStats.GetStaminaPercentage();

            // 2️⃣ 상태 이상 아이콘
            foreach (var pair in statusIconMap)
            {
                if (pair.Value != null)
                {
                    bool isActive = playerStats.IsEffectActive(pair.Key);
                    pair.Value.SetActive(isActive);
                }
            }

            // 3️⃣ 공격력 텍스트 (선택적)
            if (attackValueText != null)
                attackValueText.text = playerStats.GetFinalAttack().ToString();
        }
    }
}
