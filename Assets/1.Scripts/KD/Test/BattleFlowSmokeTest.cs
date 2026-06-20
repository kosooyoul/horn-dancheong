using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KD
{
    /// <summary>
    /// 전체 전투 흐름 스모크 테스트.
    ///
    /// 좌표 규칙:
    /// Vector2Int.x = World X
    /// Vector2Int.y = World Z
    ///
    /// 키 입력:
    /// 1 : 배치 단계 시작
    /// 2 : 파티 유닛 자동 배치
    /// 3 : 배치 확정 후 전투 시작
    /// 4 : 현재 턴 1회 진행
    /// 5 : 적 턴까지 자동 진행
    /// 9 : 상태 출력
    /// </summary>
    public class BattleFlowSmokeTest : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridManager         gridManager;
        [SerializeField] private PlayerRosterManager rosterManager;
        [SerializeField] private DeploymentRuleData  deploymentRuleData;
        [SerializeField] private EnemyPatternData    enemyPatternData;

        [Header("Test Units")]
        [SerializeField] private UnitData        enemyData;
        [SerializeField] private List<UnitData>  autoPartyUnitDatas = new List<UnitData>();

        [Header("Positions")]
        [SerializeField] private Vector2Int       enemyPos = new Vector2Int(3, 6);
        [SerializeField] private List<Vector2Int> autoDeployTiles = new List<Vector2Int>
        {
            new Vector2Int(1, 1),
            new Vector2Int(2, 1),
            new Vector2Int(1, 2),
            new Vector2Int(2, 2)
        };

        private BattleSetup             battleSetup;
        private DeploymentController    deploymentController;
        private EnemyIntentController   enemyIntentController;

        private BattleUnit              enemyUnit;

        private readonly List<BattleUnit> playerUnits = new List<BattleUnit>();
        private readonly List<BattleUnit> allUnits    = new List<BattleUnit>();
        private List<BattleUnit>          turnOrder   = new List<BattleUnit>();

        private int  currentTurnIndex;
        private bool deploymentBuilt;
        private bool battleStarted;

        private void Awake()
        {
            battleSetup = new BattleSetup();
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.digit1Key.wasPressedThisFrame) StartDeploymentTest();
            if (Keyboard.current.digit2Key.wasPressedThisFrame) AutoDeployParty();
            if (Keyboard.current.digit3Key.wasPressedThisFrame) ConfirmDeploymentAndStartBattle();
            if (Keyboard.current.digit4Key.wasPressedThisFrame) StepCurrentTurn();
            if (Keyboard.current.digit5Key.wasPressedThisFrame) RunUntilEnemyTurn();
            if (Keyboard.current.digit9Key.wasPressedThisFrame) PrintState();
        }

        // ── 1. 배치 단계 시작 ─────────────────────────────────────────────

        private void StartDeploymentTest()
        {
            Debug.Log("=== [1] 배치 단계 시작 ===");

            if (!CheckRequiredReferences()) return;

            ResetRuntimeState();
            BuildPartyIfEmpty();

            enemyUnit = battleSetup.CreateEnemyBattleUnit(enemyData, enemyPos);
            if (enemyUnit == null) { Debug.LogError("[BattleFlowSmokeTest] enemyUnit 생성 실패"); return; }

            gridManager.PlaceUnit(enemyUnit, enemyPos);

            deploymentController = new DeploymentController(deploymentRuleData, gridManager);
            deploymentController.BuildDeploymentArea(new List<BattleUnit> { enemyUnit });

            enemyIntentController = new EnemyIntentController(gridManager);

            deploymentBuilt = true;

            Debug.Log($"적 배치: {enemyUnit.Data.unitName} @ {enemyPos}");
            Debug.Log($"파티 인원: {rosterManager.SelectedParty.Count}");
            Debug.Log($"최대 배치 수: {deploymentController.MaxDeployCount}");
        }

        // ── 2. 파티 자동 배치 ─────────────────────────────────────────────

        private void AutoDeployParty()
        {
            Debug.Log("=== [2] 파티 자동 배치 ===");

            if (!deploymentBuilt || deploymentController == null)
            {
                Debug.LogWarning("[1] 배치 단계 시작을 먼저 실행하세요.");
                return;
            }

            IReadOnlyList<OwnedUnit> party = rosterManager.SelectedParty;
            int count = Mathf.Min(party.Count, autoDeployTiles.Count, deploymentController.MaxDeployCount);

            for (int i = 0; i < count; i++)
            {
                OwnedUnit  owned   = party[i];
                Vector2Int tile    = autoDeployTiles[i];
                bool       success = deploymentController.TryPlaceUnit(owned, tile);
                Debug.Log($"배치 시도: {owned.unitData.unitName} → {tile} / 결과: {success}");
            }

            PrintPlacements();
        }

        // ── 3. 배치 확정 후 전투 시작 ────────────────────────────────────

        private void ConfirmDeploymentAndStartBattle()
        {
            Debug.Log("=== [3] 배치 확정 + 전투 시작 ===");

            if (!deploymentBuilt || deploymentController == null)
            {
                Debug.LogWarning("[1] 배치 단계 시작을 먼저 실행하세요.");
                return;
            }

            if (!deploymentController.IsDeploymentReady())
            {
                Debug.LogWarning("배치된 유닛이 없습니다.");
                return;
            }

            playerUnits.Clear();
            allUnits.Clear();

            List<BattleUnit> createdPlayers =
                battleSetup.CreatePlayerBattleUnits(deploymentController.Placements);

            foreach (BattleUnit unit in createdPlayers)
            {
                playerUnits.Add(unit);
                allUnits.Add(unit);
                gridManager.PlaceUnit(unit, unit.CurrentTilePos);
            }

            allUnits.Add(enemyUnit);

            battleStarted = true;

            StartNewRound();
        }

        // ── 라운드 시작 ───────────────────────────────────────────────────

        private void StartNewRound()
        {
            Debug.Log("=== 새 라운드 시작 ===");

            RemoveDeadUnits();

            turnOrder        = TurnOrderManager.BuildTurnOrder(allUnits);
            currentTurnIndex = 0;

            PrintTurnOrder();
            PrepareEnemyIntent();
            PrintCurrentTurn();
        }

        private void PrepareEnemyIntent()
        {
            if (enemyUnit == null || enemyUnit.IsDead) return;
            if (enemyIntentController == null)
                enemyIntentController = new EnemyIntentController(gridManager);

            EnemyIntent intent = enemyIntentController.PrepareNextIntent(enemyUnit, enemyPatternData);
            if (intent == null) { Debug.LogWarning("[Enemy Intent] 생성 실패"); return; }

            Debug.Log($"[Enemy Intent] 예고 스킬: {intent.skill.skillName}");
            Debug.Log($"[Enemy Intent] 경고 타일 수: {intent.warningTiles.Count}");
            foreach (Vector2Int tile in intent.warningTiles)
                Debug.Log($"  위험 타일: {tile}");
        }

        // ── 4. 현재 턴 1회 진행 ──────────────────────────────────────────

        private void StepCurrentTurn()
        {
            if (!battleStarted) { Debug.LogWarning("[3] 전투 시작을 먼저 실행하세요."); return; }
            if (turnOrder == null || turnOrder.Count == 0) { Debug.LogWarning("turnOrder가 비어 있습니다."); return; }

            if (currentTurnIndex >= turnOrder.Count)
            {
                StartNewRound();
                return;
            }

            BattleUnit current = turnOrder[currentTurnIndex];

            if (current == null || current.IsDead)
            {
                AdvanceTurn();
                return;
            }

            if (current.TeamId == 0)
                SimulatePlayerTurn(current);
            else
                SimulateEnemyTurn(current);

            CheckBattleEnd();

            if (battleStarted)
                AdvanceTurn();
        }

        private void SimulatePlayerTurn(BattleUnit unit)
        {
            Debug.Log($"[Player Turn] {unit.Data.unitName}");
            int beforeAP = unit.CurrentAP;
            unit.Wait();
            Debug.Log($"  대기 처리: AP {beforeAP} → {unit.CurrentAP}");
        }

        private void SimulateEnemyTurn(BattleUnit enemy)
        {
            Debug.Log($"[Enemy Turn] {enemy.Data.unitName}");

            if (enemyIntentController == null || enemyIntentController.CurrentIntent == null)
            {
                Debug.LogWarning("실행할 적 Intent가 없습니다. 새로 생성합니다.");
                PrepareEnemyIntent();
            }

            PrintPlayerHP("[Before Enemy Skill]");
            enemyIntentController.ExecuteCurrentIntent();
            PrintPlayerHP("[After Enemy Skill]");
        }

        private void AdvanceTurn()
        {
            currentTurnIndex++;

            if (currentTurnIndex >= turnOrder.Count)
            {
                StartNewRound();
                return;
            }

            PrintCurrentTurn();
        }

        // ── 5. 적 턴까지 자동 진행 ───────────────────────────────────────

        private void RunUntilEnemyTurn()
        {
            Debug.Log("=== [5] 적 턴까지 자동 진행 ===");

            if (!battleStarted) { Debug.LogWarning("[3] 전투 시작을 먼저 실행하세요."); return; }

            int safety = 20;

            while (safety > 0)
            {
                if (currentTurnIndex >= turnOrder.Count)
                {
                    StartNewRound();
                    return;
                }

                BattleUnit current = turnOrder[currentTurnIndex];

                if (current != null && !current.IsDead && current.TeamId == 1)
                {
                    StepCurrentTurn();
                    return;
                }

                StepCurrentTurn();
                safety--;
            }

            Debug.LogWarning("안전 제한으로 자동 진행 중단");
        }

        // ── 9. 상태 출력 ─────────────────────────────────────────────────

        private void PrintState()
        {
            Debug.Log("=== [9] 현재 상태 출력 ===");
            Debug.Log($"deploymentBuilt: {deploymentBuilt} / battleStarted: {battleStarted}");

            if (enemyUnit != null)
                Debug.Log($"적: {enemyUnit.Data.unitName} HP {enemyUnit.CurrentHP}/{enemyUnit.Stats.maxHP} @ {enemyUnit.CurrentTilePos}");

            PrintPlacements();
            PrintTurnOrder();
            PrintPlayerHP("[Player HP]");
        }

        // ── 내부 유틸 ─────────────────────────────────────────────────────

        private void PrintPlacements()
        {
            if (deploymentController == null) { Debug.Log("[Placements] deploymentController 없음"); return; }
            Debug.Log($"[Placements] 총 {deploymentController.Placements.Count}명");
            foreach (DeploymentPlacement p in deploymentController.Placements)
                Debug.Log($"  {p.ownedUnit?.unitData?.unitName ?? "null"} @ {p.tilePos}");
        }

        private void PrintTurnOrder()
        {
            if (turnOrder == null || turnOrder.Count == 0) { Debug.Log("[TurnOrder] 비어 있음"); return; }
            Debug.Log($"[TurnOrder] 총 {turnOrder.Count}명");
            for (int i = 0; i < turnOrder.Count; i++)
            {
                BattleUnit u    = turnOrder[i];
                string     team = u.TeamId == 0 ? "Player" : "Enemy";
                Debug.Log($"  {i + 1}. [{team}] {u.Data.unitName} / initiative={u.Stats.initiative} / HP={u.CurrentHP}/{u.Stats.maxHP}");
            }
        }

        private void PrintCurrentTurn()
        {
            if (turnOrder == null || currentTurnIndex < 0 || currentTurnIndex >= turnOrder.Count) return;
            BattleUnit u    = turnOrder[currentTurnIndex];
            string     team = u.TeamId == 0 ? "Player" : "Enemy";
            Debug.Log($"[Current Turn] {currentTurnIndex + 1}/{turnOrder.Count} [{team}] {u.Data.unitName}");
        }

        private void PrintPlayerHP(string label)
        {
            Debug.Log(label);
            foreach (BattleUnit u in playerUnits)
                if (u != null)
                    Debug.Log($"  {u.Data.unitName}: HP {u.CurrentHP}/{u.Stats.maxHP} @ {u.CurrentTilePos}");
        }

        private void RemoveDeadUnits()
        {
            for (int i = allUnits.Count - 1; i >= 0; i--)
                if (allUnits[i] == null || allUnits[i].IsDead) allUnits.RemoveAt(i);

            for (int i = playerUnits.Count - 1; i >= 0; i--)
                if (playerUnits[i] == null || playerUnits[i].IsDead) playerUnits.RemoveAt(i);
        }

        private void CheckBattleEnd()
        {
            if (enemyUnit == null || enemyUnit.IsDead)
            {
                Debug.Log("[Battle End] 승리");
                battleStarted = false;
                return;
            }

            bool allDead = true;
            foreach (BattleUnit u in playerUnits)
                if (u != null && !u.IsDead) { allDead = false; break; }

            if (allDead)
            {
                Debug.Log("[Battle End] 패배");
                battleStarted = false;
            }
        }

        private void ResetRuntimeState()
        {
            deploymentBuilt  = false;
            battleStarted    = false;
            currentTurnIndex = 0;
            enemyUnit        = null;

            playerUnits.Clear();
            allUnits.Clear();
            turnOrder.Clear();

            gridManager.ClearAllHighlights();
            gridManager.ClearUnits();
        }

        private void BuildPartyIfEmpty()
        {
            if (rosterManager == null) return;
            if (rosterManager.SelectedParty != null && rosterManager.SelectedParty.Count > 0) return;

            if (autoPartyUnitDatas == null || autoPartyUnitDatas.Count == 0)
            {
                Debug.LogWarning("SelectedParty가 비어 있고 autoPartyUnitDatas도 없습니다.");
                return;
            }

            Debug.Log("[BattleFlowSmokeTest] SelectedParty 비어 있음 — autoPartyUnitDatas로 파티 구성");
            foreach (UnitData data in autoPartyUnitDatas)
            {
                if (data == null) continue;
                OwnedUnit owned = rosterManager.AddUnit(data);
                rosterManager.SelectPartyUnit(owned);
            }
        }

        private bool CheckRequiredReferences()
        {
            if (gridManager        == null) { Debug.LogError("gridManager 없음");        return false; }
            if (rosterManager      == null) { Debug.LogError("rosterManager 없음");      return false; }
            if (deploymentRuleData == null) { Debug.LogError("deploymentRuleData 없음"); return false; }
            if (enemyPatternData   == null) { Debug.LogError("enemyPatternData 없음");   return false; }
            if (enemyData          == null) { Debug.LogError("enemyData 없음");          return false; }
            return true;
        }
    }
}
