using System.Collections.Generic;
using UnityEngine;

namespace KD
{
    /// <summary>
    /// 전술 전투 메인 매니저.
    ///
    /// 흐름: 배치 페이즈 → 전투 페이즈 (initiative 순 턴 교대)
    ///
    /// 전투 담당자가 연결해야 할 것:
    ///   - 마우스 입력 → OnDeploymentTileClicked / SelectUnit / OnTileClicked / UpdateHoveredTile 호출
    ///   - UI 버튼     → ConfirmDeployment / SelectMoveAction / SelectSkillAction / WaitSelectedUnit 호출
    ///   - 적 예고 연출 → PrepareEnemyTurn() 호출 후 연출 재생, 완료 시 ExecuteEnemyTurn() 호출
    ///   - 애니메이션  → GridManager 각 TODO 위치에 추가
    /// </summary>
    public class TacticalBattleManager : MonoBehaviour, HornDancheong.Seongwoo.UI.ICombatInteractionController
    {
        [Header("References")]
        [SerializeField] private PlayerRosterManager rosterManager;
        [SerializeField] private GridManager         gridManager;

        [Header("UI References")]
        [SerializeField] private HornDancheong.Seongwoo.UI.CombatInteractionUIManager combatUIManager;
        [SerializeField] private SkillActionRunner   skillActionRunner;

        [Header("Deployment")]
        [SerializeField] private DeploymentRuleData deploymentRuleData;

        [Header("Enemy Setup")]
        [SerializeField] private List<UnitData>         enemyUnitDatas      = new List<UnitData>();
        [SerializeField] private List<Vector2Int>        enemyStartPositions = new List<Vector2Int>();
        [SerializeField] private List<EnemyPatternData>  enemyPatterns       = new List<EnemyPatternData>();

        // ── 런타임 상태 ───────────────────────────────────────────────────

        private readonly List<BattleUnit> playerUnits = new List<BattleUnit>();
        private readonly List<BattleUnit> enemyUnits  = new List<BattleUnit>();
        private readonly List<BattleUnit> allUnits    = new List<BattleUnit>();

        private BattleSetup              battleSetup;
        private CombatGridQuery          combatGridQuery;
        private SkillRangePreview        skillRangePreview;
        private DeploymentController     deploymentController;

        private readonly List<EnemyIntentController> intentControllers = new List<EnemyIntentController>();

        private List<BattleUnit> turnOrder     = new List<BattleUnit>();
        private int              turnIndex     = 0;

        private BattleUnit       selectedUnit;
        private SkillData        selectedSkill;
        private List<MoveOption> currentMoveOptions = new List<MoveOption>();

        private BattlePhase      currentPhase      = BattlePhase.None;
        private BattleActionMode currentActionMode = BattleActionMode.None;

        // ── 외부 조회용 프로퍼티 ─────────────────────────────────────────

        public BattlePhase      CurrentPhase      => currentPhase;
        public BattleActionMode CurrentActionMode => currentActionMode;
        public BattleUnit       SelectedUnit      => selectedUnit;

        public bool IsDeploymentReady
            => deploymentController != null && deploymentController.IsDeploymentReady();

        public IReadOnlyList<DeploymentPlacement> CurrentPlacements
            => deploymentController?.Placements ?? System.Array.Empty<DeploymentPlacement>();

        public int MaxDeployCount
            => deploymentController?.MaxDeployCount ?? 0;
        public EnemyIntent      CurrentEnemyIntent
        {
            get
            {
                BattleUnit current = CurrentTurnUnit;
                if (current == null) return null;
                int idx = enemyUnits.IndexOf(current);
                return idx >= 0 ? intentControllers[idx].CurrentIntent : null;
            }
        }

        private BattleUnit CurrentTurnUnit
            => (turnIndex < turnOrder.Count) ? turnOrder[turnIndex] : null;

        // ── 초기화 ────────────────────────────────────────────────────────

        private void Start()
        {
            InitializeBattle();
        }

