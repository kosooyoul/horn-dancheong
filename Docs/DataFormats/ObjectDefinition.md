# 오브젝트 정의 포맷 스펙

## 📄 파일 정보

- **파일명**: `OBJECTS.json`
- **위치**: `Assets/1.Scripts/KO/Battle/MapData/OBJECTS.json`
- **용도**: 바닥 위에 배치되는 오브젝트 종류 및 속성 정의

## 🏗️ JSON 구조

### 루트 객체
```json
{
  "name": "string",
  "description": "string", 
  "version": "string",
  "objects": [ObjectInfo]
}
```

### ObjectInfo 객체
```json
{
  "id": number,
  "name": "string",
  "description": "string", 
  "colorHex": "string",
  "isBlocking": boolean,
  "isInteractable": boolean,
  "height": number,
  "prefabPath": "string"
}
```

## 📋 필드 상세

### 루트 레벨

#### name
- **타입**: `string`
- **설명**: 오브젝트 정의 파일의 이름
- **예시**: `"Object Definitions"`

#### description
- **타입**: `string`
- **설명**: 오브젝트 시스템에 대한 설명
- **예시**: `"바닥 위에 배치되는 오브젝트 정의 (0x00FF00 부분)"`

#### version
- **타입**: `string`
- **설명**: 정의 파일 버전
- **형식**: `"major.minor"`
- **예시**: `"1.0"`

### ObjectInfo 필드

#### id
- **타입**: `number` (정수)
- **설명**: 오브젝트의 고유 식별자
- **범위**: 0 ~ 255
- **특수값**: `0` - 오브젝트 없음

#### name
- **타입**: `string`
- **설명**: 오브젝트의 표시명
- **규칙**: 영문, 공백 가능
- **예시**: `"Rock"`, `"Treasure Chest"`

#### description
- **타입**: `string`
- **설명**: 오브젝트에 대한 상세 설명
- **용도**: 툴팁, 게임 내 설명 등

#### colorHex
- **타입**: `string`
- **설명**: 오브젝트의 기본 색상 (프리팹이 없을 때 사용)
- **형식**: `"#RRGGBB"` 또는 `"#RRGGBBAA"`
- **예시**: `"#708090"`, `"#DAA520"`

#### isBlocking
- **타입**: `boolean`
- **설명**: 이동 차단 여부
- **게임플레이**: 
  - `true`: 해당 타일로 이동 불가능
  - `false`: 통과 가능 (덤불, 크리스탈 등)

#### isInteractable
- **타입**: `boolean`
- **설명**: 상호작용 가능 여부
- **게임플레이**:
  - `true`: 클릭/터치로 상호작용 가능
  - `false`: 단순 장식 또는 장애물

#### height
- **타입**: `number` (실수)
- **설명**: 오브젝트의 높이 (Unity 단위)
- **용도**: 
  - 3D 모델 스케일링
  - 시야 차단 계산
  - Y축 위치 조정

#### prefabPath
- **타입**: `string`
- **설명**: Resources 폴더 내 프리팹 경로
- **형식**: `"폴더/서브폴더/프리팹명"` (확장자 제외)
- **예시**: `"Prefabs/Objects/Rock"`
- **빈 문자열**: 기본 큐브로 생성

## 🎯 오브젝트 카테고리

### 빈 오브젝트
```json
{
  "id": 0,
  "name": "None",
  "description": "오브젝트 없음",
  "colorHex": "#000000",
  "isBlocking": false,
  "isInteractable": false,
  "height": 0,
  "prefabPath": ""
}
```

### 장애물 (이동 차단)
```json
{
  "id": 1,
  "name": "Rock", 
  "description": "바위",
  "colorHex": "#708090",
  "isBlocking": true,
  "isInteractable": false,
  "height": 1,
  "prefabPath": "Prefabs/Objects/Rock"
}
```

### 상호작용 오브젝트
```json
{
  "id": 4,
  "name": "Chest",
  "description": "보물상자",
  "colorHex": "#DAA520", 
  "isBlocking": true,
  "isInteractable": true,
  "height": 1,
  "prefabPath": "Prefabs/Objects/Chest"
}
```

### 장식 오브젝트 (통과 가능)
```json
{
  "id": 3,
  "name": "Bush",
  "description": "덤불", 
  "colorHex": "#32CD32",
  "isBlocking": false,
  "isInteractable": false,
  "height": 0.5,
  "prefabPath": "Prefabs/Objects/Bush"
}
```

### 시스템 오브젝트
```json
{
  "id": 10,
  "name": "SpawnPoint",
  "description": "스폰 지점",
  "colorHex": "#00FFFF",
  "isBlocking": false, 
  "isInteractable": false,
  "height": 0,
  "prefabPath": "Prefabs/Objects/SpawnPoint"
}
```

