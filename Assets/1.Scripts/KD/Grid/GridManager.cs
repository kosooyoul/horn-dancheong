using System.Collections.Generic;
using UnityEngine;

namespace KD
{
    /// <summary>
    /// 게임잼용 간단 GridManager.
    ///
    /// 역할:
    /// - 타일 유효성 검사
    /// - 벽 타일 관리
    /// - 유닛 점유 상태 관리
    /// - 유닛 배치/이동
    /// - 이동/스킬/위험/배치 하이라이트 요청 저장
    ///
    /// 실제 타일 색 변경, 프리팹 이동 연출, 이펙트는
    /// 전투/맵 담당자가 이 함수들 안에 추가하면 됨.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        [Header("Grid Size")]
        [SerializeField] private int width  = 7;
        [SerializeField] private int height = 7;

        [Header("Blocked Tiles")]
        [SerializeField] private List<Vector2Int> wallTiles = new List<Vector2Int>();

        private readonly HashSet<Vector2Int> wallSet = new HashSet<Vector2Int>();

        private readonly Dictionary<Vector2Int, BattleUnit> unitByTile  = new Dictionary<Vector2Int, BattleUnit>();
        private readonly Dictionary<BattleUnit, Vector2Int> tileByUnit  = new Dictionary<BattleUnit, Vector2Int>();

        private readonly List<MoveOption>  currentMoveHighlights            = new List<MoveOption>();
        private readonly List<Vector2Int>  currentSkillHighlights           = new List<Vector2Int>();
        private readonly List<Vector2Int>  currentDangerHighlights          = new List<Vector2Int>();
        private readonly List<Vector2Int>  currentDeployableHighlights      = new List<Vector2Int>();
        private readonly List<Vector2Int>  currentBlockedDeployHighlights   = new List<Vector2Int>();

        public IReadOnlyList<MoveOption>  CurrentMoveHighlights           => currentMoveHighlights;
        public IReadOnlyList<Vector2Int>  CurrentSkillHighlights          => currentSkillHighlights;
        public IReadOnlyList<Vector2Int>  CurrentDangerHighlights         => currentDangerHighlights;
        public IReadOnlyList<Vector2Int>  CurrentDeployableHighlights     => currentDeployableHighlights;
        public IReadOnlyList<Vector2Int>  CurrentBlockedDeployHighlights  => currentBlockedDeployHighlights;

        private void Awake()
        {
            RebuildWallSet();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            width  = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
        }
#endif

        private void RebuildWallSet()
        {
            wallSet.Clear();
            for (int i = 0; i < wallTiles.Count; i++)
                wallSet.Add(wallTiles[i]);
        }

        // ── 타일 판단 ─────────────────────────────────────────────────────

        public bool IsValidTile(Vector2Int pos)
            => pos.x >= 0 && pos.y >= 0 && pos.x < width && pos.y < height;

        /// <summary>이동 불가 판정 — 벽 + 유닛 점유</summary>
        public bool IsBlockedForMove(Vector2Int pos)
        {
            if (!IsValidTile(pos))    return true;
            if (wallSet.Contains(pos)) return true;
            return unitByTile.ContainsKey(pos);
        }

        /// <summary>스킬 Ray 차단 — 벽만. 유닛 있는 타일은 통과</summary>
        public bool IsBlockedForSkillRay(Vector2Int pos)
        {
            if (!IsValidTile(pos)) return true;
            return wallSet.Contains(pos);
        }

        // ── 유닛 조회 ─────────────────────────────────────────────────────

        public BattleUnit GetUnitAt(Vector2Int pos)
        {
            unitByTile.TryGetValue(pos, out BattleUnit unit);
            return unit;
        }

        // ── 유닛 배치/이동 ────────────────────────────────────────────────

        public void PlaceUnit(BattleUnit unit, Vector2Int pos)
        {
            if (unit == null) return;

            if (!IsValidTile(pos))
            {
                Debug.LogWarning($"[GridManager] 유효하지 않은 위치에 배치 시도: {pos}");
                return;
            }
            if (wallSet.Contains(pos))
            {
                Debug.LogWarning($"[GridManager] 벽 타일에 배치 시도: {pos}");
                return;
            }
            if (unitByTile.ContainsKey(pos))
            {
                Debug.LogWarning($"[GridManager] 이미 유닛이 있는 타일: {pos}");
                return;
            }

            RemoveUnitFromCurrentTile(unit);
            unitByTile[pos]  = unit;
            tileByUnit[unit] = pos;
            unit.MoveTo(pos);

            Debug.Log($"[GridManager] 유닛 배치: {unit.Data.unitName} → {pos}");
            // TODO: 전투 담당자 — 실제 프리팹 배치 연출 추가
        }

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

