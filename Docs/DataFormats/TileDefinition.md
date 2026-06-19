# 타일 정의 포맷 스펙

## 📄 파일 정보

- **파일명**: `TILES.json`
- **위치**: `Assets/1.Scripts/KO/Battle/MapData/TILES.json`
- **용도**: 바닥 타일 종류 및 속성 정의

## 🏗️ JSON 구조

### 루트 객체
```json
{
  "name": "string",
  "description": "string",
  "version": "string",
  "tiles": [TileInfo]
}
```

### TileInfo 객체
```json
{
  "id": number,
  "name": "string", 
  "description": "string",
  "colorHex": "string",
  "isWalkable": boolean,
  "isTransparent": boolean,
  "movementCost": number
}
```

## 📋 필드 상세

### 루트 레벨

#### name
- **타입**: `string`
- **설명**: 타일 정의 파일의 이름
- **예시**: `"Tile Definitions"`

#### description
- **타입**: `string`
- **설명**: 타일 시스템에 대한 설명
- **예시**: `"바닥 타일 종류 정의 (0x0000FF 부분)"`

#### version
- **타입**: `string`
- **설명**: 정의 파일 버전
- **형식**: `"major.minor"`
- **예시**: `"1.0"`

### TileInfo 필드

#### id
- **타입**: `number` (정수)
- **설명**: 타일의 고유 식별자
- **범위**: 0 ~ 255
- **특수값**: 
  - `0`: 빈 공간 (타일 없음)
  - `255`: 벽 타일 (관례상)

#### name
- **타입**: `string`
- **설명**: 타일의 표시명
- **규칙**: 영문, 공백 가능
- **예시**: `"Floor"`, `"Stone Wall"`

#### description
- **타입**: `string`
- **설명**: 타일에 대한 상세 설명
- **용도**: 툴팁, 디버깅 정보 등

#### colorHex
- **타입**: `string`
- **설명**: 타일의 기본 색상 (16진수)
- **형식**: `"#RRGGBB"` 또는 `"#RRGGBBAA"`
- **예시**: `"#E6E6FA"`, `"#00000000"` (투명)

#### isWalkable
- **타입**: `boolean`
- **설명**: 이동 가능 여부
- **게임플레이**: 경로 찾기, 이동 제한에 사용
- **기본값**: `true` (이동 가능)

#### isTransparent
- **타입**: `boolean`
- **설명**: 시야 투과 여부
- **게임플레이**: 시야 계산, 조준선에 사용
- **기본값**: `true` (투과 가능)

#### movementCost
- **타입**: `number` (정수)
- **설명**: 이동 비용 (액션 포인트)
- **범위**: 0 이상의 정수
- **특수값**:
  - `0`: 이동 불가능 (isWalkable: false와 동일)
  - `1`: 기본 이동 비용
  - `2+`: 어려운 지형 (진흙, 모래 등)

## 🎯 타일 카테고리

### 기본 타일
```json
{
  "id": 0,
  "name": "Empty", 
  "description": "빈 공간",
  "colorHex": "#000000",
  "isWalkable": false,
  "isTransparent": true,
  "movementCost": 0
}
```

### 일반 바닥
```json
{
  "id": 1,
  "name": "Floor",
  "description": "기본 바닥", 
  "colorHex": "#E6E6FA",
  "isWalkable": true,
  "isTransparent": true,
  "movementCost": 1
}
```

### 어려운 지형
```json
{
  "id": 8,
  "name": "Mud",
  "description": "진흙 바닥",
  "colorHex": "#8B4513",
  "isWalkable": true,
  "isTransparent": true,
  "movementCost": 3
}
```

### 장애물
```json
{
  "id": 255,
  "name": "Wall",
  "description": "벽",
  "colorHex": "#696969",
  "isWalkable": false,
  "isTransparent": false,
  "movementCost": 0
}
```

## ✅ 유효성 검사

### 필수 검사
1. **ID 고유성**: 중복된 ID 없음
2. **ID 범위**: 0 ~ 255
3. **필수 필드**: 모든 TileInfo 필드 존재
4. **색상 형식**: 유효한 16진수 색상 코드
5. **이동 비용**: 0 이상의 정수

### 논리적 일관성
1. **이동 불가능 타일**: `movementCost == 0` ⟺ `isWalkable == false`
2. **빈 공간**: ID 0은 항상 이동 불가능
3. **벽 타일**: 일반적으로 불투명 (`isTransparent: false`)

### 권장 사항
1. **ID 0 예약**: 빈 공간용으로 사용
2. **ID 255 예약**: 벽 타일용으로 사용  
3. **ID 1-10**: 기본 바닥 타일들
4. **ID 11-50**: 특수 바닥 타일들
5. **ID 51-254**: 확장용 예약

## 🔧 확장 가능성

### 미래 확장 필드
```json
{
  "soundEffect": "string",      // 발소리 효과
  "particle": "string",         // 파티클 효과
  "texture": "string",          // 텍스처 경로
  "heightOffset": "number",     // 높이 오프셋
  "damagePerTurn": "number",    // 턴당 데미지 (용암 등)
  "healPerTurn": "number"       // 턴당 회복 (성수 등)
}
```

### 조건부 속성
```json
{
  "conditionalEffects": [
    {
      "condition": "weather_rain",
      "movementCost": 2,
      "isWalkable": false
    }
  ]
}
```

## 📝 예시 파일

### 최소 구조
```json
{
  "name": "Basic Tiles",
  "description": "기본 타일 정의",
  "version": "1.0",
  "tiles": [
    {
      "id": 0,
      "name": "Empty",
      "description": "빈 공간",
      "colorHex": "#000000", 
      "isWalkable": false,
      "isTransparent": true,
      "movementCost": 0
    },
    {
      "id": 1,
      "name": "Floor",
      "description": "기본 바닥",
      "colorHex": "#FFFFFF",
      "isWalkable": true, 
      "isTransparent": true,
      "movementCost": 1
    }
  ]
}
```

### 완전한 구조 (현재 프로젝트)
현재 프로젝트의 `TILES.json` 파일 참조:
- 10가지 다양한 타일 타입
- 기본 바닥부터 특수 지형까지
- 게임플레이 속성 완전 정의

---
**관련 문서:**
- [오브젝트 정의 포맷](ObjectDefinition.md)
- [맵 데이터 포맷](MapDataFormat.md)
- [타일 시스템 스펙](../Systems/TileSystem.md)