using System;
using System.Collections.Generic;
using UnityEngine;

namespace KD
{
    // BFS 기반 이동 가능 범위 계산 — MoveOption 목록 반환
    // CombatGridQuery.GetMoveOptions()에서 호출
    // apCost는 UnitData.moveAPCost 고정값 사용 (거리 무관)
    public static class MovementRangeCalculator
    {
        public static List<MoveOption> Calculate(
            Vector2Int origin,
            MovementType moveType,
            int moveRange,
            int moveAPCost,
            int currentAP,
            Func<Vector2Int, bool> isValidTile,
            Func<Vector2Int, bool> isBlockedTile)
        {
            if (currentAP < moveAPCost)
                return new List<MoveOption>();

            switch (moveType)
            {
                case MovementType.KnightJump:
                    return GetKnightMoves(origin, moveAPCost, isValidTile, isBlockedTile);
                case MovementType.Teleport:
                    return GetTeleportMoves(origin, moveRange, moveAPCost, isValidTile, isBlockedTile);
                case MovementType.Charge:
                    return GetChargeMoves(origin, moveRange, moveAPCost, isValidTile, isBlockedTile);
                default: // Cardinal, EightDir, DiagonalOnly
                    return GetBFSMoves(origin, moveType, moveRange, moveAPCost, isValidTile, isBlockedTile);
            }
        }

        // BFS — Cardinal / EightDir / DiagonalOnly
        private static List<MoveOption> GetBFSMoves(
            Vector2Int origin,
            MovementType moveType,
            int moveRange,
            int moveAPCost,
            Func<Vector2Int, bool> isValidTile,
            Func<Vector2Int, bool> isBlockedTile)
        {
            var result  = new List<MoveOption>();
            var visited = new HashSet<Vector2Int> { origin };
            var queue   = new Queue<(Vector2Int pos, int dist)>();
            queue.Enqueue((origin, 0));
            List<Vector2Int> dirs = GetNeighborDirs(moveType);

            while (queue.Count > 0)
            {
                var (pos, dist) = queue.Dequeue();

                if (dist > 0)
                    result.Add(new MoveOption { tilePos = pos, distance = dist, apCost = moveAPCost });

                if (dist >= moveRange) continue;

                foreach (Vector2Int dir in dirs)
                {
                    Vector2Int next = pos + dir;
                    if (visited.Contains(next))                       continue;
                    if (isValidTile   != null && !isValidTile(next))  continue;
                    if (isBlockedTile != null && isBlockedTile(next)) continue;

                    visited.Add(next);
                    queue.Enqueue((next, dist + 1));
                }
            }

            return result;
        }

        // KnightJump — L자 8칸 (장애물 무시)
        private static List<MoveOption> GetKnightMoves(
            Vector2Int origin,
            int moveAPCost,
            Func<Vector2Int, bool> isValidTile,
            Func<Vector2Int, bool> isBlockedTile)
        {
            var result = new List<MoveOption>();
            Vector2Int[] offsets =
            {
                new Vector2Int( 1,  2), new Vector2Int(-1,  2),
                new Vector2Int( 1, -2), new Vector2Int(-1, -2),
                new Vector2Int( 2,  1), new Vector2Int(-2,  1),
                new Vector2Int( 2, -1), new Vector2Int(-2, -1)
            };

            foreach (Vector2Int offset in offsets)
            {
                Vector2Int pos = origin + offset;
                if (isValidTile   != null && !isValidTile(pos))  continue;
                if (isBlockedTile != null && isBlockedTile(pos)) continue;
                result.Add(new MoveOption { tilePos = pos, distance = 1, apCost = moveAPCost });
            }

            return result;
        }

        // Teleport — 체비쇼프 거리 moveRange 내 모든 빈 타일 (장애물 무시)
        private static List<MoveOption> GetTeleportMoves(
            Vector2Int origin,
            int moveRange,
            int moveAPCost,
            Func<Vector2Int, bool> isValidTile,
            Func<Vector2Int, bool> isBlockedTile)
        {
            var result = new List<MoveOption>();

            for (int dx = -moveRange; dx <= moveRange; dx++)
            for (int dy = -moveRange; dy <= moveRange; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int chebyshev = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                if (chebyshev > moveRange) continue;

                Vector2Int pos = new Vector2Int(origin.x + dx, origin.y + dy);
                if (isValidTile   != null && !isValidTile(pos))  continue;
                if (isBlockedTile != null && isBlockedTile(pos)) continue;
                result.Add(new MoveOption { tilePos = pos, distance = chebyshev, apCost = moveAPCost });
            }

            return result;
        }

        // Charge — 4방향 직선, 막히면 중단
        private static List<MoveOption> GetChargeMoves(
            Vector2Int origin,
            int moveRange,
            int moveAPCost,
            Func<Vector2Int, bool> isValidTile,
            Func<Vector2Int, bool> isBlockedTile)
        {
            var result = new List<MoveOption>();
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (Vector2Int dir in dirs)
            {
                for (int d = 1; d <= moveRange; d++)
                {
                    Vector2Int pos = origin + dir * d;
                    if (isValidTile   != null && !isValidTile(pos))  break;
                    if (isBlockedTile != null && isBlockedTile(pos)) break;
                    result.Add(new MoveOption { tilePos = pos, distance = d, apCost = moveAPCost });
                }
            }

            return result;
        }

        private static List<Vector2Int> GetNeighborDirs(MovementType moveType)
        {
            switch (moveType)
            {
                case MovementType.DiagonalOnly:
                    return new List<Vector2Int>
                    {
                        new Vector2Int( 1,  1), new Vector2Int(-1,  1),
                        new Vector2Int( 1, -1), new Vector2Int(-1, -1)
                    };
                case MovementType.EightDir:
                    return new List<Vector2Int>
                    {
                        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
                        new Vector2Int( 1,  1), new Vector2Int(-1,  1),
                        new Vector2Int( 1, -1), new Vector2Int(-1, -1)
                    };
                default: // Cardinal
                    return new List<Vector2Int>
                    {
                        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
                    };
            }
        }
    }
}
