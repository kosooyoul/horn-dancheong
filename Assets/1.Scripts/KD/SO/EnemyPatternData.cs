using System;
using System.Collections.Generic;
using UnityEngine;

namespace KD
{
    // 적 1체의 행동 패턴 정의 — Inspector에서 전투 담당자가 설정
    // 매 턴 steps 중 하나를 랜덤 선택해 발동
    // 새 패턴 추가: Create > KD > Enemy Pattern Data
    [CreateAssetMenu(fileName = "EnemyPatternData_", menuName = "KD/Enemy Pattern Data")]
    public class EnemyPatternData : ScriptableObject
    {
        [Tooltip("매 턴 이 중 하나를 랜덤 선택. 비어있으면 행동 없음")]
        public List<EnemyPatternStep> steps = new List<EnemyPatternStep>();
    }

    // 적 행동 1가지 — 모든 타입이 skill.targetPattern(GridPatternData)을 사용
    //
    // Fixed          : 예고 시점에 fixedForward 방향으로 패턴 계산 → 실행까지 변하지 않음
    // Tracking       : 예고·실행 양쪽에서 가장 가까운 플레이어 방향으로 패턴 재계산
    // RandomUnitTracking : 예고 시점에 랜덤 플레이어 지정 → 실행 직전 그 유닛의 현재 위치 1칸
    [Serializable]
    public class EnemyPatternStep
    {
        [Tooltip("이 행동에 사용할 스킬 (데미지/힐 효과 참조)\n모든 타입에서 skill.targetPattern을 범위로 사용")]
        public SkillData skill;

        [Tooltip("타일 결정 방식")]
        public EnemyPatternStepType stepType = EnemyPatternStepType.Fixed;

        [Tooltip("Fixed 전용 — 패턴을 펼칠 방향 (x=오른쪽, y=앞). 기본값 (0,1) = 정북\n예고 시점에 고정되며 이후 변하지 않음")]
        public Vector2Int fixedForward = Vector2Int.up;
    }

    public enum EnemyPatternStepType
    {
        Fixed,             // Inspector 지정 절대 타일 — 플레이어 이동 무관
        Tracking,          // 실행 직전 가장 가까운 플레이어 방향으로 패턴 재계산
        RandomUnitTracking,// 경고 시점에 랜덤 플레이어 1명 지정 → 실행 직전 그 유닛의 현재 위치 1칸 타격
    }
}
