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

public class BattleScript : MonoBehaviour
{
    [Header("Battle Map Settings")]
    [SerializeField] private GameObject floorCubePrefab;
    [SerializeField] private float cubeSpacing = 1f;
    
    [Header("Map Loading")]
    [SerializeField] private string mapFolderPath = "Assets/1.Scripts/KO/Battle/MapData/";
    [SerializeField] private string defaultMapName = "basic_10x10";
    [SerializeField] private bool loadFromJSON = true;
    
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
    
    // 새로운 맵 칩 시스템
    private Dictionary<int, TileInfo> tileDefinitions;
    private Dictionary<int, ObjectInfo> objectDefinitions;

    void Start()
    {
        LoadTileAndObjectDefinitions();
        LoadMapData();
        CreateBattleMap();
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

    void Update()
    {
        
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
    }
}
