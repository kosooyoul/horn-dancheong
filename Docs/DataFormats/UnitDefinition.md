# 유닛 정의 포맷 스펙

## 📄 파일 정보

- **파일명**: `ALLYS.json` (아군), `ENEMIES.json` (적군)
- **위치**: `Assets/1.Scripts/KO/Battle/MapData/`
- **용도**: 전투에 등장하는 유닛 종류 및 기본 스탯 정의
- **참조 관계**:
  - `ALLYS.json` → 맵의 `allySpawns[].id`로 참조
  - `ENEMIES.json` → 맵의 `enemySpawns[].id`로 참조

## 🏗️ JSON 구조

### 루트 객체
```json
{
  "name": "string",
  "description": "string",
  "version": "string",
  "units": [UnitDefinition]
}
```

### UnitDefinition 객체
```json
{
  "id": number,
  "name": "string",
  "description": "string",
  "colorHex": "string",
  "prefabPath": "string",
  "defaultStats": UnitDefaultStats
}
```

### UnitDefaultStats 객체
```json
{
  "agility": number,
  "spirit": number,
  "guard": number,
  "luck": number,
  "mov": number
}
```

## 📋 필드 상세

### 루트 레벨

#### name
- **타입**: `string`
- **설명**: 유닛 정의 파일의 이름
- **예시**: `"Ally Definitions"`, `"Enemy Definitions"`

#### description
- **타입**: `string`
- **설명**: 유닛 정의 파일에 대한 설명
- **예시**: `"아군 유닛 종류 정의 (allySpawns의 id로 참조)"`

#### version
- **타입**: `string`
- **설명**: 정의 파일 버전
- **형식**: `"major.minor"`
- **예시**: `"1.0"`

### UnitDefinition 필드

#### id
- **타입**: `number` (정수)
- **설명**: 유닛 종류의 고유 식별자. 맵의 스폰 정의(`allySpawns` / `enemySpawns`)에서 이 값을 참조
- **제한**: 파일 내 고유, 1 이상 권장 (0 이하는 빈 슬롯 의미로 예약)

#### name
- **타입**: `string`
- **설명**: 유닛 표시명
- **예시**: `"Warrior"`, `"Goblin"`

#### description
- **타입**: `string`
- **설명**: 유닛에 대한 상세 설명
- **용도**: 툴팁, 디버깅 정보 등

#### colorHex
- **타입**: `string`
- **설명**: 박스 마커 색상 (프리팹 미지정 시 사용)
- **형식**: `"#RRGGBB"`
- **예시**: `"#3366FF"`

#### prefabPath
- **타입**: `string`
- **설명**: `Resources` 폴더 기준 프리팹 경로. 비우면 기본 박스 마커 사용
- **예시**: `""`, `"Units/Warrior"`

#### defaultStats
- **타입**: `UnitDefaultStats`
- **설명**: 유닛의 기본(원시) 스탯 묶음

### UnitDefaultStats 필드

#### agility (민첩)
- **타입**: `number` (정수)
- **설명**: 행동 순서(이니셔티브)와 회피율에 영향
- **게임플레이**: 이니셔티브 = `agility × 10 + luck`

#### spirit (영력)
- **타입**: `number` (정수)
- **설명**: 스킬 데미지 및 회복량에 영향

#### guard (방어)
- **타입**: `number` (정수)
- **설명**: 최대 체력 및 피해 감소에 영향

#### luck (운)
- **타입**: `number` (정수)
- **설명**: 치명타/회피 확률 및 이니셔티브 동점 보정에 영향

#### mov (이동)
- **타입**: `number` (정수)
- **설명**: 한 턴에 이동 가능한 기본 칸 수. 민첩과 독립된 명시 스탯
- **게임플레이**: `BattleUnitEntry.MoveRange`로 노출됨 (정의가 없는 빈 슬롯은 0)
- **권장 범위**: 3 ~ 6

## ✅ 유효성 검사

### 필수 검사
1. **ID 고유성**: 파일 내 중복된 `id` 없음
2. **스폰 참조 일치**: 맵의 `allySpawns[].id` / `enemySpawns[].id`가 정의 파일에 존재하는지 확인
3. **필수 필드**: 모든 `UnitDefinition` 및 `UnitDefaultStats` 필드 존재
4. **색상 형식**: 유효한 `#RRGGBB` 색상 코드

### 논리적 일관성
1. **빈 슬롯**: `allySpawns[].id <= 0`이면 빈 슬롯으로 취급되어 정의를 참조하지 않음
2. **스탯 범위**: 모든 스탯은 0 이상의 정수
3. **이동 거리**: `mov`는 1 이상 권장 (0이면 이동 불가 유닛)

## 📝 예시 파일

### ALLYS.json
```json
{
  "name": "Ally Definitions",
  "description": "아군 유닛 종류 정의 (allySpawns의 id로 참조)",
  "version": "1.0",
  "units": [
    {
      "id": 1,
      "name": "Warrior",
      "description": "근접 전사",
      "colorHex": "#3366FF",
      "prefabPath": "",
      "defaultStats": { "agility": 5, "spirit": 3, "guard": 9, "luck": 4, "mov": 4 }
    },
    {
      "id": 2,
      "name": "Archer",
      "description": "원거리 궁수",
      "colorHex": "#33CCFF",
      "prefabPath": "",
      "defaultStats": { "agility": 8, "spirit": 6, "guard": 4, "luck": 6, "mov": 5 }
    }
  ]
}
```

### ENEMIES.json
```json
{
  "name": "Enemy Definitions",
  "description": "적 유닛 종류 정의 (enemySpawns의 id로 참조)",
  "version": "1.0",
  "units": [
    {
      "id": 1,
      "name": "Goblin",
      "description": "기본 근접 적",
      "colorHex": "#FF3333",
      "prefabPath": "",
      "defaultStats": { "agility": 5, "spirit": 3, "guard": 5, "luck": 3, "mov": 5 }
    },
    {
      "id": 2,
      "name": "Orc",
      "description": "강한 근접 적",
      "colorHex": "#CC0000",
      "prefabPath": "",
      "defaultStats": { "agility": 4, "spirit": 4, "guard": 9, "luck": 3, "mov": 3 }
    }
  ]
}
```

## 🔗 관련 시스템 참고

- KD 쪽 `StatCalculator`는 별도의 파생 스탯 체계를 사용하며, 이동 거리를 민첩에서 파생시킵니다(`moveRange = Clamp(2 + agility / 5, 2, 6)`). BattleScript 쪽 유닛 정의는 이와 독립적으로 `mov`를 명시값으로 다룹니다. 두 체계를 통일할 경우 별도 작업이 필요합니다.

---
**관련 문서:**
- [맵 데이터 포맷](MapDataFormat.md)
- [타일 정의 포맷](TileDefinition.md)
- [오브젝트 정의 포맷](ObjectDefinition.md)
- [BattleScript API](../API/BattleScript.md)
