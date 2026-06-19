using UnityEngine;

namespace KD
{
    // 스킬 실행 단일 진입점
    // 광역 스킬(AllAllies, AllEnemies): BattleManager가 대상 목록을 수집 후 Execute를 반복 호출
    // SkillExecutor 자체는 항상 단일 대상만 처리
    public static class SkillExecutor
    {
        // 전투 담당자는 이 함수 하나만 호출하면 됨
        // target: Self 타입이면 caster와 동일 대상을 넘길 것
        public static bool Execute(BattleUnit caster, BattleUnit target, SkillData skill)
        {
            if (!ValidateInputs(caster, target, skill))
                return false;

            ExecuteEffect(caster, target, skill);

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

        // ── MVP 구현 ─────────────────────────────────────────────────────

        private static void ApplyDamage(BattleUnit caster, BattleUnit target, SkillData skill)
        {
            float attrMultiplier = AttributeCalculator.GetDamageMultiplier(skill.attribute, target.Data.attribute);
            int rawDamage = Mathf.RoundToInt((skill.baseValue + caster.Stats.skillPower * skill.spiritMultiplier) * attrMultiplier);

            target.TakeDamage(rawDamage);

            Debug.Log($"[SkillExecutor] {caster.Data.unitName} → {target.Data.unitName} " +
                      $"'{skill.skillName}' 데미지 {rawDamage} (속성 배율 {attrMultiplier:F2}x)");
        }

        private static void ApplyHeal(BattleUnit caster, BattleUnit target, SkillData skill)
        {
            int healAmount = Mathf.RoundToInt(skill.baseValue + caster.Stats.skillPower * skill.spiritMultiplier);

            target.Heal(healAmount);

            Debug.Log($"[SkillExecutor] {caster.Data.unitName} → {target.Data.unitName} " +
                      $"'{skill.skillName}' 회복 {healAmount}");
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
