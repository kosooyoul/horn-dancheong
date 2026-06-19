using System.Collections.Generic;
using UnityEngine;

namespace KD
{
    /// <summary>
    /// 데이터 담당자가 제공하는 전투 흐름 샘플 매니저.
    ///
    /// 완성형 전투 시스템이 아니라,
    /// UnitData / OwnedUnit / BattleUnit / CombatGridQuery / SkillRangePreview / SkillExecutor를
    /// 어떤 순서로 연결해서 쓰는지 보여주는 기본 틀.
    ///
    /// 전투 담당자가 붙여야 할 것:
    ///   - 마우스 입력 → SelectUnit / OnTileClicked / UpdateHoveredTile 호출
    ///   - UI 버튼   → SelectMoveAction / SelectSkillAction / WaitSelectedUnit 호출
    ///   - 적 AI     → StartEnemyPhase 안에 구현
    ///   - 애니메이션 → GridManager 각 TODO 위치에 추가
    /// </summary>
    public class SimpleBattleManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerRosterManager rosterManager;
        [SerializeField] private GridManager         gridManager;

        [Header("Enemy Test Data")]
        [SerializeField] private List<UnitData> enemyUnitDatas = new List<UnitData>();

        [Header("Spawn Positions")]
        [SerializeField] private List<Vector2Int> playerStartPositions = new List<Vector2Int>();
        [SerializeField] private List<Vector2Int> enemyStartPositions  = new List<Vector2Int>();

        // ── 런타임 상태 ───────────────────────────────────────────────────

        private readonly List<BattleUnit>   playerUnits = new List<BattleUnit>();
        private readonly List<BattleUnit>   enemyUnits  = new List<BattleUnit>();
        private readonly List<BattleUnit>   allUnits    = new List<BattleUnit>();
        private readonly HashSet<BattleUnit> actedUnits = new HashSet<BattleUnit>();

        private BattleSetup       battleSetup;
        private CombatGridQuery   combatGridQuery;
        private SkillRangePreview skillRangePreview;

        private BattleUnit       selectedUnit;
        private SkillData        selectedSkill;
        private List<MoveOption> currentMoveOptions = new List<MoveOption>();

        private BattlePhase      currentPhase      = BattlePhase.None;
        private BattleActionMode currentActionMode = BattleActionMode.None;

        // ── 외부 조회용 프로퍼티 ─────────────────────────────────────────

        public BattlePhase      CurrentPhase      => currentPhase;
        public BattleActionMode CurrentActionMode => currentActionMode;
        public BattleUnit       SelectedUnit      => selectedUnit;
        public SkillData        SelectedSkill     => selectedSkill;

        // ── 초기화 ────────────────────────────────────────────────────────

        private void Start()
        {
            InitializeBattle();
        }

        /// <summary>전투 초기화. 전투 담당자는 씬 로딩/배치 UI를 여기서 연결.</summary>
        public void InitializeBattle()
        {
            if (rosterManager == null) { Debug.LogError("[BattleManager] rosterManager 없음"); return; }
            if (gridManager   == null) { Debug.LogError("[BattleManager] gridManager 없음");   return; }

            battleSetup = new BattleSetup();

            combatGridQuery = new CombatGridQuery(
                isValidTile:          gridManager.IsValidTile,
                isBlockedForMove:     gridManager.IsBlockedForMove,
                isBlockedForSkillRay: gridManager.IsBlockedForSkillRay
            );

            skillRangePreview = new SkillRangePreview(
                combatGridQuery,
                gridManager.HighlightSkillTiles,
                gridManager.ClearHighlight
            );

            CreatePlayerBattleUnits();
            CreateEnemyBattleUnits();
            StartPlayerPhase();

            Debug.Log("[BattleManager] 전투 초기화 완료");
        }