            Debug.Log($"[GridManager] 이동: {unit.Data.unitName} → {targetPos} / AP: {unit.CurrentAP}");
            // TODO: 전투 담당자 — 이동 애니메이션 연출 추가

            return true;
        }

        public void RemoveUnit(BattleUnit unit) => RemoveUnitFromCurrentTile(unit);

        private void RemoveUnitFromCurrentTile(BattleUnit unit)
        {
            if (unit == null) return;
            if (!tileByUnit.TryGetValue(unit, out Vector2Int oldPos)) return;
            unitByTile.Remove(oldPos);
            tileByUnit.Remove(unit);
        }

        // ── 하이라이트 ────────────────────────────────────────────────────

        public void HighlightMoveTiles(List<MoveOption> moveOptions)
        {
            currentMoveHighlights.Clear();
            if (moveOptions != null) currentMoveHighlights.AddRange(moveOptions);
            Debug.Log($"[GridManager] 이동 하이라이트: {currentMoveHighlights.Count}개");
            // TODO: 전투 담당자 — 이동 가능 타일 색상 표시
        }

        public void HighlightSkillTiles(List<Vector2Int> tiles)
        {
            currentSkillHighlights.Clear();
            if (tiles != null) currentSkillHighlights.AddRange(tiles);
            Debug.Log($"[GridManager] 스킬 하이라이트: {currentSkillHighlights.Count}개");
            // TODO: 전투 담당자 — 스킬 범위 타일 색상 표시
        }

        public void HighlightDangerTiles(List<Vector2Int> tiles)
        {
            currentDangerHighlights.Clear();
            if (tiles != null) currentDangerHighlights.AddRange(tiles);
            Debug.Log($"[GridManager] 위험 지역 하이라이트: {currentDangerHighlights.Count}개");
            // TODO: 전투 담당자 — 적 예고 공격 범위 표시 (빨간색 등)
        }

        public void HighlightDeployableTiles(List<Vector2Int> tiles)
        {
            currentDeployableHighlights.Clear();
            if (tiles != null) currentDeployableHighlights.AddRange(tiles);
            Debug.Log($"[GridManager] 배치 가능 타일: {currentDeployableHighlights.Count}개");
            // TODO: 전투 담당자 — 배치 가능 타일 표시
        }

        public void HighlightBlockedDeploymentTiles(List<Vector2Int> tiles)
        {
            currentBlockedDeployHighlights.Clear();
            if (tiles != null) currentBlockedDeployHighlights.AddRange(tiles);
            Debug.Log($"[GridManager] 배치 불가 타일: {currentBlockedDeployHighlights.Count}개");
            // TODO: 전투 담당자 — 배치 불가 타일 표시
        }

        // SimpleBattleManager에서 호출 — 이동/스킬 하이라이트만 지움 (위험 지역 유지)
        public void ClearHighlight()
        {
            currentMoveHighlights.Clear();
            currentSkillHighlights.Clear();
            Debug.Log("[GridManager] 행동 하이라이트 제거");
            // TODO: 전투 담당자 — 이동/스킬 타일 표시 제거
        }

        public void ClearDangerHighlight()
        {
            currentDangerHighlights.Clear();
            // TODO: 전투 담당자 — 적 예고 범위 제거
        }

        public void ClearDeploymentHighlight()
        {
            currentDeployableHighlights.Clear();
            currentBlockedDeployHighlights.Clear();
            // TODO: 전투 담당자 — 배치 표시 제거
        }

        public void ClearAllHighlights()
        {
            currentMoveHighlights.Clear();
            currentSkillHighlights.Clear();
            currentDangerHighlights.Clear();
            currentDeployableHighlights.Clear();
            currentBlockedDeployHighlights.Clear();
            // TODO: 전투 담당자 — 모든 타일 표시 제거
        }

        // ── 배치 프리뷰 ───────────────────────────────────────────────────

        public void PlaceDeploymentPreview(OwnedUnit ownedUnit, Vector2Int tile)
        {
            if (ownedUnit == null) return;
            Debug.Log($"[GridManager] 배치 프리뷰: {ownedUnit.unitData.unitName} → {tile}");
            // TODO: 전투 담당자 — 실제 유닛 배치 전 프리뷰 오브젝트 표시
        }

        public void ClearDeploymentPreview()
        {
            // TODO: 전투 담당자 — 배치 프리뷰 오브젝트 제거
        }
    }
}
