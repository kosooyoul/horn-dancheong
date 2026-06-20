using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using KD;

public class PreBattleUI : MonoBehaviour
{
    [Header("유닛 버튼 (4개)")]
    [SerializeField] private Button[] ownedUnitsBtn;

    [Header("참조")]
    [SerializeField] private PlayerRosterManager    rosterManager;
    [SerializeField] private TacticalBattleManager  battleManager;
    [SerializeField] private GridManager            gridManager;
    [SerializeField] private KDBattleTurnController turnController;
    [SerializeField] private Camera                 battleCamera;

    [Header("드래그 고스트 아이콘 (선택)")]
    [Tooltip("드래그 중 마우스를 따라다닐 UI 오브젝트. 비워두면 고스트 없이 동작.")]
    [SerializeField] private RectTransform dragGhostIcon;

    [Header("버튼 색상")]
    [SerializeField] private Color selectedColor   = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color unselectedColor = Color.white;

    // ── 런타임 ────────────────────────────────────────────────────────────

    private OwnedUnit[] unitSlots;
    private OwnedUnit   selectedUnit;

    private bool    isDragging;
    private Vector2 dragStartScreenPos;
    private const float DragThreshold = 15f; // 이 픽셀 이상 움직여야 드래그로 인정

    // ── 초기화 ───────────────────────────────────────────────────────────

    private void Start()
    {
        unitSlots = new OwnedUnit[ownedUnitsBtn.Length];
        RefreshRoster();
        SetupButtonDragListeners();

        if (dragGhostIcon != null)
            dragGhostIcon.gameObject.SetActive(false);
    }

    /// <summary>PlayerRosterManager에서 유닛 목록을 읽어 버튼에 반영한다.</summary>
    public void RefreshRoster()
    {
        if (rosterManager == null) return;

        // selectedParty 우선, 없으면 ownedUnits 전체 사용
        System.Collections.Generic.IReadOnlyList<OwnedUnit> source =
            rosterManager.SelectedParty.Count > 0
                ? (System.Collections.Generic.IReadOnlyList<OwnedUnit>)rosterManager.SelectedParty
                : rosterManager.OwnedUnits;

        for (int i = 0; i < ownedUnitsBtn.Length; i++)
        {
            if (i < source.Count && source[i]?.unitData != null)
            {
                unitSlots[i] = source[i];
                SetButtonText(i, source[i].unitData.unitName);
                ownedUnitsBtn[i].interactable = true;
            }
            else
            {
                unitSlots[i] = null;
                SetButtonText(i, "—");
                ownedUnitsBtn[i].interactable = false;
            }

            SetButtonColor(i, unselectedColor);
        }
    }

    // ── 버튼 드래그 이벤트 등록 ──────────────────────────────────────────

    private void SetupButtonDragListeners()
    {
        for (int i = 0; i < ownedUnitsBtn.Length; i++)
        {
            int index = i;

            var et = ownedUnitsBtn[i].GetComponent<EventTrigger>()
                  ?? ownedUnitsBtn[i].gameObject.AddComponent<EventTrigger>();

            // PointerDown → 드래그 시작 + 유닛 선택
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entry.callback.AddListener(_ => OnButtonPointerDown(index));
            et.triggers.Add(entry);
        }
    }

    private void OnButtonPointerDown(int index)
    {
        if (unitSlots[index] == null) return;

        selectedUnit       = unitSlots[index];
        isDragging         = true;
        dragStartScreenPos = Mouse.current.position.ReadValue();

        // KDBattleTurnController에도 전달 (클릭-후-타일클릭 경로도 지원)
        turnController?.SelectDeployUnit(selectedUnit);

        UpdateButtonHighlights();
        ShowGhostIcon(selectedUnit.unitData.unitName);
    }

    // ── Update: 드래그 추적 + 드롭 처리 ─────────────────────────────────

    private void Update()
    {
        if (!isDragging) return;

        // 고스트 아이콘 마우스 따라가기
        if (dragGhostIcon != null && dragGhostIcon.gameObject.activeSelf)
            dragGhostIcon.position = Mouse.current.position.ReadValue();

        // 마우스 버튼 해제 → 드롭 처리
        if (Mouse.current.leftButton.wasReleasedThisFrame)
            OnDrop();
    }

    private void OnDrop()
    {
        isDragging = false;
        HideGhostIcon();

        // 드래그 거리 미달 (= 단순 클릭) → 배치 시도 안 함. 타일 클릭으로 배치 가능.
        float movedPx = Vector2.Distance(Mouse.current.position.ReadValue(), dragStartScreenPos);
        if (movedPx < DragThreshold) return;

        // UI 위에서 드롭됐으면 배치 안 함
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (selectedUnit == null) return;
        if (!TryGetGridUnderCursor(out Vector2Int tile)) return;

        battleManager.OnDeploymentTileClicked(tile, selectedUnit);
    }

    // ── 타일 Raycast ─────────────────────────────────────────────────────

    private bool TryGetGridUnderCursor(out Vector2Int grid)
    {
        grid = default;
        Camera cam = battleCamera != null ? battleCamera : Camera.main;
        if (cam == null || gridManager == null) return false;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            grid = gridManager.WorldToGrid(hit.point);
            return gridManager.IsValidTile(grid);
        }

        // 콜라이더 없으면 y=0 평면 폴백
        var ground = new Plane(Vector3.up, Vector3.zero);
        if (ground.Raycast(ray, out float dist))
        {
            grid = gridManager.WorldToGrid(ray.GetPoint(dist));
            return gridManager.IsValidTile(grid);
        }

        return false;
    }

    // ── 고스트 아이콘 ────────────────────────────────────────────────────

    private void ShowGhostIcon(string label)
    {
        if (dragGhostIcon == null) return;
        dragGhostIcon.gameObject.SetActive(true);
        dragGhostIcon.position = Mouse.current.position.ReadValue();
        SetGhostText(label);
    }

    private void HideGhostIcon()
    {
        if (dragGhostIcon != null)
            dragGhostIcon.gameObject.SetActive(false);
    }

    private void SetGhostText(string text)
    {
        if (dragGhostIcon == null) return;
        var tmp = dragGhostIcon.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (tmp != null) { tmp.text = text; return; }
        var leg = dragGhostIcon.GetComponentInChildren<Text>();
        if (leg != null) leg.text = text;
    }

    // ── 버튼 헬퍼 ────────────────────────────────────────────────────────

    private void SetButtonText(int i, string text)
    {
        var tmp = ownedUnitsBtn[i].GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (tmp != null) { tmp.text = text; return; }
        var leg = ownedUnitsBtn[i].GetComponentInChildren<Text>();
        if (leg != null) leg.text = text;
    }

    private void SetButtonColor(int i, Color color)
    {
        var img = ownedUnitsBtn[i].GetComponent<Image>();
        if (img != null) img.color = color;
    }

    private void UpdateButtonHighlights()
    {
        for (int i = 0; i < ownedUnitsBtn.Length; i++)
            SetButtonColor(i, unitSlots[i] == selectedUnit ? selectedColor : unselectedColor);
    }
}
