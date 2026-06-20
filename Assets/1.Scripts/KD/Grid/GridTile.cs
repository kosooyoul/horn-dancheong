using UnityEngine;

namespace KD
{
    /// <summary>
    /// 바닥 타일 큐브에 부착되는 컴포넌트.
    ///
    /// 두 가지 역할:
    ///   1. GridPos   — 레이캐스트 히트 후 좌표 역산 없이 타일 위치를 직접 반환
    ///   2. 위험 상태  — FloorCubeStater.ChangeSafetyType()을 래핑해
    ///                  DangerS/M/L/XL 전환을 트리거한다.
    ///                  FloorCubeStater가 없으면 GridManager가 폴백으로 material.color를 사용.
    ///
    /// 초기화:
    ///   GridManager.EnsureGridTile(tile, gameObject)에서 자동으로 Init() 호출됨.
    /// </summary>
    public class GridTile : MonoBehaviour
    {
        public Vector2Int GridPos  { get; private set; }
        public bool       HasStater => stater != null;

        private FloorCubeStater stater;

        // ── 초기화 ────────────────────────────────────────────────────────

        public void Init(Vector2Int pos)
        {
            GridPos = pos;
            stater = GetComponentInParent<FloorCubeStater>();
            Debug.Log($"[GridTile] Init {pos} on '{gameObject.name}' | stater={(stater != null ? stater.gameObject.name : "없음")}");

            float checkerOffset = ((pos.x + pos.y) % 2 == 0) ? 0f : 1f;
            FloorCubeVisual visual = GetComponentInParent<FloorCubeVisual>();
            if (visual != null)
                visual.SetCheckerAlphaOffset(checkerOffset);
        }

        // ── SafetyType 제어 ───────────────────────────────────────────────

        /// <summary>위험 상태 설정. FloorCubeStater가 있으면 트랜지션 시작.</summary>
        public void SetSafety(SafetyType safety)
        {
            stater?.ChangeSafetyType(safety);
        }

        /// <summary>Safe 상태로 복원.</summary>
        public void ResetSafety()
        {
            stater?.ChangeSafetyType(SafetyType.Safe);
        }
    }
}
