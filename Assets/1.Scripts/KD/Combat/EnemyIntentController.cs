using System;
using System.Collections.Generic;
using UnityEngine;

namespace KD
{
    // 적 1체의 행동 예고·실행 담당
    // 사용 순서:
    //   1. PrepareNextIntent(enemy, pattern, playerUnits)  — 예고 타일 표시
    //   2. (연출 대기)
    //   3. ExecuteCurrentIntent(playerUnits)               — 예고 타일 위 유닛에 스킬 적용
    //
    // 타입별 타일 결정:
    //   Fixed          : 예고 시 fixedForward 방향으로 패턴 계산 → 실행까지 고정
    //   Tracking       : 예고·실행 모두 가장 가까운 플레이어 방향으로 재계산
    //   RandomUnitTracking : 예고 시 랜덤 플레이어 지정 → 실행 직전 그 유닛의 현재 위치 1칸
    public class EnemyIntentController
    {
        private readonly GridManager gridManager;
        private EnemyIntent          currentIntent;

        public EnemyIntent CurrentIntent => currentIntent;

        public EnemyIntentController(GridManager gridManager)
        {
            this.gridManager = gridManager;
        }

        // 랜덤 스텝 선택 → 예고 타일 하이라이트
        public EnemyIntent PrepareNextIntent(BattleUnit enemy, EnemyPatternData pattern, IReadOnlyList<BattleUnit> playerUnits = null)
        {
            currentIntent = null;

            if (pattern == null || pattern.steps == null || pattern.steps.Count == 0)
            {
                Debug.Log($"[EnemyIntentController] {enemy?.Data.unitName}: 패턴 없음, 행동 패스");
                return null;
            }

            EnemyPatternStep step = pattern.steps[UnityEngine.Random.Range(0, pattern.steps.Count)];

            if (step.skill == null)
            {
                Debug.LogWarning($"[EnemyIntentController] {enemy?.Data.unitName}: 스텝에 스킬이 없음");
                return null;
            }

            List<Vector2Int> tiles;
            BattleUnit trackedUnit = null;

            switch (step.stepType)
            {
                case EnemyPatternStepType.Fixed:
                    if (step.skill.targetPattern == null)
                    {
                        Debug.LogWarning($"[EnemyIntentController] {enemy?.Data.unitName}: skill.targetPattern이 없음");
                        return null;
                    }
                    tiles = GridPatternResolver.GetCells(
                        step.skill.targetPattern,
                        enemy.CurrentTilePos,
                        step.fixedForward == Vector2Int.zero ? Vector2Int.up : step.fixedForward,
                        gridManager.IsValidTile,
                        null);
                    break;

                case EnemyPatternStepType.Tracking:
                    if (step.skill.targetPattern == null)
                    {
                        Debug.LogWarning($"[EnemyIntentController] {enemy?.Data.unitName}: skill.targetPattern이 없음");
                        return null;
                    }
                    tiles = GridPatternResolver.GetCells(
                        step.skill.targetPattern,
                        enemy.CurrentTilePos,
                        GetDirectionToNearestPlayer(enemy, playerUnits),
                        gridManager.IsValidTile,
                        null);
                    break;

                case EnemyPatternStepType.RandomUnitTracking:
                    trackedUnit = PickRandomAlivePlayer(playerUnits);
                    if (trackedUnit == null)
                    {
                        Debug.LogWarning($"[EnemyIntentController] {enemy?.Data.unitName}: 살아있는 플레이어 없음");
                        return null;
                    }
                    tiles = new List<Vector2Int> { trackedUnit.CurrentTilePos };
                    break;

                default:
                    Debug.LogWarning($"[EnemyIntentController] 알 수 없는 stepType: {step.stepType}");
                    return null;
            }

            currentIntent = new EnemyIntent
            {
                caster      = enemy,
                skill       = step.skill,
                warningTiles = tiles,
                isTracking  = step.stepType == EnemyPatternStepType.Tracking,
                sourceStep  = step,
                trackedUnit = trackedUnit,
            };

            SafetyType level = DangerLevelFromTileCount(currentIntent.warningTiles.Count);
            gridManager.HighlightDangerTiles(currentIntent.warningTiles, level);

            Debug.Log($"[EnemyIntentController] {enemy.Data.unitName} 예고({step.stepType}): {step.skill.skillName} / {currentIntent.warningTiles.Count}타일 ({level})");
            return currentIntent;
        }

