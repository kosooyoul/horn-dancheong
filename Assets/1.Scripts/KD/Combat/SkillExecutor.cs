using UnityEngine;

namespace KD
{
    // 스킬 실행 단일 진입점
    // 광역 스킬: BattleManager가 GridPatternResolver로 범위 내 대상 목록을 수집 후 Execute를 반복 호출
    // SkillExecutor 자체는 항상 단일 대상만 처리
    // AP 소모 및 쿨타임 적용은 Execute 내부에서 자동 처리
    //
    // ── 최종 데미지 공식 ──────────────────────────────────────────────────
    // 최종 데미지
    // = 스킬 데미지
    // × 치명타 계수
    // × 방어력 계수
    // × 피해 감소 계수
    // × 회피 계수
    // × 약점 계수
    // × 연산 보정
    //
    // 스킬 데미지    = GetFinalAttackPower() × skillCoefficient
    // 치명타 계수    = 1.5 / 1.0
    // 방어력 계수    = 100 / (100 + 방어력)
    // 피해 감소 계수 = 1 - 대상의 피해 감소 수치
    // 회피 계수      = 0 (회피 성공 = Miss) / 1.0 (실패)
    // 약점 계수      = 1.5 (속성 약점) / 0.5 (반대 속성) / 1.0 (무관)
    // 연산 보정      = 소 ×1.5 / 중 ×2.0 / 대 ×2.5 / 특대 ×4.0
    //
    // ── 회복 공식 ─────────────────────────────────────────────────────────
    // 최종 회복 = GetFinalHealPower() × skillCoefficient × 연산 보정
    public static class SkillExecutor
    {
        private const float CritMultiplier = 1.5f;

        // 전투 담당자는 이 함수 하나만 호출하면 됨
        // target: Self 타입이면 caster와 동일 대상을 넘길 것
        public static bool Execute(BattleUnit caster, BattleUnit target, SkillData skill)
        {
            if (!ValidateInputs(caster, target, skill))
                return false;

            ExecuteEffect(caster, target, skill);
            caster.TrySpendAP(skill.apCost);

            if (skill.cooldown > 0)
                caster.SetCooldown(skill.skillId, skill.cooldown);

            return true;
        }

        // ── 입력값 검증 ──────────────────────────────────────────────────

        private static bool ValidateInputs(BattleUnit caster, BattleUnit target, SkillData skill)
        {
            if (caster == null || !caster.IsAlive)
            {
                Debug.LogWarning("[SkillExecutor] 시전자가 없거나 사망 상태입니다.");
                return false;
            }
            if (skill == null)
            {
                Debug.LogWarning("[SkillExecutor] 스킬 데이터가 없습니다.");
                return false;
            }
            if (caster.GetCooldown(skill.skillId) > 0)
            {
                Debug.Log($"[SkillExecutor] '{skill.skillName}' 쿨타임 중 ({caster.GetCooldown(skill.skillId)}턴 남음)");
                return false;
            }
            if (!caster.HasEnoughAP(skill.apCost))
            {
                Debug.Log($"[SkillExecutor] '{skill.skillName}' AP 부족 (필요: {skill.apCost}, 현재: {caster.CurrentAP})");
                return false;
            }
            if (skill.targetType != TargetType.Self && (target == null || !target.IsAlive))
            {
                Debug.LogWarning("[SkillExecutor] 유효한 타겟이 없습니다.");
                return false;
            }
            return true;
        }

        // ── 효과 분기 ────────────────────────────────────────────────────

        private static void ExecuteEffect(BattleUnit caster, BattleUnit target, SkillData skill)
        {
            BattleUnit effectTarget = skill.targetType == TargetType.Self ? caster : target;

            switch (skill.effectType)
            {
                case SkillEffectType.Damage:
                    ApplyDamage(caster, effectTarget, skill);
                    break;

                case SkillEffectType.Heal:
                    ApplyHeal(caster, effectTarget, skill);
                    break;

                case SkillEffectType.Buff:
                    // TODO: 버프 시스템 구현 후 연결
                    Debug.Log($"[SkillExecutor] '{skill.skillName}' Buff — 미구현");
                    break;

                case SkillEffectType.Debuff:
                    // TODO: 디버프 시스템 구현 후 연결
                    Debug.Log($"[SkillExecutor] '{skill.skillName}' Debuff — 미구현");
                    break;

                default:
                    Debug.LogWarning($"[SkillExecutor] 처리되지 않은 효과 타입: {skill.effectType}");
                    break;
            }

            PlayEffect(skill, effectTarget);
        }

