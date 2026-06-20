using System.Collections.Generic;
using UnityEngine;

namespace KD
{
    /// <summary>
    /// 전술 전투의 그리드 상태를 단독으로 소유한다.
    ///
    /// 역할:
    ///   - 타일 유효성 / 이동 가능 / 스킬 Ray 차단 판정
    ///   - 유닛 점유 상태 관리
    ///   - 유닛 배치·이동 (BattleUnit 좌표 + UnitMover 비주얼 동기화)
    ///   - 이동/스킬/위험/배치 하이라이트 렌더링
    ///
    /// BattleMapProvider가 연결되면 모든 타일 판정과 좌표 변환을
    /// BattleScript(맵 전용 모드)로 위임한다.
    /// 연결되지 않으면 width/height 기반 폴백을 사용한다.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────

        [Header("Map Provider (BattleScript 맵 연동)")]
        [SerializeField] private BattleMapProvider mapProvider;

        [Header("Grid Size (mapProvider 없을 때 폴백)")]
        [SerializeField] private int   width      = 7;
        [SerializeField] private int   height     = 7;
        [SerializeField] private float cellSize   = 1f;
        [SerializeField] private float fixedWorldY = 0f;

        [Header("Blocked Tiles (mapProvider 없을 때 폴백)")]
        [SerializeField] private List<Vector2Int> wallTiles = new List<Vector2Int>();

        [Header("Unit Visuals")]
        [SerializeField] private GameObject defaultUnitMarkerPrefab;
        [SerializeField] private Transform  unitVisualParent;
        [SerializeField] private float      unitMoveSpeed   = 6f;
        [SerializeField] private float      unitHeightOffset = 0.5f;

        [Header("Highlight Colors")]
        [SerializeField] private Color moveTileColor       = new Color(0.3f, 1f,  0.45f);
        [SerializeField] private Color skillTileColor      = new Color(1f,  0.9f, 0.2f);
        [SerializeField] private Color dangerTileColor     = new Color(1f,  0.2f, 0.2f);
        [SerializeField] private Color deployableTileColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color blockedDeployColor  = new Color(0.9f, 0.3f, 0.1f);
        [SerializeField, Range(0f, 1f)] private float highlightStrength = 0.55f;

        // ── 런타임 상태 ────────────────────────────────────────────────────

        private readonly HashSet<Vector2Int>             wallSet     = new HashSet<Vector2Int>();
        private readonly Dictionary<Vector2Int, BattleUnit> unitByTile  = new Dictionary<Vector2Int, BattleUnit>();
        private readonly Dictionary<BattleUnit, Vector2Int> tileByUnit  = new Dictionary<BattleUnit, Vector2Int>();
        private readonly Dictionary<BattleUnit, GameObject> visualByUnit = new Dictionary<BattleUnit, GameObject>();

        // 하이라이트 목록
        private readonly List<MoveOption>  currentMoveHighlights           = new List<MoveOption>();
        private readonly List<Vector2Int>  currentSkillHighlights          = new List<Vector2Int>();
        private readonly List<Vector2Int>  currentDeployableHighlights     = new List<Vector2Int>();
        private readonly List<Vector2Int>  currentBlockedDeployHighlights  = new List<Vector2Int>();

        // danger 하이라이트: 타일 → SafetyType (DangerS/M/L/XL)
        private readonly Dictionary<Vector2Int, SafetyType> currentDangerHighlights = new Dictionary<Vector2Int, SafetyType>();

        // FloorCubeStater를 통해 SafetyType이 적용된 타일 (복원 시 Safe로 되돌릴 목록)
        private readonly HashSet<Vector2Int> activeStaterTiles = new HashSet<Vector2Int>();

        // 하이라이트 이전 원본 색상 보존 (Renderer 키)
        private readonly Dictionary<Renderer, Color> originalColors = new Dictionary<Renderer, Color>();

        // SG_FloorCubeSide 셰이더는 _Color 대신 _BaseColor를 사용 → MPB로 읽고 씀
        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock highlightMPB;

        // ── 외부 조회 프로퍼티 ────────────────────────────────────────────

