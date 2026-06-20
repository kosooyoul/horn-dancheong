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

        // [Tooltip("true: targetTiles를 맵 절대 좌표로 사용 (고정 예고 AoE)\nfalse: 스킬의 targetPattern을 시전자 기준으로 계산")]
        // public bool useAbsoluteTiles = true;

        [Tooltip("예고 및 타격 대상 타일 목록 (맵 절대 좌표). 여기 있는 플레이어 유닛이 피해를 받음")]
        public List<Vector2Int> targetTiles = new List<Vector2Int>();
    }
}
