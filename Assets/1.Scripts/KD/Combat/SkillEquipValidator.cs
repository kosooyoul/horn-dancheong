namespace KD
{
    // 직군별 교체 스킬 장착 가능 여부 검증
    // SkillData.allowedRoles가 비어있으면 전 직군 장착 가능
    public static class SkillEquipValidator
    {
        public static bool CanEquip(UnitRole role, SkillData skill)
        {
            if (skill == null) return false;
            if (skill.allowedRoles == null || skill.allowedRoles.Length == 0) return true;

            foreach (UnitRole allowed in skill.allowedRoles)
            {
                if (allowed == role) return true;
            }
            return false;
        }
    }
}
