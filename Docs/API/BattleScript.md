# BattleScript API 레퍼런스

## 📋 클래스 개요

`BattleScript`는 전투 맵의 로딩, 생성, 관리를 담당하는 핵심 컴포넌트입니다.

```csharp
public class BattleScript : MonoBehaviour
```

## 🔧 Inspector 설정

### Battle Map Settings
```csharp
[Header("Battle Map Settings")]
[SerializeField] private GameObject floorCubePrefab;
[SerializeField] private float cubeSpacing = 1f;
```

### Map Loading
```csharp
[Header("Map Loading")]
[SerializeField] private string mapFolderPath = "Assets/1.Scripts/KO/Battle/MapData/";
[SerializeField] private string defaultMapName = "basic_10x10";
[SerializeField] private bool loadFromJSON = true;
```

## 🎯 공개 메서드 (Public Methods)

### 맵 정보 조회

#### `GetMapChip(int x, int z)`
특정 위치의 맵 칩 번호를 반환합니다.
```csharp
public int GetMapChip(int x, int z)
```
- **매개변수**: `x, z` - 그리드 좌표
- **반환값**: 맵 칩 번호 (32비트 정수)
- **예외**: 유효하지 않은 좌표는 0 반환

#### `GetTileType(int x, int z)`
특정 위치의 타일 ID를 반환합니다.
```csharp
public int GetTileType(int x, int z)
```
- **매개변수**: `x, z` - 그리드 좌표  
- **반환값**: 타일 ID (0~255)

#### `GetObjectType(int x, int z)`
특정 위치의 오브젝트 ID를 반환합니다.
```csharp
public int GetObjectType(int x, int z)
```
- **매개변수**: `x, z` - 그리드 좌표
- **반환값**: 오브젝트 ID (0~255)

### 게임플레이 지원

#### `IsWalkable(int x, int z)`
해당 위치로 이동 가능한지 판단합니다.
```csharp
public bool IsWalkable(int x, int z)
```
- **매개변수**: `x, z` - 그리드 좌표
- **반환값**: 이동 가능 여부
- **판단 기준**: 
  - 타일의 `isWalkable` 속성
  - 오브젝트의 `isBlocking` 속성

#### `GetMovementCost(int x, int z)`
해당 위치의 이동 비용을 반환합니다.
```csharp
public int GetMovementCost(int x, int z)
```
- **매개변수**: `x, z` - 그리드 좌표
- **반환값**: 이동 비용 (1~N)

#### `HasTile(int x, int z)`
해당 위치에 타일이 있는지 확인합니다.
```csharp
public bool HasTile(int x, int z)
```

#### `HasObject(int x, int z)`
해당 위치에 오브젝트가 있는지 확인합니다.
```csharp
public bool HasObject(int x, int z)
```

### 좌표 변환

#### `WorldToGrid(Vector3 worldPosition)`
월드 좌표를 그리드 좌표로 변환합니다.
```csharp
public Vector2Int WorldToGrid(Vector3 worldPosition)
```
- **매개변수**: `worldPosition` - Unity 월드 좌표
- **반환값**: 그리드 좌표 (Vector2Int)

#### `GridToWorld(int gridX, int gridZ)`
그리드 좌표를 월드 좌표로 변환합니다.
```csharp
public Vector3 GridToWorld(int gridX, int gridZ)
```
- **매개변수**: `gridX, gridZ` - 그리드 좌표
- **반환값**: Unity 월드 좌표 (Vector3)

### 맵 관리

#### `LoadMap(string mapName)`
런타임에 새로운 맵을 로딩합니다.
```csharp
public bool LoadMap(string mapName)
```
- **매개변수**: `mapName` - 로딩할 맵 이름 (확장자 제외)
- **반환값**: 로딩 성공 여부
- **부작용**: 기존 맵 오브젝트들이 삭제됨

#### `GetAvailableMaps()`
사용 가능한 맵 목록을 반환합니다.
```csharp
public List<string> GetAvailableMaps()
```
- **반환값**: 맵 이름 리스트 (확장자 제외)

