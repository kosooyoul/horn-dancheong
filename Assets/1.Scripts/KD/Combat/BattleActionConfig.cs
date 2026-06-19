namespace KD
{
    // 전투 전역 규칙 상수
    // 이동 AP 비용은 UnitData.moveAPCost 참조
    // 스킬 AP 비용은 SkillData.apCost 참조
    public static class BattleActionConfig
    {
        public const int MaxAP          = 100; // 유닛 최대 AP
        public const int WaitAPRecovery = 30;  // 대기 선택 시 AP 회복량
    }
}
