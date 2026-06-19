using System;

namespace KD
{
    // StatCalculator가 UnitBaseStats를 읽어 계산한 전투 수치
    // BattleUnit 생성 시 1회 계산 후 보관 — 전투 중 직접 수정 금지
    // struct: 값 복사로 원본 오염 방지
    [Serializable]
    public struct UnitDerivedStats
    {
        public int maxHP;           // 최대 체력 (guard 기반)
        public int skillPower;      // 스킬 데미지/회복 기반 수치 (spirit 기반)
        public int defense;         // 받는 피해 감소 (guard 기반)
        public int moveRange;       // 이동 가능 최대 칸 수 (agility 기반)
        public int initiative;      // 행동 순서 — 높을수록 먼저 행동 (agility + luck 기반)
        public float critChance;    // 치명타 확률 0~1 (luck 기반)
        public float evasionChance; // 회피 확률 0~1 (agility + luck 기반)

        public UnitDerivedStats(
            int maxHP,
            int skillPower,
            int defense,
            int moveRange,
            int initiative,
            float critChance,
            float evasionChance)
        {
            this.maxHP          = maxHP;
            this.skillPower     = skillPower;
            this.defense        = defense;
            this.moveRange      = moveRange;
            this.initiative     = initiative;
            this.critChance     = critChance;
            this.evasionChance  = evasionChance;
        }
    }
}