        private void CreatePlayerBattleUnits()
        {
            playerUnits.Clear();
            List<BattleUnit> units = battleSetup.CreatePlayerBattleUnits(
                rosterManager.SelectedParty, playerStartPositions);

            foreach (BattleUnit unit in units)
            {
                playerUnits.Add(unit);
                allUnits.Add(unit);
                gridManager.PlaceUnit(unit, unit.CurrentTilePos);
            }
        }

        private void CreateEnemyBattleUnits()
        {
            enemyUnits.Clear();
            List<BattleUnit> units = battleSetup.CreateEnemyBattleUnits(
                enemyUnitDatas, enemyStartPositions);

            foreach (BattleUnit unit in units)
            {
                enemyUnits.Add(unit);
                allUnits.Add(unit);
                gridManager.PlaceUnit(unit, unit.CurrentTilePos);
            }
        }

        // ── 페이즈 흐름 ───────────────────────────────────────────────────

        private void StartPlayerPhase()
        {
            currentPhase      = BattlePhase.PlayerPhase;
            currentActionMode = BattleActionMode.None;
            selectedUnit      = null;
            selectedSkill     = null;
            currentMoveOptions.Clear();
            actedUnits.Clear();
            gridManager.ClearHighlight();

            // AP는 전투 시작 시 100으로 초기화 후 유지.
            // 차례마다 리셋하면 Wait() 의미가 없어지므로 여기서 ResetAP 호출 안 함.

            Debug.Log("[BattleManager] 플레이어 페이즈 시작");
        }

        private void EndPlayerPhase()
        {
            Debug.Log("[BattleManager] 플레이어 페이즈 종료");
            currentPhase = BattlePhase.EnemyPhase;
            StartEnemyPhase();
        }

        private void StartEnemyPhase()
        {
            Debug.Log("[BattleManager] 적 페이즈 시작");
            // TODO: 전투 담당자 — 적 AI 구현.
            // MVP에서는 바로 플레이어 페이즈로 복귀.
            StartPlayerPhase();
        }

        // ── 외부 호출 API (UI/입력 담당자가 연결) ─────────────────────────

        /// <summary>유닛 클릭 시 호출. UI 또는 GridInput 담당자가 연결.</summary>
        public void SelectUnit(BattleUnit unit)
        {
            if (currentPhase != BattlePhase.PlayerPhase) return;
            if (unit == null || unit.IsDead)             return;

            if (unit.TeamId != 0)
            {
                Debug.Log("[BattleManager] 플레이어 유닛만 선택 가능");
                return;
            }
            if (actedUnits.Contains(unit))
            {
                Debug.Log("[BattleManager] 이미 행동한 유닛");
                return;
            }

            selectedUnit      = unit;
            selectedSkill     = null;
            currentActionMode = BattleActionMode.None;
            currentMoveOptions.Clear();
            gridManager.ClearHighlight();

            Debug.Log($"[BattleManager] 유닛 선택: {unit.Data.unitName} / AP: {unit.CurrentAP}");
        }

        /// <summary>이동 버튼 클릭 시 호출.</summary>
        public void SelectMoveAction()
        {
            if (selectedUnit == null) return;

            selectedSkill     = null;
            currentActionMode = BattleActionMode.Move;
            currentMoveOptions = combatGridQuery.GetMoveOptions(selectedUnit);

            gridManager.ClearHighlight();
            gridManager.HighlightMoveTiles(currentMoveOptions);

            Debug.Log($"[BattleManager] 이동 모드 / 가능 타일: {currentMoveOptions.Count}개");
        }

        /// <summary>스킬 버튼 클릭 시 호출.</summary>
        public void SelectSkillAction(SkillData skill)
        {
            if (selectedUnit == null || skill == null) return;

            if (!selectedUnit.HasSkill(skill))
            {
                Debug.Log("[BattleManager] 보유하지 않은 스킬");
                return;
            }
            if (!selectedUnit.HasEnoughAP(skill.apCost))
            {
                Debug.Log($"[BattleManager] AP 부족 (필요: {skill.apCost}, 현재: {selectedUnit.CurrentAP})");
                return;
            }

            selectedSkill     = skill;
            currentActionMode = BattleActionMode.Skill;
            currentMoveOptions.Clear();
            gridManager.ClearHighlight();

            Debug.Log($"[BattleManager] 스킬 모드: {skill.skillName}");
        }

