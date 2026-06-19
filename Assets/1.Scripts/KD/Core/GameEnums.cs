namespace KD
{
    // 유닛 직군 — 장착 가능한 교체 스킬과 전투 역할을 결정
    public enum UnitRole
    {
        Dealer,     // 공격 특화, 집행
        Tanker,     // 방어 특화, 금군
        Supporter,  // HP 회복, 의관
        Healer      // 버프/디버프, 사관
    }

    // 유닛 속성 — AttributeCalculator에서 상성 배율 계산에 사용
    public enum UnitAttribute
    {
        Red,        // 적 계열
        Blue,       // 청 계열
        Yellow,     // 황 계열
        White,      // 백 계열
        Black       // 흑 계열
    }

    // 유닛 이동 방식 — GridManager에서 이동 가능 타일 계산 시 사용
    // 이동 거리(moveRange)는 민첩 스탯으로 결정, 이동 방식만 여기서 정의
    public enum MovementType
    {
        Cardinal,       // 상하좌우 4방향 이동 (기본)
        EightDir,       // 8방향 이동 (대각 포함)
        KnightJump,     // L자 점프 (장애물 무시, 체스 나이트)
        Teleport,       // 범위 내 자유 이동 (장애물 무시)
        Charge,         // 직선 돌진 (막힐 때까지)
        DiagonalOnly    // 대각선 방향만 (체스 비숍)
    }

    // 스킬 타겟 타입 — SkillExecutor에서 타겟 선택 로직 분기에 사용
    public enum TargetType
    {
        Enemy,      // 적 유닛
        Ally,       // 아군 유닛
        Self,       // 자기 자신
        AllAllies,  // 아군 전체
        AllEnemies, // 적군 전체
        Any         // 아군/적군 모두
    }

    // 스킬 효과 타입 — SkillExecutor.ExecuteEffect()에서 분기에 사용
    // MVP: Damage, Heal 우선 구현 / 나머지는 구조만 잡아둠
    public enum SkillEffectType
    {
        Damage,     // MVP
        Heal,       // MVP
        Buff,       // 추후
        Debuff,     // 추후
    }

    // 패턴 방향 모드 — GridPatternData에서 사용 (스킬 범위 전용)
    // UseSelectedDirection : 플레이어가 선택한 방향 기준으로만 패턴 생성
    // AllDirections        : DirectionSet에 따라 여러 방향으로 패턴 자동 반복
    public enum PatternDirectionMode
    {
        UseSelectedDirection,
        AllDirections
    }

    // 방향 집합 — PatternDirectionMode.AllDirections일 때 반복 방향 결정
    public enum DirectionSet
    {
        Cardinal4,      // 상, 하, 좌, 우
        Diagonal4,      // 좌상, 우상, 좌하, 우하
        EightDirections // 8방향 전체
    }
}