        public IReadOnlyList<MoveOption>                          CurrentMoveHighlights          => currentMoveHighlights;
        public IReadOnlyList<Vector2Int>                          CurrentSkillHighlights         => currentSkillHighlights;
        public IReadOnlyDictionary<Vector2Int, SafetyType>        CurrentDangerHighlights        => currentDangerHighlights;
        public IReadOnlyList<Vector2Int>                          CurrentDeployableHighlights    => currentDeployableHighlights;
        public IReadOnlyList<Vector2Int>                          CurrentBlockedDeployHighlights => currentBlockedDeployHighlights;

        // ── 초기화 ────────────────────────────────────────────────────────

        private void Awake()
        {
            highlightMPB = new MaterialPropertyBlock();
            RebuildWallSet();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            width    = Mathf.Max(1, width);
            height   = Mathf.Max(1, height);
            cellSize = Mathf.Max(0.01f, cellSize);
        }
#endif

        private void RebuildWallSet()
        {
            wallSet.Clear();
            foreach (var t in wallTiles) wallSet.Add(t);
        }

        // ── 좌표 변환 ─────────────────────────────────────────────────────

        /// <summary>Grid 좌표 → 월드 좌표 (바닥 y, 타일 중앙)</summary>
        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            if (mapProvider != null)
                return mapProvider.GridToWorld(gridPos);
            return new Vector3(gridPos.x * cellSize, fixedWorldY, gridPos.y * cellSize);
        }

        private bool mapProviderWarned;

        /// <summary>월드 좌표 → 가장 가까운 Grid 좌표</summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            if (mapProvider != null)
                return mapProvider.WorldToGrid(worldPos);
            if (!mapProviderWarned)
            {
                Debug.LogWarning("[GridManager] mapProvider가 null — Inspector에서 BattleMapProvider를 연결하세요. offset 없이 fallback WorldToGrid 사용 중.");
                mapProviderWarned = true;
            }
            return new Vector2Int(
                Mathf.RoundToInt(worldPos.x / cellSize),
                Mathf.RoundToInt(worldPos.z / cellSize));
        }

        // ── 타일 판단 ─────────────────────────────────────────────────────

        public bool IsValidTile(Vector2Int pos)
        {
            if (mapProvider != null)
                return mapProvider.IsValidTile(pos);
            return pos.x >= 0 && pos.y >= 0 && pos.x < width && pos.y < height;
        }

        /// <summary>이동 불가 판정 — 맵 범위 밖 / 벽(또는 isWalkable=false) / 유닛 점유</summary>
        public bool IsBlockedForMove(Vector2Int pos)
        {
            if (!IsValidTile(pos)) return true;
            if (mapProvider != null)
                return !mapProvider.IsWalkable(pos) || unitByTile.ContainsKey(pos);
            if (wallSet.Contains(pos)) return true;
            return unitByTile.ContainsKey(pos);
        }

        /// <summary>스킬 Ray 차단 — 벽(또는 isWalkable=false)만. 유닛 점유는 무시.</summary>
        public bool IsBlockedForSkillRay(Vector2Int pos)
        {
            if (!IsValidTile(pos)) return true;
            if (mapProvider != null)
                return !mapProvider.IsWalkable(pos);
            return wallSet.Contains(pos);
        }

        // ── 유닛 조회 ─────────────────────────────────────────────────────

        public BattleUnit GetUnitAt(Vector2Int pos)
        {
            unitByTile.TryGetValue(pos, out BattleUnit unit);
            return unit;
        }

        // ── 유닛 배치/이동/제거 ───────────────────────────────────────────

        /// <summary>
        /// 유닛을 즉시 배치한다 (배치 단계·전투 시작 초기 스냅용).
        /// 벽·점유 타일이면 경고 후 무시. 같은 유닛의 재배치는 기존 위치를 먼저 해제한다.
        /// </summary>
        public void PlaceUnit(BattleUnit unit, Vector2Int pos)
        {
            if (unit == null) return;

            if (!IsValidTile(pos))
            {
                Debug.LogWarning($"[GridManager] 유효하지 않은 위치: {pos}");
                return;
            }

            bool wallBlocked = mapProvider != null
                ? !mapProvider.IsWalkable(pos)
                : wallSet.Contains(pos);
            if (wallBlocked)
            {
                Debug.LogWarning($"[GridManager] 이동 불가 타일에 배치 시도: {pos}");
                return;
            }

            if (unitByTile.TryGetValue(pos, out BattleUnit occupant) && occupant != unit)
            {
                Debug.LogWarning($"[GridManager] 이미 다른 유닛이 있는 타일: {pos}");
                return;
            }

            RemoveUnitFromCurrentTile(unit);
            unitByTile[pos] = unit;
            tileByUnit[unit] = pos;
            unit.MoveTo(pos);

            // 비주얼: 배치는 항상 즉시 스냅
            GameObject visual = GetOrCreateUnitVisual(unit);
            Vector3 worldPos  = GridToWorld(pos);
            worldPos.y += unitHeightOffset;
            UnitMover mover = visual.GetComponent<UnitMover>();
            if (mover != null) mover.SnapTo(worldPos);
            else visual.transform.position = worldPos;

            Debug.Log($"[GridManager] 유닛 배치: {unit.Data.unitName} → {pos}");
        }

        /// <summary>
        /// 이동 가능 여부·AP를 검증하고 유닛을 이동시킨다.
        /// 실제 이동 연출은 UnitMover가 담당한다.
        /// </summary>
        public bool TryMoveUnit(BattleUnit unit, MoveOption move)
        {
            if (unit == null) return false;

            Vector2Int targetPos = move.tilePos;

            if (!IsValidTile(targetPos))       return false;
            if (IsBlockedForMove(targetPos))   return false;
            if (!unit.TrySpendAP(move.apCost)) return false;

            RemoveUnitFromCurrentTile(unit);
            unitByTile[targetPos] = unit;
            tileByUnit[unit]      = targetPos;
            unit.MoveTo(targetPos);

            // 비주얼: 이동은 보간
            GameObject visual = GetOrCreateUnitVisual(unit);
            Vector3 worldPos  = GridToWorld(targetPos);
            worldPos.y += unitHeightOffset;
            UnitMover mover = visual.GetComponent<UnitMover>();
            if (mover != null) mover.MoveTo(worldPos);
            else visual.transform.position = worldPos;

            Debug.Log($"[GridManager] 이동: {unit.Data.unitName} → {targetPos} / AP: {unit.CurrentAP}");
            return true;
        }

        public void RemoveUnit(BattleUnit unit) => RemoveUnitFromCurrentTile(unit);

        /// <summary>모든 유닛 점유 정보와 비주얼을 제거한다.</summary>
        public void ClearUnits()
        {
            unitByTile.Clear();
            tileByUnit.Clear();
            foreach (var kvp in visualByUnit)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value);
            }
            visualByUnit.Clear();
        }

        private void RemoveUnitFromCurrentTile(BattleUnit unit)
        {
            if (unit == null) return;
            if (!tileByUnit.TryGetValue(unit, out Vector2Int oldPos)) return;
            unitByTile.Remove(oldPos);
            tileByUnit.Remove(unit);
        }

        // ── 유닛 비주얼 ──────────────────────────────────────────────────

        private GameObject GetOrCreateUnitVisual(BattleUnit unit)
        {
            if (visualByUnit.TryGetValue(unit, out GameObject existing))
                return existing;

            Transform parent = unitVisualParent != null ? unitVisualParent : transform;

            // 우선순위: 유닛 고유 prefab → GridManager defaultUnitMarkerPrefab → 큐브 폴백
            GameObject unitPrefab = (unit.Data != null && unit.Data.prefab != null)
                ? unit.Data.prefab
                : defaultUnitMarkerPrefab;

            GameObject marker;
            if (unitPrefab != null)
            {
                marker = Instantiate(unitPrefab, parent);
            }
            else
            {
                marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.transform.SetParent(parent);
                marker.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
            }

            UnitMover mover = marker.GetComponent<UnitMover>();
            if (mover == null) mover = marker.AddComponent<UnitMover>();
            mover.SetMoveSpeed(unitMoveSpeed);

            visualByUnit[unit] = marker;
            return marker;
        }

        /// <summary>해당 유닛의 마커가 현재 이동 보간 중인지. 입력 잠금에 사용.</summary>
        public bool IsUnitMoving(BattleUnit unit)
        {
            if (!visualByUnit.TryGetValue(unit, out GameObject visual)) return false;
            UnitMover mover = visual.GetComponent<UnitMover>();
            return mover != null && mover.IsMoving;
        }

        // ── 하이라이트 공개 API ───────────────────────────────────────────

        public void HighlightMoveTiles(List<MoveOption> moveOptions)
        {
            currentMoveHighlights.Clear();
            if (moveOptions != null) currentMoveHighlights.AddRange(moveOptions);
            RefreshAllHighlights();
            Debug.Log($"[GridManager] 이동 하이라이트: {currentMoveHighlights.Count}개");
        }

        public void HighlightSkillTiles(List<Vector2Int> tiles)
        {
            currentSkillHighlights.Clear();
            if (tiles != null) currentSkillHighlights.AddRange(tiles);
            RefreshAllHighlights();
            Debug.Log($"[GridManager] 스킬 하이라이트: {currentSkillHighlights.Count}개");
        }

        /// <summary>
        /// 위험 타일 하이라이트.
        /// FloorCubeStater가 있는 큐브는 SafetyType 트랜지션으로 표시,
        /// 없으면 material.color 폴백을 사용한다.
        /// </summary>
        public void HighlightDangerTiles(List<Vector2Int> tiles, SafetyType dangerLevel = SafetyType.DangerS)
        {
            currentDangerHighlights.Clear();
            if (tiles != null)
                foreach (var t in tiles)
                    currentDangerHighlights[t] = dangerLevel;
            RefreshAllHighlights();
            Debug.Log($"[GridManager] 위험 하이라이트: {currentDangerHighlights.Count}개 ({dangerLevel})");
        }

        public void HighlightDeployableTiles(List<Vector2Int> tiles)
        {
            currentDeployableHighlights.Clear();
            if (tiles != null) currentDeployableHighlights.AddRange(tiles);
            RefreshAllHighlights();
            Debug.Log($"[GridManager] 배치 가능 하이라이트: {currentDeployableHighlights.Count}개");
        }

        public void HighlightBlockedDeploymentTiles(List<Vector2Int> tiles)
        {
            currentBlockedDeployHighlights.Clear();
            if (tiles != null) currentBlockedDeployHighlights.AddRange(tiles);
            RefreshAllHighlights();
            Debug.Log($"[GridManager] 배치 불가 하이라이트: {currentBlockedDeployHighlights.Count}개");
        }

        /// <summary>이동·스킬 하이라이트만 제거. 위험 타일(적 예고)은 유지.</summary>
        public void ClearHighlight()
        {
            currentMoveHighlights.Clear();
            currentSkillHighlights.Clear();
            RefreshAllHighlights();
        }

        public void ClearDangerHighlight()
        {
            currentDangerHighlights.Clear();
            RefreshAllHighlights();
        }

        /// <summary>
        /// 타일 GameObject에 GridTile이 없으면 추가하고 초기화.
        /// go를 직접 넘기는 버전 — go는 항상 mapProvider.GetFloorTile()이 반환한 오브젝트여야 함.
        /// </summary>
        public GridTile EnsureGridTile(Vector2Int tile, GameObject go)
        {
            if (go == null) return null;
            GridTile gt = go.GetComponent<GridTile>();
            if (gt == null)
            {
                gt = go.AddComponent<GridTile>();
                gt.Init(tile);
            }
            return gt;
        }

        /// <summary>
        /// mapProvider를 통해 올바른 바닥 타일 오브젝트를 얻어 GridTile을 부착.
        /// 레이캐스트 폴백 등에서 go 참조 없이 호출할 때 사용.
        /// </summary>
        public GridTile EnsureGridTile(Vector2Int tile)
        {
            if (mapProvider == null) return null;
            GameObject go = mapProvider.GetFloorTile(tile);
            return EnsureGridTile(tile, go);
        }

        public void ClearDeploymentHighlight()
        {
            currentDeployableHighlights.Clear();
            currentBlockedDeployHighlights.Clear();
            RefreshAllHighlights();
        }

        public void ClearAllHighlights()
        {
            currentMoveHighlights.Clear();
            currentSkillHighlights.Clear();
            currentDangerHighlights.Clear();
            currentDeployableHighlights.Clear();
            currentBlockedDeployHighlights.Clear();
            RefreshAllHighlights();
        }

        // ── 하이라이트 렌더링 ─────────────────────────────────────────────

        /// <summary>
        /// 모든 활성 하이라이트를 다시 그린다.
        ///
        /// Danger 타일: FloorCubeStater → SafetyType 트랜지션 (DangerS/M/L/XL).
        ///              FloorCubeStater 없으면 material.color 폴백.
        /// 나머지 타일: material.color Lerp.
        /// </summary>
        private void RefreshAllHighlights()
        {
            // 이전 danger 타일을 Safe로 되돌림 (FloorCubeStater 경로)
            foreach (Vector2Int tile in activeStaterTiles)
            {
                GameObject go = GetFloorGameObject(tile);
                if (go == null) continue;
                go.GetComponent<GridTile>()?.ResetSafety();
            }
            activeStaterTiles.Clear();

            // 저장된 Renderer를 원본 색으로 복원 (MPB _BaseColor 경로)
            foreach (var kvp in originalColors)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.GetPropertyBlock(highlightMPB);
                    highlightMPB.SetColor(BaseColorID, kvp.Value);
                    kvp.Key.SetPropertyBlock(highlightMPB);
                }
            }
            originalColors.Clear();

            // 낮은 우선순위 → 높은 우선순위 (material.color)
            ApplyHighlightList(currentBlockedDeployHighlights, blockedDeployColor);
            ApplyHighlightList(currentDeployableHighlights,    deployableTileColor);
            ApplyMoveHighlights();
            ApplyHighlightList(currentSkillHighlights,         skillTileColor);

            // Danger: FloorCubeStater 우선, 없으면 color 폴백
            foreach (var kvp in currentDangerHighlights)
                ApplyDangerHighlight(kvp.Key, kvp.Value);
        }

        private void ApplyDangerHighlight(Vector2Int tile, SafetyType level)
        {
            GameObject go = GetFloorGameObject(tile);
            if (go == null) return;

            GridTile gt = EnsureGridTile(tile, go);
            if (gt != null && gt.HasStater)
            {
                gt.SetSafety(level);
                activeStaterTiles.Add(tile);
            }
            else
            {
                // FloorCubeStater 없는 큐브: material.color 폴백
                ApplyTileHighlight(tile, dangerTileColor);
            }
        }

        private void ApplyHighlightList(List<Vector2Int> tiles, Color color)
        {
            for (int i = 0; i < tiles.Count; i++)
                ApplyTileHighlight(tiles[i], color);
        }

        private void ApplyMoveHighlights()
        {
            for (int i = 0; i < currentMoveHighlights.Count; i++)
                ApplyTileHighlight(currentMoveHighlights[i].tilePos, moveTileColor);
        }

        private void ApplyTileHighlight(Vector2Int tile, Color color)
        {
            GameObject go = GetFloorGameObject(tile);
            if (go == null) return;
            Renderer r = go.GetComponent<Renderer>();
            if (r == null) return;

            if (!originalColors.ContainsKey(r))
            {
                r.GetPropertyBlock(highlightMPB);
                Color orig = highlightMPB.GetColor(BaseColorID);
                if (orig == Color.clear)
                    orig = r.sharedMaterial != null ? r.sharedMaterial.GetColor(BaseColorID) : Color.white;
                originalColors[r] = orig;
            }

            r.GetPropertyBlock(highlightMPB);
            highlightMPB.SetColor(BaseColorID, Color.Lerp(originalColors[r], color, highlightStrength));
            r.SetPropertyBlock(highlightMPB);
        }

        private GameObject GetFloorGameObject(Vector2Int tile)
        {
            if (mapProvider == null) return null;
            GameObject go = mapProvider.GetFloorTile(tile);
            // 첫 접근 시 GridTile 자동 부착
            if (go != null) EnsureGridTile(tile, go);
            return go;
        }

        // ── 배치 프리뷰 ───────────────────────────────────────────────────

        public void PlaceDeploymentPreview(OwnedUnit ownedUnit, Vector2Int tile)
        {
            if (ownedUnit == null) return;
            Debug.Log($"[GridManager] 배치 프리뷰: {ownedUnit.unitData.unitName} → {tile}");
            // TODO: 배치 단계 프리뷰 오브젝트 표시
        }

        public void ClearDeploymentPreview()
        {
            // TODO: 배치 프리뷰 오브젝트 제거
        }
    }
}
