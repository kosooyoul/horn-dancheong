using System.Collections.Generic;
using UnityEngine;

namespace KD
{
    // 적이 이번 턴에 실행할 행동 — 예고 표시 후 실제 실행에 사용
    public class EnemyIntent
    {
        public BattleUnit        caster;
        public SkillData         skill;
        public List<Vector2Int>  warningTiles = new List<Vector2Int>();

        // 추적형일 때 true — ExecuteCurrentIntent에서 타일을 재계산함
        public bool              isTracking;
        public EnemyPatternStep  sourceStep;  // Tracking 재계산용

        // RandomUnitTracking 전용 — 경고 시 지정된 대상 유닛 (실행 직전 현재 위치 사용)
        public BattleUnit        trackedUnit;
    }
}
