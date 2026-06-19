namespace KD
{
    // 속성 상성에 따른 데미지 배율 계산
    // 상성 순환: Red > White > Blue > Yellow > Black > Red (오각형 순환)
    // 배율 수정 시 Advantage / Disadvantage 상수만 조정
    public static class AttributeCalculator
    {
        private const float Advantage    = 1.25f;
        private const float Disadvantage = 0.75f;
        private const float Neutral      = 1.0f;

        // 스킬 속성(attacker) vs 대상 속성(defender) → 데미지 배율 반환
        public static float GetDamageMultiplier(UnitAttribute attacker, UnitAttribute defender)
        {
            if (IsAdvantage(attacker, defender)) return Advantage;
            if (IsAdvantage(defender, attacker)) return Disadvantage;
            return Neutral;
        }

        // 상성 관계: A가 B를 이기는지 여부
        // Red > White > Blue > Yellow > Black > Red
        private static bool IsAdvantage(UnitAttribute a, UnitAttribute b)
        {
            switch (a)
            {
                case UnitAttribute.Red:    return b == UnitAttribute.White;
                case UnitAttribute.White:  return b == UnitAttribute.Blue;
                case UnitAttribute.Blue:   return b == UnitAttribute.Yellow;
                case UnitAttribute.Yellow: return b == UnitAttribute.Black;
                case UnitAttribute.Black:  return b == UnitAttribute.Red;
                default:                   return false;
            }
        }
    }
}