        public void InitializeBattle()
        {
            if (rosterManager      == null) { Debug.LogError("[TacticalBattleManager] rosterManager 없음"); return; }
            if (gridManager        == null) { Debug.LogError("[TacticalBattleManager] gridManager 없음");   return; }
            if (deploymentRuleData == null) { Debug.LogError("[TacticalBattleManager] deploymentRuleData 없음"); return; }

            battleSetup = new BattleSetup();

            combatGridQuery = new CombatGridQuery(
                gridManager.IsValidTile,
                gridManager.IsBlockedForMove,
                gridManager.IsBlockedForSkillRay);

            skillRangePreview = new SkillRangePreview(
                combatGridQuery,
                gridManager.HighlightSkillTiles,
                gridManager.ClearHighlight);

            CreateEnemyBattleUnits();

            deploymentController = new DeploymentController(deploymentRuleData, gridManager);
            deploymentController.BuildDeploymentArea(enemyUnits);

            gridManager.InitializeCheckerboard();

            currentPhase = BattlePhase.Deployment;
            Debug.Log("[TacticalBattleManager] 배치 페이즈 시작");
        }

        private void CreateEnemyBattleUnits()
        {
            enemyUnits.Clear();
            intentControllers.Clear();

            List<BattleUnit> units = battleSetup.CreateEnemyBattleUnits(enemyUnitDatas, enemyStartPositions);

            foreach (BattleUnit unit in units)
            {
                enemyUnits.Add(unit);
                allUnits.Add(unit);
                gridManager.PlaceUnit(unit, unit.CurrentTilePos);
                intentControllers.Add(new EnemyIntentController(gridManager));
            }
        }

        // ── 배치 페이즈 API ───────────────────────────────────────────────

        /// <summary>유닛 선택 시 배치 가능 타일 하이라이트 표시.</summary>
        public void ShowDeploymentHighlights()
        {
            if (currentPhase == BattlePhase.Deployment)
                deploymentController?.ShowHighlights();
        }

        /// <summary>유닛 선택 해제 또는 배치 확정 시 배치 하이라이트 제거.</summary>
        public void HideDeploymentHighlights()
        {
            deploymentController?.HideHighlights();
        }

        /// <summary>배치 타일 클릭. unit != null이면 배치 시도, null이면 해당 타일 배치 취소. 성공 여부 반환.</summary>
        public bool OnDeploymentTileClicked(Vector2Int tile, OwnedUnit unit)
        {
            if (currentPhase != BattlePhase.Deployment) return false;

            if (unit != null)
            {
                if (deploymentController.TryPlaceUnit(unit, tile))
                {
                    gridManager.PlaceDeploymentPreview(unit, tile);
                    return true;
                }
                return false;
            }
            else
            {
                bool removed = deploymentController.TryRemovePlacement(tile);
                if (removed) gridManager.ClearDeploymentPreview();
                return removed;
            }
        }

        /// <summary>배치 확정 버튼 클릭.</summary>
        public void ConfirmDeployment()
        {
            if (currentPhase != BattlePhase.Deployment) return;

            if (!deploymentController.IsDeploymentReady())
            {
                Debug.Log("[TacticalBattleManager] 최소 1명 이상 배치해야 합니다.");
                return;
            }

            gridManager.ClearDeploymentHighlight();
            gridManager.ClearDeploymentPreview();

            List<BattleUnit> units = battleSetup.CreatePlayerBattleUnits(deploymentController.Placements);
            foreach (BattleUnit unit in units)
            {
                playerUnits.Add(unit);
                allUnits.Add(unit);
                gridManager.PlaceUnit(unit, unit.CurrentTilePos);
            }

            StartBattlePhase();
        }

        // ── 전투 페이즈 흐름 ──────────────────────────────────────────────

        private void StartBattlePhase()
        {
            turnOrder = TurnOrderManager.BuildTurnOrder(allUnits);
            turnIndex = 0;

            Debug.Log("[TacticalBattleManager] 전투 시작 / 턴 순서:");
            for (int i = 0; i < turnOrder.Count; i++)
                Debug.Log($"  {i + 1}. {turnOrder[i].Data.unitName} (initiative: {turnOrder[i].Stats.initiative})");

            ProcessCurrentTurn();
        }

