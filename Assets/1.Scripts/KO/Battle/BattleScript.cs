using UnityEngine;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class MapData
{
    public string mapName;
    public string description;
    public int width;
    public int height;
    public int[] tiles; // 1차원 배열로 저장 (row-major order) - 32비트 값으로 타일+오브젝트 정보 포함
    public TileTypeInfo[] tileTypes;
    public UnitPlacement[] allySpawns;   // 아군이 배치될 수 있는 빈 스폰 슬롯
    public EnemyPlacement[] enemySpawns; // 맵에 고정 배치되는 적 유닛
}

// 아군 스폰 슬롯 정의 — id가 0 이하면 플레이어가 채울 빈 슬롯
[System.Serializable]
public class UnitPlacement
{
    public int x;
    public int y;
    public int id; // ALLYS.json의 유닛 정의 id. 0 이하면 빈 슬롯
}

// 적 유닛 배치 정의 — id로 ENEMIES.json의 유닛 정의를 참조
[System.Serializable]
public class EnemyPlacement
{
    public int x;
    public int y;
    public int id; // ENEMIES.json의 유닛 정의 id
}

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

[System.Serializable]
public class TileCollection
{
    public string name;
    public string description;
    public string version;
    public TileInfo[] tiles;
}

[System.Serializable]
public class ObjectCollection
{
    public string name;
    public string description;
    public string version;
    public ObjectInfo[] objects;
}

// 유닛 기본 스탯 — KD.UnitBaseStats와 동일한 4개 원시 스탯 (추후 StatCalculator로 매핑)
[System.Serializable]
public class UnitDefaultStats
{
    public int agility; // 민첩 — 행동 순서, 회피율
    public int spirit;  // 영력 — 스킬 데미지 및 회복량
    public int guard;   // 방어 — 최대 체력 및 피해 감소
    public int luck;    // 운 — 치명타/회피 확률
    public int mov;     // 이동 — 한 턴에 이동 가능한 기본 칸 수
}

// 아군/적군 유닛 종류 정의 (ALLYS.json / ENEMIES.json 공용)
[System.Serializable]
public class UnitDefinition
{
    public int id;
    public string name;
    public string description;
    public string colorHex;   // 박스 마커 색상 (#RRGGBB)
    public string prefabPath; // Resources 프리팹 경로 (비우면 박스 사용)
    public UnitDefaultStats defaultStats; // 기본 스탯
}

[System.Serializable]
public class UnitDefinitionCollection
{
    public string name;
    public string description;
    public string version;
    public UnitDefinition[] units;
}

[System.Serializable]
public class TileTypeInfo
{
    public int id;
    public string name;
    public string colorHex; // #FFFFFF 형식
}

[System.Serializable]
public class MapCollection
{
    public MapData[] maps;
}

// 맵에 배치된 유닛의 런타임 정보 — 마커/정의/위치/진영을 담는다.
public class BattleUnitEntry
{
    public GameObject marker;        // 화면에 표시되는 유닛 마커
    public UnitDefinition definition; // ALLYS.json / ENEMIES.json 정의 (빈 슬롯이면 null)
    public Vector2Int grid;          // 현재 위치
    public bool isEnemy;             // true면 적군, false면 아군

    public BattleUnitEntry(GameObject marker, UnitDefinition definition, Vector2Int grid, bool isEnemy)
    {
        this.marker = marker;
        this.definition = definition;
        this.grid = grid;
        this.isEnemy = isEnemy;

        // TODO: 에러 발생, 임시 주석 처리
        // int guard = definition != null && definition.defaultStats != null ? definition.defaultStats.guard : 0;
        // this._maxHp = guard > 0 ? guard * 10f : 100f;
        // this._currentHp = this._maxHp;
    }

    // 빈 슬롯(정의 없음)은 스탯을 0으로 취급
    public int MoveRange => definition != null && definition.defaultStats != null ? definition.defaultStats.mov : 0;

    public string DisplayName => definition != null && !string.IsNullOrWhiteSpace(definition.name)
        ? definition.name
        : "Unit";

    // 표시 이름 별칭
    public string UnitName => DisplayName;

    // 최대 체력 — guard 기반 (KD.StatCalculator와 동일한 공식: 50 + guard * 10)
    public int MaxHP => definition != null && definition.defaultStats != null
        ? 50 + definition.defaultStats.guard * 10
        : 0;

    // 런타임 체력 상태 — 음수면 미초기화로 보고 최대 체력으로 취급
    private int currentHP = -1;

    public int CurrentHP
    {
        get => currentHP < 0 ? MaxHP : currentHP;
        set => currentHP = Mathf.Clamp(value, 0, MaxHP);
    }
}

public class BattleScript : MonoBehaviour
{
    // [Header("UI Integration")]
    // [SerializeField] private GameObject initiativeUIGameObject;
    // private IInitiativeUI InitiativeUI;

