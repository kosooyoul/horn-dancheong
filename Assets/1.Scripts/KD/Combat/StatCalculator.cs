using UnityEngine;

namespace KD
{
    // UnitBaseStats → UnitDerivedStats 변환 담당
    //
    // 최종 스탯 공식:
    // 민첩 1당 → 행동 순서 +1, 이동 수치 +1
    // 이동 칸수 → 기본 1칸 + 민첩 5당 1칸 증가, 최대 5칸
    //
    // 영력 1당 → 공격력 +5, 회복력 +5
    // 보호 1당 → 방어력 +5
    // 행운 1당 → 치명타 확률 +1%, 회피 확률 +1%
    // 치명타/회피 확률 → 기본 5%, 최대 100%
    public static class StatCalculator
    {
        private const int FixedMaxSP = 150;

        private const int MoveRangeMin = 1;
        private const int MoveRangeMax = 5;
        private const int MoveStatPerTile = 5;

        private const float CritEvasionBase = 0.05f; // 기본 5%
        private const float CritEvasionPerLuck = 0.01f; // 행운 1당 1%
        private const float CritEvasionMax = 1.00f; // 최대 100%

        public static UnitDerivedStats Calculate(UnitBaseStats baseStats)
        {
            int maxHP = 50 + baseStats.guard * 10;
            int maxSP = FixedMaxSP;

            // 행동 순서: 민첩이 높을수록 선턴
            int initiative = baseStats.agility;

            // 이동 칸수: 기본 1칸 + 민첩 5당 1칸 증가, 최대 5칸
            int moveRange = Mathf.Clamp(
                MoveRangeMin + baseStats.agility / MoveStatPerTile,
                MoveRangeMin,
                MoveRangeMax
            );

            int attackPower = baseStats.spirit * 5;
            int healPower = baseStats.spirit * 5;
            int defense = baseStats.guard * 5;

            float critChance = Mathf.Clamp(
                CritEvasionBase + baseStats.luck * CritEvasionPerLuck,
                CritEvasionBase,
                CritEvasionMax
            );

            float evasionChance = Mathf.Clamp(
                CritEvasionBase + baseStats.luck * CritEvasionPerLuck,
                CritEvasionBase,
                CritEvasionMax
            );

            return new UnitDerivedStats(
                maxHP,
                maxSP,
                initiative,
                moveRange,
                attackPower,
                healPower,
                defense,
                critChance,
                evasionChance
            );
        }
    }
}