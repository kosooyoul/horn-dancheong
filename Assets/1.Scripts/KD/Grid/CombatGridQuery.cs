using System;
using System.Collections.Generic;
using UnityEngine;

namespace KD
{
    // 전투 담당자용 그리드 질의 API
    // GridPatternResolver / MovementRangeCalculator를 직접 호출하지 않아도 됨
    //
    // 생성 예 (BattleManager 등에서):
    //   var query = new CombatGridQuery(
    //       isValidTile:          pos => gridManager.IsValidTile(pos),
    //       isBlockedForMove:     pos => gridManager.IsBlockedForMove(pos),   // 벽 + 유닛 점유
    //       isBlockedForSkillRay: pos => gridManager.IsBlockedForSkillRay(pos) // 벽만
    //   );
    //
    // 스킬 범위:  query.GetSkillRange(caster, skill, hoveredTile)
    // 이동 범위:  query.GetMoveOptions(unit)
    public class CombatGridQuery
    {
        private readonly Func<Vector2Int, bool> _isValidTile;
        private readonly Func<Vector2Int, bool> _isBlockedForMove;
        private readonly Func<Vector2Int, bool> _isBlockedForSkillRay;

        // isBlockedForMove     : 벽 + 유닛 점유 — 이동 불가 타일
        // isBlockedForSkillRay : 벽만 — 유닛이 있어도 스킬 Ray는 통과
        public CombatGridQuery(
            Func<Vector2Int, bool> isValidTile,
            Func<Vector2Int, bool> isBlockedForMove,
            Func<Vector2Int, bool> isBlockedForSkillRay)
        {
            _isValidTile          = isValidTile;
            _isBlockedForMove     = isBlockedForMove;
            _isBlockedForSkillRay = isBlockedForSkillRay;
        }

        // 스킬 범위 타일 목록 — 마우스 타일 방향으로 DirectionHelper 스냅 적용
        // Ray 블로킹은 isBlockedForSkillRay 사용 (유닛 타일도 범위에 포함됨)
        public List<Vector2Int> GetSkillRange(BattleUnit caster, SkillData skill, Vector2Int mouseTile)
        {
            if (caster == null)
            {
                Debug.LogWarning("[CombatGridQuery] caster null");
                return new List<Vector2Int>();
            }
            if (skill == null)
            {
                Debug.LogWarning("[CombatGridQuery] skill null");
                return new List<Vector2Int>();
            }
            if (skill.targetPattern == null)
            {
                Debug.LogWarning("[CombatGridQuery] targetPattern null");
                return new List<Vector2Int>();
            }

            GridPatternData pattern = skill.targetPattern;
            Vector2Int forward = DirectionHelper.GetSelectedForward(
                caster.CurrentTilePos, mouseTile, pattern.directionSet);

            Debug.Log($"[CombatGridQuery] origin={caster.CurrentTilePos}, mouse={mouseTile}, forward={forward}");

            List<Vector2Int> cells = GridPatternResolver.GetCells(
                pattern, caster.CurrentTilePos, forward,
                _isValidTile, _isBlockedForSkillRay);

            Debug.Log($"[CombatGridQuery] result count={cells.Count}");
            return cells;
        }

        // 이동 가능 타일 목록 — 현재 AP가 부족하면 빈 목록 반환
        // 블로킹은 isBlockedForMove 사용 (벽 + 유닛 점유 모두 막음)
        public List<MoveOption> GetMoveOptions(BattleUnit unit)
        {
            if (unit == null)
                return new List<MoveOption>();

            return MovementRangeCalculator.Calculate(
                unit.CurrentTilePos,
                unit.Data.movementType,
                unit.Stats.moveRange,
                unit.Data.moveAPCost,
                unit.CurrentAP,
                _isValidTile,
                _isBlockedForMove);
        }
    }
}