    [Header("Map Only Mode")]
    [Tooltip("true면 맵 생성까지만 실행. 유닛 배치/턴 시스템은 KD 시스템이 담당한다. MVP에서는 true.")]
    [SerializeField] private bool mapOnlyMode = true;

    [Header("Battle Map Settings")]
    [SerializeField] private GameObject floorCubePrefab;
    [SerializeField] private float cubeSpacing = 1f;
    
    [Header("Map Loading")]
    [SerializeField] private string mapFolderPath = "Assets/1.Scripts/KO/Battle/MapData/";
    [SerializeField] private string defaultMapName = "basic_10x10";
    [SerializeField] private bool loadFromJSON = true;
    
    [Header("Unit Placement")]
    [Tooltip("켜면 비어 있는 아군 스폰 슬롯에도 시작 시 자동으로 박스를 올림 (테스트용)")]
    [SerializeField] private bool autoFillAllySpawns = true;
    [Tooltip("유닛 박스 마커 크기")]
    [SerializeField] private Vector3 unitMarkerSize = new Vector3(0.6f, 1f, 0.6f);
    [Tooltip("아군 마커 색상")]
    [SerializeField] private Color allyColor = new Color(0.2f, 0.4f, 1f);
    [Tooltip("적군 마커 색상")]
    [SerializeField] private Color enemyColor = new Color(1f, 0.2f, 0.2f);
    [Tooltip("유닛이 한 칸 이동할 때의 보간 속도 (월드 단위/초)")]
    [SerializeField] private float unitMoveSpeed = 6f;
    
