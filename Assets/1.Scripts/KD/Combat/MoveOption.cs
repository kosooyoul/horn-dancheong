using System;
using UnityEngine;

namespace KD
{
    // 이동 가능 타일 하나를 표현
    // MovementRangeCalculator가 List<MoveOption>으로 반환
    // apCost는 UnitData.moveAPCost (이동 1회 고정 비용, 거리 무관)
    [Serializable]
    public struct MoveOption
    {
        public Vector2Int tilePos;  // 이동 목적지 타일 좌표
        public int        distance; // 이동 거리 (칸 수, 경로 길이)
        public int        apCost;   // 이 이동을 실행할 때 소모할 AP
    }
}
