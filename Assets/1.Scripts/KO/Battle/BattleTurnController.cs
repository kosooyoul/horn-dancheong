using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 현재 턴(행동 순서 첫 번째) 유닛을 방향키/WASD 또는 마우스 클릭으로 이동시키는 테스트용 컨트롤러.
// 이동 가능한 칸은 바닥 타일 색으로 강조 표시하고, 그 칸을 클릭하면 경로를 따라 이동한다.
// Enter 키로 다음 턴 유닛으로 전환한다.
// 프로젝트가 새 Input System 패키지(activeInputHandler=1)로 설정돼 있어 Keyboard.current / Mouse.current를 사용한다.
public class BattleTurnController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private BattleScript battleScript;
    [Tooltip("이동 클릭 판정에 사용할 카메라. 비우면 BattleCameraFollow 또는 메인 카메라를 자동으로 찾는다.")]
    [SerializeField] private Camera battleCamera;

    [Header("이동 범위 표시")]
    [Tooltip("이동 가능한 칸에 덧입힐 강조 색상")]
    [SerializeField] private Color reachableTint = new Color(0.3f, 1f, 0.45f);
    [Tooltip("강조 색을 원래 타일 색에 섞는 정도 (0=원색, 1=강조색)")]
    [SerializeField, Range(0f, 1f)] private float reachableTintStrength = 0.55f;

    [Header("행동 메뉴")]
    [Tooltip("행동 메뉴 패널 너비(px)")]
    [SerializeField] private float actionMenuWidth = 150f;

    [Header("행동 순서 GUI")]
    [SerializeField] private Vector2 turnOrderPanelPosition = new Vector2(10f, 300f);
    [SerializeField] private float turnOrderPanelWidth = 260f;
    [Tooltip("앞으로 표시할 예상 행동 순서 개수")]
    [SerializeField] private int turnOrderPreviewCount = 12;

    private BattleUnitEntry lastReportedUnit;

    // 적 AI 실행용 타이머
    private float enemyActionDelay = 1.0f; // 적 행동 간 대기 시간 (초)
    private float enemyActionTimer = 0f;
    private bool isEnemyActing = false; // 적 행동(이동 보간 포함)이 진행 중인지 — 중복 실행 방지

    // 이동 범위 강조 상태 — 색을 바꾼 타일의 렌더러와 원래 색을 보관해 두었다가 복원한다.
    private readonly Dictionary<Renderer, Color> highlightedTiles = new Dictionary<Renderer, Color>();
    // 강조가 그려진 시점의 유닛/위치 — 바뀌면 다시 계산한다.
    private BattleUnitEntry highlightUnit;
    private Vector2Int highlightGrid;

    // ── 행동 메뉴 상태 ──
    // 행동 메뉴가 열려 있는 동안에는 이동 입력을 잠그고 메뉴만 표시한다.
    private bool isActionMenuOpen;
    // 이동(보간) 완료를 기다리는 중 — 도착하면 메뉴를 연다. 이 동안 입력을 잠근다.
    private bool isWaitingForMove;
    // 제자리(이동 안 함)를 선택했는지 — true면 메뉴에 "휴식"을 추가한다.
    private bool actionMenuIncludesRest;
    // "공격"으로 연 스킬(공격) UI 흐름이 진행 중인지 — 끝나면 결과에 따라 턴 종료/행동 메뉴 복귀.
    private bool isSkillFlowActive;
    // 선택한 목적지 칸 — 강조 표시와 메뉴 위치 계산에 사용한다.
    private Vector2Int actionDestination;
    // OnGUI 버튼 클릭을 다음 Update에서 처리하기 위한 보류 행동 (GUILayout 깨짐 방지)
    private string pendingAction;

    private void Start()
    {
        if (battleScript == null)
        {
            battleScript = FindObjectOfType<BattleScript>();
        }

        ReportCurrentUnit();
    }

    private void Update()
    {
        if (battleScript == null) return;

        // "공격"으로 연 스킬 UI가 진행 중이면 입력을 막고, 종료 시 결과에 따라 분기한다.
        if (isSkillFlowActive)
        {
            if (!battleScript.IsSkillUIActive)
            {
                isSkillFlowActive = false;

                if (battleScript.ConsumeSkillUsedFlag())
                {
                    // 스킬을 실제로 사용했으면 턴을 넘긴다.
                    AdvanceTurnAndResetSelection();
                }
                else
                {
                    // 스킬을 쓰지 않고 빠져나왔으면 행동 메뉴로 되돌아간다.
                    OpenActionMenu(actionDestination, actionMenuIncludesRest);
                }
            }
            return;
        }

        // 행동 메뉴가 열려 있으면 이동 입력을 막고, 보류된 메뉴 선택만 처리한다.
        if (isActionMenuOpen)
        {
            if (pendingAction != null)
            {
                string action = pendingAction;
                pendingAction = null;
                PerformAction(action);
            }
            return;
        }

        // 목적지로 이동하는 중이면 입력을 막고, 도착하면 행동 메뉴를 연다.
        if (isWaitingForMove)
        {
            if (!IsCurrentUnitMoving())
            {
                isWaitingForMove = false;
                OpenActionMenu(actionDestination, includeRest: false);
            }
            return;
        }

        // 현재 턴이 적 유닛이면 AI로 자동 행동 실행
        if (battleScript.IsCurrentTurnEnemy())
        {
            HandleEnemyTurn();
            return;
        }

        Vector2Int direction = ReadDirectionInput();
        if (direction != Vector2Int.zero)
        {
            battleScript.TryMoveCurrentUnit(direction);
        }

        HandleMouseSelection();

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.enterKey.wasPressedThisFrame)
        {
            AdvanceTurnAndResetSelection();
        }

        ReportCurrentUnit();
        // 이동(보간) 중에는 유닛 grid가 즉시 갱신되므로, 같은 프레임에 강조를 다시 그리지 않는다.
        if (!isWaitingForMove)
        {
            UpdateReachableHighlight();
        }
    }

    // ── 적 AI 처리 ──────────────────────────────────────────────────────

    // 적 유닛의 턴을 처리한다
    private void HandleEnemyTurn()
    {
        // 이미 적 행동(이동 보간 포함)이 진행 중이면 새 행동을 시작하지 않는다.
        if (isEnemyActing) return;

        enemyActionTimer += Time.deltaTime;

        // 대기 시간이 지나면 AI 행동 실행
        if (enemyActionTimer >= enemyActionDelay)
        {
            enemyActionTimer = 0f;
            isEnemyActing = true;
            StartCoroutine(ExecuteEnemyTurnRoutine());
        }
    }

    // 적 AI 행동을 실행하고, 이동 보간이 끝나기를 기다린 뒤 턴을 넘기는 코루틴
    private System.Collections.IEnumerator ExecuteEnemyTurnRoutine()
    {
        bool actionExecuted = battleScript.ExecuteEnemyAI();

        if (actionExecuted)
        {
            // 플레이어와 동일하게 이동 보간이 끝날 때까지 대기한다.
            while (IsCurrentUnitMoving())
            {
                yield return null;
            }
            yield return new WaitForSeconds(0.5f); // 도착 후 잠시 대기
        }

        isEnemyActing = false;
        AdvanceTurnAndResetSelection();
    }

    private void OnDisable()
    {
        ClearReachableHighlight();
    }

    // ── 마우스 클릭 이동 / 행동 선택 ────────────────────────────────────

    // 좌클릭한 칸을 판정한다.
    //  - 현재 유닛의 칸을 클릭하면 제자리 → 휴식 포함 메뉴
    //  - 이동 가능한 다른 칸을 클릭하면 그 칸으로 이동 후 메뉴
    private void HandleMouseSelection()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        if (!TryGetGridUnderCursor(out Vector2Int grid)) return;

        BattleUnitEntry current = battleScript.GetCurrentTurnUnit();
        if (current == null) return;

        // 제자리(이동 안 함) 선택 — 즉시 휴식 포함 메뉴
        if (grid == current.grid)
        {
            OpenActionMenu(grid, includeRest: true);
            return;
        }

        // 이동 가능한 칸 선택 — 이동 가능 영역 강조를 즉시 끄고(별도 강조 없음),
        // 이동(보간)이 끝나면 Update에서 메뉴를 연다.
        if (battleScript.MoveCurrentUnitTo(grid))
        {
            ClearReachableHighlight();
            // grid가 즉시 갱신되므로 추적 상태도 맞춰 두어, isWaitingForMove 해제 직후 재강조를 방지한다.
            highlightUnit = current;
            highlightGrid = grid;
            actionDestination = grid;
            isWaitingForMove = true;
        }
    }

    // 현재 턴 유닛이 보간 이동 중인지 — UnitMover가 없으면 즉시 도착으로 간주한다.
    private bool IsCurrentUnitMoving()
    {
        BattleUnitEntry current = battleScript.GetCurrentTurnUnit();
        if (current == null || current.marker == null) return false;

        UnitMover mover = current.marker.GetComponent<UnitMover>();
        return mover != null && mover.IsMoving;
    }

    // 커서 아래의 그리드 좌표를 구한다 — 콜라이더 우선, 실패 시 바닥(y=0) 평면으로 폴백.
    private bool TryGetGridUnderCursor(out Vector2Int grid)
    {
        grid = default;

        Camera camera = GetBattleCamera();
        Mouse mouse = Mouse.current;
        if (camera == null || mouse == null) return false;

        Ray ray = camera.ScreenPointToRay(mouse.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            grid = battleScript.WorldToGrid(hit.point);
            return battleScript.IsValidPosition(grid.x, grid.y);
        }

        var ground = new Plane(Vector3.up, Vector3.zero);
        if (ground.Raycast(ray, out float distance))
        {
            grid = battleScript.WorldToGrid(ray.GetPoint(distance));
            return battleScript.IsValidPosition(grid.x, grid.y);
        }

        return false;
    }

    private Camera GetBattleCamera()
    {
        if (battleCamera != null) return battleCamera;

        BattleCameraFollow follow = FindObjectOfType<BattleCameraFollow>();
        if (follow != null)
        {
            battleCamera = follow.GetComponent<Camera>();
        }

        if (battleCamera == null)
        {
            battleCamera = Camera.main;
        }

        return battleCamera;
    }

    // ── 이동 범위 강조 표시 ─────────────────────────────────────────────

    // 현재 턴 유닛 또는 그 위치가 바뀐 경우에만 강조를 다시 그린다.
    private void UpdateReachableHighlight()
    {
        BattleUnitEntry current = battleScript.GetCurrentTurnUnit();
        Vector2Int grid = current != null ? current.grid : Vector2Int.zero;

        if (current == highlightUnit && grid == highlightGrid) return;

        highlightUnit = current;
        highlightGrid = grid;
        RefreshReachableHighlight();
    }

    private void RefreshReachableHighlight()
    {
        ClearReachableHighlight();

        if (battleScript.GetCurrentTurnUnit() == null) return;

        foreach (Vector2Int tile in battleScript.GetReachableTilesForCurrentUnit())
        {
            TintTile(tile, reachableTint, reachableTintStrength);
        }
    }

    // 한 칸의 바닥 타일 색을 강조색과 섞어 칠하고, 복원을 위해 원래 색을 보관한다.
    private void TintTile(Vector2Int tile, Color tint, float strength)
    {
        GameObject floorCube = battleScript.GetFloorCube(tile.x, tile.y);
        if (floorCube == null) return;

        Renderer renderer = floorCube.GetComponent<Renderer>();
        if (renderer == null) return;

        // 이미 강조된 타일이면 원래 색을 보존한 채 색만 다시 입힌다(목적지가 도달 영역 위에 겹칠 때).
        Color original = highlightedTiles.ContainsKey(renderer)
            ? highlightedTiles[renderer]
            : renderer.material.color;

        highlightedTiles[renderer] = original;
        renderer.material.color = Color.Lerp(original, tint, strength);
    }

    private void ClearReachableHighlight()
    {
        foreach (KeyValuePair<Renderer, Color> entry in highlightedTiles)
        {
            if (entry.Key != null)
            {
                entry.Key.material.color = entry.Value;
            }
        }

        highlightedTiles.Clear();
    }

    // ── 행동 메뉴 ───────────────────────────────────────────────────────

    // 목적지를 강조하고 행동 메뉴를 연다. includeRest=true면 "휴식"을 추가한다.
    private void OpenActionMenu(Vector2Int destination, bool includeRest)
    {
        isActionMenuOpen = true;
        actionMenuIncludesRest = includeRest;
        actionDestination = destination;
        pendingAction = null;

        // 이동 가능 영역 강조를 끈다(목적지 강조는 표시하지 않음).
        ClearReachableHighlight();
    }

    // 메뉴에서 고른 행동을 처리한다 — 현재는 로그 후 턴을 넘긴다(타겟팅/스킬 시스템 연동 지점).
    private void PerformAction(string action)
    {
        if (action == "취소")
        {
            CancelActionSelection();
            return;
        }

        BattleUnitEntry current = battleScript.GetCurrentTurnUnit();
        string unitName = current != null ? current.DisplayName : "(없음)";
        Debug.Log($"[BattleTurnController] {unitName} 행동 선택: {action}");

        // "공격"은 스킬 선택 메뉴를 연다(스킬 = 공격으로 통합). 흐름이 끝나면 Update에서 턴 처리.
        if (action == "공격" && current != null)
        {
            isActionMenuOpen = false; // actionMenuIncludesRest는 취소 시 복귀를 위해 유지
            isSkillFlowActive = true;
            battleScript.OpenSkillMenu(current.grid);
            return;
        }

        isActionMenuOpen = false;
        actionMenuIncludesRest = false;
        AdvanceTurnAndResetSelection();
    }

    // 메뉴를 닫고 이동을 턴 시작 위치로 되돌린 뒤, 다시 이동 선택 단계로 돌아간다.
    private void CancelActionSelection()
    {
        isActionMenuOpen = false;
        actionMenuIncludesRest = false;

        // 이동했던 경우 턴 시작 위치로 복귀시킨다(턴은 넘기지 않음).
        BattleUnitEntry current = battleScript.GetCurrentTurnUnit();
        Vector2Int origin = battleScript.GetCurrentTurnOrigin();
        if (current != null && current.grid != origin)
        {
            battleScript.MoveUnit(current, origin, instant: true);
        }

        // 이동 가능 영역을 다시 표시한다.
        highlightUnit = null;
        RefreshReachableHighlight();
        highlightUnit = battleScript.GetCurrentTurnUnit();
        highlightGrid = highlightUnit != null ? highlightUnit.grid : Vector2Int.zero;
    }

    // 턴을 넘기고 다음 유닛의 이동 범위 강조를 즉시 다시 계산하도록 추적 상태를 초기화한다.
    private void AdvanceTurnAndResetSelection()
    {
        battleScript.AdvanceTurn();
        highlightUnit = null;
        RefreshReachableHighlight();
        highlightUnit = battleScript.GetCurrentTurnUnit();
        highlightGrid = highlightUnit != null ? highlightUnit.grid : Vector2Int.zero;
    }

    // 방향 입력 읽기 — x는 열, y는 행(맵의 z축 = 위쪽)
    private Vector2Int ReadDirectionInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return Vector2Int.zero;

        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) return Vector2Int.up;
        if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) return Vector2Int.down;
        if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) return Vector2Int.left;
        if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) return Vector2Int.right;
        return Vector2Int.zero;
    }

    // 현재 턴 유닛이 바뀌면 한 번만 로그 출력
    private void ReportCurrentUnit()
    {
        BattleUnitEntry current = battleScript.GetCurrentTurnUnit();
        if (current == lastReportedUnit) return;

        lastReportedUnit = current;
        if (current == null) return;

        string faction = current.isEnemy ? "적" : "아군";
        Debug.Log($"[BattleTurnController] 현재 턴: [{faction}] {current.DisplayName} (민첩 {current.Agility})");
    }

    private void OnGUI()
    {
        if (battleScript == null) return;

        // DrawCurrentTurnPanel();
        // DrawTurnOrderPanel();
        DrawActionMenu();
    }

    // 선택한 목적지 옆에 2D 행동 메뉴를 표시한다.
    // 버튼 클릭은 곧바로 처리하지 않고 pendingAction에 담아 다음 Update에서 처리한다
    // (OnGUI 도중 메뉴 구조를 바꾸면 GUILayout 오류가 나기 때문).
    private void DrawActionMenu()
    {
        if (!isActionMenuOpen) return;

        // 공격/대기 (+휴식) + 취소
        int buttonCount = (actionMenuIncludesRest ? 3 : 2) + 1;
        float panelHeight = 44f + buttonCount * 30f;
        Vector2 panelPosition = GetActionMenuScreenPosition(panelHeight);

        var area = new Rect(panelPosition.x, panelPosition.y, actionMenuWidth, panelHeight);
        GUILayout.BeginArea(area, GUI.skin.box);
        GUILayout.Label("<b>행동 선택</b>", RichLabelStyle());

        if (GUILayout.Button("공격")) pendingAction = "공격";
        if (GUILayout.Button("대기")) pendingAction = "대기";
        if (actionMenuIncludesRest && GUILayout.Button("휴식")) pendingAction = "휴식";
        if (GUILayout.Button("취소")) pendingAction = "취소";

        GUILayout.EndArea();
    }

    // 목적지 칸의 화면 좌표를 구해 메뉴 위치(좌상단)를 계산한다. 화면 밖이면 우측 상단으로 폴백.
    private Vector2 GetActionMenuScreenPosition(float panelHeight)
    {
        Camera camera = GetBattleCamera();
        if (camera != null)
        {
            Vector3 world = battleScript.GridToWorld(actionDestination.x, actionDestination.y);
            Vector3 screen = camera.WorldToScreenPoint(world);

            if (screen.z > 0f)
            {
                float x = Mathf.Clamp(screen.x + 30f, 0f, Screen.width - actionMenuWidth);
                // 스크린 좌표는 좌하단 원점, GUI는 좌상단 원점이므로 y를 뒤집는다.
                float y = Mathf.Clamp(Screen.height - screen.y - panelHeight * 0.5f, 0f, Screen.height - panelHeight);
                return new Vector2(x, y);
            }
        }

        return new Vector2(Screen.width - actionMenuWidth - 10f, 10f);
    }

    private void DrawCurrentTurnPanel()
    {
        BattleUnitEntry current = battleScript.GetCurrentTurnUnit();

        GUILayout.BeginArea(new Rect(10, 170, 320, 120));
        GUILayout.Box("턴 컨트롤러");

        if (current != null)
        {
            string faction = current.isEnemy ? "적" : "아군";
            GUILayout.Label($"현재 턴: [{faction}] {current.DisplayName}");
            GUILayout.Label($"위치: ({current.grid.x}, {current.grid.y}) / 민첩 {current.Agility}");
        }
        else
        {
            GUILayout.Label("행동할 유닛이 없습니다.");
        }

        GUILayout.Space(6);
        GUILayout.Label("[방향키 / WASD] 한 칸 이동");
        GUILayout.Label("[마우스 클릭] 칸 이동 후 행동 메뉴");
        GUILayout.Label("[제자리 클릭] 휴식 포함 행동 메뉴");
        GUILayout.Label("[Enter] 다음 턴");

        GUILayout.EndArea();
    }

    // 앞으로의 행동 순서를 2D GUI 목록으로 표시 — 첫 번째(현재 차례)는 강조.
    // 민첩이 높은 유닛은 짧은 구간에서 여러 번 등장한다(틱 게이지 모델).
    private void DrawTurnOrderPanel()
    {
        int previewCount = Mathf.Max(1, turnOrderPreviewCount);
        IReadOnlyList<BattleUnitEntry> order = battleScript.PeekTurnOrder(previewCount);

        float panelHeight = 60f + order.Count * 22f;
        var area = new Rect(turnOrderPanelPosition.x, turnOrderPanelPosition.y, turnOrderPanelWidth, panelHeight);

        GUILayout.BeginArea(area, GUI.skin.box);
        GUILayout.Label("<b>행동 순서 (민첩 빈도순)</b>", RichLabelStyle());

        if (order.Count == 0)
        {
            GUILayout.Label("참가 유닛 없음");
        }
        else
        {
            for (int i = 0; i < order.Count; i++)
            {
                BattleUnitEntry unit = order[i];
                bool isCurrent = i == 0;
                string faction = unit.isEnemy ? "적" : "아군";
                string marker = isCurrent ? "▶ " : "   ";
                string moveInfo = isCurrent
                    ? $"이동 {battleScript.GetCurrentTurnMovesRemaining()}/{unit.MoveRange}"
                    : $"이동 {unit.MoveRange}";
                string line = $"{marker}{i + 1}. [{faction}] {unit.DisplayName}  (민첩 {unit.Agility}, {moveInfo})";

                GUILayout.Label(line, EntryStyle(isCurrent, unit.isEnemy));
            }
        }

        GUILayout.EndArea();
    }

    private static GUIStyle cachedRichStyle;
    private GUIStyle RichLabelStyle()
    {
        if (cachedRichStyle == null)
        {
            cachedRichStyle = new GUIStyle(GUI.skin.label) { richText = true, fontStyle = FontStyle.Bold };
        }
        return cachedRichStyle;
    }

    private GUIStyle EntryStyle(bool isCurrent, bool isEnemy)
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontStyle = isCurrent ? FontStyle.Bold : FontStyle.Normal
        };

        Color color = isEnemy ? new Color(1f, 0.5f, 0.5f) : new Color(0.6f, 0.8f, 1f);
        if (isCurrent) color = Color.yellow;
        style.normal.textColor = color;

        return style;
    }
}