    [Header("Fallback Map Layout (JSON 비활성화 시 사용)")]
    [SerializeField] private int[,] fallbackMapLayout = new int[10, 10] 
    {
        {1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
        {1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
        {1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
        {1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
        {1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
        {1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
        {1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
        {1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
        {1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
        {1, 1, 1, 1, 1, 1, 1, 1, 1, 1}
    };
    
    private GameObject[,] floorCubes;
    private GameObject[,] objectCubes;
    private Transform battleMapParent;
    private Transform objectMapParent;
    private int mapWidth;
    private int mapHeight;
    private int[,] currentMapLayout;
    private MapData currentMapData;
    private Dictionary<int, Color> tileColors;
    
    // 맵 칩 시스템
    private Dictionary<int, TileInfo> tileDefinitions;
    private Dictionary<int, ObjectInfo> objectDefinitions;
    
    // 유닛 종류 정의 (ALLYS.json / ENEMIES.json)
    private Dictionary<int, UnitDefinition> allyDefinitions;
    private Dictionary<int, UnitDefinition> enemyDefinitions;
    
    // 유닛 배치 런타임 상태
    private Transform unitParent;
    private List<GameObject> enemyUnits;
    private List<GameObject> allyUnits;
    private List<Vector2Int> allySpawnSlots;
    private Dictionary<Vector2Int, GameObject> occupiedTiles;

    // 맵에 배치된 모든 유닛 목록
    private List<BattleUnitEntry> battleUnits;

    void Start()
    {
        // if (initiativeUIGameObject != null)
        // {
        //     InitiativeUI = initiativeUIGameObject.GetComponent<IInitiativeUI>();
        // }

        LoadTileAndObjectDefinitions();
        LoadUnitDefinitions();
        LoadMapData();
        CreateBattleMap();
        if (!mapOnlyMode) PlaceUnits();
    }

    // ALLYS.json / ENEMIES.json에서 유닛 종류 정의 로딩
    private void LoadUnitDefinitions()
    {
        allyDefinitions = LoadUnitDefinitionFile("ALLYS.json");
        enemyDefinitions = LoadUnitDefinitionFile("ENEMIES.json");
    }

    private Dictionary<int, UnitDefinition> LoadUnitDefinitionFile(string fileName)
    {
        var definitions = new Dictionary<int, UnitDefinition>();
        string filePath = Path.Combine(mapFolderPath, fileName);

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"[BattleScript] 유닛 정의 파일을 찾을 수 없습니다: {filePath}");
            return definitions;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            UnitDefinitionCollection collection = JsonUtility.FromJson<UnitDefinitionCollection>(json);

            if (collection != null && collection.units != null)
            {
                foreach (UnitDefinition unit in collection.units)
                {
                    definitions[unit.id] = unit;
                }
            }

            Debug.Log($"[BattleScript] {fileName} 로딩 완료: {definitions.Count}개");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BattleScript] {fileName} 로딩 실패: {e.Message}");
        }

        return definitions;
    }

    private void LoadTileAndObjectDefinitions()
    {
        tileDefinitions = new Dictionary<int, TileInfo>();
        objectDefinitions = new Dictionary<int, ObjectInfo>();
        
        // TILES.json 로딩
        string tilesPath = Path.Combine(mapFolderPath, "TILES.json");
        if (File.Exists(tilesPath))
        {
            try
            {
                string tilesJson = File.ReadAllText(tilesPath);
                TileCollection tileCollection = JsonUtility.FromJson<TileCollection>(tilesJson);
                
                foreach (var tile in tileCollection.tiles)
                {
                    tileDefinitions[tile.id] = tile;
                }
                
                Debug.Log($"타일 정의 로딩 완료: {tileDefinitions.Count}개");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"TILES.json 로딩 실패: {e.Message}");
            }
        }
        
        // OBJECTS.json 로딩
        string objectsPath = Path.Combine(mapFolderPath, "OBJECTS.json");
        if (File.Exists(objectsPath))
        {
            try
            {
                string objectsJson = File.ReadAllText(objectsPath);
                ObjectCollection objectCollection = JsonUtility.FromJson<ObjectCollection>(objectsJson);
                
                foreach (var obj in objectCollection.objects)
                {
                    objectDefinitions[obj.id] = obj;
                }
                
                Debug.Log($"오브젝트 정의 로딩 완료: {objectDefinitions.Count}개");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"OBJECTS.json 로딩 실패: {e.Message}");
            }
        }
    }

    private void LoadMapData()
    {
        tileColors = new Dictionary<int, Color>();
        
        if (loadFromJSON)
        {
            if (LoadMapFromJSON(defaultMapName))
            {
                Debug.Log($"맵 '{currentMapData.mapName}' 로딩 완료!");
                return;
            }
            else
            {
                Debug.LogWarning($"JSON 맵 로딩 실패. 기본 맵을 사용합니다.");
            }
        }
        
        // JSON 로딩 실패 또는 비활성화 시 기본 맵 사용
        UseDefaultMap();
    }

    private bool LoadMapFromJSON(string mapName)
    {
        string filePath = Path.Combine(mapFolderPath, mapName + ".json");
        
        if (!File.Exists(filePath))
        {
            Debug.LogError($"맵 파일을 찾을 수 없습니다: {filePath}");
            return false;
        }

        try
        {
            string jsonContent = File.ReadAllText(filePath);
            currentMapData = JsonUtility.FromJson<MapData>(jsonContent);
            
            // 1차원 배열을 2차원 배열로 변환
            ConvertToMapLayout(currentMapData);
            
            // 타일 색상 정보 저장
            LoadTileColors(currentMapData.tileTypes);
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"JSON 파싱 오류: {e.Message}");
            return false;
        }
    }

    private void ConvertToMapLayout(MapData mapData)
    {
        mapWidth = mapData.width;
        mapHeight = mapData.height;
        currentMapLayout = new int[mapHeight, mapWidth];
        
        for (int i = 0; i < mapData.tiles.Length; i++)
        {
            int row = i / mapWidth;
            int col = i % mapWidth;
            currentMapLayout[row, col] = mapData.tiles[i];
        }
    }

    private void LoadTileColors(TileTypeInfo[] tileTypes)
    {
        if (tileTypes == null) return;
        
        foreach (var tileType in tileTypes)
        {
            if (ColorUtility.TryParseHtmlString(tileType.colorHex, out Color color))
            {
                tileColors[tileType.id] = color;
            }
        }
    }

    private void UseDefaultMap()
    {
        currentMapLayout = fallbackMapLayout;
        mapHeight = currentMapLayout.GetLength(0);
        mapWidth = currentMapLayout.GetLength(1);
        
        // 기본 색상 설정
        tileColors[1] = Color.white;
        tileColors[2] = Color.red;
        tileColors[3] = Color.black;
    }
    
    // 맵 칩 번호에서 타일 ID 추출 (0x0000FF 부분)
    private int GetTileId(int mapChip)
    {
        return mapChip & 0x0000FF;
    }
    
    // 맵 칩 번호에서 오브젝트 ID 추출 (0x00FF00 부분)
    private int GetObjectId(int mapChip)
    {
        return (mapChip & 0x00FF00) >> 8;
    }
    
    // 타일 ID와 오브젝트 ID를 맵 칩 번호로 결합
    private int CombineMapChip(int tileId, int objectId)
    {
        return (tileId & 0xFF) | ((objectId & 0xFF) << 8);
    }
    
    // 타일 정보 가져오기
    public TileInfo GetTileInfo(int tileId)
    {
        if (tileDefinitions.ContainsKey(tileId))
        {
            return tileDefinitions[tileId];
        }
        return null;
    }
    
    // 오브젝트 정보 가져오기
    public ObjectInfo GetObjectInfo(int objectId)
    {
        if (objectDefinitions.ContainsKey(objectId))
        {
            return objectDefinitions[objectId];
        }
        return null;
    }

    private void CreateBattleMap()
    {
        // 배틀맵 부모 객체 생성
        GameObject battleMapObject = new GameObject("BattleMap");
        battleMapParent = battleMapObject.transform;
        battleMapParent.SetParent(transform);
        
        // 오브젝트맵 부모 객체 생성
        GameObject objectMapObject = new GameObject("ObjectMap");
        objectMapParent = objectMapObject.transform;
        objectMapParent.SetParent(transform);

        // 바닥 큐브 및 오브젝트 배열 초기화
        floorCubes = new GameObject[mapHeight, mapWidth];
        objectCubes = new GameObject[mapHeight, mapWidth];

        // 맵 중앙을 기준으로 오프셋 계산
        float offsetX = (mapWidth - 1) * cubeSpacing * 0.5f;
        float offsetZ = (mapHeight - 1) * cubeSpacing * 0.5f;

        // 맵 레이아웃에 따라 바닥 큐브 및 오브젝트 생성
        for (int z = 0; z < mapHeight; z++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                int mapChip = currentMapLayout[z, x];
                int tileId = GetTileId(mapChip);
                int objectId = GetObjectId(mapChip);
                
                Vector3 position = new Vector3(
                    x * cubeSpacing - offsetX,
                    0f,
                    z * cubeSpacing - offsetZ
                );
                
                // 타일 생성 (0이 아닌 경우)
                if (tileId > 0)
                {
                    GameObject floorCube = CreateFloorCube(position, x, z, tileId);
                    floorCubes[z, x] = floorCube;
                }
                
                // 오브젝트 생성 (0이 아닌 경우)
                if (objectId > 0)
                {
                    GameObject objectCube = CreateObjectCube(position, x, z, objectId);
                    objectCubes[z, x] = objectCube;
                }
            }
        }
    }

    private GameObject CreateFloorCube(Vector3 position, int gridX, int gridZ, int tileId)
    {
        GameObject cube;

        if (floorCubePrefab != null)
        {
            // 프리팹이 있으면 프리팹 사용
            cube = Instantiate(floorCubePrefab, position, Quaternion.identity, battleMapParent);
        }
        else
        {
            // 프리팹이 없으면 기본 큐브 생성
            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = position;
            cube.transform.SetParent(battleMapParent);
        }

        // 타일 정보 가져오기
        TileInfo tileInfo = GetTileInfo(tileId);
        string tileName = tileInfo != null ? tileInfo.name : "Unknown";
        
        // 큐브 이름 설정
        cube.name = $"FloorTile_{gridX}_{gridZ}_{tileName}";

        // 타일 색상 설정 (기본 큐브인 경우)
        if (floorCubePrefab == null)
        {
            SetTileColorFromDefinition(cube, tileId);
        }

        return cube;
    }
    
    private GameObject CreateObjectCube(Vector3 position, int gridX, int gridZ, int objectId)
    {
        ObjectInfo objectInfo = GetObjectInfo(objectId);
        if (objectInfo == null)
        {
            Debug.LogWarning($"오브젝트 ID {objectId}에 대한 정의를 찾을 수 없습니다.");
            return null;
        }
        
        GameObject objectCube;
        
        // 프리팹 경로가 있으면 리소스에서 로딩 시도
        if (!string.IsNullOrEmpty(objectInfo.prefabPath))
        {
            GameObject prefab = Resources.Load<GameObject>(objectInfo.prefabPath);
            if (prefab != null)
            {
                objectCube = Instantiate(prefab, position, Quaternion.identity, objectMapParent);
            }
            else
            {
                // 프리팹을 찾을 수 없으면 기본 큐브 생성
                objectCube = CreateDefaultObjectCube(position, objectInfo);
            }
        }
        else
        {
            // 프리팹 경로가 없으면 기본 큐브 생성
            objectCube = CreateDefaultObjectCube(position, objectInfo);
        }
        
        // 오브젝트 높이 적용
        Vector3 newPosition = position;
        newPosition.y += objectInfo.height * 0.5f;
        objectCube.transform.position = newPosition;
        
        // 오브젝트 이름 설정
        objectCube.name = $"Object_{gridX}_{gridZ}_{objectInfo.name}";
        
        return objectCube;
    }
    
    private GameObject CreateDefaultObjectCube(Vector3 position, ObjectInfo objectInfo)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = position;
        cube.transform.SetParent(objectMapParent);
        
        // 오브젝트 크기 조정
        Vector3 scale = Vector3.one;
        scale.y = objectInfo.height;
        cube.transform.localScale = scale;
        
        // 색상 설정
        Renderer renderer = cube.GetComponent<Renderer>();
        if (renderer != null && ColorUtility.TryParseHtmlString(objectInfo.colorHex, out Color color))
        {
            renderer.material.color = color;
        }
        
        return cube;
    }

    private void SetTileColor(GameObject cube, int tileType)
    {
        Renderer renderer = cube.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (tileColors.ContainsKey(tileType))
            {
                renderer.material.color = tileColors[tileType];
            }
            else
            {
                // 정의되지 않은 타일 타입은 회색으로 표시
                renderer.material.color = Color.gray;
                Debug.LogWarning($"정의되지 않은 타일 타입: {tileType}");
            }
        }
    }
    
