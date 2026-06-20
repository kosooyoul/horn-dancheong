using UnityEngine;

/// <summary>
/// BattleScript의 맵 기능만 KD.GridManager에 제공하는 어댑터.
/// BattleScript 전체를 전투 로직으로 사용하지 않고, 맵 관련 기능만 빌려온다.
///
/// 씬 구성:
///   BattleMap 오브젝트 → BattleScript + BattleMapProvider
///   BattleSystem 오브젝트 → KD.GridManager (mapProvider 슬롯에 이 컴포넌트를 연결)
/// </summary>
public class BattleMapProvider : MonoBehaviour
{
    [SerializeField] private BattleScript battleScript;

    private void Awake()
    {
        if (battleScript == null)
            battleScript = GetComponent<BattleScript>();
    }

    public bool IsValidTile(Vector2Int tile)
    {
        if (battleScript == null) return false;
        return battleScript.IsValidPosition(tile.x, tile.y);
    }

    public bool IsWalkable(Vector2Int tile)
    {
        if (battleScript == null) return false;
        return battleScript.IsWalkable(tile.x, tile.y);
    }

    /// <summary>Grid 좌표 → 월드 좌표 (y=0, 타일 중앙)</summary>
    public Vector3 GridToWorld(Vector2Int tile)
    {
        if (battleScript == null)
            return new Vector3(tile.x, 0f, tile.y);
        return battleScript.GridToWorld(tile.x, tile.y);
    }

    /// <summary>월드 좌표 → 가장 가까운 Grid 좌표</summary>
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        if (battleScript == null)
            return new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.z));
        return battleScript.WorldToGrid(worldPos);
    }

    /// <summary>해당 Grid 좌표의 바닥 타일 GameObject 반환 (하이라이트 렌더링용)</summary>
    public GameObject GetFloorTile(Vector2Int tile)
    {
        if (battleScript == null) return null;
        return battleScript.GetFloorCube(tile.x, tile.y);
    }
}
