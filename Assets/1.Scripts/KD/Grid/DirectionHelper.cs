using System.Collections.Generic;
using UnityEngine;

namespace KD
{
    // 시전자 타일과 마우스 타일로 스킬 방향 벡터를 계산
    // DirectionSet에 따라 가장 가까운 방향으로 스냅
    //
    // 사용 예:
    //   Vector2Int forward = DirectionHelper.GetSelectedForward(caster.CurrentTilePos, hoveredTile, DirectionSet.Cardinal4);
    public static class DirectionHelper
    {
        private static readonly List<Vector2Int> s_cardinal4 = new List<Vector2Int>
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        private static readonly List<Vector2Int> s_diagonal4 = new List<Vector2Int>
        {
            new Vector2Int( 1,  1), new Vector2Int(-1,  1),
            new Vector2Int( 1, -1), new Vector2Int(-1, -1)
        };

        private static readonly List<Vector2Int> s_eight = new List<Vector2Int>
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int( 1,  1), new Vector2Int(-1,  1),
            new Vector2Int( 1, -1), new Vector2Int(-1, -1)
        };

        public static Vector2Int GetSelectedForward(Vector2Int origin, Vector2Int mouseTile, DirectionSet directionSet)
        {
            Vector2Int raw = mouseTile - origin;
            if (raw == Vector2Int.zero)
                return Vector2Int.up;

            return SnapToNearest(raw, GetCandidates(directionSet));
        }

        private static List<Vector2Int> GetCandidates(DirectionSet directionSet)
        {
            switch (directionSet)
            {
                case DirectionSet.Cardinal4:       return s_cardinal4;
                case DirectionSet.Diagonal4:       return s_diagonal4;
                case DirectionSet.EightDirections: return s_eight;
                default:                           return s_cardinal4;
            }
        }

        // 내적이 가장 큰 방향 = 마우스와 가장 같은 방향
        private static Vector2Int SnapToNearest(Vector2Int raw, List<Vector2Int> candidates)
        {
            Vector2Int best    = candidates[0];
            float      bestDot = float.MinValue;

            foreach (Vector2Int dir in candidates)
            {
                float dot = raw.x * dir.x + raw.y * dir.y;
                if (dot > bestDot)
                {
                    bestDot = dot;
                    best    = dir;
                }
            }

            return best;
        }
    }
}