    private void SetTileColorFromDefinition(GameObject cube, int tileId)
    {
        Renderer renderer = cube.GetComponent<Renderer>();
        if (renderer != null)
        {
            TileInfo tileInfo = GetTileInfo(tileId);
            if (tileInfo != null && ColorUtility.TryParseHtmlString(tileInfo.colorHex, out Color color))
            {
                renderer.material.color = color;
            }
            else
            {
                // 정의되지 않은 타일은 회색으로 표시
                renderer.material.color = Color.gray;
                Debug.LogWarning($"정의되지 않은 타일 ID: {tileId}");
            }
        }
    }

    // ── 유닛 배치 ────────────────────────────────────────────────────

    // 맵 생성 직후 호출 — 적은 즉시 배치하고, 아군은 빈 스폰 슬롯만 등록
    private void PlaceUnits()
    {
        DestroyUnits();

        GameObject unitMapObject = new GameObject("UnitMap");
        unitParent = unitMapObject.transform;
        unitParent.SetParent(transform);

        enemyUnits = new List<GameObject>();
        allyUnits = new List<GameObject>();
        allySpawnSlots = new List<Vector2Int>();
        occupiedTiles = new Dictionary<Vector2Int, GameObject>();
        battleUnits = new List<BattleUnitEntry>();

        PlaceEnemies();
        RegisterAllySpawnSlots();
    }

