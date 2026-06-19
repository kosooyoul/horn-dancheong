# 맵 데이터 포맷 스펙

## 📄 파일 구조

### 기본 정보
- **파일명**: `{맵이름}.json`
- **인코딩**: UTF-8
- **위치**: `Assets/1.Scripts/KO/Battle/MapData/`

## 🏗️ JSON 구조

### 루트 객체
```json
{
  "mapName": "string",
  "description": "string", 
  "width": number,
  "height": number,
  "tiles": [number],
  "tileTypes": [TileTypeInfo],
  "mapChipExamples": MapChipExamples,
  "placedObjects": PlacedObjects
}
```

### 필수 필드

#### mapName
- **타입**: `string`
- **설명**: 맵의 표시명
- **예시**: `"Small Arena with Objects"`

#### description  
- **타입**: `string`
- **설명**: 맵에 대한 상세 설명
- **예시**: `"작은 아레나 형태의 배틀맵 - 다양한 오브젝트 배치"`

#### width, height
- **타입**: `number` (정수)
- **설명**: 맵의 가로/세로 크기 (그리드 단위)
- **제한**: 1 이상의 정수
- **예시**: `21, 21`

#### tiles
- **타입**: `number[]` (정수 배열)
- **설명**: 맵 칩 번호들의 1차원 배열 (row-major order)
- **길이**: `width × height`
- **값 범위**: 0 ~ 4294967295 (32비트)

### 선택적 필드

#### tileTypes (호환성용)
```json
{
  "id": number,
  "name": "string", 
  "colorHex": "string"
}
```
- **설명**: 기존 타일 타입 정의 (하위 호환용)

#### mapChipExamples (문서용)
```json
{
  "description": "string",
  "examples": [
    {
      "value": number,
      "description": "string", 
      "breakdown": "string"
    }
  ]
}
```

#### placedObjects (문서용)
```json
{
  "description": "string",
  "objects": [
    {
      "name": "string",
      "id": number,
      "locations": "string",
      "purpose": "string"
    }
  ]
}
```

## 🔢 맵 칩 번호 규칙

### 비트 구성
```
32비트: 0x00OOTTTT
- TT: 타일 ID (0x0000FF)
- OO: 오브젝트 ID (0x00FF00)
```

### 계산 방법
```javascript
mapChip = (objectId << 8) | tileId
tileId = mapChip & 0xFF
objectId = (mapChip >> 8) & 0xFF
```

### 유효한 값 범위
- **타일 ID**: 0 ~ 255
- **오브젝트 ID**: 0 ~ 255  
- **결합 값**: 0 ~ 65535

## 📏 배열 인덱싱

### Row-Major Order
```
그리드 위치 (x, z) → 배열 인덱스: z * width + x
배열 인덱스 i → 그리드 위치: (i % width, i / width)
```

### 예시 (3×3 맵)
```
그리드:     배열 인덱스:
0 1 2       0 1 2
3 4 5  →    3 4 5  
6 7 8       6 7 8
```

## ✅ 유효성 검사

### 필수 검사 항목
1. **배열 길이**: `tiles.length === width × height`
2. **맵 크기**: `width > 0 && height > 0`
3. **맵 칩 값**: `모든 값 >= 0 && <= 65535`
4. **문자열 필드**: `null이 아니고 공백이 아님`

### 권장 검사 항목
1. **타일 ID 유효성**: TILES.json에 정의된 ID인지 확인
2. **오브젝트 ID 유효성**: OBJECTS.json에 정의된 ID인지 확인
3. **맵 대칭성**: 대칭 맵의 경우 구조 검증

## 🔧 도구 및 유틸리티

### 맵 칩 계산기
```csharp
// C# 유틸리티 함수
public static int CombineMapChip(int tileId, int objectId) 
{
    return (tileId & 0xFF) | ((objectId & 0xFF) << 8);
}

public static (int tileId, int objectId) SplitMapChip(int mapChip)
{
    return (mapChip & 0xFF, (mapChip >> 8) & 0xFF);
}
```

### JSON 검증 스키마
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "required": ["mapName", "width", "height", "tiles"],
  "properties": {
    "mapName": {"type": "string", "minLength": 1},
    "width": {"type": "integer", "minimum": 1},
    "height": {"type": "integer", "minimum": 1},
    "tiles": {"type": "array", "items": {"type": "integer", "minimum": 0, "maximum": 65535}}
  }
}
```

## 📝 예시 파일

### 최소 구조
```json
{
  "mapName": "Simple Test",
  "description": "테스트용 2x2 맵",
  "width": 2,
  "height": 2, 
  "tiles": [1, 1, 1, 1]
}
```

### 오브젝트 포함 구조
```json
{
  "mapName": "Arena with Objects",
  "description": "오브젝트가 포함된 아레나",
  "width": 3,
  "height": 3,
  "tiles": [
    255, 255, 255,
    255, 1281, 255,
    255, 255, 255
  ]
}
```

---
**관련 문서:**
- [타일 정의 포맷](TileDefinition.md)
- [오브젝트 정의 포맷](ObjectDefinition.md)
- [맵 시스템 스펙](../Systems/MapSystem.md)