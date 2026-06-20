using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KD
{
    // 적 예고 공격 스모크 테스트
    //
    // 키보드 입력:
    //   1 : 적·플레이어 배치
    //   2 : PrepareNextIntent  — 랜덤 스텝 선택 + 경고 타일 하이라이트
    //   3 : ExecuteCurrentIntent — 경고 타일 위 플레이어 타격
    //   4 : 2·3 반복 (다른 스텝이 나오는지 확인)
    //
    // 확인 목표:
    //   - 랜덤 스텝 선택
    //   - HighlightDangerTiles 호출
    //   - warningTiles 위 플레이어만 타격 (적 제외)
    //   - 실행 후 경고 타일 초기화
    public class EnemyIntentSmokeTest : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridManager gridManager;

        [Header("Test Data")]
        [SerializeField] private UnitData        enemyData;
        [SerializeField] private UnitData        playerData;
        [SerializeField] private EnemyPatternData enemyPattern;

        [Header("Test Positions")]
        [SerializeField] private Vector2Int enemyPos  = new Vector2Int(3, 3);
        [SerializeField] private Vector2Int playerPos = new Vector2Int(1, 1);

        private BattleUnit             enemy;
        private BattleUnit             player;
        private EnemyIntentController  intentCtrl;

        private void Update()
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) RunSetup();
            if (Keyboard.current.digit2Key.wasPressedThisFrame) RunPrepare();
            if (Keyboard.current.digit3Key.wasPressedThisFrame) RunExecute();
            if (Keyboard.current.digit4Key.wasPressedThisFrame) { RunPrepare(); RunExecute(); }
        }

        // ── 1 ─────────────────────────────────────────────────────────────

        private void RunSetup()
        {
            Debug.Log("=== [1] EnemyIntent Setup ===");

            if (gridManager == null || enemyData == null || playerData == null || enemyPattern == null)
            {
                Debug.LogError("Inspector 참조가 누락되었습니다.");
                return;
            }

            BattleSetup setup = new BattleSetup();

            enemy  = setup.CreateEnemyBattleUnit(enemyData, enemyPos);
            player = new BattleUnit(playerData, teamId: 0, startTilePos: playerPos);

            gridManager.PlaceUnit(enemy,  enemyPos);
            gridManager.PlaceUnit(player, playerPos);

            intentCtrl = new EnemyIntentController(gridManager);

            Debug.Log($"적: {enemy.Data.unitName} @ {enemyPos}");
            Debug.Log($"플레이어: {player.Data.unitName} @ {playerPos} / HP: {player.CurrentHP}");
            Debug.Log($"패턴 스텝 수: {enemyPattern.steps.Count}");
        }

        // ── 2 ─────────────────────────────────────────────────────────────

        private void RunPrepare()
        {
            Debug.Log("=== [2] PrepareNextIntent ===");
            if (!CheckReady()) return;

            EnemyIntent intent = intentCtrl.PrepareNextIntent(enemy, enemyPattern);

            if (intent == null)
            {
                Debug.Log("선택된 intent 없음 (패턴 비어있거나 스텝 누락)");
                return;
            }

            Debug.Log($"선택 스킬: {intent.skill.skillName} / 경고 타일: {intent.warningTiles.Count}개");
            foreach (Vector2Int tile in intent.warningTiles)
                Debug.Log($"  경고: {tile}");
        }

        // ── 3 ─────────────────────────────────────────────────────────────

        private void RunExecute()
        {
            Debug.Log("=== [3] ExecuteCurrentIntent ===");
            if (!CheckReady()) return;

            if (intentCtrl.CurrentIntent == null)
            {
                Debug.LogWarning("실행할 intent가 없습니다. [2]를 먼저 실행하세요.");
                return;
            }

            int hpBefore = player.CurrentHP;
            intentCtrl.ExecuteCurrentIntent();
            int hpAfter = player.CurrentHP;

            Debug.Log($"플레이어 HP: {hpBefore} → {hpAfter} (변화: {hpAfter - hpBefore})");

            if (player.IsDead)
                Debug.Log("플레이어 사망");
        }

        // ── 유틸 ──────────────────────────────────────────────────────────

        private bool CheckReady()
        {
            if (intentCtrl != null) return true;
            Debug.LogWarning("먼저 [1]을 눌러 Setup을 실행하세요.");
            return false;
        }
    }
}
