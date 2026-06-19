using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 현재 턴(행동 순서 첫 번째) 유닛을 방향키/WASD로 한 칸씩 이동시키는 테스트용 컨트롤러.
// Enter 키로 다음 턴 유닛으로 전환한다.
// 프로젝트가 새 Input System 패키지(activeInputHandler=1)로 설정돼 있어 Keyboard.current를 사용한다.
public class BattleTurnController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private BattleScript battleScript;

    [Header("행동 순서 GUI")]
    [SerializeField] private Vector2 turnOrderPanelPosition = new Vector2(10f, 300f);
    [SerializeField] private float turnOrderPanelWidth = 260f;
    [Tooltip("앞으로 표시할 예상 행동 순서 개수")]
    [SerializeField] private int turnOrderPreviewCount = 12;

    private BattleUnitEntry lastReportedUnit;

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

        Vector2Int direction = ReadDirectionInput();
        if (direction != Vector2Int.zero)
        {
            battleScript.TryMoveCurrentUnit(direction);
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.enterKey.wasPressedThisFrame)
        {
            battleScript.AdvanceTurn();
        }

        ReportCurrentUnit();
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
        DrawTurnOrderPanel();
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
        GUILayout.Label("[방향키 / WASD] 이동");
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