        private void ProcessCurrentTurn()
        {
            // 사망한 유닛 건너뜀
            while (turnIndex < turnOrder.Count && turnOrder[turnIndex].IsDead)
                turnIndex++;

            if (turnIndex >= turnOrder.Count)
            {
                StartNewRound();
                return;
            }

            BattleUnit current = turnOrder[turnIndex];

            if (current.TeamId == 0)
            {
                // 플레이어 턴
                currentPhase = BattlePhase.PlayerPhase;
                selectedUnit = current;
                currentActionMode = BattleActionMode.None;
                Debug.Log($"[TacticalBattleManager] 플레이어 턴: {current.Data.unitName} / AP: {current.CurrentAP}");

                // UI 갱신 및 표시
                if (combatUIManager != null)
                {
                    combatUIManager.Initialize(new HornDancheong.Seongwoo.UI.BattleUnitAdapter(current, true), this);
                }
            }
            else
            {
                // 적 턴
                currentPhase = BattlePhase.EnemyPhase;
                PrepareEnemyTurn(current);
                // TODO: 전투 담당자 — 예고 연출 후 ExecuteEnemyTurn() 호출
                // MVP: 즉시 실행
                ExecuteEnemyTurn();
            }
        }

        // ── 적 턴 API ─────────────────────────────────────────────────────

        /// <summary>적 예고 단계 — 경고 타일 표시. 연출 후 ExecuteEnemyTurn() 호출.</summary>
        public void PrepareEnemyTurn(BattleUnit enemy)
        {
            int idx = enemyUnits.IndexOf(enemy);
            if (idx < 0) return;

            EnemyPatternData pattern = idx < enemyPatterns.Count ? enemyPatterns[idx] : null;
            intentControllers[idx].PrepareNextIntent(enemy, pattern);
        }

        /// <summary>적 예고 실행 — 경고 타일 위 플레이어 유닛 타격 후 턴 종료.</summary>
        public void ExecuteEnemyTurn()
        {
            BattleUnit current = CurrentTurnUnit;
            if (current == null || current.TeamId != 1) return;

            int idx = enemyUnits.IndexOf(current);
            if (idx >= 0)
                intentControllers[idx].ExecuteCurrentIntent();

            EndCurrentTurn();
        }

        // ── 플레이어 턴 API ───────────────────────────────────────────────

        /// <summary>이동 버튼 클릭.</summary>
        public void SelectMoveAction()
        {
            if (currentPhase != BattlePhase.PlayerPhase || selectedUnit == null) return;

            selectedSkill     = null;
            currentActionMode = BattleActionMode.Move;
            currentMoveOptions = combatGridQuery.GetMoveOptions(selectedUnit);

            gridManager.ClearHighlight();
            gridManager.HighlightMoveTiles(currentMoveOptions);
            Debug.Log($"[TacticalBattleManager] 이동 모드 / {currentMoveOptions.Count}타일");
        }

        /// <summary>스킬 버튼 클릭.</summary>
        public void SelectSkillAction(SkillData skill)
        {
            if (currentPhase != BattlePhase.PlayerPhase || selectedUnit == null || skill == null) return;
            if (!selectedUnit.HasSkill(skill))            { Debug.Log("[TacticalBattleManager] 보유하지 않은 스킬"); return; }
            if (!selectedUnit.HasEnoughAP(skill.apCost))  { Debug.Log($"[TacticalBattleManager] AP 부족 (필요: {skill.apCost})"); return; }

            selectedSkill     = skill;
            currentActionMode = BattleActionMode.Skill;
            currentMoveOptions.Clear();
            gridManager.ClearHighlight();
            Debug.Log($"[TacticalBattleManager] 스킬 모드: {skill.skillName}");
        }

        /// <summary>마우스가 타일 위에 올라갈 때 호출 — 스킬 범위 갱신.</summary>
        public void UpdateHoveredTile(Vector2Int hoveredTile)
        {
            if (currentPhase      != BattlePhase.PlayerPhase) return;
            if (currentActionMode != BattleActionMode.Skill)  return;
            if (selectedUnit == null || selectedSkill == null) return;

            skillRangePreview.Show(selectedUnit, selectedSkill, hoveredTile);
        }