        /// <summary>마우스가 타일 위에 올라갈 때 호출. 스킬 모드에서만 범위 갱신.</summary>
        public void UpdateHoveredTile(Vector2Int hoveredTile)
        {
            if (currentPhase      != BattlePhase.PlayerPhase) return;
            if (currentActionMode != BattleActionMode.Skill)  return;
            if (selectedUnit == null || selectedSkill == null) return;

            skillRangePreview.Show(selectedUnit, selectedSkill, hoveredTile);
        }

        /// <summary>타일 클릭 시 호출. 현재 모드에 따라 이동/스킬로 분기.</summary>
        public void OnTileClicked(Vector2Int clickedTile)
        {
            if (currentPhase != BattlePhase.PlayerPhase) return;

            switch (currentActionMode)
            {
                case BattleActionMode.Move:  TryMoveSelectedUnit(clickedTile);  break;
                case BattleActionMode.Skill: TryUseSelectedSkill(clickedTile);  break;
            }
        }

        /// <summary>대기 버튼 클릭 시 호출.</summary>
        public void WaitSelectedUnit()
        {
            if (selectedUnit == null) return;
            selectedUnit.Wait();
            Debug.Log($"[BattleManager] 대기 / AP: {selectedUnit.CurrentAP}");
            EndSelectedUnitAction();
        }

        // ── 이동/스킬 실행 내부 로직 ──────────────────────────────────────

        private void TryMoveSelectedUnit(Vector2Int clickedTile)
        {
            if (selectedUnit == null) return;

            if (!TryGetMoveOption(clickedTile, out MoveOption move))
            {
                Debug.Log("[BattleManager] 이동 가능 타일 아님");
                return;
            }

            if (!gridManager.TryMoveUnit(selectedUnit, move))
            {
                Debug.Log("[BattleManager] 이동 실패");
                return;
            }

            Debug.Log($"[BattleManager] 이동 완료 → {clickedTile}");
            EndSelectedUnitAction();
        }

        private void TryUseSelectedSkill(Vector2Int clickedTile)
        {
            if (selectedUnit == null || selectedSkill == null) return;

            if (!skillRangePreview.Contains(clickedTile))
            {
                Debug.Log("[BattleManager] 스킬 범위 밖");
                return;
            }

            BattleUnit target = selectedSkill.targetType == TargetType.Self
                ? selectedUnit
                : gridManager.GetUnitAt(clickedTile);

            if (target == null && selectedSkill.targetType != TargetType.Tile)
            {
                Debug.Log("[BattleManager] 대상 없음");
                return;
            }

            if (!SkillExecutor.Execute(selectedUnit, target, selectedSkill))
            {
                Debug.Log("[BattleManager] 스킬 실행 실패");
                return;
            }

            Debug.Log($"[BattleManager] 스킬 완료: {selectedSkill.skillName}");
            EndSelectedUnitAction();
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

        // ── 행동 종료 ─────────────────────────────────────────────────────

        private void EndSelectedUnitAction()
        {
            if (selectedUnit == null) return;

            actedUnits.Add(selectedUnit);
            selectedUnit.OnTurnEnd();

            selectedUnit      = null;
            selectedSkill     = null;
            currentActionMode = BattleActionMode.None;
            currentMoveOptions.Clear();

            skillRangePreview.Clear();
            gridManager.ClearHighlight();

            CheckPlayerPhaseEnd();
        }

        private void CheckPlayerPhaseEnd()
        {
            foreach (BattleUnit unit in playerUnits)
            {
                if (unit == null || unit.IsDead) continue;
                if (!actedUnits.Contains(unit)) return;
            }
            EndPlayerPhase();
        }
    }
}
