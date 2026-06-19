using System.Collections.Generic;
using UnityEngine;

namespace KD
{
    // 교체 가능 스킬 전체 목록 관리 — 전투 시작 전 스킬 선택 UI에서 사용
    // 새 스킬 추가: Create > KD > Skill Database
    // 사용법: skillDatabase.GetSkillsFor(unit.weaponType)
    [CreateAssetMenu(fileName = "SkillDatabase", menuName = "KD/Skill Database")]
    public class SkillDatabase : ScriptableObject
    {
        [Tooltip("교체 장착 가능한 스킬 전체 목록. 새 스킬 추가 시 여기에 등록")]
        public List<SkillData> optionalSkills = new List<SkillData>();

        // 특정 무기 타입에 장착 가능한 스킬 목록 반환
        public List<SkillData> GetSkillsFor(WeaponType weaponType)
        {
            var result = new List<SkillData>();
            foreach (SkillData skill in optionalSkills)
            {
                if (skill == null) continue;
                if (SkillEquipValidator.CanEquip(weaponType, skill))
                    result.Add(skill);
            }
            return result;
        }

        // BattleUnit에서 바로 쓸 수 있는 오버로드
        public List<SkillData> GetSkillsFor(BattleUnit unit)
            => GetSkillsFor(unit.Data.weaponType);

#if UNITY_EDITOR
        private void OnValidate()
        {
            for (int i = optionalSkills.Count - 1; i >= 0; i--)
            {
                if (optionalSkills[i] == null)
                {
                    optionalSkills.RemoveAt(i);
                    UnityEngine.Debug.LogWarning($"[SkillDatabase] null 항목을 제거했습니다.", this);
                }
            }
        }
#endif
    }
}
