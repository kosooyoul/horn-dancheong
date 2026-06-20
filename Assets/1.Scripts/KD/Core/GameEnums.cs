namespace KD
{
    // 유닛 직군 — 전투 역할 (UI 표시, 스탯 보너스 등에 활용)
    public enum UnitRole
    {
        Dealer,     // 공격 특화
        Tanker,     // 방어 특화
        Supporter,  // 버프/디버프/위치 보조
        Healer      // HP 회복/보호막/상태이상 해제
    }

    // 무기 종류 — 장착 가능한 교체 스킬을 결정
    // 같은 WeaponType을 가진 유닛은 동일한 스킬 풀을 공유
    public enum WeaponType
    {
        Sword,  // 검 — 근접 물리
        Bow,    // 궁 — 원거리 물리
        Staff,  // 장 — 마법/버프
        Book    // 책 — 회복/보조
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

    // 유닛 이동 방식 — MovementRangeCalculator에서 이동 가능 타일 계산 시 사용
    // 이동 거리(moveRange)는 agility 스탯으로 결정, 이동 방식만 여기서 정의
    public enum MovementType
    {
        Cardinal,       // 상하좌우 BFS 이동 (기본)
        EightDir,       // 8방향 BFS 이동 (대각 포함)
        KnightJump,     // L자 점프 (장애물 무시, 체스 나이트)
        Teleport,       // 범위 내 자유 이동 (장애물 무시)
        Charge,         // 직선 돌진 (막힐 때까지)
        DiagonalOnly    // 대각선 BFS 이동 (체스 비숍)
    }

    // 스킬 타겟 타입 — SkillExecutor에서 타겟 선택 로직 분기에 사용
    // 광역 여부는 TargetType이 아니라 GridPatternData로 처리한다
    // 예: Enemy + around_8 패턴 = 주변 8칸 적 전체 타격
    public enum TargetType
    {
        Enemy,      // 적 유닛
        Ally,       // 아군 유닛
        Self,       // 자기 자신
        AnyUnit,    // 아군/적군 모두
        Tile        // 특정 타일 선택 (유닛 없어도 사용 가능)
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
    // UseSelectedDirection : DirectionSet으로 마우스 방향을 스냅해 단일 방향으로 패턴 생성
    // AllDirections        : DirectionSet에 따라 여러 방향으로 패턴 자동 반복
    public enum PatternDirectionMode
    {
        UseSelectedDirection,
        AllDirections
    }

    // 방향 집합
    // UseSelectedDirection 일 때: 마우스 방향을 어느 방향 집합으로 스냅할지 결정
    // AllDirections 일 때: 패턴을 몇 방향으로 반복할지 결정
    public enum DirectionSet
    {
        Cardinal4,      // 상, 하, 좌, 우
        Diagonal4,      // 좌상, 우상, 좌하, 우하
        EightDirections // 8방향 전체
    }

    // 피해 정도 (small, medium, large, Xlarge)

    public enum Scale
    {
        Small, Medium, Large, Xlarge
    }

    // 스킬 연출 방식 — SimpleSkillFxPlayer에서 이 타입으로 연출 분기
    //
    // SlashBeam     : 시전자 위에 검기(띠 모양)가 나타났다 사라짐 (탱커 일반/단일, 도깨비 일반)
    // Projectile    : 시전자 → 대상으로 오브젝트가 날아감 (딜러 일반·단일·광역, 힐러 일반)
    // Pillar        : 타겟 타일 바닥에서 빛기둥이 솟아오름 (탱커 피해감소 / 힐러 광역회복)
    // AreaRise      : 타일 전체에 땅·먼지 효과 (딜러 광역, 도깨비 광역)
    // DroneDeliver  : 드론이 하늘에서 등장 후 아이템 투하 (힐러 부활)
    // Lightning     : 하늘에서 번개가 대상에게 내리꽂힘 (도깨비 단일)
    // DustShockwave : 시전자 주변 먼지+카메라 진동 (도깨비 전체 공격)
    // None          : 연출 없음 (폴백)
    public enum SkillFxType
    {
        None,
        SlashBeam,
        Projectile,
        Pillar,
        AreaRise,
        DroneDeliver,
        Lightning,
        DustShockwave
    }
}
