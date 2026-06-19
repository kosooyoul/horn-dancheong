using System.Collections.Generic;
using UnityEngine;

namespace KD
{
    // 전투 맵별 배치 규칙 — Inspector에서 맵 에디터가 설정
    // 새 맵 배치 규칙 추가: Create > KD > Deployment Rule Data
    [CreateAssetMenu(fileName = "DeploymentRuleData_", menuName = "KD/Deployment Rule Data")]
    public class DeploymentRuleData : ScriptableObject
    {
        [Header("배치 후보 타일")]
        [Tooltip("플레이어가 유닛을 배치할 수 있는 후보 타일 목록")]
        public List<Vector2Int> candidateDeployTiles = new List<Vector2Int>();

        [Header("배치 금지 구역 (적 기준)")]
        [Tooltip("각 적 유닛 위치를 중심으로 배치를 금지할 패턴. AllDirections + 3x3 fixedCells 등 사용\nnull이면 금지 구역 없음")]
        public GridPatternData forbiddenPatternFromEnemy;

        [Header("배치 제한")]
        [Tooltip("이번 전투에서 배치 가능한 최대 유닛 수")]
        public int maxDeployCount = 4;

#if UNITY_EDITOR
        private void OnValidate()
        {
            maxDeployCount = Mathf.Max(1, maxDeployCount);
        }
#endif
    }
}
