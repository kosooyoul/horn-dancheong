using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KD
{
    // 턴 순서 스모크 테스트
    //
    // 키보드 입력:
    //   1 : 플레이어 + 적 유닛 생성 후 initiative 순서 출력
    //   2 : 동률 처리 확인 (initiative가 같을 때 TeamId 0 우선인지 확인)
    //   3 : 사망 유닛 제외 확인 (첫 번째 플레이어를 사망 처리 후 재정렬)
    //
    // 확인 목표:
    //   - initiative 내림차순 정렬
    //   - 동률이면 플레이어(TeamId 0)가 먼저
    //   - 사망 유닛은 순서에서 제외
    public class TurnOrderSmokeTest : MonoBehaviour
    {
        [Header("Test Data — 플레이어")]
        [SerializeField] private List<UnitData> playerDatas = new List<UnitData>();

        [Header("Test Data — 적")]
        [SerializeField] private List<UnitData> enemyDatas = new List<UnitData>();

        [Header("Start Positions")]
        [SerializeField] private List<Vector2Int> playerPositions = new List<Vector2Int>();
        [SerializeField] private List<Vector2Int> enemyPositions  = new List<Vector2Int>();

        private List<BattleUnit> allUnits = new List<BattleUnit>();

        private void Update()
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) RunBuildOrder();
            if (Keyboard.current.digit2Key.wasPressedThisFrame) RunTieBreak();
            if (Keyboard.current.digit3Key.wasPressedThisFrame) RunDeadExclusion();
        }

        // ── 1 : 기본 순서 출력 ────────────────────────────────────────────

        private void RunBuildOrder()
        {
            Debug.Log("=== [1] TurnOrderManager.BuildTurnOrder ===");

            allUnits.Clear();
            BattleSetup setup = new BattleSetup();

            for (int i = 0; i < playerDatas.Count; i++)
            {
                if (playerDatas[i] == null) continue;
                Vector2Int pos = i < playerPositions.Count ? playerPositions[i] : new Vector2Int(i, 0);
                allUnits.Add(new BattleUnit(playerDatas[i], teamId: 0, startTilePos: pos));
            }

            for (int i = 0; i < enemyDatas.Count; i++)
            {
                if (enemyDatas[i] == null) continue;
                Vector2Int pos = i < enemyPositions.Count ? enemyPositions[i] : new Vector2Int(i, 6);
                allUnits.Add(setup.CreateEnemyBattleUnit(enemyDatas[i], pos));
            }

            PrintOrder(TurnOrderManager.BuildTurnOrder(allUnits));
        }

        // ── 2 : 동률 처리 ─────────────────────────────────────────────────

        private void RunTieBreak()
        {
            Debug.Log("=== [2] initiative 동률 시 플레이어 우선 확인 ===");

            if (playerDatas.Count == 0 || enemyDatas.Count == 0)
            {
                Debug.LogWarning("playerDatas, enemyDatas 모두 1개 이상 필요합니다.");
                return;
            }

            // 같은 UnitData를 플레이어·적에 할당 → initiative 동일
            var units = new List<BattleUnit>
            {
                new BattleUnit(playerDatas[0], teamId: 0, startTilePos: Vector2Int.zero),
                new BattleUnit(enemyDatas[0],  teamId: 1, startTilePos: Vector2Int.one)
            };

            // playerData와 enemyData의 initiative가 다를 수 있으므로 로그로 직접 확인
            Debug.Log($"  플레이어 initiative: {units[0].Stats.initiative}");
            Debug.Log($"  적       initiative: {units[1].Stats.initiative}");
            Debug.Log("  initiative가 같을 때만 동률 테스트 의미 있음");

            PrintOrder(TurnOrderManager.BuildTurnOrder(units));
        }

        // ── 3 : 사망 유닛 제외 ────────────────────────────────────────────

        private void RunDeadExclusion()
        {
            Debug.Log("=== [3] 사망 유닛 제외 확인 ===");

            if (allUnits.Count == 0)
            {
                Debug.LogWarning("[1]을 먼저 실행해 유닛을 생성하세요.");
                return;
            }

            // 첫 번째 플레이어 유닛 사망 처리
            BattleUnit firstPlayer = null;
            foreach (BattleUnit u in allUnits)
            {
                if (u.TeamId == 0) { firstPlayer = u; break; }
            }

            if (firstPlayer == null)
            {
                Debug.LogWarning("플레이어 유닛이 없습니다.");
                return;
            }

            // 강제로 HP를 0으로 내려 사망 처리 (TakeDamage를 충분히 큰 값으로)
            firstPlayer.TakeDamage(99999);
            Debug.Log($"{firstPlayer.Data.unitName} 사망 처리 / IsDead: {firstPlayer.IsDead}");

            PrintOrder(TurnOrderManager.BuildTurnOrder(allUnits));

            // 복구는 없음 — 테스트 목적이므로 PlayMode 재실행으로 초기화
        }

        // ── 유틸 ──────────────────────────────────────────────────────────

        private void PrintOrder(List<BattleUnit> order)
        {
            Debug.Log($"행동 순서 ({order.Count}명):");
            for (int i = 0; i < order.Count; i++)
            {
                BattleUnit u = order[i];
                string team = u.TeamId == 0 ? "플레이어" : "적";
                Debug.Log($"  {i + 1}. [{team}] {u.Data.unitName}  initiative={u.Stats.initiative}  HP={u.CurrentHP}/{u.Stats.maxHP}");
            }
        }
    }
}
