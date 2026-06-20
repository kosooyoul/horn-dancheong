using System;
using System.Collections.Generic;
using UnityEngine;

namespace KD
{
    // 적 1체의 행동 예고·실행 담당
    // 사용 순서:
    //   1. PrepareNextIntent(enemy, pattern, playerUnits)  — 예고 타일 표시
    //   2. (연출 대기)
    //   3. ExecuteCurrentIntent()                          — 예고 타일 위 유닛에 스킬 적용
    public class EnemyIntentController
    {
        private readonly GridManager     gridManager;
        private readonly CombatGridQuery gridQuery;
        private EnemyIntent              currentIntent;

        public EnemyIntent CurrentIntent => currentIntent;

        // gridQuery: 플레이어 스킬 사거리와 '동일한' 계산기 (시전자 기준 offset)
        public EnemyIntentController(GridManager gridManager, CombatGridQuery gridQuery)
        {
            this.gridManager = gridManager;
            this.gridQuery   = gridQuery;
        }

        // 랜덤 스텝 선택 → 예고 타일 하이라이트
        // playerUnits: 사거리 방향(forward) 기준이 될 대상 후보(가장 가까운 플레이어를 바라봄)
        // 반환값: 선택된 EnemyIntent (null이면 행동 없음)
        public EnemyIntent PrepareNextIntent(BattleUnit enemy, EnemyPatternData pattern, IReadOnlyList<BattleUnit> playerUnits)
        {
            currentIntent = null;

            if (pattern == null || pattern.steps == null || pattern.steps.Count == 0)
            {
                Debug.Log($"[EnemyIntentController] {enemy?.Data.unitName}: 패턴 없음, 행동 패스");
                return null;
            }

            // 실제로 시전 가능한(쿨타임 0 + AP 충분) 스텝만 후보로 고른다.
            // → 못 쓰는 스킬을 예고만 띄우는 일이 없고, AP가 부족하면 행동 자체를 패스한다.
            var castableSteps = new List<EnemyPatternStep>();
            foreach (EnemyPatternStep s in pattern.steps)
            {
                if (s == null || s.skill == null)                    continue;
                if (s.targetTiles == null || s.targetTiles.Count == 0) continue;
                if (enemy.GetCooldown(s.skill.skillId) > 0)          continue;
                if (!enemy.HasEnoughAP(s.skill.apCost))              continue;
                castableSteps.Add(s);
            }

            if (castableSteps.Count == 0)
            {
                Debug.Log($"[EnemyIntentController] {enemy?.Data.unitName}: 사용 가능한 스킬 없음(쿨타임/AP 부족), 대기");
                return null;
            }

            // 시전 가능한 스텝 중 '사거리 안에 실제로 아군(플레이어)이 들어있는' 스텝만 후보로 둔다.
            // 사거리에 대상이 없으면 빈 칸을 때리지 않고 패스한다.
            var validSteps = new List<EnemyPatternStep>();
            foreach (EnemyPatternStep s in castableSteps)
            {
                List<Vector2Int> tiles = ResolveTargetTiles(enemy, s, playerUnits);
                if (tiles != null && tiles.Count > 0 && HasHostileInTiles(enemy, tiles))
                    validSteps.Add(s);
            }

            if (validSteps.Count == 0)
            {
                Debug.Log($"[EnemyIntentController] {enemy?.Data.unitName}: 사거리 안에 대상이 없어 패스");
                return null;
            }

            EnemyPatternStep step         = validSteps[Random.Range(0, validSteps.Count)];
            List<Vector2Int> warningTiles = ResolveTargetTiles(enemy, step, playerUnits);
            if (warningTiles == null || warningTiles.Count == 0)
                return null;

            currentIntent = new EnemyIntent
            {
                caster       = enemy,
                skill        = step.skill,
                warningTiles = warningTiles
            };

            SafetyType level = DangerLevelFromTileCount(currentIntent.warningTiles.Count);
            gridManager.HighlightDangerTiles(currentIntent.warningTiles, level);

            Debug.Log($"[EnemyIntentController] {enemy.Data.unitName} 예고({step.stepType}): {step.skill.skillName} / {currentIntent.warningTiles.Count}타일 ({level})");
            return currentIntent;
        }

