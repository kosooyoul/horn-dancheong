using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KD
{
    /// <summary>
    /// KD 전투 시스템용 입력 컨트롤러.
    ///
    /// 역할:
    ///   - 마우스/키보드 입력을 받아 TacticalBattleManager API를 호출한다.
    ///   - 턴을 직접 진행하지 않는다.
    ///   - BattleScript 의존성 없음.
    ///
    /// 씬 구성:
    ///   BattleSystem 오브젝트 → 이 컴포넌트 + GridManager + TacticalBattleManager
    ///
    /// Inspector 연결:
    ///   gridManager   = KD.GridManager
    ///   battleManager = TacticalBattleManager
    ///   battleCamera  = 탑뷰 카메라 (비워두면 Camera.main 사용)
    ///
    /// 배치 단계 OwnedUnit 선택:
    ///   UI 버튼에서 SelectDeployUnit(unit)을 호출해 선택할 유닛을 지정한다.
    ///   좌클릭 → 선택된 유닛을 그 타일에 배치.
    ///   우클릭 → 그 타일의 배치를 취소.
    ///
    /// 전투 단계 키 단축키:
    ///   1 또는 M     : 이동 모드
    ///   2 또는 K     : 첫 번째 사용 가능 스킬 선택
    ///   W 또는 Space : 대기
    ///   Escape       : 현재 행동 취소
    ///   Enter        : 배치 확정 (배치 단계)
    /// </summary>
    public class KDBattleTurnController : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private GridManager            gridManager;
        [SerializeField] private TacticalBattleManager  battleManager;
        [Tooltip("탑뷰 카메라. 비워두면 Camera.main을 사용한다.")]
        [SerializeField] private Camera                  battleCamera;

        [Header("행동 메뉴 UI")]
        [SerializeField] private float actionMenuWidth = 150f;

        // ── 배치 단계 ─────────────────────────────────────────────────────

        private OwnedUnit selectedDeployUnit;

        /// <summary>배치 단계에서 배치할 OwnedUnit을 지정한다 (로스터 UI 버튼에서 호출).</summary>
        public void SelectDeployUnit(OwnedUnit unit)
        {
            selectedDeployUnit = unit;
            Debug.Log($"[KDBattleTurnController] 배치 선택: {unit?.unitData.unitName ?? "(없음)"}");
        }

        // ── 행동 메뉴 상태 ────────────────────────────────────────────────

        private bool   isActionMenuOpen;
        private string pendingAction;
        private Vector2Int actionMenuAnchorTile;

        // ── 마우스 호버 추적 ──────────────────────────────────────────────

        private Vector2Int lastHoveredTile;
        private bool       hasHoveredTile;

        // ── Update ────────────────────────────────────────────────────────

        private void Update()
        {
            if (gridManager == null || battleManager == null) return;

            // 선택된 유닛의 마커가 이동 보간 중이면 입력 잠금
            if (IsSelectedUnitMoving()) return;

            // 행동 메뉴 보류 처리
            if (isActionMenuOpen)
            {
                if (pendingAction != null)
                {
                    string action = pendingAction;
                    pendingAction = null;
                    ExecutePendingAction(action);
                }
                return;
            }

            HandleMouseHover();

            switch (battleManager.CurrentPhase)
            {
                case BattlePhase.Deployment:
                    HandleDeploymentInput();
                    break;
                case BattlePhase.PlayerPhase:
                    HandlePlayerPhaseInput();
                    break;
            }
        }

        private bool IsSelectedUnitMoving()
        {
            BattleUnit unit = battleManager.SelectedUnit;
            return unit != null && gridManager.IsUnitMoving(unit);
        }

        // ── 배치 단계 입력 ────────────────────────────────────────────────

        private void HandleDeploymentInput()
        {
            HandleDeploymentMouseInput();
            HandleDeploymentKeyInput();
        }

        private void HandleDeploymentMouseInput()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (TryGetGridUnderCursor(out Vector2Int tile))
                    battleManager.OnDeploymentTileClicked(tile, selectedDeployUnit);
            }
            else if (mouse.rightButton.wasPressedThisFrame)
            {
                if (TryGetGridUnderCursor(out Vector2Int tile))
                    battleManager.OnDeploymentTileClicked(tile, null); // null = 배치 취소
            }
        }

        private void HandleDeploymentKeyInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.enterKey.wasPressedThisFrame)
                battleManager.ConfirmDeployment();
        }

        // ── 플레이어 턴 입력 ──────────────────────────────────────────────

        private void HandlePlayerPhaseInput()
        {
            BattleActionMode mode = battleManager.CurrentActionMode;

            if (mode == BattleActionMode.Move || mode == BattleActionMode.Skill)
            {
                HandleActionModeMouseClick();
                HandleCancelKey();
            }
            else
            {
                // None 모드: 행동 메뉴가 비어 있으면 열지 않음. OnGUI 에서 항상 표시.
                HandleKeyboardShortcuts();
            }
        }

        private void HandleActionModeMouseClick()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

            if (TryGetGridUnderCursor(out Vector2Int tile))
                battleManager.OnTileClicked(tile);
        }

        private void HandleCancelKey()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame)
                battleManager.CancelCurrentAction();
        }

        private void HandleKeyboardShortcuts()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.mKey.wasPressedThisFrame)
                battleManager.SelectMoveAction();

            if (keyboard.digit2Key.wasPressedThisFrame || keyboard.kKey.wasPressedThisFrame)
                SelectFirstAvailableSkill();

            if (keyboard.wKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                battleManager.WaitSelectedUnit();
        }

        private void SelectFirstAvailableSkill()
        {
            BattleUnit unit = battleManager.SelectedUnit;
            if (unit == null) return;
            List<SkillData> usable = unit.GetUsableSkills();
            if (usable.Count > 0)
                battleManager.SelectSkillAction(usable[0]);
        }

        // ── 마우스 호버 (스킬 범위 미리보기) ──────────────────────────────

        private void HandleMouseHover()
        {
            if (battleManager.CurrentPhase != BattlePhase.PlayerPhase) return;
            if (battleManager.CurrentActionMode != BattleActionMode.Skill)  return;

            if (!TryGetGridUnderCursor(out Vector2Int tile)) return;
            if (hasHoveredTile && tile == lastHoveredTile)   return;

            lastHoveredTile = tile;
            hasHoveredTile  = true;
            battleManager.UpdateHoveredTile(tile);
        }

        // ── Ray 캐스트 ────────────────────────────────────────────────────

        private bool TryGetGridUnderCursor(out Vector2Int grid)
        {
            grid = default;
            Camera cam = GetBattleCamera();
            Mouse  mouse = Mouse.current;
            if (cam == null || mouse == null) return false;

            Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                grid = gridManager.WorldToGrid(hit.point);
                return gridManager.IsValidTile(grid);
            }

            // 콜라이더 없으면 y=0 평면으로 폴백
            var ground = new Plane(Vector3.up, Vector3.zero);
            if (ground.Raycast(ray, out float dist))
            {
                grid = gridManager.WorldToGrid(ray.GetPoint(dist));
                return gridManager.IsValidTile(grid);
            }

            return false;
        }

        private Camera GetBattleCamera()
        {
            if (battleCamera != null) return battleCamera;
            battleCamera = Camera.main;
            return battleCamera;
        }

        // ── 행동 메뉴 처리 ────────────────────────────────────────────────

        private void ExecutePendingAction(string action)
        {
            isActionMenuOpen = false;

            switch (action)
            {
                case "이동":   battleManager.SelectMoveAction(); break;
                case "스킬":   SelectFirstAvailableSkill();      break;
                case "대기":   battleManager.WaitSelectedUnit(); break;
                case "취소":   /* 메뉴만 닫음 */                 break;
                case "배치확정": battleManager.ConfirmDeployment(); break;
            }
        }

        // ── OnGUI ─────────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (battleManager == null) return;

            switch (battleManager.CurrentPhase)
            {
                case BattlePhase.Deployment:
                    DrawDeploymentPanel();
                    break;
                case BattlePhase.PlayerPhase:
                    if (battleManager.CurrentActionMode == BattleActionMode.None)
                        DrawActionMenu();
                    break;
            }
        }

        // 배치 단계: 우상단에 확정 버튼 + 현재 선택 유닛 표시
        private void DrawDeploymentPanel()
        {
            var area = new Rect(Screen.width - 180f, 10f, 170f, 90f);
            GUILayout.BeginArea(area, GUI.skin.box);
            GUILayout.Label("<b>배치 단계</b>", RichStyle());

            string unitName = selectedDeployUnit?.unitData.unitName ?? "(선택 없음)";
            GUILayout.Label($"선택: {unitName}");

            if (GUILayout.Button("배치 확정 [Enter]"))
                pendingAction = "배치확정";

            GUILayout.EndArea();
        }

        // 플레이어 턴 (None 모드): 이동/스킬/대기 행동 메뉴
        private void DrawActionMenu()
        {
            BattleUnit unit = battleManager.SelectedUnit;
            if (unit == null) return;

            float panelHeight = 44f + 4 * 30f;
            Vector2 pos = GetMenuScreenPosition(panelHeight);
            var area = new Rect(pos.x, pos.y, actionMenuWidth, panelHeight);

            GUILayout.BeginArea(area, GUI.skin.box);
            GUILayout.Label($"<b>{unit.Data.unitName}</b>  AP:{unit.CurrentAP}", RichStyle());

            if (GUILayout.Button("이동 [1/M]"))  pendingAction = "이동";
            if (GUILayout.Button("스킬 [2/K]"))  pendingAction = "스킬";
            if (GUILayout.Button("대기 [W]"))    pendingAction = "대기";
            if (GUILayout.Button("취소 [Esc]"))  pendingAction = "취소";

            GUILayout.EndArea();
        }

        private Vector2 GetMenuScreenPosition(float panelHeight)
        {
            Camera cam = GetBattleCamera();
            if (cam != null && gridManager != null && battleManager.SelectedUnit != null)
            {
                Vector2Int tile  = battleManager.SelectedUnit.CurrentTilePos;
                Vector3 world    = gridManager.GridToWorld(tile);
                Vector3 screen   = cam.WorldToScreenPoint(world);

                if (screen.z > 0f)
                {
                    float x = Mathf.Clamp(screen.x + 30f, 0f, Screen.width - actionMenuWidth);
                    float y = Mathf.Clamp(Screen.height - screen.y - panelHeight * 0.5f, 0f, Screen.height - panelHeight);
                    return new Vector2(x, y);
                }
            }
            return new Vector2(Screen.width - actionMenuWidth - 10f, 10f);
        }

        private static GUIStyle cachedRichStyle;
        private static GUIStyle RichStyle()
        {
            if (cachedRichStyle == null)
                cachedRichStyle = new GUIStyle(GUI.skin.label) { richText = true };
            return cachedRichStyle;
        }
    }
}
