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
    public int[] tiles; // 1차원 배열로 저장 (row-major order)
    public TileTypeInfo[] tileTypes;
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
    private Transform battleMapParent;
    private int mapWidth;
    private int mapHeight;
    private int[,] currentMapLayout;
    private MapData currentMapData;
    private Dictionary<int, Color> tileColors;

    void Start()
    {
        LoadMapData();
        CreateBattleMap();
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

    private void CreateBattleMap()
    {
        // 배틀맵 부모 객체 생성
        GameObject battleMapObject = new GameObject("BattleMap");
        battleMapParent = battleMapObject.transform;
        battleMapParent.SetParent(transform);

        // 바닥 큐브 배열 초기화
        floorCubes = new GameObject[mapHeight, mapWidth];

        // 맵 중앙을 기준으로 오프셋 계산
        float offsetX = (mapWidth - 1) * cubeSpacing * 0.5f;
        float offsetZ = (mapHeight - 1) * cubeSpacing * 0.5f;

        // 맵 레이아웃에 따라 바닥 큐브 생성
        for (int z = 0; z < mapHeight; z++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                int tileType = currentMapLayout[z, x];
                
                // 0이면 빈 공간, 1 이상이면 타일 생성
                if (tileType > 0)
                {
                    Vector3 position = new Vector3(
                        x * cubeSpacing - offsetX,
                        0f,
                        z * cubeSpacing - offsetZ
                    );

                    GameObject floorCube = CreateFloorCube(position, x, z, tileType);
                    floorCubes[z, x] = floorCube;
                }
            }
        }
    }

    private GameObject CreateFloorCube(Vector3 position, int gridX, int gridZ, int tileType)
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

        // 큐브 이름 설정 (타일 타입 포함)
        cube.name = $"FloorCube_{gridX}_{gridZ}_Type{tileType}";

        // 타일 타입에 따른 색상 설정 (기본 큐브인 경우)
        if (floorCubePrefab == null)
        {
            SetTileColor(cube, tileType);
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

    // 특정 위치의 바닥 큐브 가져오기
    public GameObject GetFloorCube(int x, int z)
    {
        if (IsValidPosition(x, z))
        {
            return floorCubes[z, x];
        }
        return null;
    }

    // 특정 위치의 타일 타입 가져오기
    public int GetTileType(int x, int z)
    {
        if (IsValidPosition(x, z))
        {
            return currentMapLayout[z, x];
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
        return IsValidPosition(x, z) && currentMapLayout[z, x] > 0;
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
    }
}
