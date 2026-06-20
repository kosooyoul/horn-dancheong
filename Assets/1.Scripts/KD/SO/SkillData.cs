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

        [Header("버프 수치")]
        [Tooltip("피해 감소 비율. Buff 스킬에서 사용. 0.3 = 30% 피해 감소")]
        public float damageReductionValue = 0f;

        [Header("연출 — 연출 타입")]
        [Tooltip("SimpleSkillFxPlayer가 이 값으로 재생 방식을 결정한다")]
        public SkillFxType fxType = SkillFxType.None;

        [Header("연출 — VFX Prefab")]
        [Tooltip("시전자 위 / 타일 바닥에서 재생되는 기본 이펙트")]
        public GameObject castVfxPrefab;
        [Tooltip("날아가는 투사체 (Projectile, DroneDeliver 타입에서 사용)")]
        public GameObject projectilePrefab;
        [Tooltip("대상에게 닿았을 때 재생되는 임팩트 이펙트")]
        public GameObject impactVfxPrefab;
        [Tooltip("범위 전체 타일에 깔리는 이펙트 (Pillar 반짝이, AreaRise 먼지 등)")]
        public GameObject areaVfxPrefab;

        [Header("연출 — SFX")]
        public AudioClip castSfx;
        public AudioClip projectileSfx;
        public AudioClip impactSfx;

        [Header("연출 — 타이밍 (초)")]
        [Tooltip("시전 이펙트 재생 후 투사체/임팩트 전까지 대기 시간")]
        public float castDelay   = 0f;
        [Tooltip("투사체 도착 후 실제 피해/효과 적용 전 대기 시간")]
        public float impactDelay = 0f;
        [Tooltip("임팩트 이펙트 후 다음 행동으로 넘어가기 전 여운 시간")]
        public float endDelay    = 0.3f;

        [Header("연출 — 투사체")]
        [Tooltip("투사체 이동 속도 (유닛/초)")]
        public float projectileSpeed     = 8f;
        [Tooltip("포물선 궤도 여부 (Projectile 타입에서 사용)")]
        public bool  projectileArc;
        [Tooltip("포물선 최대 높이")]
        public float projectileArcHeight = 1.5f;

        [Header("연출 — 카메라 흔들림")]
        [Tooltip("임팩트 타이밍에 카메라를 흔들지 여부")]
        public bool  useCameraShake;
        [Tooltip("카메라 흔들림 지속 시간 (초)")]
        public float shakeDuration  = 0.2f;
        [Tooltip("카메라 흔들림 강도")]
        public float shakeMagnitude = 0.15f;

        [Header("연출 — VFX 수명")]
        [Tooltip("Instantiate된 VFX 오브젝트가 자동 삭제되는 시간 (초)")]
        public float vfxLifetime = 3f;

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
