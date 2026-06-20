using UnityEngine;
using UnityEngine.Serialization;

namespace KD
{
    // 스킬의 고정 설계도 — Inspector에서 설정, 전투 중 절대 수정 금지
    // 새 스킬 추가: Create > KD > Skill Data
    //
    // 데미지 공식: 최종 공격력 × skillCoefficient × 치명타 × 방어계수 × 회피계수 × 약점 × 연산보정(scale)
    // 회복  공식: 회복력 × skillCoefficient × 연산보정(scale)
    [CreateAssetMenu(fileName = "SkillData_", menuName = "KD/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [Header("기본 정보")]
        public string skillId;
        public string skillName;
        [TextArea] public string description;
        public Sprite icon;
        public GameObject effectPrefab;

        [Header("장착 조건")]
        [Tooltip("이 스킬을 장착할 수 있는 무기 타입. 비어있으면 전 무기 장착 가능")]
        public WeaponType[] allowedWeaponTypes;

        [Header("타겟")]
        public TargetType targetType;

        [Header("사거리 패턴")]
        [Tooltip("스킬이 닿는 타일 범위를 정의하는 패턴 에셋")]
        public GridPatternData targetPattern;

        [Header("비용 & 쿨타임")]
        [Tooltip("사용 시 소모하는 AP. BattleActionConfig.MaxAP = 100 기준\n기본 공격: 25~35 / 강한 스킬: 45~60 / 광역: 50~70")]
        public int apCost = 30;
        [Tooltip("사용 후 재사용까지 걸리는 턴 수. 0이면 쿨타임 없음")]
        public int cooldown = 1;

        [Header("효과")]
        public SkillEffectType effectType;
        [Tooltip("스킬 자체의 속성. AttributeCalculator에서 상성 계산에 사용")]
        public UnitAttribute attribute;
        [Tooltip("스킬 계수. 데미지: 최종 공격력 × 이 값 / 회복: 회복력 × 이 값\n1.0 = 100%, 1.5 = 150%")]
        [FormerlySerializedAs("spiritMultiplier")]
        public float skillCoefficient = 1.0f;
        [Tooltip("연산 보정 단계. 소=×1.5 / 중=×2.0 / 대=×2.5 / 특대=×4.0")]
        public Scale scale;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(skillId))
                Debug.LogWarning($"[{name}] skillId가 비어 있습니다.", this);

            if (string.IsNullOrWhiteSpace(skillName))
                Debug.LogWarning($"[{name}] skillName이 비어 있습니다.", this);

            if (targetPattern == null)
                Debug.LogWarning($"[{name}] targetPattern이 없습니다. GridPatternData를 할당하세요.", this);

            apCost           = Mathf.Clamp(apCost, 1, 100);
            cooldown         = Mathf.Max(0, cooldown);
            skillCoefficient = Mathf.Max(0f, skillCoefficient);
        }
#endif
    }
}