    private void PlaceEnemies()
    {
        if (currentMapData == null || currentMapData.enemySpawns == null) return;

        foreach (EnemyPlacement placement in currentMapData.enemySpawns)
        {
            if (!CanPlaceUnitAt(placement.x, placement.y, "적 스폰")) continue;

            UnitDefinition definition = GetEnemyDefinition(placement.id);
            if (definition == null)
            {
                Debug.LogWarning($"[BattleScript] 적 id {placement.id}에 대한 정의를 ENEMIES.json에서 찾을 수 없습니다.");
                continue;
            }

            Vector2Int grid = new Vector2Int(placement.x, placement.y);
            GameObject marker = SpawnUnitMarker(grid, true, definition);
            enemyUnits.Add(marker);
        }

        Debug.Log($"[BattleScript] 적 {enemyUnits.Count}체 배치 완료");
    }

    private void RegisterAllySpawnSlots()
    {
        if (currentMapData == null || currentMapData.allySpawns == null) return;

        foreach (UnitPlacement placement in currentMapData.allySpawns)
        {
            if (!CanPlaceUnitAt(placement.x, placement.y, "아군 스폰 슬롯")) continue;

            Vector2Int slot = new Vector2Int(placement.x, placement.y);
            allySpawnSlots.Add(slot);

            bool hasUnit = placement.id > 0;

            // id가 지정됐거나 자동 채우기가 켜져 있으면 시작 시 박스 배치
            if (!hasUnit && !autoFillAllySpawns) continue;

            UnitDefinition definition = hasUnit ? GetAllyDefinition(placement.id) : null;
            if (hasUnit && definition == null)
            {
                Debug.LogWarning($"[BattleScript] 아군 id {placement.id}에 대한 정의를 ALLYS.json에서 찾을 수 없습니다.");
                continue;
            }

            GameObject marker = SpawnUnitMarker(slot, false, definition);
            allyUnits.Add(marker);
        }

        Debug.Log($"[BattleScript] 아군 스폰 슬롯 {allySpawnSlots.Count}개 등록 (자동 배치 {allyUnits.Count}체)");
    }

    // 두 그리드 좌표 사이의 맨해튼 거리 (상하좌우 이동 기준 칸 수)
    private static int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    // ── 유닛 이동 ────────────────────────────────────────────────────

