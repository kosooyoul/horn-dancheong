# 스킬 정의 포맷 스펙

## 📄 파일 정보

- **파일명**: `SKILLS.json`
- **위치**: `Assets/1.Scripts/KO/Battle/SkillData/`
- **용도**: 전투에서 사용할 수 있는 스킬 정의
- **참조 관계**: 유닛의 `skillIds` 배열로 참조

## 🏗️ JSON 구조

### 루트 객체
```json
{
  "name": "string",
  "description": "string", 
  "version": "string",
  "skills": [SkillDefinition]
}
```

### SkillDefinition 객체
```json
{
  "id": number,
  "name": "string",
  "description": "string",
  "iconPath": "string",
  "skillType": "active" | "passive",
  "targetType": "enemy" | "ally" | "area" | "self",
  "mpCost": number,
  "cooldown": number,
  "range": SkillRange,
  "effects": [SkillEffect],
  "requirements": SkillRequirements,
  "animations": SkillAnimations
}
```

### SkillRange 객체 (2차원 배열 시스템)
```json
{
  "pattern": [
    [0,0,1,0,0],
    [0,1,1,1,0], 
    [1,1,0,1,1],
    [0,1,1,1,0],
    [0,0,1,0,0]
  ],
  "actorPosition": [2, 2]
}
```

### SkillEffect 객체
```json
{
  "effectType": "damage" | "heal" | "buff" | "debuff",
  "basePower": number,
  "spiritMultiplier": number,
  "agilityMultiplier": number,
  "guardMultiplier": number,
  "luckMultiplier": number,
  "element": "fire" | "ice" | "holy" | "physical" | "explosion",
  "statType": "agility" | "spirit" | "guard" | "luck" | "hp" | "mp",
  "value": number,
  "duration": number,
  "probability": number
}
```

### SkillRequirements 객체
```json
{
  "minimumLevel": number,
  "requiredStats": {
    "agility": number,
    "spirit": number,
    "guard": number,
    "luck": number
  }
}
```

### SkillAnimations 객체
```json
{
  "castAnimation": "string",
  "hitAnimation": "string", 
  "effectPrefab": "string"
}
```

## 📋 필드 상세

### SkillDefinition 필드

#### id
- **타입**: `number` (정수)
- **설명**: 스킬 고유 식별자
- **제한**: 파일 내 고유, 1 이상 권장

#### name
- **타입**: `string`
- **설명**: 스킬 표시명
- **예시**: `"파이어볼"`, `"치유"`

#### skillType
- **타입**: `"active" | "passive"`
- **설명**: 스킬 활성화 방식
- **active**: 수동 발동 (플레이어가 직접 사용)
- **passive**: 자동 발동 (조건 충족 시 자동)

#### targetType
- **타입**: `"enemy" | "ally" | "area" | "self"`
- **설명**: 스킬 대상 타입
- **enemy**: 적 대상
- **ally**: 아군 대상
- **area**: 범위 대상 (적/아군 구분 없음)
- **self**: 자기 자신

#### mpCost
- **타입**: `number` (정수)
- **설명**: 마나 소모량
- **범위**: 0 이상

#### cooldown
- **타입**: `number` (정수) 
- **설명**: 쿨다운 턴 수
- **범위**: 0 이상 (0이면 쿨다운 없음)

### SkillRange 필드 (핵심 기능)

#### pattern
- **타입**: `number[][]` (2차원 배열)
- **설명**: 스킬 영향 범위를 나타내는 2차원 격자
- **값**: `0` (영향 없음), `1` (영향 있음)
- **크기**: 제한 없음 (권장: 3x3 ~ 7x7)

#### actorPosition
- **타입**: `[number, number]` (x, y 좌표)
- **설명**: `pattern` 배열 내에서 시전자의 위치
- **예시**: `[2, 2]`는 5x5 배열의 중앙을 의미

### 범위 패턴 예시

#### 십자 모양 (치유)
```json
{
  "pattern": [
    [0,0,1,0,0],
    [0,1,1,1,0],
    [1,1,0,1,1], 
    [0,1,1,1,0],
    [0,0,1,0,0]
  ],
  "actorPosition": [2, 2]
}
```