        /// <summary>타일 클릭 — 이동 또는 스킬 발동.</summary>
        public void OnTileClicked(Vector2Int tile)
        {
            if (currentPhase != BattlePhase.PlayerPhase) return;
            if (skillActionRunner != null && skillActionRunner.IsRunning) return;

            switch (currentActionMode)
            {
                case BattleActionMode.Move:  TryMoveSelectedUnit(tile);  break;
                case BattleActionMode.Skill: TryUseSelectedSkill(tile);  break;
            }
        }

        /// <summary>대기 버튼 클릭.</summary>
        public void WaitSelectedUnit()
        {
            if (currentPhase != BattlePhase.PlayerPhase || selectedUnit == null) return;
            selectedUnit.Wait();
            Debug.Log($"[TacticalBattleManager] 대기 / AP: {selectedUnit.CurrentAP}");
            EndCurrentTurn();
        }

        /// <summary>현재 행동 모드를 취소하고 None 상태로 돌아간다. 이동/스킬 하이라이트를 제거한다.</summary>
        public void CancelCurrentAction()
        {
            if (currentPhase != BattlePhase.PlayerPhase) return;
            currentActionMode = BattleActionMode.None;
            selectedSkill     = null;
            currentMoveOptions.Clear();
            skillRangePreview.Clear();
            gridManager.ClearHighlight();
            Debug.Log("[TacticalBattleManager] 행동 취소");

            // UI를 액션 메뉴 상태로 복구
            if (combatUIManager != null)
            {
                combatUIManager.ShowActionMenu();
            }
        }

        // ── 이동·스킬 내부 로직 ──────────────────────────────────────────

        private void TryMoveSelectedUnit(Vector2Int tile)
        {
            if (!TryGetMoveOption(tile, out MoveOption move)) { Debug.Log("[TacticalBattleManager] 이동 가능 타일 아님"); return; }
            if (!gridManager.TryMoveUnit(selectedUnit, move)) { Debug.Log("[TacticalBattleManager] 이동 실패");          return; }

            Debug.Log($"[TacticalBattleManager] 이동 완료 → {tile}");
            EndCurrentTurn();
        }

        private void TryUseSelectedSkill(Vector2Int tile)
        {
            if (!skillRangePreview.Contains(tile)) { Debug.Log("[TacticalBattleManager] 스킬 범위 밖"); return; }

            if (selectedUnit.GetCooldown(selectedSkill.skillId) > 0) { Debug.Log("[TacticalBattleManager] 스킬 쿨타임"); return; }
            if (!selectedUnit.HasEnoughAP(selectedSkill.apCost))     { Debug.Log("[TacticalBattleManager] AP 부족"); return; }

            List<BattleUnit>  targets     = CollectSkillTargets();
            List<Vector2Int>  targetTiles = new(skillRangePreview.CurrentRange);
            SkillData         skill       = selectedSkill;

            if (targets.Count == 0 && skill.targetType != TargetType.Tile)
            {
                Debug.Log("[TacticalBattleManager] 유효한 대상 없음");
                return;
            }

            if (skillActionRunner != null)
            {
                skillActionRunner.StartUseSkill(selectedUnit, targets, targetTiles, skill, OnSkillComplete);
            }
            else
            {
                if (targets.Count == 1)
                    SkillExecutor.Execute(selectedUnit, targets[0], skill);
                else if (targets.Count > 1)
                    SkillExecutor.ExecuteArea(selectedUnit, targets, skill);
                OnSkillComplete();
            }
        }

