using System.Collections.Generic;
using UnityEngine;

namespace KD
{
    // 적 1체의 행동 예고·실행 담당
    // 사용 순서:
    //   1. PrepareNextIntent(enemy, pattern, playerUnits)  — 예고 타일 표시
    //   2. (연출 대기)
    //   3. ExecuteCurrentIntent(playerUnits)               — 예고 타일 위 유닛에 스킬 적용
    public class EnemyIntentController
    {
        private readonly GridManager gridManager;
        private EnemyIntent          currentIntent;

        public EnemyIntent CurrentIntent => currentIntent;

        public EnemyIntentController(GridManager gridManager)
        {
            this.gridManager = gridManager;
        }

        // 랜덤 스텝 선택 → 예고 타일 하이라이트
        // 반환값: 선택된 EnemyIntent (null이면 행동 없음)
        public EnemyIntent PrepareNextIntent(BattleUnit enemy, EnemyPatternData pattern)
        {
            currentIntent = null;

            if (pattern == null || pattern.steps == null || pattern.steps.Count == 0)
            {
                Debug.Log($"[EnemyIntentController] {enemy?.Data.unitName}: 패턴 없음, 행동 패스");
                return null;
            }

            EnemyPatternStep step = pattern.steps[Random.Range(0, pattern.steps.Count)];

            if (step.skill == null || step.targetTiles == null || step.targetTiles.Count == 0)
            {
                Debug.LogWarning($"[EnemyIntentController] {enemy?.Data.unitName}: 스텝에 스킬 또는 타일이 없음");
                return null;
            }

            currentIntent = new EnemyIntent
            {
                caster       = enemy,
                skill        = step.skill,
                warningTiles = new List<Vector2Int>(step.targetTiles)
            };

            gridManager.HighlightDangerTiles(currentIntent.warningTiles);

            Debug.Log($"[EnemyIntentController] {enemy.Data.unitName} 예고: {step.skill.skillName} / {currentIntent.warningTiles.Count}타일");
            return currentIntent;
        }

        // 예고 타일 위에 있는 플레이어 유닛에 스킬 실행
        // 적 광역 스킬의 apCost는 0으로 설정 권장 (여러 유닛 연속 타격 시 AP 부족 방지)
        public void ExecuteCurrentIntent()
        {
            if (currentIntent == null) return;

            BattleUnit caster = currentIntent.caster;
            SkillData  skill  = currentIntent.skill;

            foreach (Vector2Int tile in currentIntent.warningTiles)
            {
                BattleUnit unitOnTile = gridManager.GetUnitAt(tile);
                if (unitOnTile == null || unitOnTile.IsDead) continue;
                if (unitOnTile.TeamId == caster.TeamId)      continue; // 아군 제외

                SkillExecutor.Execute(caster, unitOnTile, skill);
            }

            gridManager.ClearDangerHighlight();
            currentIntent = null;

            Debug.Log($"[EnemyIntentController] {caster.Data.unitName} → {skill.skillName} 실행 완료");
        }
    }
}