    // 유닛을 목표 칸으로 이동 — 맵 범위/이동 가능/빈 칸 여부를 검증
    // instant=true면 보간 없이 즉시 위치를 맞춘다.
    public bool MoveUnit(BattleUnitEntry unit, Vector2Int target, bool instant = false)
    {
        if (unit == null || unit.marker == null) return false;

        if (!IsWalkable(target.x, target.y))
        {
            Debug.LogWarning($"[BattleScript] ({target.x}, {target.y})로 이동할 수 없습니다 (장애물 또는 맵 밖).");
            return false;
        }

        if (IsTileOccupied(target))
        {
            Debug.LogWarning($"[BattleScript] ({target.x}, {target.y})에는 이미 다른 유닛이 있습니다.");
            return false;
        }

        // 점유 정보 갱신
        occupiedTiles.Remove(unit.grid);
        occupiedTiles[target] = unit.marker;
        unit.grid = target;

        // 마커 월드 위치 갱신 — y(높이)는 기존 값을 유지해 박스/프리팹 모두 자연스럽게 이동
        Vector3 world = GridToWorld(target.x, target.y);
        Vector3 markerTarget = unit.marker.transform.position;
        markerTarget.x = world.x;
        markerTarget.z = world.z;

        // UnitMover가 있으면 보간/즉시 이동, 없으면 transform을 직접 갱신
        UnitMover mover = unit.marker.GetComponent<UnitMover>();
        if (mover != null)
        {
            if (instant)
            {
                mover.SnapTo(markerTarget);
            }
            else
            {
                mover.MoveTo(markerTarget);
            }
        }
        else
        {
            unit.marker.transform.position = markerTarget;
        }

        Debug.Log($"[BattleScript] {unit.DisplayName} 이동 → ({target.x}, {target.y})");
        return true;
    }

    // ── 도달 가능 영역 / 경로 (이동 범위 표시용) ────────────

    // 상하좌우 4방향 (대각선 이동 없음)
    private static readonly Vector2Int[] MoveDirections =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    // 지정 원점 기준으로 BFS로 도달 가능한 칸과 경로(부모 맵)를 계산한다.
    //  - origin에서 맨해튼 거리 MoveRange 이내
    //  - 걷기 가능(IsWalkable) + 다른 유닛이 없는(IsTileOccupied) 칸만 통과
    // 반환 맵의 key 집합이 도달 가능한 칸이며, value는 그 칸으로 오기 직전 칸이다.
    // 시작 칸(unit.grid)은 맵에 포함되지 않는다.
    public Dictionary<Vector2Int, Vector2Int> ComputeReachable(BattleUnitEntry unit, Vector2Int origin)
    {
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        if (unit == null) return cameFrom;

        int budget = unit.MoveRange;
        if (budget <= 0) return cameFrom;

        Vector2Int start = unit.grid;
        var visited = new HashSet<Vector2Int> { start };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            foreach (Vector2Int dir in MoveDirections)
            {
                Vector2Int next = current + dir;
                if (visited.Contains(next)) continue;
                if (ManhattanDistance(next, origin) > budget) continue;
                if (!IsWalkable(next.x, next.y)) continue;
                if (IsTileOccupied(next)) continue;

                visited.Add(next);
                cameFrom[next] = current;
                queue.Enqueue(next);
            }
        }

