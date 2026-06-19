using System;
using UnityEngine;

namespace KD
{
    // 플레이어가 실제로 보유한 유닛 1명을 표현
    // UnitData = 원본 설계도, OwnedUnit = 내가 보유한 개별 캐릭터
    // 같은 UnitData로 두 명을 보유할 수 있음 (instanceId로 구분)
    [Serializable]
    public class OwnedUnit
    {
        [HideInInspector]
        public string instanceId;

        [Tooltip("이 유닛의 원본 데이터")]
        public UnitData unitData;

        [Tooltip("교체 슬롯에 장착된 스킬. null이면 미장착")]
        public SkillData equippedOptionalSkill;

        public int level = 1;

        public OwnedUnit(UnitData unitData)
        {
            this.instanceId = Guid.NewGuid().ToString();
            this.unitData   = unitData;
        }

        // 교체 스킬 장착
        // database를 넘기면 "DB 등록 여부 + 무기 타입 호환" 모두 검사
        // database가 null이면 무기 타입 호환만 검사 (직접 호출용 fallback)
        public bool EquipOptionalSkill(SkillData skill, SkillDatabase database = null)
        {
            if (skill == null) return false;

            bool canEquip = database != null
                ? database.CanEquip(this, skill)
                : SkillEquipValidator.CanEquip(unitData.weaponType, skill);

            if (!canEquip)
            {
                Debug.LogWarning($"[OwnedUnit] {unitData.unitName}({unitData.weaponType})은 '{skill.skillName}'을 장착할 수 없습니다.");
                return false;
            }

            equippedOptionalSkill = skill;
            return true;
        }

        public void UnequipOptionalSkill()
        {
            equippedOptionalSkill = null;
        }
    }
}