#### `GetMapSize()`
현재 맵의 크기를 반환합니다.
```csharp
public Vector2Int GetMapSize()
```
- **반환값**: 맵 크기 (width, height)

#### `GetFloorCube(int x, int z)`
특정 위치의 바닥 큐브 GameObject를 반환합니다.
```csharp
public GameObject GetFloorCube(int x, int z)
```

### 정보 조회

#### `GetTileInfo(int tileId)`
타일 ID에 해당하는 상세 정보를 반환합니다.
```csharp
public TileInfo GetTileInfo(int tileId)
```
- **반환값**: `TileInfo` 객체 또는 `null`

#### `GetObjectInfo(int objectId)`
오브젝트 ID에 해당하는 상세 정보를 반환합니다.
```csharp
public ObjectInfo GetObjectInfo(int objectId)
```
- **반환값**: `ObjectInfo` 객체 또는 `null`

#### `GetCurrentMapData()`
현재 로딩된 맵 데이터를 반환합니다.
```csharp
public MapData GetCurrentMapData()
```

## 🔒 비공개 메서드 (Private Methods)

### 초기화
- `LoadTileAndObjectDefinitions()` - 타일/오브젝트 정의 로딩
- `LoadMapData()` - 맵 데이터 초기화
- `LoadMapFromJSON(string mapName)` - JSON 파일에서 맵 로딩

### 맵 생성
- `CreateBattleMap()` - 맵 오브젝트들 생성
- `CreateFloorCube(...)` - 바닥 타일 생성
- `CreateObjectCube(...)` - 오브젝트 생성
- `CreateDefaultObjectCube(...)` - 기본 오브젝트 큐브 생성

### 유틸리티
- `GetTileId(int mapChip)` - 맵 칩에서 타일 ID 추출
- `GetObjectId(int mapChip)` - 맵 칩에서 오브젝트 ID 추출
- `CombineMapChip(int tileId, int objectId)` - 맵 칩 번호 생성

## 📊 데이터 구조

### MapData
```csharp
[System.Serializable]
public class MapData
{
    public string mapName;
    public string description;
    public int width;
    public int height;
    public int[] tiles;
    public TileTypeInfo[] tileTypes;
}
```

### TileInfo
```csharp
[System.Serializable]
public class TileInfo
{
    public int id;
    public string name;
    public string description;
    public string colorHex;
    public bool isWalkable;
    public bool isTransparent;
    public int movementCost;
}
```

### ObjectInfo
```csharp
[System.Serializable] 
public class ObjectInfo
{
    public int id;
    public string name;
    public string description;
    public string colorHex;
    public bool isBlocking;
    public bool isInteractable;
    public float height;
    public string prefabPath;
}
```

## 🎮 사용 예시

### 기본 사용법
```csharp
// BattleScript 컴포넌트 참조
BattleScript battleScript = GetComponent<BattleScript>();

// 특정 위치 이동 가능 여부 확인
bool canMove = battleScript.IsWalkable(5, 3);

// 맵 크기 조회
Vector2Int mapSize = battleScript.GetMapSize();

// 새 맵 로딩
bool success = battleScript.LoadMap("arena_large");
```

### 경로 찾기 연동
```csharp
public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
{
    // A* 알고리즘에서 이동 가능 여부 확인
    if (!battleScript.IsWalkable(x, z)) return null;
    
    // 이동 비용 계산
    int cost = battleScript.GetMovementCost(x, z);
    // ... 경로 찾기 로직
}
```

### 상호작용 시스템
```csharp
public void OnTileClicked(Vector2Int gridPos)
{
    if (battleScript.HasObject(gridPos.x, gridPos.y))
    {
        int objectId = battleScript.GetObjectType(gridPos.x, gridPos.y);
        ObjectInfo info = battleScript.GetObjectInfo(objectId);
        
        if (info.isInteractable)
        {
            // 상호작용 로직 실행
            InteractWithObject(info);
        }
    }
}
```

---
**관련 문서:**
- [맵 시스템 스펙](../Systems/MapSystem.md)
- [맵 제작 가이드](../Guides/MapCreation.md)
- [맵 데이터 포맷](../DataFormats/MapDataFormat.md)