using System.Collections.Generic;
using UnityEngine;

namespace KD
{
    // 배치 페이즈 로직 담당
    //
    // 좌표 규칙:
    // Vector2Int.x = World X
    // Vector2Int.y = World Z
    //
    // 사용 순서:
    // 1. BuildDeploymentArea(enemyUnits)
    // 2. TryPlaceUnit / TryRemovePlacement
    // 3. IsDeploymentReady()
    // 4. Placements를 BattleSetup에 전달
    public class DeploymentController
    {
        private readonly DeploymentRuleData ruleData;
        private readonly GridManager gridManager;

        private readonly HashSet<Vector2Int>     deployableSet = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int>     forbiddenSet  = new HashSet<Vector2Int>();
        private readonly List<DeploymentPlacement> placements  = new List<DeploymentPlacement>();

        // 하이라이트용 캐시 — BuildDeploymentArea 후 ShowHighlights()로 필요할 때 적용
        private readonly List<Vector2Int> deployableTiles = new List<Vector2Int>();
        private readonly List<Vector2Int> blockedTiles    = new List<Vector2Int>();

        public IReadOnlyList<DeploymentPlacement> Placements => placements;
        public int MaxDeployCount => ruleData != null ? ruleData.maxDeployCount : 0;

        public DeploymentController(DeploymentRuleData ruleData, GridManager gridManager)
        {
            this.ruleData = ruleData;
            this.gridManager = gridManager;
        }

        public void BuildDeploymentArea(IReadOnlyList<BattleUnit> enemyUnits)
        {
            deployableSet.Clear();
            forbiddenSet.Clear();
            placements.Clear();
            deployableTiles.Clear();
            blockedTiles.Clear();

            if (ruleData == null || gridManager == null)
            {
                Debug.LogError("[DeploymentController] ruleData 또는 gridManager가 없습니다.");
                return;
            }

            if (ruleData.forbiddenPatternFromEnemy != null && enemyUnits != null)
            {
                foreach (BattleUnit enemy in enemyUnits)
                {
                    if (enemy == null || enemy.IsDead)
                        continue;

                    List<Vector2Int> forbidden = GridPatternResolver.GetCells(
                        ruleData.forbiddenPatternFromEnemy,
                        enemy.CurrentTilePos,
                        Vector2Int.up,
                        gridManager.IsValidTile,
                        null
                    );

                    foreach (Vector2Int tile in forbidden)
                        forbiddenSet.Add(tile);
                }
            }

            foreach (Vector2Int tile in ruleData.candidateDeployTiles)
            {
                if (!gridManager.IsValidTile(tile))
                    continue;

                if (forbiddenSet.Contains(tile))
                    blockedTiles.Add(tile);
                else
                {
                    deployableSet.Add(tile);
                    deployableTiles.Add(tile);
                }
            }

            // 하이라이트는 즉시 적용하지 않음 — ShowHighlights()를 명시적으로 호출할 것

            Debug.Log($"[DeploymentController] 배치 가능: {deployableTiles.Count}타일 / 금지: {blockedTiles.Count}타일");
        }

        /// <summary>유닛 선택 시 호출 — 배치 가능/금지 타일을 하이라이트한다.</summary>
        public void ShowHighlights()
        {
            gridManager.HighlightDeployableTiles(deployableTiles);
            gridManager.HighlightBlockedDeploymentTiles(blockedTiles);
        }

        /// <summary>유닛 선택 해제 또는 배치 확정 시 호출 — 배치 하이라이트를 제거한다.</summary>
        public void HideHighlights()
        {
            gridManager.ClearDeploymentHighlight();
        }

        public bool TryPlaceUnit(OwnedUnit ownedUnit, Vector2Int tile)
        {
            if (ownedUnit == null)
            { Debug.Log($"[DeploymentController] 실패: ownedUnit null"); return false; }

            if (ruleData == null || gridManager == null)
            { Debug.Log($"[DeploymentController] 실패: ruleData={ruleData==null} gridManager={gridManager==null}"); return false; }

            if (!deployableSet.Contains(tile))
            { Debug.Log($"[DeploymentController] 실패: {tile}는 배치 가능 타일이 아님 (deployable={deployableSet.Count}개)"); return false; }

            if (gridManager.GetUnitAt(tile) != null)
            { Debug.Log($"[DeploymentController] 실패: {tile}에 이미 유닛 있음"); return false; }

            if (IsAlreadyPlacedAt(tile))
            { Debug.Log($"[DeploymentController] 실패: {tile}에 이미 배치됨"); return false; }

            bool alreadyPlaced = IsAlreadyPlacedUnit(ownedUnit);

            if (!alreadyPlaced && placements.Count >= ruleData.maxDeployCount)
            { Debug.Log($"[DeploymentController] 실패: 최대 배치 수({ruleData.maxDeployCount}) 초과"); return false; }

            // 같은 유닛이 이미 다른 칸에 배치되어 있으면 기존 배치를 지우고 새 위치로 이동
            RemovePlacementOf(ownedUnit);

            placements.Add(new DeploymentPlacement(ownedUnit, tile));

            Debug.Log($"[DeploymentController] 배치: {ownedUnit.unitData.unitName} → {tile} ({placements.Count}/{ruleData.maxDeployCount})");

            return true;
        }

        public bool TryRemovePlacement(Vector2Int tile)
        {
            for (int i = 0; i < placements.Count; i++)
            {
                if (placements[i].tilePos == tile)
                {
                    Debug.Log($"[DeploymentController] 배치 취소: {placements[i].ownedUnit.unitData.unitName} ({tile})");
                    placements.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public bool IsDeploymentReady()
        {
            return placements.Count > 0;
        }

        private bool IsAlreadyPlacedAt(Vector2Int tile)
        {
            foreach (DeploymentPlacement placement in placements)
            {
                if (placement.tilePos == tile)
                    return true;
            }

            return false;
        }

        private bool IsAlreadyPlacedUnit(OwnedUnit ownedUnit)
        {
            foreach (DeploymentPlacement placement in placements)
            {
                if (placement.ownedUnit == ownedUnit)
                    return true;
            }

            return false;
        }

        private void RemovePlacementOf(OwnedUnit ownedUnit)
        {
            for (int i = placements.Count - 1; i >= 0; i--)
            {
                if (placements[i].ownedUnit == ownedUnit)
                    placements.RemoveAt(i);
            }
        }
    }
}