## 🎮 게임플레이 속성 조합

### 이동 차단 + 상호작용
- **용도**: 보물상자, 문, 스위치
- **특징**: 접근해서 상호작용 필요
- **예시**: `isBlocking: true, isInteractable: true`

### 통과 가능 + 상호작용  
- **용도**: 크리스탈, 포탈, 아이템
- **특징**: 위에 서서 상호작용 가능
- **예시**: `isBlocking: false, isInteractable: true`

### 이동 차단 + 비상호작용
- **용도**: 바위, 기둥, 조각상
- **특징**: 순수 장애물 또는 엄폐물
- **예시**: `isBlocking: true, isInteractable: false`

### 통과 가능 + 비상호작용
- **용도**: 덤불, 횃불, 장식품
- **특징**: 시각적 효과, 분위기 연출
- **예시**: `isBlocking: false, isInteractable: false`

## 🎨 프리팹 시스템

### 프리팹 우선순위
1. **Resources 프리팹**: `prefabPath`가 유효한 경우
2. **기본 큐브**: 프리팹을 찾을 수 없는 경우
3. **색상 적용**: 기본 큐브에 `colorHex` 적용

### 프리팹 규칙
- **위치**: `Resources/` 폴더 하위
- **스케일**: 프리팹은 1x1x1 기준으로 제작
- **피벗**: 바닥 중앙 (0, 0, 0)
- **높이**: 코드에서 `height` 값으로 Y축 스케일링

### 예시 폴더 구조
```
Resources/
├── Prefabs/
│   └── Objects/
│       ├── Rock.prefab
│       ├── Tree.prefab
│       ├── Chest.prefab
│       └── ...
```

## ✅ 유효성 검사

### 필수 검사
1. **ID 고유성**: 중복된 ID 없음
2. **ID 범위**: 0 ~ 255  
3. **필수 필드**: 모든 ObjectInfo 필드 존재
4. **높이 값**: 0 이상의 숫자
5. **색상 형식**: 유효한 16진수 색상 코드

### 논리적 일관성
1. **빈 오브젝트**: ID 0은 항상 비차단, 비상호작용
2. **높이 0**: 바닥과 같은 높이 (스폰포인트, 포탈 등)
3. **상호작용 오브젝트**: 게임플레이적으로 의미 있는 배치

### 권장 사항
1. **ID 0 예약**: 오브젝트 없음
2. **ID 1-10**: 기본 장애물 (바위, 나무 등)
3. **ID 11-20**: 상호작용 오브젝트 (상자, 문 등)
4. **ID 21-30**: 장식 오브젝트 (덤불, 횃불 등)
5. **ID 31-40**: 시스템 오브젝트 (스폰포인트, 포탈 등)

## 🔧 확장 가능성

### 미래 확장 필드
```json
{
  "hitPoints": number,          // 체력 (파괴 가능 오브젝트)
  "soundEffect": "string",      // 상호작용 시 효과음
  "animation": "string",        // 애니메이션 트리거
  "lightRadius": number,        // 조명 반경 (횃불 등)
  "durability": number,         // 내구도 (배럴, 상자 등)
  "lootTable": "string"         // 드롭 아이템 테이블
}
```

### 조건부 속성
```json
{
  "requirements": {
    "playerLevel": 5,           // 상호작용 필요 레벨
    "hasKey": "string",         // 필요한 열쇠
    "questCompleted": "string"  // 완료 필요 퀘스트
  }
}
```

## 📝 예시 파일

### 최소 구조
```json
{
  "name": "Basic Objects",
  "description": "기본 오브젝트 정의", 
  "version": "1.0",
  "objects": [
    {
      "id": 0,
      "name": "None",
      "description": "오브젝트 없음",
      "colorHex": "#000000",
      "isBlocking": false,
      "isInteractable": false, 
      "height": 0,
      "prefabPath": ""
    },
    {
      "id": 1,
      "name": "Rock", 
      "description": "바위",
      "colorHex": "#708090",
      "isBlocking": true,
      "isInteractable": false,
      "height": 1,
      "prefabPath": "Prefabs/Objects/Rock"
    }
  ]
}
```

### 완전한 구조 (현재 프로젝트)
현재 프로젝트의 `OBJECTS.json` 파일 참조:
- 12가지 다양한 오브젝트 타입
- 장애물, 상호작용, 장식, 시스템 오브젝트
- 완전한 게임플레이 속성 정의

---
**관련 문서:**
- [타일 정의 포맷](TileDefinition.md)
- [맵 데이터 포맷](MapDataFormat.md)
- [오브젝트 시스템 스펙](../Systems/ObjectSystem.md)