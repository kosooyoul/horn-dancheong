using UnityEngine;
using System.Collections.Generic;

public class MapTester : MonoBehaviour
{
    [Header("맵 테스터")]
    [SerializeField] private BattleScript battleScript;
    [SerializeField] private KeyCode nextMapKey = KeyCode.Space;
    [SerializeField] private KeyCode prevMapKey = KeyCode.B;
    
    private List<string> availableMaps;
    private int currentMapIndex = 0;

    void Start()
    {
        if (battleScript == null)
        {
            battleScript = FindObjectOfType<BattleScript>();
        }
        
        LoadAvailableMaps();
        ShowCurrentMapInfo();
    }

    void Update()
    {
        if (Input.GetKeyDown(nextMapKey))
        {
            LoadNextMap();
        }
        
        if (Input.GetKeyDown(prevMapKey))
        {
            LoadPreviousMap();
        }
    }

    private void LoadAvailableMaps()
    {
        availableMaps = battleScript.GetAvailableMaps();
        
        if (availableMaps.Count == 0)
        {
            Debug.LogWarning("사용 가능한 맵이 없습니다.");
            return;
        }
        
        Debug.Log($"사용 가능한 맵 개수: {availableMaps.Count}");
        foreach (string mapName in availableMaps)
        {
            Debug.Log($"- {mapName}");
        }
    }

    private void LoadNextMap()
    {
        if (availableMaps.Count == 0) return;
        
        currentMapIndex = (currentMapIndex + 1) % availableMaps.Count;
        LoadCurrentMap();
    }

    private void LoadPreviousMap()
    {
        if (availableMaps.Count == 0) return;
        
        currentMapIndex = (currentMapIndex - 1 + availableMaps.Count) % availableMaps.Count;
        LoadCurrentMap();
    }

    private void LoadCurrentMap()
    {
        string mapName = availableMaps[currentMapIndex];
        
        if (battleScript.LoadMap(mapName))
        {
            ShowCurrentMapInfo();
        }
        else
        {
            Debug.LogError($"맵 로딩 실패: {mapName}");
        }
    }

    private void ShowCurrentMapInfo()
    {
        if (availableMaps.Count == 0) return;
        
        MapData currentMap = battleScript.GetCurrentMapData();
        if (currentMap != null)
        {
            Debug.Log($"현재 맵: {currentMap.mapName} ({currentMap.description})");
            Debug.Log($"크기: {currentMap.width}x{currentMap.height}");
            Debug.Log($"조작법: [{nextMapKey}] 다음 맵, [{prevMapKey}] 이전 맵");
        }
    }

    void OnGUI()
    {
        if (availableMaps == null || availableMaps.Count == 0) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 150));
        GUILayout.Box("맵 테스터");
        
        MapData currentMap = battleScript.GetCurrentMapData();
        if (currentMap != null)
        {
            GUILayout.Label($"현재 맵: {currentMap.mapName}");
            GUILayout.Label($"설명: {currentMap.description}");
            GUILayout.Label($"크기: {currentMap.width}x{currentMap.height}");
        }
        
        GUILayout.Space(10);
        GUILayout.Label($"[{nextMapKey}] 다음 맵");
        GUILayout.Label($"[{prevMapKey}] 이전 맵");
        
        GUILayout.EndArea();
    }
}