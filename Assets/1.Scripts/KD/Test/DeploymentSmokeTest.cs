using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KD
{
    // 배치 시스템 스모크 테스트
    //
    // 키보드 입력:
    //   1 : 배치 영역 계산 (적 위치 기준 금지 구역 + 가능 타일 분리)
    //   2 : 파티 첫 번째 유닛을 testDeployTile에 배치
    //   3 : 파티 두 번째 유닛을 testDeployTile2에 배치
    //   4 : 같은 유닛(첫 번째)을 testDeployTile2로 이동 시도 (중복 방지 + 위치 변경 확인)
    //   5 : testDeployTile의 배치 취소
    //   9 : 현재 placements 목록 출력
    //
    // 확인 목표:
    //   - 배치 가능/금지 타일 분리
    //   - 같은 유닛 중복 배치 방지 (이전 위치 자동 해제)
    //   - TryRemovePlacement 정상 동작
    public class DeploymentSmokeTest : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridManager        gridManager;
        [SerializeField] private DeploymentRuleData deploymentRuleData;
        [SerializeField] private PlayerRosterManager rosterManager;

        [Header("Test Data")]
        [SerializeField] private UnitData    enemyData;
        [SerializeField] private Vector2Int  enemyPos        = new Vector2Int(3, 6);
        [SerializeField] private Vector2Int  testDeployTile  = new Vector2Int(1, 1);
        [SerializeField] private Vector2Int  testDeployTile2 = new Vector2Int(2, 1);

        private BattleUnit           enemy;
        private DeploymentController deployment;

        private void Update()
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) RunBuildDeploymentArea();
            if (Keyboard.current.digit2Key.wasPressedThisFrame) RunPlaceFirstUnit();
            if (Keyboard.current.digit3Key.wasPressedThisFrame) RunPlaceSecondUnit();
            if (Keyboard.current.digit4Key.wasPressedThisFrame) RunMoveSameUnit();
            if (Keyboard.current.digit5Key.wasPressedThisFrame) RunRemovePlacement();
            if (Keyboard.current.digit9Key.wasPressedThisFrame) PrintPlacements();
        }

        // ── 1 ─────────────────────────────────────────────────────────────

        private void RunBuildDeploymentArea()
        {
            Debug.Log("=== [1] BuildDeploymentArea ===");

            if (gridManager == null || deploymentRuleData == null)
            {
                Debug.LogError("gridManager 또는 deploymentRuleData가 없습니다.");
                return;
            }

            BattleSetup setup = new BattleSetup();
            enemy = setup.CreateEnemyBattleUnit(enemyData, enemyPos);

            if (enemy == null) { Debug.LogError("enemy 생성 실패"); return; }

            gridManager.PlaceUnit(enemy, enemyPos);
            Debug.Log($"적 배치: {enemy.Data.unitName} @ {enemyPos}");

            deployment = new DeploymentController(deploymentRuleData, gridManager);
            deployment.BuildDeploymentArea(new List<BattleUnit> { enemy });

            Debug.Log($"최대 배치 수: {deployment.MaxDeployCount}");
            Debug.Log($"파티 인원: {rosterManager?.SelectedParty?.Count ?? 0}명");
        }

        // ── 2 ─────────────────────────────────────────────────────────────

        private void RunPlaceFirstUnit()
        {
            Debug.Log("=== [2] TryPlaceUnit (첫 번째 유닛) ===");
            if (!CheckReady()) return;

            OwnedUnit first = GetPartyUnit(0);
            if (first == null) return;

            bool ok = deployment.TryPlaceUnit(first, testDeployTile);
            Debug.Log($"배치 결과: {ok} / {first.unitData.unitName} → {testDeployTile}");
            PrintPlacements();
        }

        // ── 3 ─────────────────────────────────────────────────────────────

        private void RunPlaceSecondUnit()
        {
            Debug.Log("=== [3] TryPlaceUnit (두 번째 유닛) ===");
            if (!CheckReady()) return;

            OwnedUnit second = GetPartyUnit(1);
            if (second == null) return;

            bool ok = deployment.TryPlaceUnit(second, testDeployTile2);
            Debug.Log($"배치 결과: {ok} / {second.unitData.unitName} → {testDeployTile2}");
            PrintPlacements();
        }

        // ── 4 : 중복 방지 + 위치 변경 ─────────────────────────────────────

        private void RunMoveSameUnit()
        {
            Debug.Log("=== [4] 같은 유닛을 다른 칸으로 이동 (중복 방지 확인) ===");
            if (!CheckReady()) return;

            OwnedUnit first = GetPartyUnit(0);
            if (first == null) return;

            bool ok = deployment.TryPlaceUnit(first, testDeployTile2);
            Debug.Log($"이동 결과: {ok} / {first.unitData.unitName} → {testDeployTile2} (기존 위치 자동 해제 기대)");
            PrintPlacements();
        }

        // ── 5 ─────────────────────────────────────────────────────────────

        private void RunRemovePlacement()
        {
            Debug.Log("=== [5] TryRemovePlacement ===");
            if (!CheckReady()) return;

            bool ok = deployment.TryRemovePlacement(testDeployTile);
            Debug.Log($"취소 결과: {ok} / 타일: {testDeployTile}");
            PrintPlacements();
        }

        // ── 유틸 ──────────────────────────────────────────────────────────

        private void PrintPlacements()
        {
            if (deployment == null) { Debug.Log("[Placements] deployment 없음"); return; }

            Debug.Log($"[Placements] 총 {deployment.Placements.Count}명:");
            foreach (DeploymentPlacement p in deployment.Placements)
                Debug.Log($"  {p.ownedUnit?.unitData?.unitName ?? "null"} @ {p.tilePos}");
        }

        private bool CheckReady()
        {
            if (deployment != null) return true;
            Debug.LogWarning("먼저 [1]을 눌러 BuildDeploymentArea를 실행하세요.");
            return false;
        }

        private OwnedUnit GetPartyUnit(int index)
        {
            if (rosterManager == null || rosterManager.SelectedParty == null ||
                rosterManager.SelectedParty.Count <= index)
            {
                Debug.LogWarning($"파티 유닛[{index}]가 없습니다. PlayerRosterManager.SelectedParty를 확인하세요.");
                return null;
            }
            return rosterManager.SelectedParty[index];
        }
    }
}
