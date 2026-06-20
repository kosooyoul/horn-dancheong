using System;

namespace KD
{
    // StatCalculator가 UnitBaseStats를 읽어 계산한 전투 수치
    // BattleUnit 생성 시 1회 계산 후 보관 — 전투 중 직접 수정 금지
    // struct: 값 복사로 원본 오염 방지
    [Serializable]
    public struct UnitDerivedStats
    {
        public int   maxHP;          // 최대 체력 (guard 기반)
        public int   maxSP;          // 최대 SP — 150 고정
        public int   initiative;     // 행동 순서 (agility = 민첩, 높을수록 선턴)
        public int   moveRange;      // 실제 이동 칸수 (agility / 5, 최솟값 1, 최댓값 5)
        public int   attackPower;    // 공격력 (spirit × 5)
        public int   healPower;      // 회복력 (spirit × 5)
        public int   defense;        // 방어력 (guard × 5)
        public float critChance;     // 치명타 확률 0~1 (luck × 1%, 최솟값 5%, 최댓값 100%)
        public float evasionChance;  // 회피 확률 0~1 (luck × 1%, 최솟값 5%, 최댓값 100%)

        public UnitDerivedStats(
            int   maxHP,
            int   maxSP,
            int   initiative,
            int   moveRange,
            int   attackPower,
            int   healPower,
            int   defense,
            float critChance,
            float evasionChance)
        {
            this.maxHP         = maxHP;
            this.maxSP         = maxSP;
            this.initiative    = initiative;
            this.moveRange     = moveRange;
            this.attackPower   = attackPower;
            this.healPower     = healPower;
            this.defense       = defense;
            this.critChance    = critChance;
            this.evasionChance = evasionChance;
        }
    }
}
