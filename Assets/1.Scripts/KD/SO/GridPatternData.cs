using System;
using System.Collections.Generic;
using UnityEngine;

namespace KD
{
    // 스킬 사거리 패턴 정의 — SkillData.targetPattern에서 참조
    // 특수 이동 스킬(돌진, 점프 등)에도 사용 가능
    // 일반 이동은 MovementType + moveRange + BFS로 처리
    // 새로운 스킬 형태: 코드 수정 없이 이 에셋을 새로 만들기만 하면 됨
    [CreateAssetMenu(fileName = "GridPattern_", menuName = "KD/Grid Pattern")]
    public class GridPatternData : ScriptableObject
    {
        [Header("기본 정보")]
        public string patternId;
        public string patternName;

        [Header("방향 설정")]
        [Tooltip("UseSelectedDirection: 선택 방향 기준 / AllDirections: 여러 방향으로 자동 반복")]
        public PatternDirectionMode directionMode = PatternDirectionMode.UseSelectedDirection;
        [Tooltip("AllDirections일 때만 사용 — 몇 방향으로 반복할지")]
        public DirectionSet directionSet = DirectionSet.Cardinal4;

        [Header("원점 포함 여부")]
        [Tooltip("시전자 자신의 타일을 패턴에 포함할지 여부")]
        public bool includeOrigin = false;

        [Header("고정 셀 목록")]
        [Tooltip("시전자 기준 상대 좌표 목록. x = 오른쪽, y = 앞\n예) 나이트 공격: (-1,2), (1,2)")]
        public List<PatternFixedCell> fixedCells = new List<PatternFixedCell>();

        [Header("Ray 목록")]
        [Tooltip("특정 시작점에서 방향으로 반복 확장되는 범위\nminDistance는 최소 1 (0이면 자동으로 1로 보정됨)")]
        public List<PatternRay> rays = new List<PatternRay>();

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(patternId))
                Debug.LogWarning($"[{name}] patternId가 비어 있습니다.", this);

            if (string.IsNullOrWhiteSpace(patternName))
                Debug.LogWarning($"[{name}] patternName이 비어 있습니다.", this);

            if (fixedCells == null)
                fixedCells = new List<PatternFixedCell>();

            if (rays == null)
                rays = new List<PatternRay>();

            for (int i = 0; i < rays.Count; i++)
            {
                PatternRay ray = rays[i];
                // minDistance 최소 1 강제 — 0이면 원점이 ray에 포함되어 includeOrigin과 중복 위험
                ray.minDistance = Mathf.Max(1, ray.minDistance);
                ray.maxDistance = Mathf.Max(ray.minDistance, ray.maxDistance);
                if (ray.localDirection == Vector2Int.zero)
                    ray.localDirection = Vector2Int.up;
                rays[i] = ray;
            }
        }
#endif
    }

    // 고정 좌표 오프셋 — 예: 나이트 L자 위치, 특정 칸 공격
    [Serializable]
    public struct PatternFixedCell
    {
        [Tooltip("시전자 기준 상대 좌표. x = 오른쪽, y = 앞")]
        public Vector2Int localOffset;
    }

    // Ray — 시작점에서 방향으로 반복 확장되는 범위
    // 예: (0,0)에서 (0,1) 방향으로 3칸 = (0,1), (0,2), (0,3)
    [Serializable]
    public struct PatternRay
    {
        [Tooltip("Ray 시작 로컬 좌표 (보통 (0,0) 사용)")]
        public Vector2Int startLocalOffset;

        [Tooltip("진행 방향. (0,1)=앞, (1,0)=오른쪽, (1,1)=앞오른쪽 대각 등")]
        public Vector2Int localDirection;

        [Tooltip("몇 칸째부터 포함 (최소 1, 0 입력 시 자동 보정됨)")]
        public int minDistance;

        [Tooltip("몇 칸까지 포함")]
        public int maxDistance;

        [Tooltip("벽/장애물을 만나면 Ray를 중단할지 여부")]
        public bool stopOnBlocked;
    }
}
