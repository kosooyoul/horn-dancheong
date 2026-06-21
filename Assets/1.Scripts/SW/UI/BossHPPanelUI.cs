using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KD; // KD.BattleUnit 참조용

namespace HornDancheong.Seongwoo.UI
{
    /// <summary>
    /// 보스의 체력바 및 보스 이름을 제어하는 UI 컴포넌트입니다.
    /// Panel_BossHP 오브젝트에 부착하여 사용합니다.
    /// </summary>
    public class BossHPPanelUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [Tooltip("보스의 체력 슬라이더 바 (BossSliderBar/BossHPBar)")]
        [SerializeField] private Slider bossSliderBar;

        [Tooltip("보스의 이름을 표시할 텍스트")]
        [SerializeField] private TMP_Text bossNameText;

        [Tooltip("보스의 현재 체력을 표시할 텍스트 (Text_BossCurrentHPText) (옵션)")]
        [SerializeField] private TMP_Text currentHpText;

        [Tooltip("보스의 최대 체력을 표시할 텍스트 (Text_BossMaxHPText) (옵션)")]
        [SerializeField] private TMP_Text maxHpText;

        [Tooltip("보스의 체력 비율을 표시할 텍스트 (예: 50 / 100) (옵션)")]
        [SerializeField] private TMP_Text hpRatioText;

        private TacticalBattleManager battleManager;

        // 캐싱된 상태 값 (이전 데이터와 같을 경우 UI 갱신을 생략하여 최적화)
        private float lastHP = -1f;
        private float lastMaxHP = -1f;
        private string lastBossName = "";
        private bool isInitialized = false;

        private void Start()
        {
            // 씬 내의 TacticalBattleManager 탐색 및 턴 이벤트 바인딩
            battleManager = FindObjectOfType<TacticalBattleManager>();
            if (battleManager != null)
            {
                battleManager.OnTurnUpdated += AutoRefreshBossUI;
            }

            // 초기 자동 갱신 시도
            AutoRefreshBossUI();
        }

        private void OnDestroy()
        {
            if (battleManager != null)
            {
                battleManager.OnTurnUpdated -= AutoRefreshBossUI;
            }
        }

        /// <summary>
        /// 턴이 변경될 때 자동으로 감지되는 첫 번째 적의 체력과 이름을 갱신합니다.
        /// </summary>
        public void AutoRefreshBossUI()
        {
            if (battleManager == null) return;

            // 전투에 참여 중인 첫 번째 살아있는 적 탐색
            BattleUnit bossUnit = battleManager.GetFirstEnemyUnit();
            if (bossUnit == null)
            {
                // 감지되는 적이 없거나 사망한 경우 리턴 (또는 연동하지 않음)
                return;
            }

            float currentHp = bossUnit.CurrentHP;
            float maxHp = bossUnit.Stats.maxHP;
            string bossName = "도깨비";

            // 값 변화가 없다면 UI 갱신을 생략 (성능 최적화)
            if (isInitialized && 
                Mathf.Approximately(lastHP, currentHp) && 
                Mathf.Approximately(lastMaxHP, maxHp) && 
                lastBossName == bossName)
            {
                return;
            }

            // 값 갱신
            lastHP = currentHp;
            lastMaxHP = maxHp;
            lastBossName = bossName;
            isInitialized = true;

            // 1. 이름 갱신
            if (bossNameText != null)
            {
                bossNameText.text = bossName;
            }

            // 2. 슬라이더 바 값 갱신
            if (bossSliderBar != null)
            {
                bossSliderBar.maxValue = maxHp;
                bossSliderBar.value = currentHp;
            }

            // 3. 체력 텍스트 값 갱신
            if (currentHpText != null)
            {
                currentHpText.text = currentHp.ToString("F0");
            }
            if (maxHpText != null)
            {
                maxHpText.text = maxHp.ToString("F0");
            }
            if (hpRatioText != null)
            {
                hpRatioText.text = $"{currentHp:F0} / {maxHp:F0}";
            }
        }
    }
}
