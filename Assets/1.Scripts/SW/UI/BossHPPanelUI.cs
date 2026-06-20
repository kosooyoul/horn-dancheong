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

        // ====================================================================
        // [연결 준비] 적 캐릭터(보스) 데이터소스 참조
        // 실제 프로젝트 구조 및 전투 매니저에 맞춰 적합한 방식을 활성화하여 사용하세요.
        // ====================================================================

        // 1. KD.BattleUnit 형식의 적 캐릭터 참조
        private BattleUnit _bossBattleUnit;

        // 2. BattleUnitEntry 형식의 적 캐릭터 참조 (BattleScript 등 전역 사용 시)
        private BattleUnitEntry _bossBattleUnitEntry;

        // 3. ICharacterBattleInfo 인터페이스 형식의 캐릭터 정보 참조 (추상화 레이어 사용 시)
        private ICharacterBattleInfo _bossBattleInfo;

        /// <summary>
        /// KD.BattleUnit 타입의 적 캐릭터를 보스로 등록하고 UI를 갱신합니다.
        /// </summary>
        public void SetBossCharacter(BattleUnit unit)
        {
            _bossBattleUnit = unit;
            _bossBattleUnitEntry = null;
            _bossBattleInfo = null;

            UpdateBossUI();
        }

        /// <summary>
        /// BattleUnitEntry 타입의 적 캐릭터를 보스로 등록하고 UI를 갱신합니다.
        /// </summary>
        public void SetBossCharacter(BattleUnitEntry entry)
        {
            _bossBattleUnit = null;
            _bossBattleUnitEntry = entry;
            _bossBattleInfo = null;

            UpdateBossUI();
        }

        /// <summary>
        /// ICharacterBattleInfo 타입의 보스 정보를 등록하고 UI를 갱신합니다.
        /// </summary>
        public void SetBossCharacter(ICharacterBattleInfo info)
        {
            _bossBattleUnit = null;
            _bossBattleUnitEntry = null;
            _bossBattleInfo = info;

            UpdateBossUI();
        }

        /// <summary>
        /// 연결된 보스의 상태(체력 및 이름)를 불러와 UI를 실시간으로 갱신합니다.
        /// </summary>
        public void UpdateBossUI()
        {
            string bossName = "Boss";
            float currentHp = 0f;
            float maxHp = 1f; // 0으로 나누기 방지용

            if (_bossBattleUnit != null)
            {
                bossName = _bossBattleUnit.Data != null ? _bossBattleUnit.Data.unitName : "Boss";
                currentHp = _bossBattleUnit.CurrentHP;
                maxHp = _bossBattleUnit.Stats.maxHP;
            }
            else if (_bossBattleUnitEntry != null)
            {
                bossName = _bossBattleUnitEntry.DisplayName;
                currentHp = _bossBattleUnitEntry.CurrentHP;
                maxHp = _bossBattleUnitEntry.MaxHP;
            }
            else if (_bossBattleInfo != null)
            {
                bossName = _bossBattleInfo.CharacterName;
                currentHp = _bossBattleInfo.CurrentHp;
                maxHp = _bossBattleInfo.MaxHp;
            }
            else
            {
                // 데이터 소스가 연결되지 않은 경우 임시 데이터 또는 에디터 테스트 데이터를 사용할 수 있습니다.
                // Debug.LogWarning("[BossHPPanelUI] Boss Character is not assigned yet.");
                return;
            }

            // 1. 이름 갱신
            if (bossNameText != null)
            {
                bossNameText.text = bossName;
            }

            // 2. 슬라이더 바 (BossSliderBar/BossHPBar) 값 갱신
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