        // 패턴 타일 해석 — 플레이어 스킬과 '완전히 동일한' 계산 경로를 사용한다.
        // 플레이어:  CombatGridQuery.GetSkillRange(caster, skill, 마우스타일)
        // 보스(AI): CombatGridQuery.GetSkillRange(boss,  skill, 가장 가까운 플레이어 타일)
        //   → 둘 다 '시전자 타일'을 origin으로, 패턴의 상대 offset을 forward 방향으로 펼친다.
        //   useAbsoluteTiles = true  → targetTiles를 맵 절대 좌표 그대로 사용 (고정 예고 AoE)
        //   useAbsoluteTiles = false → 스킬의 targetPattern을 시전자 기준 offset으로 계산
        //                              (targetPattern이 없으면 targetTiles를 시전자 기준 상대 offset으로 사용)
        private List<Vector2Int> ResolveTargetTiles(BattleUnit enemy, EnemyPatternStep step, IReadOnlyList<BattleUnit> playerUnits)
        {
            if (step.useAbsoluteTiles)
                return new List<Vector2Int>(step.targetTiles);

            BattleUnit nearest = FindNearestPlayer(enemy, playerUnits);
            if (nearest == null)
                return null;

            Vector2Int origin     = enemy.CurrentTilePos;
            Vector2Int targetTile = nearest.CurrentTilePos;

            // 1순위: 스킬 사거리 패턴(targetPattern) — 플레이어와 동일한 GetSkillRange로 계산
            //         (마우스 타일 대신 '가장 가까운 플레이어 타일'을 방향 기준으로 전달)
            if (step.skill != null && step.skill.targetPattern != null && gridQuery != null)
            {
                List<Vector2Int> cells = gridQuery.GetSkillRange(enemy, step.skill, targetTile);
                if (cells.Count > 0)
                    return cells;
            }

            // 2순위(폴백): targetPattern이 없으면 targetTiles를 보스 기준 상대 좌표(대상 방향 회전)로 해석
            if (step.targetTiles != null && step.targetTiles.Count > 0)
            {
                Vector2Int forward = DirectionHelper.GetSelectedForward(origin, targetTile, DirectionSet.EightDirections);
                Vector2Int right   = new Vector2Int(forward.y, -forward.x);

                var resolved = new List<Vector2Int>(step.targetTiles.Count);
                foreach (Vector2Int local in step.targetTiles)
                {
                    Vector2Int world = origin + right * local.x + forward * local.y;
                    if (gridManager.IsValidTile(world))
                        resolved.Add(world);
                }
                return resolved;
            }

            return null;
        }

        private static int Manhattan(Vector2Int a, Vector2Int b)
            => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        // 주어진 타일들 중 한 곳이라도 시전자에게 적대적인(다른 팀) 생존 유닛이 있으면 true
        private bool HasHostileInTiles(BattleUnit caster, List<Vector2Int> tiles)
        {
            foreach (Vector2Int tile in tiles)
            {
                BattleUnit unit = gridManager.GetUnitAt(tile);
                if (unit == null || unit.IsDead) continue;
                if (unit.TeamId == caster.TeamId) continue;
                return true;
            }
            return false;
        }

        // enemy에서 맨해튼 거리 기준 가장 가까운 생존 플레이어 반환 (없으면 null)
        private static BattleUnit FindNearestPlayer(BattleUnit enemy, IReadOnlyList<BattleUnit> playerUnits)
        {
            if (enemy == null || playerUnits == null) return null;

            BattleUnit nearest = null;
            int bestDistance = int.MaxValue;

            foreach (BattleUnit player in playerUnits)
            {
                if (player == null || player.IsDead) continue;
                if (player.TeamId == enemy.TeamId)   continue;

                int distance = Manhattan(player.CurrentTilePos, enemy.CurrentTilePos);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest      = player;
                }
            }

            return nearest;
        }

        // 예고 타일 수 → DangerS/M/L/XL
        private static SafetyType DangerLevelFromTileCount(int count)
        {
            if (count <= 3)  return SafetyType.DangerS;
            if (count <= 8)  return SafetyType.DangerM;
            if (count <= 15) return SafetyType.DangerL;
            return SafetyType.DangerXL;
        }

        // 예고 타일 위에 있는 플레이어 유닛에 스킬 실행
        public void ExecuteCurrentIntent(IReadOnlyList<BattleUnit> playerUnits = null)
        {
            if (currentIntent == null) return;

            BattleUnit caster = currentIntent.caster;
            SkillData  skill  = currentIntent.skill;

            // 예고 범위 안의 적대 유닛을 모두 모아 1회 시전으로 동시 타격
            var targets = new List<BattleUnit>();
            foreach (Vector2Int tile in currentIntent.warningTiles)
            {
                BattleUnit unitOnTile = gridManager.GetUnitAt(tile);
                if (unitOnTile == null || unitOnTile.IsDead) continue;
                if (unitOnTile.TeamId == caster.TeamId)      continue; // 아군 제외
                if (targets.Contains(unitOnTile))            continue; // 중복 방지

                targets.Add(unitOnTile);
            }

            if (targets.Count > 0)
                SkillExecutor.ExecuteArea(caster, targets, skill);

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
