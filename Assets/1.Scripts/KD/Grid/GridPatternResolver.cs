using System;
using System.Collections.Generic;
using UnityEngine;

namespace KD
{
    // GridPatternData를 읽어 실제 월드 타일 좌표 목록을 반환
    // 스킬 사거리 하이라이트, 범위 타겟 계산 시 사용
    //
    // 사용 예:
    //   var cells = GridPatternResolver.GetCells(skill.targetPattern, caster.TilePos, selectedForward,
    //                   pos => gridManager.IsValidTile(pos),
    //                   pos => gridManager.IsWallTile(pos));
    public static class GridPatternResolver
    {
        // 메인 진입점
        // pattern        : SkillData.targetPattern
        // origin         : 시전자의 현재 타일 좌표
        // selectedForward: 시전자가 바라보는 방향 (UI에서 플레이어가 선택)
        // isValidTile    : 맵 범위 안인지 확인 (null이면 검사 생략)
        // isBlockedTile  : 벽/장애물 여부 확인 — 적 유닛은 포함하지 말 것 (null이면 검사 생략)
        public static List<Vector2Int> GetCells(
            GridPatternData pattern,
            Vector2Int origin,
            Vector2Int selectedForward,
            Func<Vector2Int, bool> isValidTile = null,
            Func<Vector2Int, bool> isBlockedTile = null)
        {
            if (pattern == null)
            {
                Debug.LogWarning("[GridPatternResolver] pattern이 null입니다.");
                return new List<Vector2Int>();
            }

            selectedForward = NormalizeDir(selectedForward);
            if (selectedForward == Vector2Int.zero)
                selectedForward = Vector2Int.up;

            HashSet<Vector2Int> result = new HashSet<Vector2Int>();
            List<Vector2Int> directions = GetDirections(pattern, selectedForward);

            foreach (Vector2Int forward in directions)
                Resolve(pattern, origin, forward, result, isValidTile, isBlockedTile);

            return new List<Vector2Int>(result);
        }

        // ── 내부 구현 ─────────────────────────────────────────────────────

        private static void Resolve(
            GridPatternData pattern,
            Vector2Int origin,
            Vector2Int forward,
            HashSet<Vector2Int> result,
            Func<Vector2Int, bool> isValidTile,
            Func<Vector2Int, bool> isBlockedTile)
        {
            if (pattern.includeOrigin)
                TryAdd(origin, result, isValidTile);

            // fixedCells: 벽 체크 적용 (스킬은 통과, 이동은 막혀야 하므로 isBlockedTile 활용)
            foreach (PatternFixedCell cell in pattern.fixedCells)
            {
                Vector2Int worldPos = LocalToWorld(origin, forward, cell.localOffset);
                if (isBlockedTile != null && isBlockedTile(worldPos))
                    continue;
                TryAdd(worldPos, result, isValidTile);
            }

            foreach (PatternRay ray in pattern.rays)
                ResolveRay(ray, origin, forward, result, isValidTile, isBlockedTile);
        }

        private static void ResolveRay(
            PatternRay ray,
            Vector2Int origin,
            Vector2Int forward,
            HashSet<Vector2Int> result,
            Func<Vector2Int, bool> isValidTile,
            Func<Vector2Int, bool> isBlockedTile)
        {
            Vector2Int dir = NormalizeDir(ray.localDirection);
            if (dir == Vector2Int.zero) return;

            Debug.Log($"[ResolveRay] origin={origin}, forward={forward}, dir={dir}, min={ray.minDistance}, max={ray.maxDistance}");

            for (int d = ray.minDistance; d <= ray.maxDistance; d++)
            {
                Vector2Int localOffset = ray.startLocalOffset + dir * d;
                Vector2Int worldPos    = LocalToWorld(origin, forward, localOffset);

                bool valid   = isValidTile   == null || isValidTile(worldPos);
                bool blocked = isBlockedTile != null && isBlockedTile(worldPos);

                Debug.Log($"[ResolveRay] d={d}, local={localOffset}, world={worldPos}, valid={valid}, blocked={blocked}");

                if (!valid)
                {
                    if (ray.stopOnBlocked) break;
                    continue;
                }
                if (blocked)
                {
                    if (ray.stopOnBlocked) break;
                    continue;
                }

                result.Add(worldPos);
            }
        }

        private static void TryAdd(Vector2Int cell, HashSet<Vector2Int> result, Func<Vector2Int, bool> isValidTile)
        {
            if (isValidTile != null && !isValidTile(cell)) return;
            result.Add(cell);
        }

        // 로컬 좌표(시전자 기준 상대 좌표) → 월드 타일 좌표 변환
        // forward 방향을 로컬 "앞(y+)"으로 회전 적용
        private static Vector2Int LocalToWorld(Vector2Int origin, Vector2Int forward, Vector2Int localOffset)
        {
            // right = forward를 시계방향 90도 회전
            Vector2Int right = new Vector2Int(forward.y, -forward.x);
            return origin + right * localOffset.x + forward * localOffset.y;
        }

        private static Vector2Int NormalizeDir(Vector2Int dir)
            => new Vector2Int(Math.Sign(dir.x), Math.Sign(dir.y));

        private static List<Vector2Int> GetDirections(GridPatternData pattern, Vector2Int selectedForward)
        {
            if (pattern.directionMode == PatternDirectionMode.UseSelectedDirection)
                return new List<Vector2Int> { selectedForward };

            switch (pattern.directionSet)
            {
                case DirectionSet.Cardinal4:
                    return new List<Vector2Int>
                    {
                        Vector2Int.up, Vector2Int.down,
                        Vector2Int.left, Vector2Int.right
                    };
                case DirectionSet.Diagonal4:
                    return new List<Vector2Int>
                    {
                        new Vector2Int(1, 1),  new Vector2Int(-1, 1),
                        new Vector2Int(1, -1), new Vector2Int(-1, -1)
                    };
                case DirectionSet.EightDirections:
                    return new List<Vector2Int>
                    {
                        Vector2Int.up, Vector2Int.down,
                        Vector2Int.left, Vector2Int.right,
                        new Vector2Int(1, 1),  new Vector2Int(-1, 1),
                        new Vector2Int(1, -1), new Vector2Int(-1, -1)
                    };
                default:
                    return new List<Vector2Int> { selectedForward };
            }
        }
    }
}