        // ── 데미지 계산 ──────────────────────────────────────────────────

        private static void ApplyDamage(BattleUnit caster, BattleUnit target, SkillData skill)
        {
            // 1. 스킬 데미지 = 최종 공격력 × 스킬 계수
            float skillDamage = caster.GetFinalAttackPower() * skill.skillCoefficient;

            // 2. 치명타 계수 (150% 고정)
            bool  isCrit     = Random.value < caster.Stats.critChance;
            float critFactor = isCrit ? CritMultiplier : 1.0f;

            // 3. 방어력 계수 = 100 / (100 + 방어력)
            float defenseFactor = 100f / (100f + target.Stats.defense);

            // 4. 피해 감소 계수 = 1 - 피해 감소 수치
            float damageReductionRate   = target.DamageReductionRate;
            float damageReductionFactor = 1f - damageReductionRate;

            // 5. 회피 계수 (성공 → Miss)
            bool  evaded        = Random.value < target.Stats.evasionChance;
            float evasionFactor = evaded ? 0f : 1.0f;

            // 6. 약점 계수
            float attrFactor = AttributeCalculator.GetDamageMultiplier(skill.attribute, target.Data.attribute);

            // 7. 연산 보정
            float scaleFactor = GetScaleMultiplier(skill.scale);

            // 최종 데미지
            float raw = skillDamage
                      * critFactor
                      * defenseFactor
                      * damageReductionFactor
                      * evasionFactor
                      * attrFactor
                      * scaleFactor;

            int finalDamage = (evaded || raw <= 0f) ? 0 : Mathf.Max(1, Mathf.RoundToInt(raw));

            target.TakeDamage(finalDamage);

            string critTag      = isCrit             ? " [치명타]"                          : "";
            string evadeTag     = evaded             ? " [회피]"                            : "";
            string attrTag      = attrFactor > 1f   ? " [약점]" : attrFactor < 1f ? " [저항]" : "";
            string reductionTag = damageReductionRate > 0f ? $" [피해감소 {damageReductionRate:P0}]" : "";

            Debug.Log($"[SkillExecutor] {caster.Data.unitName} → {target.Data.unitName} " +
                      $"'{skill.skillName}' 최종 {finalDamage}{critTag}{evadeTag}{attrTag}{reductionTag} " +
                      $"(공격력 {caster.GetFinalAttackPower()} × 계수 {skill.skillCoefficient:F2} " +
                      $"× 치명타 {critFactor:F1} × 방어 {defenseFactor:F2} " +
                      $"× 피해감소 {damageReductionFactor:F2} × 속성 {attrFactor:F1} × 연산 {scaleFactor:F1})");
        }

        // ── 회복 계산 ────────────────────────────────────────────────────

        private static void ApplyHeal(BattleUnit caster, BattleUnit target, SkillData skill)
        {
            // 회복량 = 회복력 × 스킬 회복량 계수 × 연산 보정
            float scaleFactor = GetScaleMultiplier(skill.scale);
            int   healAmount  = Mathf.Max(1, Mathf.RoundToInt(
                caster.GetFinalHealPower() * skill.skillCoefficient * scaleFactor));

            target.Heal(healAmount);

            Debug.Log($"[SkillExecutor] {caster.Data.unitName} → {target.Data.unitName} " +
                      $"'{skill.skillName}' 회복 {healAmount} " +
                      $"(회복력 {caster.GetFinalHealPower()} × 계수 {skill.skillCoefficient:F2} × 연산 {scaleFactor:F1})");
        }

        // ── 공통 유틸 ────────────────────────────────────────────────────

        // 연산 보정: 소=1.5 / 중=2.0 / 대=2.5 / 특대=4.0
        private static float GetScaleMultiplier(Scale scale)
        {
            switch (scale)
            {
                case Scale.Small:  return 1.5f;
                case Scale.Medium: return 2.0f;
                case Scale.Large:  return 2.5f;
                case Scale.Xlarge: return 4.0f;
                default:           return 1.5f;
            }
        }

        private static void PlayEffect(SkillData skill, BattleUnit target)
        {
            if (skill.effectPrefab == null) return;
            Object.Instantiate(skill.effectPrefab,
                new Vector3(target.CurrentTilePos.x, 0f, target.CurrentTilePos.y),
                Quaternion.identity);
        }
    }
}
