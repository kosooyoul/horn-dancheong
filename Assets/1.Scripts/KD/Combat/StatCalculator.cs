using UnityEngine;

namespace KD
{
    // UnitBaseStats → UnitDerivedStats 변환 담당
    // 밸런스 조정이 필요할 때 이 파일의 수식만 수정하면 됨
    public static class StatCalculator
    {
        public static UnitDerivedStats Calculate(UnitBaseStats baseStats)
        {
            int maxHP      = 50 + baseStats.guard * 10;
            int skillPower = 5  + baseStats.spirit * 3;
            int defense    = baseStats.guard * 2;
            int moveRange  = Mathf.Clamp(2 + baseStats.agility / 5, 2, 6);
            int initiative = baseStats.agility * 10 + baseStats.luck;

            float critChance    = Mathf.Clamp01(baseStats.luck * 0.01f);
            float evasionChance = Mathf.Clamp01((baseStats.agility + baseStats.luck) * 0.005f);

            return new UnitDerivedStats(
                maxHP,
                skillPower,
                defense,
                moveRange,
                initiative,
                critChance,
                evasionChance
            );
        }
    }
}