        // 예고 타일 위에 있는 플레이어 유닛에 스킬 실행
        public void ExecuteCurrentIntent(IReadOnlyList<BattleUnit> playerUnits = null)
        {
            if (currentIntent == null) return;

            BattleUnit caster = currentIntent.caster;
            SkillData  skill  = currentIntent.skill;

            List<Vector2Int> executeTiles = currentIntent.warningTiles;

            // RandomUnitTracking: 지정 유닛의 실행 직전 현재 위치
            if (currentIntent.trackedUnit != null && !currentIntent.trackedUnit.IsDead)
            {
                executeTiles = new List<Vector2Int> { currentIntent.trackedUnit.CurrentTilePos };
            }
            // Tracking: 실행 직전 재계산
            else if (currentIntent.isTracking
                && currentIntent.sourceStep != null
                && currentIntent.sourceStep.skill != null
                && currentIntent.sourceStep.skill.targetPattern != null)
            {
                executeTiles = GridPatternResolver.GetCells(
                    currentIntent.sourceStep.skill.targetPattern,
                    caster.CurrentTilePos,
                    GetDirectionToNearestPlayer(caster, playerUnits),
                    gridManager.IsValidTile,
                    null);
            }
            // Fixed: warningTiles 그대로 사용 (재계산 없음)

            foreach (Vector2Int tile in executeTiles)
            {
                BattleUnit unitOnTile = gridManager.GetUnitAt(tile);
                if (unitOnTile == null || unitOnTile.IsDead) continue;
                if (unitOnTile.TeamId == caster.TeamId)      continue;

                SkillExecutor.Execute(caster, unitOnTile, skill);
            }

            gridManager.ClearDangerHighlight();
            currentIntent = null;

            Debug.Log($"[EnemyIntentController] {caster.Data.unitName} → {skill.skillName} 실행 완료");
        }

        private static SafetyType DangerLevelFromTileCount(int count)
        {
            if (count <= 3)  return SafetyType.DangerS;
            if (count <= 8)  return SafetyType.DangerM;
            if (count <= 15) return SafetyType.DangerL;
            return SafetyType.DangerXL;
        }

        private static BattleUnit PickRandomAlivePlayer(IReadOnlyList<BattleUnit> playerUnits)
        {
            if (playerUnits == null || playerUnits.Count == 0) return null;

            var alive = new List<BattleUnit>();
            foreach (BattleUnit p in playerUnits)
                if (p != null && !p.IsDead) alive.Add(p);

            return alive.Count == 0 ? null : alive[UnityEngine.Random.Range(0, alive.Count)];
        }

        private static Vector2Int GetDirectionToNearestPlayer(BattleUnit enemy, IReadOnlyList<BattleUnit> playerUnits)
        {
            if (playerUnits == null || playerUnits.Count == 0)
                return Vector2Int.up;

            BattleUnit nearest = null;
            int minDist = int.MaxValue;

            foreach (BattleUnit p in playerUnits)
            {
                if (p == null || p.IsDead) continue;
                int dist = Math.Abs(p.CurrentTilePos.x - enemy.CurrentTilePos.x)
                         + Math.Abs(p.CurrentTilePos.y - enemy.CurrentTilePos.y);
                if (dist < minDist) { minDist = dist; nearest = p; }
            }

            if (nearest == null) return Vector2Int.up;

            Vector2Int delta = nearest.CurrentTilePos - enemy.CurrentTilePos;
            return new Vector2Int(Math.Sign(delta.x), Math.Sign(delta.y));
        }
    }
}