        return cameFrom;
    }

    // 유닛의 현재 위치를 원점으로 도달 가능 영역을 계산한다.
    public Dictionary<Vector2Int, Vector2Int> ComputeReachable(BattleUnitEntry unit)
    {
        if (unit == null) return new Dictionary<Vector2Int, Vector2Int>();
        return ComputeReachable(unit, unit.grid);
    }

    // 유닛을 배치할 수 있는 칸인지 검사 — 맵 범위 + 장애물(이동 불가) 여부 확인
    private bool CanPlaceUnitAt(int x, int z, string context)
    {
        if (!IsValidPosition(x, z))
        {
            Debug.LogWarning($"[BattleScript] {context} 위치가 맵 범위를 벗어남: ({x}, {z})");
            return false;
        }

        if (!IsWalkable(x, z))
        {
            Debug.LogWarning($"[BattleScript] {context} 위치에 장애물이 있어 배치를 건너뜁니다: ({x}, {z})");
            return false;
        }

        return true;
    }

    // 유닛 정의 조회
    public UnitDefinition GetAllyDefinition(int id)
    {
        if (allyDefinitions != null && allyDefinitions.TryGetValue(id, out UnitDefinition def)) return def;
        return null;
    }

    public UnitDefinition GetEnemyDefinition(int id)
    {
        if (enemyDefinitions != null && enemyDefinitions.TryGetValue(id, out UnitDefinition def)) return def;
        return null;
    }

    // 좌표에 유닛 마커 생성 — definition이 있으면 정의 색상/이름/프리팹 사용, 없으면 기본 박스
    private GameObject SpawnUnitMarker(Vector2Int grid, bool isEnemy, UnitDefinition definition)
    {
        Vector3 worldPosition = GridToWorld(grid.x, grid.y);

        GameObject marker = CreateUnitVisual(worldPosition, isEnemy, definition);

        // 부드러운 이동을 위한 무버 컴포넌트 부착 (초기 위치는 즉시 스냅)
        UnitMover mover = marker.GetComponent<UnitMover>();
        if (mover == null) mover = marker.AddComponent<UnitMover>();
        mover.SetMoveSpeed(unitMoveSpeed);
        mover.SnapTo(marker.transform.position);

        string prefix = isEnemy ? "Enemy" : "Ally";
        string unitName = definition != null && !string.IsNullOrWhiteSpace(definition.name)
            ? definition.name
            : "Unit";
        marker.name = $"{prefix}_{unitName}_{grid.x}_{grid.y}";

        occupiedTiles[grid] = marker;
        battleUnits.Add(new BattleUnitEntry(marker, definition, grid, isEnemy));
        return marker;
    }

    private GameObject CreateUnitVisual(Vector3 worldPosition, bool isEnemy, UnitDefinition definition)
    {
        // 프리팹 경로가 지정돼 있으면 우선 로딩 시도
        if (definition != null && !string.IsNullOrEmpty(definition.prefabPath))
        {
            GameObject prefab = Resources.Load<GameObject>(definition.prefabPath);
            if (prefab != null)
            {
                return Instantiate(prefab, worldPosition, Quaternion.identity, unitParent);
            }
            Debug.LogWarning($"[BattleScript] 유닛 프리팹을 찾을 수 없어 박스로 대체합니다: {definition.prefabPath}");
        }

        // 기본 박스 마커
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.transform.SetParent(unitParent);
        box.transform.localScale = unitMarkerSize;

        Vector3 boxPosition = worldPosition;
        boxPosition.y += unitMarkerSize.y * 0.5f; // 바닥 위에 올림
        box.transform.position = boxPosition;

        Renderer renderer = box.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = ResolveUnitColor(isEnemy, definition);
        }

        return box;
    }

    private Color ResolveUnitColor(bool isEnemy, UnitDefinition definition)
    {
        if (definition != null && !string.IsNullOrWhiteSpace(definition.colorHex)
            && ColorUtility.TryParseHtmlString(definition.colorHex, out Color color))
        {
            return color;
        }
        return isEnemy ? enemyColor : allyColor;
    }

    // 플레이어가 빈 아군 슬롯에 유닛을 배치할 때 호출 — ALLYS.json의 id 참조
    public GameObject PlaceAllyAtSlot(Vector2Int slot, int allyId)
    {
        if (!allySpawnSlots.Contains(slot))
        {
            Debug.LogWarning($"[BattleScript] ({slot.x}, {slot.y})는 아군 스폰 슬롯이 아닙니다.");
            return null;
        }

        if (!IsWalkable(slot.x, slot.y))
        {
            Debug.LogWarning($"[BattleScript] ({slot.x}, {slot.y})에 장애물이 있어 배치할 수 없습니다.");
            return null;
        }

        if (IsTileOccupied(slot))
        {
            Debug.LogWarning($"[BattleScript] ({slot.x}, {slot.y})에는 이미 유닛이 있습니다.");
            return null;
        }

        UnitDefinition definition = GetAllyDefinition(allyId);
        if (definition == null)
        {
            Debug.LogWarning($"[BattleScript] 아군 id {allyId}에 대한 정의를 ALLYS.json에서 찾을 수 없습니다.");
            return null;
        }

        GameObject marker = SpawnUnitMarker(slot, false, definition);
        allyUnits.Add(marker);
        return marker;
    }

    // 해당 타일에 유닛이 존재하는지 확인
    public bool IsTileOccupied(Vector2Int tilePos)
    {
        return occupiedTiles != null && occupiedTiles.ContainsKey(tilePos);
    }

    // 특정 좌표에 위치한 배틀 유닛을 반환한다 (없으면 null)
    public BattleUnitEntry GetUnitAt(int x, int y)
    {
        if (battleUnits == null)
            return null;

        Vector2Int target = new Vector2Int(x, y);
        foreach (BattleUnitEntry unit in battleUnits)
        {
            if (unit != null && unit.grid == target)
                return unit;
        }

        return null;
    }

    public IReadOnlyList<GameObject> GetEnemyUnits() => enemyUnits;
    public IReadOnlyList<GameObject> GetAllyUnits() => allyUnits;
    public IReadOnlyList<Vector2Int> GetAllySpawnSlots() => allySpawnSlots;

    private void DestroyUnits()
    {
        if (unitParent != null)
        {
            DestroyImmediate(unitParent.gameObject);
            unitParent = null;
        }

        enemyUnits?.Clear();
        allyUnits?.Clear();
        allySpawnSlots?.Clear();
        occupiedTiles?.Clear();
        battleUnits?.Clear();
    }

    // 특정 위치의 바닥 큐브 가져오기
    public GameObject GetFloorCube(int x, int z)
    {
        if (IsValidPosition(x, z))
        {
            return floorCubes[z, x];
        }
        return null;
    }

    // 특정 위치의 맵 칩 번호 가져오기
    public int GetMapChip(int x, int z)
    {
        if (IsValidPosition(x, z))
        {
            return currentMapLayout[z, x];
        }
        return 0;
    }
    
    // 특정 위치의 타일 ID 가져오기
    public int GetTileType(int x, int z)
    {
        if (IsValidPosition(x, z))
        {
            return GetTileId(currentMapLayout[z, x]);
        }
        return 0;
    }
    
    // 특정 위치의 오브젝트 ID 가져오기
    public int GetObjectType(int x, int z)
    {
        if (IsValidPosition(x, z))
        {
            return GetObjectId(currentMapLayout[z, x]);
        }
        return 0;
    }

    // 유효한 위치인지 확인
    public bool IsValidPosition(int x, int z)
    {
        return x >= 0 && x < mapWidth && z >= 0 && z < mapHeight;
    }

    // 해당 위치에 타일이 있는지 확인
    public bool HasTile(int x, int z)
    {
        return IsValidPosition(x, z) && GetTileType(x, z) > 0;
    }
    
    // 해당 위치에 오브젝트가 있는지 확인
    public bool HasObject(int x, int z)
    {
        return IsValidPosition(x, z) && GetObjectType(x, z) > 0;
    }
    
    // 해당 위치가 이동 가능한지 확인
    public bool IsWalkable(int x, int z)
    {
        if (!IsValidPosition(x, z))
            return false;
            
        int tileId = GetTileType(x, z);
        int objectId = GetObjectType(x, z);
        
        // 타일이 이동 불가능한 경우
        TileInfo tileInfo = GetTileInfo(tileId);
        if (tileInfo == null || !tileInfo.isWalkable)
            return false;
            
        // 오브젝트가 막고 있는 경우
        if (objectId > 0)
        {
            ObjectInfo objectInfo = GetObjectInfo(objectId);
            if (objectInfo != null && objectInfo.isBlocking)
                return false;
        }
        
        return true;
    }
    
    // 해당 위치의 이동 비용 가져오기
    public int GetMovementCost(int x, int z)
    {
        if (!IsValidPosition(x, z))
            return 0;
            
        int tileId = GetTileType(x, z);
        TileInfo tileInfo = GetTileInfo(tileId);
        
        return tileInfo != null ? tileInfo.movementCost : 1;
    }

    // 월드 좌표를 그리드 좌표로 변환
    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        float offsetX = (mapWidth - 1) * cubeSpacing * 0.5f;
        float offsetZ = (mapHeight - 1) * cubeSpacing * 0.5f;
        int gridX = Mathf.RoundToInt((worldPosition.x + offsetX) / cubeSpacing);
        int gridZ = Mathf.RoundToInt((worldPosition.z + offsetZ) / cubeSpacing);
        return new Vector2Int(gridX, gridZ);
    }

    // 그리드 좌표를 월드 좌표로 변환
    public Vector3 GridToWorld(int gridX, int gridZ)
    {
        float offsetX = (mapWidth - 1) * cubeSpacing * 0.5f;
        float offsetZ = (mapHeight - 1) * cubeSpacing * 0.5f;
        return new Vector3(
            gridX * cubeSpacing - offsetX,
            0f,
            gridZ * cubeSpacing - offsetZ
        );
    }

    // 맵 크기 정보 가져오기
    public Vector2Int GetMapSize()
    {
        return new Vector2Int(mapWidth, mapHeight);
    }

    // JSON에서 맵 로드 (런타임에서 사용 가능)
    public bool LoadMap(string mapName)
    {
        DestroyCurrentMap();
        
        if (LoadMapFromJSON(mapName))
        {
            CreateBattleMap();
            PlaceUnits();
            return true;
        }
        
        Debug.LogError($"맵 '{mapName}' 로딩 실패");
        return false;
    }

    // 맵 레이아웃 변경 (런타임에서 사용 가능)
    public void SetMapLayout(int[,] newMapLayout)
    {
        currentMapLayout = newMapLayout;
        DestroyCurrentMap();
        mapHeight = currentMapLayout.GetLength(0);
        mapWidth = currentMapLayout.GetLength(1);
        CreateBattleMap();
    }

    // 사용 가능한 맵 목록 가져오기
    public List<string> GetAvailableMaps()
    {
        List<string> mapNames = new List<string>();
        
        if (Directory.Exists(mapFolderPath))
        {
            string[] jsonFiles = Directory.GetFiles(mapFolderPath, "*.json");
            foreach (string filePath in jsonFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                mapNames.Add(fileName);
            }
        }
        
        return mapNames;
    }

    // 현재 맵 정보 가져오기
    public MapData GetCurrentMapData()
    {
        return currentMapData;
    }

    private void DestroyCurrentMap()
    {
        if (battleMapParent != null)
        {
            DestroyImmediate(battleMapParent.gameObject);
        }
        
        if (objectMapParent != null)
        {
            DestroyImmediate(objectMapParent.gameObject);
        }

        DestroyUnits();
    }
}
