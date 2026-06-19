namespace KD
{
    // 무기 타입별 교체 스킬 장착 가능 여부 검증
    // SkillData.allowedWeaponTypes가 비어있으면 전 무기 장착 가능
    public static class SkillEquipValidator
    {
        public static bool CanEquip(WeaponType weaponType, SkillData skill)
        {
            if (skill == null) return false;
            if (skill.allowedWeaponTypes == null || skill.allowedWeaponTypes.Length == 0)
                return true;

            foreach (WeaponType allowed in skill.allowedWeaponTypes)
            {
                if (allowed == weaponType) return true;
            }
            return false;
        }
    }
}
