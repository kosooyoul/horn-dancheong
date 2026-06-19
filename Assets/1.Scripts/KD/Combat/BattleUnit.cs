using System.Collections.Generic;
using UnityEngine;

namespace KD
{
    // 전투 중 유닛의 실제 상태를 관리하는 런타임 클래스
    // UnitData는 읽기 전용 참조 — 절대 수정하지 말 것
    // 현재 체력, 위치, 쿨타임 등 모든 가변 상태는 이 클래스에서만 관리
    public class BattleUnit
    {
        // ── 고정 데이터 참조 (읽기 전용) ────────────────────────────────
        public UnitData Data   { get; private set; }
        public int      TeamId { get; private set; } // 0 = 플레이어, 1 = 적
        public UnitDerivedStats Stats { get; private set; }

        // ── 가변 상태 ────────────────────────────────────────────────────
        public int CurrentHP      { get; private set; }
        public int MaxAP          { get; private set; }
        public int CurrentAP      { get; private set; }
        public Vector2Int CurrentTilePos { get; private set; }
        public bool IsAlive => CurrentHP > 0;
        public bool IsDead  => CurrentHP <= 0;

        // 장착된 교체 스킬 (null이면 미장착)
        private SkillData _equippedOptionalSkill;
        public  SkillData EquippedOptionalSkill => _equippedOptionalSkill;

        // 스킬별 남은 쿨타임 <skillId, remainingCooldown>
        private readonly Dictionary<string, int> _skillCooldowns = new Dictionary<string, int>();

        // ── 초기화 ───────────────────────────────────────────────────────
        // teamId: 0 = 플레이어 팀, 1 = 적 팀
        // optionalSkill: OwnedUnit.equippedOptionalSkill 전달 (null이면 미장착)
        public BattleUnit(UnitData data, int teamId, Vector2Int startTilePos, SkillData optionalSkill = null)
        {
            Data           = data;
            TeamId         = teamId;
            Stats          = StatCalculator.Calculate(data.baseStats);
            CurrentHP      = Stats.maxHP;
            CurrentTilePos = startTilePos;
            MaxAP          = BattleActionConfig.MaxAP;
            CurrentAP      = BattleActionConfig.MaxAP;

            if (optionalSkill != null)
                TryEquipOptionalSkill(optionalSkill);
        }

        // ── 스킬 관련 ────────────────────────────────────────────────────

        public bool TryEquipOptionalSkill(SkillData skill)
        {
            if (!SkillEquipValidator.CanEquip(Data.weaponType, skill))
            {
                Debug.LogWarning($"[BattleUnit] {Data.unitName}({Data.weaponType})은 '{skill.skillName}'을 장착할 수 없습니다.");
                return false;
            }
            _equippedOptionalSkill = skill;
            return true;
        }

        // 이 유닛이 해당 스킬을 보유하고 있는지 (쿨타임/AP 무관)
        public bool HasSkill(SkillData skill)
        {
            if (skill == null) return false;
            return skill == Data.uniqueSkill1
                || skill == Data.uniqueSkill2
                || skill == _equippedOptionalSkill;
        }

        // 쿨타임이 0인 스킬 목록 반환 — UI 하이라이트, 선택 가능 스킬 표시에 사용
        public List<SkillData> GetUsableSkills()
        {
            var usable = new List<SkillData>();
            TryAddIfReady(usable, Data.uniqueSkill1);
            TryAddIfReady(usable, Data.uniqueSkill2);
            TryAddIfReady(usable, _equippedOptionalSkill);
            return usable;
        }

        private void TryAddIfReady(List<SkillData> list, SkillData skill)
        {
            if (skill == null) return;
            if (GetCooldown(skill.skillId) > 0) return;
            list.Add(skill);
        }

        public int GetCooldown(string skillId)
        {
            int cd;
            return _skillCooldowns.TryGetValue(skillId, out cd) ? cd : 0;
        }

        public void SetCooldown(string skillId, int turns)
        {
            if (string.IsNullOrEmpty(skillId)) return;
            _skillCooldowns[skillId] = Mathf.Max(0, turns);
        }

        // 턴 종료 시 호출 — 모든 쿨타임 1 감소
        public void OnTurnEnd()
        {
            var keys = new List<string>(_skillCooldowns.Keys);
            foreach (string key in keys)
            {
                if (_skillCooldowns[key] > 0)
                    _skillCooldowns[key]--;
            }
        }

        // ── 전투 처리 ────────────────────────────────────────────────────

        // rawDamage: AttributeCalculator와 spiritMultiplier 적용 후의 값
        // 여기서 defense만 추가로 적용
        public void TakeDamage(int rawDamage)
        {
            int actual = Mathf.Max(1, rawDamage - Stats.defense);
            CurrentHP = Mathf.Max(0, CurrentHP - actual);
            Debug.Log($"[BattleUnit] {Data.unitName} 피해 {actual} (입력: {rawDamage}, 방어: {Stats.defense}) → HP {CurrentHP}/{Stats.maxHP}");
        }

        public void Heal(int amount)
        {
            int before = CurrentHP;
            CurrentHP = Mathf.Min(Stats.maxHP, CurrentHP + amount);
            Debug.Log($"[BattleUnit] {Data.unitName} 회복 {CurrentHP - before} → HP {CurrentHP}/{Stats.maxHP}");
        }

        public void MoveTo(Vector2Int targetPos)
        {
            CurrentTilePos = targetPos;
        }

        // ── AP 시스템 ─────────────────────────────────────────────────────

        public bool HasEnoughAP(int cost) => CurrentAP >= cost;

        public bool TrySpendAP(int cost)
        {
            if (!HasEnoughAP(cost))
            {
                Debug.LogWarning($"[BattleUnit] {Data.unitName} AP 부족 (필요: {cost}, 현재: {CurrentAP})");
                return false;
            }
            CurrentAP -= cost;
            return true;
        }

        public void RecoverAP(int amount)
        {
            CurrentAP = Mathf.Min(MaxAP, CurrentAP + amount);
        }

        // 전투 시작 시 1회 호출 — 차례마다 호출하면 Wait() 의미 없어짐
        public void ResetAP()
        {
            CurrentAP = MaxAP;
        }

        // 대기 선택 시 호출 — 행동하지 않고 AP 회복
        public void Wait()
        {
            RecoverAP(BattleActionConfig.WaitAPRecovery);
            Debug.Log($"[BattleUnit] {Data.unitName} 대기 → AP {CurrentAP}/{MaxAP}");
        }
    }
}