        private List<BattleUnit> CollectSkillTargets()
        {
            var result = new List<BattleUnit>();

            if (selectedSkill.targetType == TargetType.Self)
            {
                result.Add(selectedUnit);
                return result;
            }

            foreach (Vector2Int t in skillRangePreview.CurrentRange)
            {
                BattleUnit u = gridManager.GetUnitAt(t);
                if (u == null || !u.IsAlive) continue;

                if      (selectedSkill.targetType == TargetType.Enemy   && u.TeamId != selectedUnit.TeamId) result.Add(u);
                else if (selectedSkill.targetType == TargetType.Ally    && u.TeamId == selectedUnit.TeamId) result.Add(u);
                else if (selectedSkill.targetType == TargetType.AnyUnit) result.Add(u);
            }

            return result;
        }

        private void OnSkillComplete()
        {
            Debug.Log("[TacticalBattleManager] 스킬 완료");
            EndCurrentTurn();
        }

        private bool TryGetMoveOption(Vector2Int tile, out MoveOption result)
        {
            for (int i = 0; i < currentMoveOptions.Count; i++)
            {
                if (currentMoveOptions[i].tilePos == tile)
                {
                    result = currentMoveOptions[i];
                    return true;
                }
            }
            result = default;
            return false;
        }

        // ── 턴·라운드 종료 ────────────────────────────────────────────────

        private void EndCurrentTurn()
        {
            selectedUnit?.OnTurnEnd();

            selectedUnit      = null;
            selectedSkill     = null;
            currentActionMode = BattleActionMode.None;
            currentMoveOptions.Clear();
            skillRangePreview.Clear();
            gridManager.ClearHighlight();

            // UI 비활성화
            if (combatUIManager != null)
            {
                combatUIManager.gameObject.SetActive(false);
            }

            if (CheckBattleEnd()) return;

            turnIndex++;
            ProcessCurrentTurn();
        }

        private void StartNewRound()
        {
            Debug.Log("[TacticalBattleManager] 라운드 종료 → 새 라운드");
            turnOrder = TurnOrderManager.BuildTurnOrder(allUnits);
            turnIndex = 0;
            ProcessCurrentTurn();
        }

        private bool CheckBattleEnd()
        {
            bool allPlayersDead = true;
            foreach (BattleUnit u in playerUnits)
                if (u != null && !u.IsDead) { allPlayersDead = false; break; }

            bool allEnemiesDead = true;
            foreach (BattleUnit u in enemyUnits)
                if (u != null && !u.IsDead) { allEnemiesDead = false; break; }

            if (allPlayersDead)
            {
                currentPhase = BattlePhase.BattleEnd;
                Debug.Log("[TacticalBattleManager] 전투 종료 — 패배");
                return true;
            }
            if (allEnemiesDead)
            {
                currentPhase = BattlePhase.BattleEnd;
                Debug.Log("[TacticalBattleManager] 전투 종료 — 승리");
                return true;
            }
            return false;
        }

        // ── ICombatInteractionController 구현 ──

        public bool IsMoveModeActive => currentActionMode == BattleActionMode.Move;

        public void SetMoveMode(bool active)
        {
            if (active)
            {
                SelectMoveAction();
            }
            else
            {
                CancelCurrentAction();
            }
        }

        public void ExecuteSkill(string skillId)
        {
            if (selectedUnit == null) return;

            SkillData skillToUse = null;
            if (selectedUnit.Data.uniqueSkill1 != null && selectedUnit.Data.uniqueSkill1.skillId == skillId)
            {
                skillToUse = selectedUnit.Data.uniqueSkill1;
            }
            else if (selectedUnit.Data.uniqueSkill2 != null && selectedUnit.Data.uniqueSkill2.skillId == skillId)
            {
                skillToUse = selectedUnit.Data.uniqueSkill2;
            }
            else if (selectedUnit.EquippedOptionalSkill != null && selectedUnit.EquippedOptionalSkill.skillId == skillId)
            {
                skillToUse = selectedUnit.EquippedOptionalSkill;
            }

            if (skillToUse != null)
            {
                SelectSkillAction(skillToUse);
            }
            else
            {
                Debug.LogWarning($"[TacticalBattleManager] ID가 '{skillId}'인 스킬을 유닛에게서 찾을 수 없습니다.");
            }
        }

        public void ExecuteWait()
        {
            WaitSelectedUnit();
        }
    }
}