#### 일직선 (파이어볼)  
```json
{
  "pattern": [
    [0,0,1,0,0],
    [0,0,1,0,0],
    [0,0,0,0,0],
    [0,0,1,0,0], 
    [0,0,1,0,0]
  ],
  "actorPosition": [2, 2]
}
```

#### 광역 폭발
```json
{
  "pattern": [
    [0,1,1,1,0],
    [1,1,1,1,1],
    [1,1,0,1,1],
    [1,1,1,1,1],
    [0,1,1,1,0]
  ],
  "actorPosition": [2, 2]
}
```

### SkillEffect 필드

#### effectType
- **타입**: `"damage" | "heal" | "buff" | "debuff"`
- **damage**: 피해 입히기
- **heal**: 체력 회복
- **buff**: 능력치 증가
- **debuff**: 능력치 감소

#### basePower
- **타입**: `number`
- **설명**: 기본 효과값
- **계산**: 최종값 = `basePower + (시전자스탯 × 해당Multiplier)`

#### 스탯 배율들
- **spiritMultiplier**: 영력 배율 (마법 데미지/힐링)
- **agilityMultiplier**: 민첩 배율 (물리 데미지)
- **guardMultiplier**: 방어 배율 (방어형 스킬)
- **luckMultiplier**: 운 배율 (크리티컬)

#### element
- **타입**: `string`
- **설명**: 속성 (추후 속성 상성 시스템용)
- **예시**: `"fire"`, `"ice"`, `"holy"`, `"physical"`

## 🎮 스킬 시스템 사용법

### 기본 조작
1. **S키**: 스킬 메뉴 열기/닫기
2. **스킬 선택**: 메뉴에서 사용할 스킬 클릭
3. **범위 확인**: 붉은색으로 표시되는 영향 범위 확인
4. **Enter키**: 스킬 사용 확인
5. **Esc키**: 스킬 사용 취소

### 시각적 표시
- **파란색 큐브**: 스킬 시전자 위치
- **빨간색 큐브**: 스킬 영향 범위
- **UI 패널**: 스킬 정보 및 타겟 수 표시

## 📝 예시 파일

```json
{
  "name": "Skill Definitions",
  "description": "전투 스킬 정의",
  "version": "1.0", 
  "skills": [
    {
      "id": 1,
      "name": "파이어볼",
      "description": "직선으로 화염 피해를 입힙니다",
      "iconPath": "Icons/Skills/Fireball",
      "skillType": "active",
      "targetType": "enemy",
      "mpCost": 5,
      "cooldown": 0,
      "range": {
        "pattern": [
          [0,0,1,0,0],
          [0,0,1,0,0], 
          [0,0,0,0,0],
          [0,0,1,0,0],
          [0,0,1,0,0]
        ],
        "actorPosition": [2, 2]
      },
      "effects": [
        {
          "effectType": "damage",
          "basePower": 25,
          "spiritMultiplier": 1.5,
          "element": "fire"
        }
      ],
      "requirements": {
        "minimumLevel": 1,
        "requiredStats": {
          "spirit": 3
        }
      },
      "animations": {
        "castAnimation": "Cast_Fireball",
        "hitAnimation": "Hit_Fire",
        "effectPrefab": "Effects/Fireball"
      }
    }
  ]
}
```

## ✅ 유효성 검사

### 필수 검사
1. **ID 고유성**: 파일 내 중복된 `id` 없음
2. **패턴 유효성**: `pattern` 배열이 직사각형 형태
3. **액터 위치**: `actorPosition`이 `pattern` 범위 내
4. **필수 필드**: 모든 필드가 올바른 타입으로 존재

### 권장 사항
1. **패턴 크기**: 3x3 ~ 7x7 권장 (너무 크면 성능 이슈)
2. **액터 위치**: 보통 패턴의 중앙 권장
3. **효과 배율**: 너무 높은 배율은 게임 밸런스 파괴 가능

## 🔗 관련 시스템

- **유닛 시스템**: 유닛의 `skillIds` 배열로 보유 스킬 정의
- **전투 시스템**: `BattleScript.cs`에서 스킬 실행 및 효과 적용
- **UI 시스템**: 스킬 메뉴, 범위 미리보기, 확인/취소 다이얼로그

---
**관련 문서:**
- [유닛 정의 포맷](UnitDefinition.md)
- [맵 데이터 포맷](MapDataFormat.md)
- [BattleScript API](../API/BattleScript.md)